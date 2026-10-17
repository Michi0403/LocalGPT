$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $repositoryRoot "src/LocalGPT/LocalGPT.csproj"
$projectRoot = Join-Path $repositoryRoot "src/LocalGPT"
$webRoot = Join-Path $projectRoot "wwwroot"

if (-not (Test-Path -LiteralPath $projectFile -PathType Leaf)) { throw "LocalGPT project file is missing: $projectFile" }
if (-not (Test-Path -LiteralPath $webRoot -PathType Container)) { throw "LocalGPT wwwroot is missing: $webRoot" }

[xml]$project = Get-Content -LiteralPath $projectFile -Raw
$requiredAssets = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

# Use XPath instead of dynamic XML properties. With Set-StrictMode enabled, reading
# $itemGroup.Content throws PropertyNotFoundStrict for perfectly valid ItemGroup nodes
# that contain only PackageReference/ProjectReference/etc.
$contentNodes = @($project.SelectNodes("//*[local-name()='Content'][@Update or @Include]"))
foreach ($content in $contentNodes) {
    $path = if ($content.HasAttribute("Update")) {
        [string]$content.GetAttribute("Update")
    } elseif ($content.HasAttribute("Include")) {
        [string]$content.GetAttribute("Include")
    } else {
        [string]::Empty
    }

    if ([string]::IsNullOrWhiteSpace($path)) { continue }
    $normalized = $path.Replace('\', '/')
    if ($normalized.StartsWith('wwwroot/images/', [StringComparison]::OrdinalIgnoreCase) -or
        $normalized -in @(
            'wwwroot/favicon.ico',
            'wwwroot/favicon-16x16.png',
            'wwwroot/favicon-32x32.png',
            'wwwroot/android-chrome-192x192.png',
            'wwwroot/android-chrome-512x512.png',
            'wwwroot/apple-touch-icon.png',
            'wwwroot/css/site.css'
        )) {
        [void]$requiredAssets.Add($normalized)
    }
}

# These are runtime-critical even if a future SDK/default-item change stops producing
# explicit Content metadata in the project file.
foreach ($relative in @(
    'wwwroot/js/documentationViewer.js',
    'wwwroot/images/TacosLogos.svg',
    'wwwroot/images/Information.svg'
)) {
    [void]$requiredAssets.Add($relative)
}

$missing = [System.Collections.Generic.List[string]]::new()
foreach ($relative in ($requiredAssets | Sort-Object)) {
    $full = Join-Path $projectRoot $relative
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
        $missing.Add($relative)
    }
}

if ($missing.Count -gt 0) {
    throw "LocalGPT static-web-asset validation failed. Missing $($missing.Count) maintained asset(s): $($missing -join ', ')"
}
# Council evidence uses DevExpress portal/FormLayout wrappers. Its native content grid
# can only occupy the popup's 90dvh height when the entire intermediate layout stretches.
$siteCssPath = Join-Path $webRoot 'css/site.css'
$siteCss = [IO.File]::ReadAllText($siteCssPath)
foreach ($requiredCssContract in @(
    'dxbl-modal-dialog.dxbl-popup:has(.localgpt-council-evidence-layout) .dxbl-modal-body',
    '.localgpt-council-evidence-panes',
    'grid-template-rows: minmax(0, 1fr);',
    '.localgpt-council-evidence-pane > .localgpt-live-participant-waiting')) {
    if (-not $siteCss.Contains($requiredCssContract)) {
        throw "Council evidence popup no longer stretches/scrolls within the DevExpress viewport: $requiredCssContract"
    }
}

Write-Host "Static web asset validation passed for $($requiredAssets.Count) maintained LocalGPT files." -ForegroundColor Green
