param(
    [string] $RepoRoot = (Join-Path $PSScriptRoot '../..'),
    [string] $Adb = 'adb',
    [string] $Serial,
    [string] $DraftText,
    [string] $SaveText = 'Salva spesa'
)

$ErrorActionPreference = 'Stop'
$pages = Join-Path $RepoRoot 'src/Centello.App/Pages'
$namespaces = @{
    ui = 'http://schemas.microsoft.com/dotnet/2021/maui'
    controls = 'clr-namespace:Centello.App.Controls'
}

$forms = [ordered]@{
    ExpenseEditorPage = @('Amount', 'Picker.Categories', 'SaveCommand')
    CategoryEditorPage = @('Name', 'PreviewName', 'SaveCommand')
    RecurringPaymentEditorPage = @('Amount', 'FrequencyIndex', 'SaveCommand')
    RecurringPaymentsPage = @('List.Active', 'List.Paused')
    SettingsPage = @('RecurringSummary', 'ApiKeyInput')
    BackupPage = @()
    RestorePage = @('FileName', 'Rows')
    LicensesPage = @('LicenseTerms', 'Components')
}

foreach ($page in $forms.Keys) {
    $path = Join-Path $pages "$page.xaml"
    $form = Select-Xml -LiteralPath $path -Namespace $namespaces -XPath '//controls:PhoneLandscapeForm'
    if (@($form).Count -ne 1) {
        throw "$page must host one adaptive form so rotation keeps the live controls."
    }

    $statePath = '//ui:VisualStateGroup[ui:VisualState/ui:VisualState.StateTriggers/controls:PhoneLandscapeTrigger]/ui:VisualState'
    $states = @(Select-Xml -LiteralPath $path -Namespace $namespaces -XPath $statePath)
    $switchToPortrait = '*[local-name()="VisualState.StateTriggers"]/*[local-name()="StateTrigger"][@IsActive="True"]'
    if ($states.Count -lt 2 -or -not $states[1].Node.SelectSingleNode($switchToPortrait)) {
        throw "$page must return to its default layout after the landscape trigger deactivates."
    }

    $formXml = $form.Node.OuterXml
    foreach ($binding in $forms[$page]) {
        $pattern = '\{Binding\s+' + [regex]::Escape($binding) + '}'
        if ([regex]::Matches($formXml, $pattern).Count -ne 1) {
            throw "$page must have one binding for $binding in the adaptive form."
        }
    }

    $primary = $form.Node.SelectSingleNode('*[local-name()="PhoneLandscapeForm.Primary"]')
    $secondary = $form.Node.SelectSingleNode('*[local-name()="PhoneLandscapeForm.Secondary"]')
    if (-not $primary.HasChildNodes -or -not $secondary.HasChildNodes) {
        throw "$page must provide content for both landscape panes."
    }

    if ($forms[$page] -contains 'SaveCommand') {
        $footer = $form.Node.SelectSingleNode('*[local-name()="PhoneLandscapeForm.Footer"]')
        if ($footer.OuterXml -notmatch '\{Binding SaveCommand}') {
            throw "$page must keep save reachable outside the scrolling fields."
        }
    }
}

Write-Host 'All editor and utility pages keep one set of controls across layout changes.'

# Native smoke test. Open an expense in portrait and focus a note containing only DraftText.
# Use a disposable draft: this test inserts and removes one marker to verify the caret.
if (-not $DraftText) {
    return
}

if (-not $Serial) {
    throw 'Pass an explicit Android serial when running the native draft test.'
}

$dumpPath = '/data/local/tmp/centello-phone-landscape-forms.xml'
function Invoke-Device {
    param([string[]] $Arguments)

    $output = & $Adb -s $Serial @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "ADB failed: $Arguments"
    }

    return $output
}

function Get-Window {
    Invoke-Device -Arguments @('shell', 'uiautomator', 'dump', $dumpPath) | Out-Null
    return [xml] ((Invoke-Device -Arguments @('shell', 'cat', $dumpPath)) -join [Environment]::NewLine)
}

function Get-DisplayRotation {
    $defaultDisplay = $false
    foreach ($line in (Invoke-Device -Arguments @('shell', 'dumpsys', 'window', 'displays'))) {
        if ($line -match '^\s*Display:\s+mDisplayId=(\d+)\b') {
            $defaultDisplay = $Matches[1] -eq '0'
            continue
        }

        if ($defaultDisplay -and $line -match '^\s*mRotation=([0-3]|ROTATION_(?:0|90|180|270))\b') {
            switch ($Matches[1]) {
                'ROTATION_0' { return 0 }
                'ROTATION_90' { return 1 }
                'ROTATION_180' { return 2 }
                'ROTATION_270' { return 3 }
                default { return [int] $Matches[1] }
            }
        }
    }

    return -1
}

function Wait-Draft {
    param(
        [string] $Text,
        [int] $Rotation
    )

    for ($attempt = 0; $attempt -lt 8; $attempt++) {
        $actualRotation = Get-DisplayRotation
        if ($actualRotation -ne $Rotation) {
            Start-Sleep -Milliseconds 250
            continue
        }

        $window = Get-Window
        $input = $window.SelectNodes('//node[@focused="true" and @class="android.widget.EditText"]') |
            Where-Object {
                $label = $_.'content-desc'
                $_.text -ceq $Text -or ($label -and $_.text -ceq "$label, $Text")
            }
        $save = $window.SelectNodes('//node') | Where-Object {
            $_.text -ceq $SaveText -or $_.'content-desc' -ceq $SaveText
        }
        $landscape = $window.SelectNodes('//node[@text="Categoria"]') | Where-Object {
            $_.bounds -match '^\[(\d+),' -and [int] $Matches[1] -gt 200
        }
        $ime = (Invoke-Device -Arguments @('shell', 'dumpsys', 'input_method')) -join [Environment]::NewLine
        $keyboardShown = $ime -match '\bm(?:IsInputViewShown|InputShown)=true\b'
        $fullscreen = $ime -match '\bm(?:IsFullscreen|InFullscreenMode)=true\b'
        $inline = $ime -match '\bm(?:IsFullscreen|InFullscreenMode)=false\b' -and !$fullscreen
        if ($input -and $save -and $keyboardShown -and $inline -and ($Rotation -eq 0 -or $landscape)) {
            return
        }

        Start-Sleep -Milliseconds 250
    }

    throw "Expected the focused draft '$Text' and visible '$SaveText' with the keyboard shown inline at display rotation $Rotation (reported $actualRotation)."
}

if ($DraftText -notmatch '^[a-zA-Z]{3,}$') {
    throw 'Use three or more ASCII letters for the disposable draft text.'
}

$initialRotation = (Invoke-Device -Arguments @('shell', 'settings', 'get', 'system', 'user_rotation')).Trim()
$initialAutoRotate = (Invoke-Device -Arguments @('shell', 'settings', 'get', 'system', 'accelerometer_rotation')).Trim()
$draftWithMarker = $DraftText.Insert(1, 'X')
$markerInserted = $false
try {
    Wait-Draft -Text $DraftText -Rotation 0
    Invoke-Device -Arguments @('shell', 'settings', 'put', 'system', 'accelerometer_rotation', '0') | Out-Null
    Invoke-Device -Arguments @('shell', 'input', 'keyevent', 'KEYCODE_MOVE_HOME', 'KEYCODE_DPAD_RIGHT') | Out-Null

    foreach ($rotation in '1', '3', '0') {
        Invoke-Device -Arguments @('shell', 'settings', 'put', 'system', 'user_rotation', $rotation) | Out-Null
        Wait-Draft -Text $DraftText -Rotation $rotation
        Invoke-Device -Arguments @('shell', 'input', 'text', 'X') | Out-Null
        $markerInserted = $true
        Wait-Draft -Text $draftWithMarker -Rotation $rotation
        Invoke-Device -Arguments @('shell', 'input', 'keyevent', 'KEYCODE_DEL') | Out-Null
        $markerInserted = $false
    }

    Write-Host 'The native draft, caret, keyboard focus and save action survived both landscape rotations.'
}
finally {
    if ($markerInserted) {
        Invoke-Device -Arguments @('shell', 'input', 'keyevent', 'KEYCODE_DEL') | Out-Null
    }

    Invoke-Device -Arguments @('shell', 'settings', 'put', 'system', 'user_rotation', $initialRotation) | Out-Null
    Invoke-Device -Arguments @('shell', 'settings', 'put', 'system', 'accelerometer_rotation', $initialAutoRotate) | Out-Null
    Invoke-Device -Arguments @('shell', 'rm', '-f', $dumpPath) | Out-Null
}
