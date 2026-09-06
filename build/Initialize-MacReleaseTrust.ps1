param(
    [Parameter(Mandatory)][string]$ProductName,
    [Parameter(Mandatory)][string[]]$SelectedRuntimes,
    [switch]$AllowUnsignedMacPackages
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$isMacHost = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::OSX)
if (-not $isMacHost -or @($SelectedRuntimes | Where-Object { $_.StartsWith('osx-') }).Count -eq 0) { return }

function Get-EnvironmentText([string]$Name) {
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace([string]$value)) { return $null }
    return ([string]$value).Trim()
}
function Get-ExternalCommandPath([string]$Name) {
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command -and -not [string]::IsNullOrWhiteSpace([string]$command.Source)) { return [string]$command.Source }
    return $null
}
function Get-FirstDeveloperIdentity([string[]]$Lines,[string]$Prefix) {
    foreach ($line in $Lines) {
        if ([string]$line -match ('"(' + [regex]::Escape($Prefix) + '[^"]+)"')) { return $Matches[1] }
    }
    return $null
}

function Test-MacNotaryCredentialRecoveryRequired([string]$Details) {
    if ([string]::IsNullOrWhiteSpace($Details)) { return $false }
    return $Details -match '(?i)(No Keychain password item found|User interaction is not allowed|errSecInteractionNotAllowed|specified item could not be found in the keychain|keychain[^\r\n]*(?:locked|unavailable|interaction)|credential profile[^\r\n]*(?:missing|unavailable))'
}
function Test-MacNotaryTransientServiceRecoveryRequired([string]$Details) {
    if ([string]::IsNullOrWhiteSpace($Details)) { return $false }
    return $Details -match '(?i)(NSURLError|internet connection[^\r\n]*(?:offline|lost)|network[^\r\n]*(?:offline|unavailable|failed|error)|could not connect to (?:the )?server|connection[^\r\n]*(?:reset|refused|lost)|request[^\r\n]*timed out|operation[^\r\n]*timed out|HTTP[^\r\n]*(?:408|425|429|500|502|503|504)|service[^\r\n]*(?:temporarily unavailable|unavailable|busy)|server[^\r\n]*(?:temporarily unavailable|unavailable|busy))'
}
function Test-CompleteApiNotaryCredentials {
    return [bool]((Get-EnvironmentText 'APPLE_NOTARY_KEY_ID') -and (Get-EnvironmentText 'APPLE_NOTARY_ISSUER') -and (Get-EnvironmentText 'APPLE_NOTARY_KEY_PATH'))
}
function Test-CompleteAppleIdNotaryCredentials {
    return [bool]((Get-EnvironmentText 'APPLE_NOTARY_APPLE_ID') -and (Get-EnvironmentText 'APPLE_NOTARY_TEAM_ID') -and (Get-EnvironmentText 'APPLE_NOTARY_PASSWORD'))
}

if ($AllowUnsignedMacPackages) {
    if (-not (Get-EnvironmentText 'MACOS_REQUIRE_NOTARIZATION')) { $env:MACOS_REQUIRE_NOTARIZATION = '0' }
    Write-Warning "$ProductName macOS public-release trust enforcement was explicitly disabled with -AllowUnsignedMacPackages. Generated local-only artifacts can still be blocked by Gatekeeper after Internet download."
    return
}

# A normal release started on macOS is a public-distribution build by default. This prevents a
# multi-hour coordinator run from quietly returning ad-hoc/unnotarized packages by mistake.
$env:MACOS_REQUIRE_NOTARIZATION = '1'

$requiredTools = @('security','codesign','pkgbuild','hdiutil','xcrun')
$missingTools = @($requiredTools | Where-Object { -not (Get-ExternalCommandPath $_) })
if ($missingTools.Count -gt 0) {
    throw "$ProductName macOS public-release signing requires Xcode command-line tools. Missing: $($missingTools -join ', ')."
}

$security = Get-ExternalCommandPath 'security'
$identityLines = @(& $security find-identity -v 2>$null | ForEach-Object { [string]$_ })
if ($LASTEXITCODE -ne 0) { throw 'The macOS keychain signing identities could not be enumerated with security find-identity -v.' }

$appIdentity = Get-EnvironmentText 'MACOS_DEVELOPER_ID_APPLICATION'
if (-not $appIdentity) { $appIdentity = Get-FirstDeveloperIdentity -Lines $identityLines -Prefix 'Developer ID Application:' }
$installerIdentity = Get-EnvironmentText 'MACOS_DEVELOPER_ID_INSTALLER'
if (-not $installerIdentity) { $installerIdentity = Get-FirstDeveloperIdentity -Lines $identityLines -Prefix 'Developer ID Installer:' }
if (-not $appIdentity) {
    throw 'Developer ID Application identity is missing. Install the Apple Developer Program certificate/private key in this Mac keychain or set MACOS_DEVELOPER_ID_APPLICATION.'
}
if (-not $installerIdentity) {
    throw 'Developer ID Installer identity is missing. Install the Apple Developer Program certificate/private key in this Mac keychain or set MACOS_DEVELOPER_ID_INSTALLER.'
}
$env:MACOS_DEVELOPER_ID_APPLICATION = $appIdentity
$env:MACOS_DEVELOPER_ID_INSTALLER = $installerIdentity

$profile = Get-EnvironmentText 'MACOS_NOTARY_KEYCHAIN_PROFILE'
$hasApiCredentials = Test-CompleteApiNotaryCredentials
$hasAppleIdCredentials = Test-CompleteAppleIdNotaryCredentials
if (-not $profile -and -not $hasApiCredentials -and -not $hasAppleIdCredentials) {
    # Future2 uses one build-machine keychain profile for both LocalGPT and PublisherStudio.
    $profile = 'future2-notary'
    $env:MACOS_NOTARY_KEYCHAIN_PROFILE = $profile
}

$xcrun = Get-ExternalCommandPath 'xcrun'
if ($profile) {
    Write-Host "Validating Apple notarization keychain profile '$profile' before the expensive release build..." -ForegroundColor Cyan
    $profileArguments = @('--keychain-profile', $profile)
    $keychainPath = Get-EnvironmentText 'MACOS_NOTARY_KEYCHAIN_PATH'
    if ($keychainPath) { $profileArguments += @('--keychain', $keychainPath) }
    $probeSucceeded = $false
    $lastDetails = ''
    for ($attempt = 1; $attempt -le 5; $attempt++) {
        $nativePreferenceVariable = Get-Variable -Name PSNativeCommandUseErrorActionPreference -ErrorAction SilentlyContinue
        $previousNativePreference = $null
        if ($null -ne $nativePreferenceVariable) {
            $previousNativePreference = [bool]$PSNativeCommandUseErrorActionPreference
            $PSNativeCommandUseErrorActionPreference = $false
        }
        try {
            $probe = @(& $xcrun notarytool history @profileArguments --output-format json --no-progress 2>&1 | ForEach-Object { [string]$_ })
            $probeExitCode = [int]$LASTEXITCODE
        }
        finally {
            if ($null -ne $nativePreferenceVariable) { $PSNativeCommandUseErrorActionPreference = $previousNativePreference }
        }
        if ($probeExitCode -eq 0) { $probeSucceeded = $true; break }
        $lastDetails = (($probe | Select-Object -Last 12) -join [Environment]::NewLine)
        if ((Test-MacNotaryCredentialRecoveryRequired -Details $lastDetails) -or (Test-MacNotaryTransientServiceRecoveryRequired -Details $lastDetails)) {
            if ($attempt -lt 5) {
                Write-Warning "Apple notarization startup probe attempt $attempt failed transiently; retrying in 10 seconds before any expensive build work starts."
                Write-Warning $lastDetails
                Start-Sleep -Seconds 10
                continue
            }
        }
        break
    }
    if (-not $probeSucceeded) {
        $diagnostic = "xcrun notarytool history --keychain-profile '$profile'"
        if ($keychainPath) { $diagnostic += " --keychain '$keychainPath'" }
        throw "The Apple notarization profile '$profile' is not readable before the release starts. No expensive build work has been performed. Read-only diagnostic: $diagnostic$([Environment]::NewLine)$lastDetails"
    }
    Write-Host "Apple notarization credentials are ready through keychain profile '$profile'." -ForegroundColor Green
}
elseif ($hasApiCredentials) {
    $keyPath = Get-EnvironmentText 'APPLE_NOTARY_KEY_PATH'
    if (-not (Test-Path -LiteralPath $keyPath -PathType Leaf)) { throw "APPLE_NOTARY_KEY_PATH does not exist: $keyPath" }
    Write-Host 'Apple notarization credentials are configured through an App Store Connect API key.' -ForegroundColor Green
}
elseif ($hasAppleIdCredentials) {
    Write-Host 'Apple notarization credentials are configured through Apple ID/team/app-specific-password environment variables.' -ForegroundColor Green
}
else {
    throw "No Apple notarization credentials are configured. Use Apple's Xcode tool through xcrun, for example: xcrun notarytool store-credentials future2-notary <authentication options>."
}

Write-Host "macOS public-release trust preflight passed for $ProductName." -ForegroundColor Green
Write-Host "Developer ID Application: $appIdentity" -ForegroundColor DarkCyan
Write-Host "Developer ID Installer: $installerIdentity" -ForegroundColor DarkCyan
if (@($SelectedRuntimes | Where-Object { $_.StartsWith('win-') }).Count -gt 0) {
    Write-Host 'Windows cross-published PE files are intentionally not signed with Apple Developer ID. Windows trust requires a separate Authenticode code-signing certificate; Apple Developer ID certificates are not a Windows trust identity.' -ForegroundColor DarkCyan
}
