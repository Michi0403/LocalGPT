Set-StrictMode -Version Latest

function Initialize-Future2RepositoryBuildStorage {
    param(
        [Parameter(Mandatory = $true)][string]$RepositoryRoot,
        [AllowEmptyString()][string]$DocumentationCacheRoot = ''
    )

    $repository = [IO.Path]::GetFullPath($RepositoryRoot)
    $configuredRoot = [string]$env:FUTURE2_BUILD_STORAGE_ROOT
    $storageRoot = if (-not [string]::IsNullOrWhiteSpace($configuredRoot)) {
        [IO.Path]::GetFullPath($configuredRoot)
    }
    else {
        Join-Path $repository 'artifacts/.build-storage'
    }

    $tempRoot = Join-Path $storageRoot 'tmp'
    $dotnetHome = Join-Path $storageRoot 'dotnet-home'
    $nugetPackages = Join-Path $storageRoot 'nuget/packages'
    $nugetHttpCache = Join-Path $storageRoot 'nuget/http-cache'
    $nugetPluginCache = Join-Path $storageRoot 'nuget/plugins-cache'
    $nugetScratch = Join-Path $storageRoot 'nuget/scratch'
    $npmCache = Join-Path $storageRoot 'npm-cache'
    $xdgCache = Join-Path $storageRoot 'xdg-cache'
    $documentationRoot = if (-not [string]::IsNullOrWhiteSpace($DocumentationCacheRoot)) {
        [IO.Path]::GetFullPath($DocumentationCacheRoot)
    }
    else {
        Join-Path $storageRoot 'documentation'
    }

    foreach ($path in @(
        $storageRoot,
        $tempRoot,
        $dotnetHome,
        $nugetPackages,
        $nugetHttpCache,
        $nugetPluginCache,
        $nugetScratch,
        $npmCache,
        $xdgCache,
        $documentationRoot
    )) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
    }

    # Heavy release-build state belongs with the checkout by default. This matters when the
    # repository is on an external APFS volume: per-user caches and the OS temp folder otherwise
    # consume the internal system volume even though bin/obj/artifacts are external.
    $env:FUTURE2_BUILD_STORAGE_ROOT = $storageRoot
    $env:FUTURE2_DOCUMENTATION_CACHE_ROOT = $documentationRoot
    $env:DOTNET_CLI_HOME = $dotnetHome
    $env:NUGET_PACKAGES = $nugetPackages
    $env:NUGET_HTTP_CACHE_PATH = $nugetHttpCache
    $env:NUGET_PLUGINS_CACHE_PATH = $nugetPluginCache
    $env:NUGET_SCRATCH = $nugetScratch
    $env:NPM_CONFIG_CACHE = $npmCache
    $env:XDG_CACHE_HOME = $xdgCache
    $env:TMPDIR = $tempRoot
    $env:TMP = $tempRoot
    $env:TEMP = $tempRoot

    Write-Host "Repository build storage: $storageRoot" -ForegroundColor DarkCyan
    Write-Host "Repository build temp: $tempRoot" -ForegroundColor DarkCyan
    Write-Host "Repository NuGet cache: $nugetPackages" -ForegroundColor DarkCyan
    Write-Host "Repository documentation cache: $documentationRoot" -ForegroundColor DarkCyan

    return [pscustomobject]@{
        Root = $storageRoot
        Temp = $tempRoot
        DotNetHome = $dotnetHome
        NuGetPackages = $nugetPackages
        NuGetHttpCache = $nugetHttpCache
        NuGetPluginCache = $nugetPluginCache
        NuGetScratch = $nugetScratch
        NpmCache = $npmCache
        XdgCache = $xdgCache
        Documentation = $documentationRoot
    }
}
