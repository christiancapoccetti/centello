param([string]$Root = (Join-Path $PSScriptRoot "../.."))

$ErrorActionPreference = "Stop"
function Find-Control($document, $name) {
    $document.SelectSingleNode("//*[@*[local-name()='Name' and .='$name']]")
}

$failures = [System.Collections.Generic.List[string]]::new()
$scrollPanes = @{
    Today = @("TodayOverview", "TodayActivity")
    Expenses = @("ChipScroll", "HistoryList")
    Insights = @("InsightsSummary", "InsightsReports")
    CategoryDetail = @("CategorySummary", "CategoryMovements")
    Categories = @("CategoryList")
}

foreach ($page in $scrollPanes.Keys) {
    [xml]$document = Get-Content -Raw -LiteralPath (Join-Path $Root "src/Centello.App/Pages/${page}Page.xaml")
    if (!$document.SelectSingleNode("//*[local-name()='PhoneLandscapeTrigger']")) {
        $failures.Add("$page does not adapt to the phone landscape breakpoint")
    }

    foreach ($pane in $scrollPanes[$page]) {
        $control = Find-Control $document $pane
        if (!$control -or $control.LocalName -notin "ScrollView", "CollectionView") {
            $failures.Add("$page is missing the $pane scroll pane")
        }
    }

    if ($page -eq "Categories" -and !$document.SelectSingleNode("//*[local-name()='Setter' and @Property='GridItemsLayout.Span' and @Value='2']")) {
        $failures.Add("Categories does not use a two-column collection in landscape")
    }
}

foreach ($page in "Today", "Insights") {
    $code = Get-Content -Raw -LiteralPath (Join-Path $Root "src/Centello.App/Pages/${page}Page.xaml.cs")
    if (!$code.Contains("CaptureBeforeReflow()") -or !$code.Contains("RestoreAfterLayout()")) {
        $failures.Add("$page does not preserve its visible content when it changes layout")
    }
}

if ($failures.Count) {
    throw ($failures -join [Environment]::NewLine)
}
Write-Output "Verified landscape browse panes and category columns."
