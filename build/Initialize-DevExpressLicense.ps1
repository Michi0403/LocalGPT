[CmdletBinding()]
param(
    [switch]$Require
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-LocalGptDevExpressDefaultLicenseDirectory {
    $isWindows = [IO.Path]::DirectorySeparatorChar -eq '\'
    if ($isWindows) {
        if ([string]::IsNullOrWhiteSpace($env:APPDATA)) { return $null }
        return Join-Path $env:APPDATA 'DevExpress'
    }

    $homePath = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
    if ([string]::IsNullOrWhiteSpace($homePath)) { $homePath = $env:HOME }
    if ([string]::IsNullOrWhiteSpace($homePath)) { return $null }

    $unixName = ''
    try {
        $unixName = [string](& uname -s 2>$null | Select-Object -First 1)
    }
    catch {
        $unixName = ''
    }

    if ([string]::Equals($unixName.Trim(), 'Darwin', [StringComparison]::OrdinalIgnoreCase)) {
        return Join-Path (Join-Path $homePath 'Library') 'Application Support/DevExpress'
    }

    return Join-Path $homePath '.config/DevExpress'
}

function Write-LocalGptDevExpressCaseWarnings {
    $expectedNames = @('DevExpress_License', 'DevExpress_LicensePath')
    $environmentVariables = [Environment]::GetEnvironmentVariables()
    foreach ($expectedName in $expectedNames) {
        foreach ($key in $environmentVariables.Keys) {
            $actualName = [string]$key
            if ([string]::Equals($actualName, $expectedName, [StringComparison]::OrdinalIgnoreCase) -and
                -not [string]::Equals($actualName, $expectedName, [StringComparison]::Ordinal)) {
                Write-Warning "DevExpress licensing environment variable '$actualName' has the wrong casing. Unix-like systems require '$expectedName' exactly."
            }
        }
    }
}

Write-LocalGptDevExpressCaseWarnings

$licenseValue = [Environment]::GetEnvironmentVariable('DevExpress_License')
if (-not [string]::IsNullOrWhiteSpace($licenseValue)) {
    Write-Host 'DevExpress license preflight: using DevExpress_License from the current process environment (value not displayed).' -ForegroundColor DarkGreen
    return
}

$licensePathSetting = [Environment]::GetEnvironmentVariable('DevExpress_LicensePath')
if (-not [string]::IsNullOrWhiteSpace($licensePathSetting)) {
    $resolvedSetting = [Environment]::ExpandEnvironmentVariables($licensePathSetting.Trim())
    if (Test-Path -LiteralPath $resolvedSetting -PathType Leaf) {
        if ([string]::Equals([IO.Path]::GetFileName($resolvedSetting), 'DevExpress_License.txt', [StringComparison]::Ordinal)) {
            $resolvedSetting = Split-Path -Parent ([IO.Path]::GetFullPath($resolvedSetting))
            [Environment]::SetEnvironmentVariable('DevExpress_LicensePath', $resolvedSetting, [EnvironmentVariableTarget]::Process)
        }
        else {
            Write-Warning "DevExpress_LicensePath points to a file named '$([IO.Path]::GetFileName($resolvedSetting))'. DevExpress expects a folder containing DevExpress_License.txt."
        }
    }

    $customLicenseFile = Join-Path $resolvedSetting 'DevExpress_License.txt'
    if (Test-Path -LiteralPath $customLicenseFile -PathType Leaf) {
        $file = Get-Item -LiteralPath $customLicenseFile
        if ($file.Length -gt 0) {
            Write-Host "DevExpress license preflight: found DevExpress_License.txt through DevExpress_LicensePath at '$resolvedSetting'." -ForegroundColor DarkGreen
            return
        }
    }

    $message = "DevExpress_LicensePath is set to '$resolvedSetting', but a non-empty DevExpress_License.txt was not found there."
    if ($Require) { throw $message }
    Write-Warning $message
    return
}

$defaultDirectory = Get-LocalGptDevExpressDefaultLicenseDirectory
$defaultLicenseFile = if ([string]::IsNullOrWhiteSpace($defaultDirectory)) { $null } else { Join-Path $defaultDirectory 'DevExpress_License.txt' }
if (-not [string]::IsNullOrWhiteSpace($defaultLicenseFile) -and (Test-Path -LiteralPath $defaultLicenseFile -PathType Leaf)) {
    $file = Get-Item -LiteralPath $defaultLicenseFile
    if ($file.Length -gt 0) {
        # DevExpress already probes its platform default folder. Exporting the same folder explicitly to
        # the child dotnet process also makes VS Code/pwsh build behavior deterministic on macOS/Linux.
        [Environment]::SetEnvironmentVariable('DevExpress_LicensePath', $defaultDirectory, [EnvironmentVariableTarget]::Process)
        Write-Host "DevExpress license preflight: found the platform license file at '$defaultLicenseFile' and exported DevExpress_LicensePath for child dotnet processes." -ForegroundColor DarkGreen
        return
    }
}

$expected = if ([string]::IsNullOrWhiteSpace($defaultLicenseFile)) { 'the platform-specific DevExpress license directory' } else { $defaultLicenseFile }
$message = @"
No DevExpress .NET license key was found for this build process.
Expected a non-empty DevExpress_License.txt at: $expected
Alternatively set the case-sensitive DevExpress_LicensePath (folder) or DevExpress_License (key value) environment variable.
LocalGPT uses DevExpress 25.2.x; the registered key must support that major version. Do not commit the key to this repository.
"@.Trim()

if ($Require) { throw $message }
Write-Warning $message
