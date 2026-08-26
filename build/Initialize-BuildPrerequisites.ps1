param(
    [switch]$AllowMissingDevExpressLicense,
    [switch]$SkipDocumentationNodeProvisioning
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
if ($null -eq (Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1)) {
    throw 'dotnet was not found on PATH. Install the repository-required .NET SDK and reopen the terminal before running this build script.'
}

$requiredDocumentationSources = @(
    'docs/index.md',
    'docs/docfx.json',
    'docs/DocfxDependencies.csproj',
    'docs/toc.yml',
    'docs/pdf/toc.yml',
    'docs/architecture/system-overview.md',
    'docs/architecture/council-runtime.md',
    'docs/architecture/frontend-and-themes.md',
    'docs/architecture/ai-host.md',
    'docs/architecture/project-data.md',
    'docs/architecture/onewire-security.md',
    'docs/engineering/build-validation.md',
    'docs/reference/capability-map.md',
    'docs/templates/localgpt/public/main.css',
    'docs/templates/localgpt/public/main.js',
    'docs/templates/localgpt/public/favicon.ico',
    'docs/templates/localgpt/public/favicon.svg',
    'docs/templates/localgpt/public/logo.svg'
)
$missingDocumentationSources = @(
    $requiredDocumentationSources | Where-Object {
        -not (Test-Path -LiteralPath (Join-Path $repositoryRoot $_) -PathType Leaf)
    }
)
if ($missingDocumentationSources.Count -gt 0) {
    throw ('The LocalGPT source tree is missing required documentation source files. The source archive is incomplete: ' + ($missingDocumentationSources -join ', '))
}
Write-Host "Documentation source preflight: $($requiredDocumentationSources.Count) required source file(s) are present." -ForegroundColor DarkGreen

$python = Get-Command python -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $python) { $python = Get-Command python3 -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1 }
if ($null -eq $python) {
    throw 'Python 3 was not found on PATH. It is required for LocalGPT documentation accessibility/link validation and GitHub Pages snapshot preparation.'
}
& $python.Source -c "import sys; raise SystemExit(0 if sys.version_info.major >= 3 else 1)"
if ($LASTEXITCODE -ne 0) { throw 'The resolved Python executable is not Python 3.' }
Write-Host "Documentation Python preflight: using $($python.Source)." -ForegroundColor DarkGreen

& (Join-Path $repositoryRoot 'build/Initialize-DevExpressLicense.ps1') -Require:(-not $AllowMissingDevExpressLicense)

if (-not $SkipDocumentationNodeProvisioning) {
    . (Join-Path $repositoryRoot 'build/NodeRuntime.Common.ps1')
    $toolCacheRoot = Get-LocalGptDocumentationToolCacheRoot -FallbackRoot (Join-Path $repositoryRoot 'docs/.tools')
    $nodeInfo = Resolve-LocalGptNodeRuntime `
        -CacheRoot $toolCacheRoot `
        -Version '22.23.2' `
        -MinimumMajor 20 `
        -MaximumPreferredMajor 22 `
        -AllowProvisioning `
        -PreferCompatibleLts

    if ($null -eq $nodeInfo) {
        throw 'Node.js 20-22 is required for DocFX PDF generation and could not be resolved.'
    }

    $origin = if ([bool]$nodeInfo.Provisioned) { 'LocalGPT per-user tool cache' } else { 'existing installation' }
    Write-Host "Documentation Node.js preflight: using $($nodeInfo.Version) from $origin ($($nodeInfo.Platform)-$($nodeInfo.Architecture))." -ForegroundColor DarkGreen
}
