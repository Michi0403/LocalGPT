$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $repositoryRoot "src/LocalGPT/LocalGPT.csproj"
$projectRoot = Join-Path $repositoryRoot "src/LocalGPT"
$webRoot = Join-Path $projectRoot "wwwroot"

if (-not (Test-Path -LiteralPath $projectFile -PathType Leaf)) { throw "LocalGPT project file is missing: $projectFile" }
if (-not (Test-Path -LiteralPath $webRoot -PathType Container)) { throw "LocalGPT wwwroot is missing: $webRoot" }

[xml]$project = Get-Content -LiteralPath $projectFile -Raw
$required = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

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
    $normalized = $path.Replace('/', '\')
    if ($normalized.StartsWith('wwwroot\images\', [StringComparison]::OrdinalIgnoreCase) -or
        $normalized -in @(
            'wwwroot\favicon.ico',
            'wwwroot\favicon-16x16.png',
            'wwwroot\favicon-32x32.png',
            'wwwroot\android-chrome-192x192.png',
            'wwwroot\android-chrome-512x512.png',
            'wwwroot\apple-touch-icon.png',
            'wwwroot\css\site.css'
        )) {
        [void]$required.Add($normalized)
    }
}

# These are runtime-critical even if a future SDK/default-item change stops producing
# explicit Content metadata in the project file.
foreach ($relative in @(
    'wwwroot\js\documentationViewer.js',
    'wwwroot\images\TacosLogos.svg',
    'wwwroot\images\Information.svg'
)) {
    [void]$required.Add($relative)
}

$missing = [System.Collections.Generic.List[string]]::new()
foreach ($relative in ($required | Sort-Object)) {
    $full = Join-Path $projectRoot $relative
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
        $missing.Add($relative)
    }
}

if ($missing.Count -gt 0) {
    throw "LocalGPT static-web-asset validation failed. Missing $($missing.Count) maintained asset(s): $($missing -join ', ')"
}

Write-Host "Static web asset validation passed for $($required.Count) maintained LocalGPT files." -ForegroundColor Green
