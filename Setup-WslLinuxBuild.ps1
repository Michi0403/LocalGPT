[CmdletBinding()]
param(
    [string]$Distribution = '',
    [switch]$InstallUbuntu,
    [switch]$Provision,
    [string]$DevExpressLicenseFile = '',
    [switch]$CopyWindowsDevExpressLicense,
    [switch]$SkipAppImageTool,
    [ValidateSet('IfStarted','Always','Never')][string]$Shutdown = 'IfStarted'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$common = Join-Path $root 'build/WslRelease.Common.ps1'
if (-not (Test-Path -LiteralPath $common -PathType Leaf)) { throw "WSL release helper is missing: $common" }
. $common

if (-not (Test-WslReleaseWindowsHost)) { throw 'Setup-WslLinuxBuild.ps1 is a Windows helper for WSL. Run the normal Linux Build-Release.ps1 directly on native Linux.' }
$wsl = Get-WslReleaseExecutable
if ([string]::IsNullOrWhiteSpace($wsl)) { throw 'wsl.exe is not available. Install Windows Subsystem for Linux from an elevated PowerShell session, restart Windows if requested, and run this helper again.' }

$distro = Resolve-WslReleaseDistribution -WslExecutable $wsl -RequestedDistribution $Distribution
if ([string]::IsNullOrWhiteSpace($distro) -and $InstallUbuntu) {
    Write-Host 'Installing the Ubuntu WSL distribution through the Windows WSL command...' -ForegroundColor Cyan
    & $wsl --install -d Ubuntu
    if ($LASTEXITCODE -ne 0) { throw "wsl.exe --install -d Ubuntu failed with exit code $LASTEXITCODE." }
    Write-Host 'Ubuntu installation was requested. Complete any Windows restart and the one-time Ubuntu user initialization, then rerun Setup-WslLinuxBuild.ps1 -Provision.' -ForegroundColor Yellow
    return
}
if ([string]::IsNullOrWhiteSpace($distro)) {
    throw 'No usable WSL distribution is registered. Install and initialize Ubuntu first, or rerun with -InstallUbuntu.'
}

$runningBefore = @(Get-WslReleaseRunningDistributions -WslExecutable $wsl)
$wasRunning = @($runningBefore | Where-Object { [string]::Equals($_, $distro, [StringComparison]::OrdinalIgnoreCase) }).Count -gt 0
try {
    Write-Host "WSL Linux build backend: $distro" -ForegroundColor Cyan
    $initialStatus = Get-WslReleaseBuildStatus -WslExecutable $wsl -Distribution $distro
    if (-not $initialStatus.Available) { throw "WSL distribution '$distro' could not run the build status probe. $($initialStatus.Detail)" }
    if (-not $initialStatus.Wsl2) { throw "WSL distribution '$distro' is not running as WSL2. Convert it from Windows with: wsl.exe --set-version `"$distro`" 2" }

    if ($Provision) {
        $provisionWindows = Join-Path $root 'build/wsl/Provision-WslLinuxBuild.sh'
        if (-not (Test-Path -LiteralPath $provisionWindows -PathType Leaf)) { throw "WSL provisioning script is missing: $provisionWindows" }
        $provisionWsl = ConvertTo-WslReleasePath -WslExecutable $wsl -Distribution $distro -WindowsPath $provisionWindows
        $arguments = @('-d',$distro,'--','bash',$provisionWsl)
        if ($SkipAppImageTool) { $arguments += '--skip-appimagetool' }
        & $wsl @arguments | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0) { throw "WSL build-host provisioning failed in '$distro' with exit code $LASTEXITCODE." }
    }

    $licenseSource = $DevExpressLicenseFile
    if ([string]::IsNullOrWhiteSpace($licenseSource) -and $CopyWindowsDevExpressLicense) {
        $directory = Get-WslReleaseWindowsDevExpressLicenseDirectory
        if (-not [string]::IsNullOrWhiteSpace($directory)) { $licenseSource = Join-Path $directory 'DevExpress_License.txt' }
    }
    if (-not [string]::IsNullOrWhiteSpace($licenseSource)) {
        $licenseSource = [IO.Path]::GetFullPath($licenseSource)
        if (-not (Test-Path -LiteralPath $licenseSource -PathType Leaf) -or (Get-Item -LiteralPath $licenseSource).Length -le 0) { throw "DevExpress license file is missing or empty: $licenseSource" }
        $licenseWsl = ConvertTo-WslReleasePath -WslExecutable $wsl -Distribution $distro -WindowsPath $licenseSource
        $copyScript = 'set -e; umask 077; mkdir -p "$HOME/.config/DevExpress"; cp -f "$1" "$HOME/.config/DevExpress/DevExpress_License.txt"; chmod 0600 "$HOME/.config/DevExpress/DevExpress_License.txt"'
        & $wsl -d $distro -- bash -c $copyScript '_' $licenseWsl
        if ($LASTEXITCODE -ne 0) { throw "Could not register the DevExpress license inside WSL '$distro'." }
        Write-Host 'Registered DevExpress_License.txt in the WSL user profile without copying it into either repository.' -ForegroundColor Green
    }

    $status = Get-WslReleaseBuildStatus -WslExecutable $wsl -Distribution $distro
    if (-not $status.Available) { throw "WSL distribution '$distro' could not run the build status probe. $($status.Detail)" }
    Write-Host "Linux: $($status.Linux) $($status.Architecture)" -ForegroundColor DarkCyan
    Write-Host "Core tools: $(Get-WslReleaseReadinessMessage $status)" -ForegroundColor $(if ($status.CoreReady) { 'Green' } else { 'Yellow' })
    Write-Host "RPM: $(if ($status.RpmBuild) { 'rpmbuild ready' } else { 'optional rpmbuild missing' })" -ForegroundColor $(if ($status.RpmBuild) { 'Green' } else { 'Yellow' })
    Write-Host "AppImage: $(if ($status.AppImageTool) { 'appimagetool ready' } else { 'optional appimagetool missing' })" -ForegroundColor $(if ($status.AppImageTool) { 'Green' } else { 'Yellow' })
    $windowsLicenseDirectory = Get-WslReleaseWindowsDevExpressLicenseDirectory
    $licenseAvailable = $status.DevExpressFile -or (-not [string]::IsNullOrWhiteSpace($windowsLicenseDirectory)) -or (-not [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable('DevExpress_License')))
    if ($licenseAvailable) {
        if ($status.DevExpressFile) { Write-Host 'DevExpress: WSL user license file ready.' -ForegroundColor Green }
        else { Write-Host 'DevExpress: Windows license bridge ready; the private key is not copied into the repository.' -ForegroundColor Green }
    }
    else {
        Write-Warning 'DevExpress build license is not available to WSL. Put DevExpress_License.txt in the normal Windows DevExpress folder, set DevExpress_License/DevExpress_LicensePath, or rerun this helper with -DevExpressLicenseFile <path>.'
    }

    if ($status.CoreReady) {
        Write-Host "WSL '$distro' is ready for headless Linux release builds. Normal Windows Build-Release.ps1 uses it automatically when WSL mode is Auto." -ForegroundColor Green
    }
    else {
        Write-Warning "WSL '$distro' is not yet release-ready. Rerun with -Provision to install the maintained Ubuntu/Debian prerequisites."
    }
}
finally {
    $terminate = $false
    if ($Shutdown -eq 'Always') { $terminate = $true }
    elseif ($Shutdown -eq 'IfStarted' -and -not $wasRunning) { $terminate = $true }
    if ($terminate) {
        Write-Host "Stopping WSL distribution '$distro' after setup/status work..." -ForegroundColor DarkCyan
        & $wsl --terminate $distro 2>$null | Out-Null
    }
}
