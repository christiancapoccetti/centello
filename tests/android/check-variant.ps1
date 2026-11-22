param(
    [Parameter(Mandatory)][string]$Manifest,
    [Parameter(Mandatory)][ValidateSet('Dev', 'Play')][string]$Variant
)

$ErrorActionPreference = 'Stop'
[xml]$document = Get-Content -Raw -LiteralPath $Manifest
$android = 'http://schemas.android.com/apk/res/android'
$package = 'it.christiancapoccetti.centesimo'
$label = 'Centello'
$prefix = ''
if ($Variant -eq 'Dev') {
    $package += '.dev'
    $label += ' Dev'
    $prefix = 'Dev - '
}

$app = $document.manifest.application
if ($document.manifest.package -cne $package -or $app.GetAttribute('label', $android) -cne $label) {
    throw "Expected package '$package' with label '$label' in $Manifest"
}

$panel = @($app.activity | Where-Object { $_.GetAttribute('name', $android) -like '*.PanelActivity' })
if ($panel.Count -ne 1 -or $panel[0].GetAttribute('taskAffinity', $android) -cne "$package.panel") {
    throw "Expected the panel activity to use '$package.panel'"
}

$fileProvider = @($app.provider | Where-Object {
    $_.GetAttribute('name', $android) -ceq 'microsoft.maui.essentials.fileProvider'
})
if ($fileProvider.Count -ne 1 -or $fileProvider[0].GetAttribute('authorities', $android) -cne "$package.fileProvider") {
    throw "Expected the file provider for '$package'"
}
foreach ($provider in $app.provider) {
    foreach ($authority in $provider.GetAttribute('authorities', $android).Split(';')) {
        if (!$authority.StartsWith("$package.", [StringComparison]::Ordinal) -or
            ($Variant -eq 'Play' -and $authority.StartsWith("$package.dev.", [StringComparison]::Ordinal))) {
            throw "Provider authority '$authority' belongs to another package"
        }
    }
}

$widgets = @($app.receiver | Where-Object {
    $_.SelectSingleNode("meta-data[@*[local-name()='name']='android.appwidget.provider']")
})
$labels = @($widgets | ForEach-Object { $_.GetAttribute('label', $android) })
foreach ($widget in 'Aggiungi spesa', 'Il mese', 'Il ritmo', 'Scorciatoie') {
    if ($labels -cnotcontains "$prefix$widget") {
        throw "Missing widget label '$prefix$widget'"
    }
}
Write-Output "Verified $Variant manifest for $package"
