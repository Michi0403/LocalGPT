[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('LocalGPT','PublisherStudio')][string]$ProductName,
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][string]$PreparedDocumentationRoot,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][ValidateSet('linux-x64','linux-arm64')][string[]]$Runtimes,
    [ValidateSet('Release','Debug')][string]$Configuration = 'Release',
    [string]$Distribution = '',
    [string]$ReleasePackagingPackagePath = '',
    [switch]$UseContainerPackaging,
    [switch]$RequireOptionalNativePackages,
    [switch]$KeepBuildTree,
    [ValidateSet('IfStarted','Always','Never')][string]$Shutdown = 'IfStarted'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$common = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'WslRelease.Common.ps1'
if (-not (Test-Path -LiteralPath $common -PathType Leaf)) { throw "WSL release helper is missing: $common" }
. $common

if (-not (Test-WslReleaseWindowsHost)) { throw 'The WSL Linux release bridge can only be invoked from Windows.' }
$wsl = Get-WslReleaseExecutable
if ([string]::IsNullOrWhiteSpace($wsl)) { throw 'wsl.exe is not installed or not available on PATH.' }
$distro = Resolve-WslReleaseDistribution -WslExecutable $wsl -RequestedDistribution $Distribution
if ([string]::IsNullOrWhiteSpace($distro)) { throw 'No usable WSL Linux distribution was found. Install/initialize Ubuntu or pass -WslDistribution explicitly.' }

$runningBefore = @(Get-WslReleaseRunningDistributions -WslExecutable $wsl)
$wasRunning = @($runningBefore | Where-Object { [string]::Equals($_, $distro, [StringComparison]::OrdinalIgnoreCase) }).Count -gt 0
$bridgeState = $null
$importDirectory = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) ('.wsl-import-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $importDirectory -Force | Out-Null

try {
    $status = Get-WslReleaseBuildStatus -WslExecutable $wsl -Distribution $distro
    if (-not $status.Available -or -not $status.CoreReady) {
        $message = if ([string]::IsNullOrWhiteSpace([string]$status.Detail)) { Get-WslReleaseReadinessMessage $status } else { [string]$status.Detail }
        throw "WSL distribution '$distro' is not release-ready: $message. Run .\Setup-WslLinuxBuild.ps1 -Provision from this repository on Windows."
    }
    if (-not (Test-WslReleaseDevExpressLicenseAvailable -WslExecutable $wsl -Distribution $distro -Status $status)) {
        throw "WSL distribution '$distro' has no usable DevExpress build license. The bridge accepts the Windows DevExpress_License/DevExpress_LicensePath environment variables, the normal Windows %APPDATA%\DevExpress\DevExpress_License.txt file, or the Linux `$HOME/.config/DevExpress/DevExpress_License.txt file."
    }

    $bridgeState = Enable-WslReleaseDevExpressBridge
    $repositoryPath = ConvertTo-WslReleasePath -WslExecutable $wsl -Distribution $distro -WindowsPath ([IO.Path]::GetFullPath($RepositoryRoot))
    $outputPath = ConvertTo-WslReleasePath -WslExecutable $wsl -Distribution $distro -WindowsPath ([IO.Path]::GetFullPath($importDirectory))
    $documentationPath = ConvertTo-WslReleasePath -WslExecutable $wsl -Distribution $distro -WindowsPath ([IO.Path]::GetFullPath($PreparedDocumentationRoot))
    $shellScriptWindows = Join-Path ([IO.Path]::GetFullPath($RepositoryRoot)) 'build/wsl/Invoke-LinuxRelease.sh'
    if (-not (Test-Path -LiteralPath $shellScriptWindows -PathType Leaf)) { throw "WSL Linux child script is missing: $shellScriptWindows" }
    $shellScriptPath = ConvertTo-WslReleasePath -WslExecutable $wsl -Distribution $distro -WindowsPath $shellScriptWindows

    $runtimeArgument = if ($Runtimes.Count -gt 1) { 'all' } else { [string]$Runtimes[0] }
    $arguments = [System.Collections.Generic.List[string]]::new()
    foreach ($value in @('-d',$distro,'--','bash',$shellScriptPath,'--product',$ProductName,'--source',$repositoryPath,'--output',$outputPath,'--docs',$documentationPath,'--version',$Version,'--configuration',$Configuration,'--runtime',$runtimeArgument)) { $arguments.Add([string]$value) }
    if (-not [string]::IsNullOrWhiteSpace($ReleasePackagingPackagePath)) {
        if (-not (Test-Path -LiteralPath $ReleasePackagingPackagePath -PathType Leaf)) { throw "Release-packaging package for WSL is missing: $ReleasePackagingPackagePath" }
        $packagePath = ConvertTo-WslReleasePath -WslExecutable $wsl -Distribution $distro -WindowsPath ([IO.Path]::GetFullPath($ReleasePackagingPackagePath))
        $arguments.Add('--release-packaging-package'); $arguments.Add($packagePath)
    }
    if ($UseContainerPackaging) { $arguments.Add('--use-container-packaging') }
    if ($RequireOptionalNativePackages) { $arguments.Add('--require-optional-native-packages') }
    if ($KeepBuildTree) { $arguments.Add('--keep-build-tree') }

    $rpmLabel = if ($status.RpmBuild) { 'RPM ready' } else { 'RPM tool missing (optional unless strict)' }
    $appImageLabel = if ($status.AppImageTool) { 'AppImage tool ready' } else { 'AppImage tool missing (optional unless strict)' }
    Write-Host "Using WSL distribution '$distro' ($($status.Architecture)) as the headless Linux release backend: $rpmLabel; $appImageLabel." -ForegroundColor Cyan
    & $wsl @arguments | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { throw "WSL Linux release build failed in '$distro' with exit code $LASTEXITCODE." }

    $files = @(Get-ChildItem -LiteralPath $importDirectory -File -ErrorAction SilentlyContinue | Sort-Object Name)
    foreach ($rid in $Runtimes) {
        foreach ($mode in @('full','light')) {
            foreach ($extension in @('.tar.gz','.deb')) {
                $expected = "$ProductName-$Version-$rid-$mode$extension"
                if (-not @($files | Where-Object { [string]::Equals($_.Name, $expected, [StringComparison]::Ordinal) })) {
                    throw "WSL completed without mandatory Linux artifact '$expected'."
                }
            }
        }
    }
    if ($files.Count -eq 0) { throw 'WSL completed without returning Linux release artifacts.' }

    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    $result = [System.Collections.Generic.List[string]]::new()
    foreach ($file in $files) {
        $destination = Join-Path $OutputDirectory $file.Name
        Move-Item -LiteralPath $file.FullName -Destination $destination -Force
        $result.Add([IO.Path]::GetFullPath($destination))
    }
    Write-Host "Imported $($result.Count) Linux release artifact(s) from WSL '$distro'." -ForegroundColor Green
    $result
}
finally {
    if ($null -ne $bridgeState) { Disable-WslReleaseDevExpressBridge -State $bridgeState }
    Remove-Item -LiteralPath $importDirectory -Recurse -Force -ErrorAction SilentlyContinue
    $terminate = $false
    if ($Shutdown -eq 'Always') { $terminate = $true }
    elseif ($Shutdown -eq 'IfStarted' -and -not $wasRunning) { $terminate = $true }
    if ($terminate) {
        Write-Host "Stopping WSL distribution '$distro' after the release helper finished..." -ForegroundColor DarkCyan
        & $wsl --terminate $distro 2>$null | Out-Null
    }
}
