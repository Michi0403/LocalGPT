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
