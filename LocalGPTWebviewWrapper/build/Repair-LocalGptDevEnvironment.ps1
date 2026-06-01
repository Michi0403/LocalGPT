[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet("x64", "x86", "arm64")]
    [string]$Platform = "x64",

    [switch]$InstallMissingRuntime,
    [switch]$SkipBuild,
    [switch]$Register,
    [switch]$Launch
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$solutionPath = Join-Path $repoRoot "LocalGPTWebviewWrapper.sln"
$packageRoot = Join-Path $repoRoot "LocalGPTWebviewWrapper (Package)"
$appxManifest = Join-Path $packageRoot "bin\$Platform\$Configuration\AppxManifest.xml"
$certificateScript = Join-Path $PSScriptRoot "New-LocalPackageCertificate.ps1"
$packageIdentityName = "a6e38587-f17a-4a2e-8022-248694f372b3"

function Find-MsBuild {
    $candidates = @(
        "$env:ProgramFiles\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe",
        "$env:ProgramFiles\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "$env:ProgramFiles\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "$env:ProgramFiles\Microsoft Visual Studio\17\Community\MSBuild\Current\Bin\MSBuild.exe",
        "$env:ProgramFiles\Microsoft Visual Studio\17\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "$env:ProgramFiles\Microsoft Visual Studio\17\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "Could not find Visual Studio MSBuild. Install Visual Studio with .NET desktop, ASP.NET, and Windows app workloads."
}

function Test-DotNetDesktopRuntime10 {
    $runtimes = & dotnet --list-runtimes 2>$null
    return ($runtimes | Select-String -Pattern "^Microsoft.WindowsDesktop.App 10\." -Quiet)
}

Write-Host "LocalGPT development repair"
Write-Host "Repository: $repoRoot"
Write-Host "Configuration: $Configuration"
Write-Host "Platform: $Platform"

if (-not (Test-DotNetDesktopRuntime10)) {
    if ($InstallMissingRuntime) {
        Write-Host "Installing .NET 10 Desktop Runtime with winget..."
        & winget install --id Microsoft.DotNet.DesktopRuntime.10 --source winget --accept-package-agreements --accept-source-agreements
    }
    else {
        Write-Warning ".NET 10 Desktop Runtime is missing. Re-run with -InstallMissingRuntime or install Microsoft.DotNet.DesktopRuntime.10."
    }
}
else {
    Write-Host ".NET 10 Desktop Runtime found."
}

if (Test-Path $certificateScript) {
    Write-Host "Ensuring local package certificate..."
    & $certificateScript
}
else {
    Write-Warning "Certificate script not found: $certificateScript"
}

if (-not $SkipBuild) {
    Get-Process LocalGPTWebviewWrapper -ErrorAction SilentlyContinue | Stop-Process -Force

    $msbuild = Find-MsBuild
    Write-Host "Building solution with $msbuild"
    & $msbuild $solutionPath "/p:Platform=$Platform" "/p:Configuration=$Configuration" /m /v:minimal
}

if ($Register) {
    if (-not (Test-Path $appxManifest)) {
        throw "AppX manifest not found at $appxManifest. Build first."
    }

    Write-Host "Registering loose AppX layout..."
    try {
        Add-AppxPackage -Register $appxManifest -ForceApplicationShutdown -ForceUpdateFromAnyVersion
    }
    catch {
        $existingPackage = Get-AppxPackage $packageIdentityName -ErrorAction SilentlyContinue
        if ($null -eq $existingPackage) {
            throw
        }

        Write-Warning "Registration failed while a LocalGPT development package was already registered. Removing the stale package and retrying once."
        $existingPackage | Remove-AppxPackage
        Add-AppxPackage -Register $appxManifest -ForceApplicationShutdown -ForceUpdateFromAnyVersion
    }
}

if ($Launch) {
    Write-Host "Launching LocalGPT..."
    $app = Get-StartApps | Where-Object { $_.AppID -like "${packageIdentityName}_*" } | Select-Object -First 1
    if ($null -eq $app) {
        throw "LocalGPT app registration was not found. Run with -Register first."
    }

    Start-Process "shell:AppsFolder\$($app.AppID)"
}

Write-Host "Done."
