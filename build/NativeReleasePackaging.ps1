param(
    [Parameter(Mandatory)][string]$ProductName,
    [Parameter(Mandatory)][string]$ExecutableName,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$Rid,
    [Parameter(Mandatory)][ValidateSet('Full')][string]$Mode,
    [Parameter(Mandatory)][string]$PayloadDirectory,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][string]$PackagingTool,
    [ValidateSet('LocalGPT','PublisherStudio')][string]$DependencyPolicy = 'LocalGPT',
    [switch]$UseContainerFallback,
    [switch]$ProvisionHomebrewTools,
    [switch]$RequireOptionalPackages,
    [switch]$ForceRebuildArtifacts,
    [switch]$ProbeExistingArtifactsOnly,
    [string]$MacIconSource = '',
    [string]$DmgBackgroundPath = ''
)
$ErrorActionPreference = 'Stop'
# Provider progress records from large Remove-Item/Copy-Item operations become corrupt when
# interleaved with external DocFX/package progress. Keep cleanup deterministic and line-oriented.
$ProgressPreference = 'SilentlyContinue'
Set-StrictMode -Version Latest

$isWindowsHost = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)
$isLinuxHost = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Linux)
$isMacHost = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::OSX)
$hostArchitecture = [Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()

function Get-RelativePathPortable([string]$BasePath,[string]$TargetPath) {
    $relativeMethod = [IO.Path].GetMethod('GetRelativePath', [Type[]]@([string], [string]))
    if ($relativeMethod) {
        return [string]$relativeMethod.Invoke($null, @($BasePath, $TargetPath))
    }

    $baseFull = [IO.Path]::GetFullPath($BasePath).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $targetFull = [IO.Path]::GetFullPath($TargetPath)
    $baseUri = New-Object Uri($baseFull)
    $targetUri = New-Object Uri($targetFull)
    return [Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', [IO.Path]::DirectorySeparatorChar)
}
function Test-TargetMatchesHostArchitecture([string]$RuntimeIdentifier) {
    if ($RuntimeIdentifier.EndsWith('arm64')) { return $hostArchitecture -eq 'arm64' }
    if ($RuntimeIdentifier.EndsWith('x64')) { return $hostArchitecture -eq 'x64' }
    return $false
}
function Write-Utf8NoBom([string]$Path, [string]$Text) {
    [IO.File]::WriteAllText($Path, $Text, [Text.UTF8Encoding]::new($false))
}
function Set-UnixExecutable([string]$Path) {
    if ($isWindowsHost) { return }
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Cannot mark missing file executable: $Path" }
    $chmod = Get-Command chmod -ErrorAction SilentlyContinue
    if (-not $chmod) { throw "chmod is required to prepare native Unix launchers: $Path" }
    & $chmod.Source '0755' $Path
    if ($LASTEXITCODE -ne 0) { throw "chmod failed while preparing executable file: $Path" }
}
function Get-ExternalCommandPath([string]$Name) {
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command -and -not [string]::IsNullOrWhiteSpace([string]$command.Source)) { return [string]$command.Source }
    return $null
}
function Resolve-HomebrewFormulaExecutable([string]$Name,[string]$Formula) {
    $direct = Get-ExternalCommandPath $Name
    if ($direct) { return $direct }
    if (-not $isMacHost) { return $null }
    $brew = Get-ExternalCommandPath 'brew'
    if (-not $brew) { return $null }
    $prefixOutput = @(& $brew --prefix $Formula 2>$null | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    if ($LASTEXITCODE -ne 0 -or $prefixOutput.Count -eq 0) { return $null }
    $candidate = Join-Path ([string]$prefixOutput[-1]) "bin/$Name"
    if (Test-Path -LiteralPath $candidate -PathType Leaf) { return [IO.Path]::GetFullPath($candidate) }
    return $null
}
function Resolve-RpmBuildPath {
    $rpmbuild = Resolve-HomebrewFormulaExecutable 'rpmbuild' 'rpm'
    if ($rpmbuild -or -not $isMacHost) { return $rpmbuild }
    $brew = Get-ExternalCommandPath 'brew'
    if (-not $brew) {
        Write-Warning "Homebrew is not installed, so macOS cannot provision rpmbuild automatically. Install Homebrew/rpm or use a Linux builder."
        return $null
    }
    Write-Host "rpmbuild is missing on macOS; provisioning the supported Homebrew rpm formula automatically..." -ForegroundColor Cyan
    & $brew install rpm | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Homebrew could not install rpm. Linux TAR.GZ and DEB outputs remain available."
        return $null
    }
    return Resolve-HomebrewFormulaExecutable 'rpmbuild' 'rpm'
}
function Complete-OptionalPackageFailure([string]$Kind,[string]$Message) {
    if ($RequireOptionalPackages) { throw "$Kind packaging failed for $Rid. $Message" }
    Write-Warning "Skipping $Kind for $Rid. $Message"
}

function Invoke-PackagingTool([string[]]$Arguments) {
    & $PackagingTool @Arguments | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { throw "LocalGPT.ReleasePackaging failed: $($Arguments -join ' ')" }
}
function Add-Artifact([Collections.Generic.List[string]]$List,[string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Expected native package was not created: $Path" }
    $List.Add([IO.Path]::GetFullPath($Path))
}
function Resolve-ExistingReleaseArtifactPath([string]$PrimaryPath) {
    if (Test-Path -LiteralPath $PrimaryPath -PathType Leaf) { return [IO.Path]::GetFullPath($PrimaryPath) }
    $versionDirectory = Join-Path $OutputDirectory $Version
    $bundled = Join-Path $versionDirectory ([IO.Path]::GetFileName($PrimaryPath))
    if (Test-Path -LiteralPath $bundled -PathType Leaf) { return [IO.Path]::GetFullPath($bundled) }
    return [IO.Path]::GetFullPath($PrimaryPath)
}
function Write-DependencyHelper([string]$Destination) {
    if ($DependencyPolicy -eq 'LocalGPT') {
@'
#!/bin/sh
set -eu
printf '%s\n' \
  'Optional LocalGPT AI runtime helper:' \
  '  1) Install Ollama (Homebrew when available)' \
  '  2) Pull an Ollama model' \
  '  3) Open the official LM Studio download page' \
  '  4) Show detected runtime status' \
  '  5) Skip'
printf 'Choice [5]: '
read choice || choice=5
case "${choice:-5}" in
  1)
    if command -v ollama >/dev/null 2>&1; then
      printf '%s\n' "Ollama is already installed: $(command -v ollama)"
    elif command -v brew >/dev/null 2>&1; then
      printf '%s\n' 'Installing Ollama with Homebrew...'
      brew install ollama
    else
      printf '%s\n' 'Homebrew is not installed. Opening the official Ollama download page.'
      if command -v open >/dev/null 2>&1; then open 'https://ollama.com/download' >/dev/null 2>&1 || true; fi
    fi
    ;;
  2)
    if ! command -v ollama >/dev/null 2>&1; then
      printf '%s\n' 'Ollama is not installed yet. Run this helper again and choose option 1 first.'
      exit 1
    fi
    printf 'Model to pull [llama3.2:3b]: '
    read model || model=''
    model=${model:-llama3.2:3b}
    printf '%s\n' "Pulling $model..."
    ollama pull "$model"
    ;;
  3)
    printf '%s\n' 'Opening the official LM Studio download page. LocalGPT does not redistribute LM Studio.'
    if command -v open >/dev/null 2>&1; then open 'https://lmstudio.ai/download' >/dev/null 2>&1 || true; fi
    ;;
  4)
    if command -v ollama >/dev/null 2>&1; then
      printf '%s\n' "Ollama: $(command -v ollama)"
      ollama list 2>/dev/null || true
    else
      printf '%s\n' 'Ollama: not detected'
    fi
    if [ -d '/Applications/LM Studio.app' ]; then printf '%s\n' 'LM Studio: /Applications/LM Studio.app'; else printf '%s\n' 'LM Studio: not detected in /Applications'; fi
    ;;
  *) printf '%s\n' 'Skipped. LocalGPT does not redistribute third-party AI runtimes or models.' ;;
esac
'@ | Set-Content -LiteralPath $Destination -Encoding UTF8
    $raw = Get-Content -LiteralPath $Destination -Raw; Write-Utf8NoBom $Destination $raw
    } else {
@'
#!/bin/sh
set -eu
if command -v ffmpeg >/dev/null 2>&1; then
  ffmpeg -version | head -n 1
  exit 0
fi
printf '%s\n' 'FFmpeg is not bundled by PublisherStudio. Install it with your platform package manager or official distribution.'
'@ | Set-Content -LiteralPath $Destination -Encoding UTF8
    $raw = Get-Content -LiteralPath $Destination -Raw; Write-Utf8NoBom $Destination $raw
    }
    Set-UnixExecutable $Destination
}
function New-MacBundleIcon([string]$AppPath) {
    if ([string]::IsNullOrWhiteSpace($MacIconSource)) { return $null }
    if (-not $isMacHost) { return $null }
    if (-not (Test-Path -LiteralPath $MacIconSource -PathType Leaf)) {
        throw "macOS icon source was not found: $MacIconSource"
    }
    $sips = Get-ExternalCommandPath 'sips'
    $iconutil = Get-ExternalCommandPath 'iconutil'
    if (-not $sips -or -not $iconutil) {
        throw 'macOS application icon generation requires the built-in sips and iconutil tools.'
    }
    $iconSet = Join-Path ([IO.Path]::GetTempPath()) ("$ProductName-" + [Guid]::NewGuid().ToString('N') + '.iconset')
    $destination = Join-Path $AppPath 'Contents/Resources/AppIcon.icns'
    New-Item -ItemType Directory -Path $iconSet -Force | Out-Null
    try {
        $sizes = @(
            @{ Name='icon_16x16.png'; Size=16 }, @{ Name='icon_16x16@2x.png'; Size=32 },
            @{ Name='icon_32x32.png'; Size=32 }, @{ Name='icon_32x32@2x.png'; Size=64 },
            @{ Name='icon_128x128.png'; Size=128 }, @{ Name='icon_128x128@2x.png'; Size=256 },
            @{ Name='icon_256x256.png'; Size=256 }, @{ Name='icon_256x256@2x.png'; Size=512 },
            @{ Name='icon_512x512.png'; Size=512 }, @{ Name='icon_512x512@2x.png'; Size=1024 }
        )
        foreach ($item in $sizes) {
            & $sips -z $item.Size $item.Size $MacIconSource --out (Join-Path $iconSet $item.Name) | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "sips failed while generating macOS icon size $($item.Size)." }
        }
        & $iconutil -c icns $iconSet -o $destination | Out-Null
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $destination -PathType Leaf)) {
            throw "iconutil failed while creating $destination"
        }
        return 'AppIcon'
    }
    finally { Remove-Item -LiteralPath $iconSet -Recurse -Force -ErrorAction SilentlyContinue }
}
$script:MacSigningResolved = $false
$script:MacApplicationSigningIdentity = $null
$script:MacInstallerSigningIdentity = $null
$script:MacNotaryWarningIssued = $false

function Get-EnvironmentText([string]$Name) {
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace([string]$value)) { return $null }
    return ([string]$value).Trim()
}
function Resolve-MacDistributionSigning {
    if ($script:MacSigningResolved -or -not $isMacHost) { return }
    $script:MacSigningResolved = $true

    $script:MacApplicationSigningIdentity = Get-EnvironmentText 'MACOS_DEVELOPER_ID_APPLICATION'
    $script:MacInstallerSigningIdentity = Get-EnvironmentText 'MACOS_DEVELOPER_ID_INSTALLER'

    $security = Get-ExternalCommandPath 'security'
    if ($security -and ([string]::IsNullOrWhiteSpace([string]$script:MacApplicationSigningIdentity) -or [string]::IsNullOrWhiteSpace([string]$script:MacInstallerSigningIdentity))) {
        $identityLines = @(& $security find-identity -v 2>$null | ForEach-Object { [string]$_ })
        if ($LASTEXITCODE -eq 0) {
            foreach ($line in $identityLines) {
                if ([string]::IsNullOrWhiteSpace([string]$script:MacApplicationSigningIdentity) -and $line -match '"(Developer ID Application:[^"]+)"') {
                    $script:MacApplicationSigningIdentity = $Matches[1]
                }
                if ([string]::IsNullOrWhiteSpace([string]$script:MacInstallerSigningIdentity) -and $line -match '"(Developer ID Installer:[^"]+)"') {
                    $script:MacInstallerSigningIdentity = $Matches[1]
                }
            }
        }
    }

    if ($script:MacApplicationSigningIdentity) {
        Write-Host "macOS distribution signing: Developer ID Application identity '$($script:MacApplicationSigningIdentity)'." -ForegroundColor Cyan
    } else {
        Write-Warning "No Developer ID Application identity was found. $ProductName.app will receive only an ad-hoc signature; Gatekeeper can reject copies downloaded on another Mac. Set MACOS_DEVELOPER_ID_APPLICATION or install the certificate in the login keychain."
    }
    if ($script:MacInstallerSigningIdentity) {
        Write-Host "macOS installer signing: Developer ID Installer identity '$($script:MacInstallerSigningIdentity)'." -ForegroundColor Cyan
    } else {
        Write-Warning "No Developer ID Installer identity was found. PKG output cannot be trusted by Gatekeeper after Internet download. Set MACOS_DEVELOPER_ID_INSTALLER or install the certificate in the login keychain."
    }
}
function Get-MacNotaryKeychainProfile {
    return (Get-EnvironmentText 'MACOS_NOTARY_KEYCHAIN_PROFILE')
}
function Get-MacNotaryKeychainPath {
    return (Get-EnvironmentText 'MACOS_NOTARY_KEYCHAIN_PATH')
}
function Test-MacNotaryCredentialsAvailable {
    if (Get-MacNotaryKeychainProfile) { return $true }
    $keyId = Get-EnvironmentText 'APPLE_NOTARY_KEY_ID'
    $issuer = Get-EnvironmentText 'APPLE_NOTARY_ISSUER'
    $keyPath = Get-EnvironmentText 'APPLE_NOTARY_KEY_PATH'
    if ($keyId -and $issuer -and $keyPath) { return $true }
    $appleId = Get-EnvironmentText 'APPLE_NOTARY_APPLE_ID'
    $teamId = Get-EnvironmentText 'APPLE_NOTARY_TEAM_ID'
    $password = Get-EnvironmentText 'APPLE_NOTARY_PASSWORD'
    return [bool]($appleId -and $teamId -and $password)
}
function Get-MacNotaryArguments {
    $profile = Get-MacNotaryKeychainProfile
    if ($profile) {
        $arguments = @('--keychain-profile', $profile)
        $keychainPath = Get-MacNotaryKeychainPath
        if ($keychainPath) { $arguments += @('--keychain', $keychainPath) }
        return $arguments
    }

    $keyId = Get-EnvironmentText 'APPLE_NOTARY_KEY_ID'
    $issuer = Get-EnvironmentText 'APPLE_NOTARY_ISSUER'
    $keyPath = Get-EnvironmentText 'APPLE_NOTARY_KEY_PATH'
    if ($keyId -and $issuer -and $keyPath) {
        if (-not (Test-Path -LiteralPath $keyPath -PathType Leaf)) { throw "APPLE_NOTARY_KEY_PATH does not exist: $keyPath" }
        return @('--key', $keyPath, '--key-id', $keyId, '--issuer', $issuer)
    }

    $appleId = Get-EnvironmentText 'APPLE_NOTARY_APPLE_ID'
    $teamId = Get-EnvironmentText 'APPLE_NOTARY_TEAM_ID'
    $password = Get-EnvironmentText 'APPLE_NOTARY_PASSWORD'
    if ($appleId -and $teamId -and $password) {
        return @('--apple-id', $appleId, '--team-id', $teamId, '--password', $password)
    }
    return @()
}
function Test-MacNotaryCredentialRecoveryRequired([string]$Details) {
    if ([string]::IsNullOrWhiteSpace($Details)) { return $false }
    return $Details -match '(?i)(No Keychain password item found|User interaction is not allowed|errSecInteractionNotAllowed|specified item could not be found in the keychain|keychain[^\r\n]*(?:locked|unavailable|interaction)|credential profile[^\r\n]*(?:missing|unavailable))'
}
function Test-MacNotaryTransientServiceRecoveryRequired([string]$Details) {
    if ([string]::IsNullOrWhiteSpace($Details)) { return $false }
    return $Details -match '(?i)(NSURLError|internet connection[^\r\n]*(?:offline|lost)|network[^\r\n]*(?:offline|unavailable|failed|error)|could not connect to (?:the )?server|connection[^\r\n]*(?:reset|refused|lost)|request[^\r\n]*timed out|operation[^\r\n]*timed out|HTTP[^\r\n]*(?:408|425|429|500|502|503|504)|service[^\r\n]*(?:temporarily unavailable|unavailable|busy)|server[^\r\n]*(?:temporarily unavailable|unavailable|busy))'
}
function Get-MacNotaryCredentialRetrySeconds {
    $configured = 15
    $value = Get-EnvironmentText 'MACOS_NOTARY_CREDENTIAL_RETRY_SECONDS'
    if ($value) {
        $parsed = 0
        if ([int]::TryParse($value, [ref]$parsed) -and $parsed -ge 1 -and $parsed -le 3600) { return $parsed }
    }
    return $configured
}
function Get-MacNotaryCredentialDiagnosticCommand {
    $profile = Get-MacNotaryKeychainProfile
    if (-not $profile) { return 'xcrun notarytool history' }
    $command = "xcrun notarytool history --keychain-profile '$profile'"
    $keychainPath = Get-MacNotaryKeychainPath
    if ($keychainPath) { $command += " --keychain '$keychainPath'" }
    return $command
}
function Invoke-MacNotaryToolOnce([string]$Operation,[string[]]$Arguments) {
    $xcrun = Get-ExternalCommandPath 'xcrun'
    if (-not $xcrun) { throw 'xcrun is required for Apple notarization.' }
    $nativePreferenceVariable = Get-Variable -Name PSNativeCommandUseErrorActionPreference -ErrorAction SilentlyContinue
    $previousNativePreference = $null
    if ($null -ne $nativePreferenceVariable) {
        $previousNativePreference = [bool]$PSNativeCommandUseErrorActionPreference
        $PSNativeCommandUseErrorActionPreference = $false
    }
    try {
        $output = @(& $xcrun notarytool @Arguments 2>&1)
        $exitCode = [int]$LASTEXITCODE
    }
    finally {
        if ($null -ne $nativePreferenceVariable) { $PSNativeCommandUseErrorActionPreference = $previousNativePreference }
    }
    $details = if ($exitCode -eq 0) { '' } else { @($output | Select-Object -Last 12) -join ' | ' }
    return [pscustomobject]@{ ExitCode = $exitCode; Output = @($output); Details = $details; Operation = $Operation }
}
function Invoke-MacNotaryToolWithCredentialRecovery([string]$Operation,[string[]]$Arguments) {
    # This retry wrapper is intentionally reserved for idempotent notarytool operations such as
    # history, info, wait-state queries, and log retrieval. Never call it for `notarytool submit`:
    # a submit can reach Apple even when the local process reports an ambiguous failure.
    $credentialFailureCount = 0
    while ($true) {
        $result = Invoke-MacNotaryToolOnce -Operation $Operation -Arguments $Arguments
        if ($result.ExitCode -eq 0) { return $result }
        $details = [string]$result.Details
        $profile = Get-MacNotaryKeychainProfile
        if ($profile -and (Test-MacNotaryCredentialRecoveryRequired -Details $details)) {
            $credentialFailureCount++
            $retrySeconds = Get-MacNotaryCredentialRetrySeconds
            Write-Warning "Apple notarytool temporarily cannot read keychain profile '$profile' during $Operation (read-only attempt $credentialFailureCount). Retrying this idempotent query in $retrySeconds seconds; no artifact is uploaded by this retry. Ctrl+C remains the explicit operator stop."
            if (-not [string]::IsNullOrWhiteSpace($details)) { Write-Warning $details }
            if (($credentialFailureCount % 5) -eq 0) {
                Write-Warning "Manual read-only diagnostic, if desired in another terminal: $(Get-MacNotaryCredentialDiagnosticCommand)"
                Write-Warning "Do not recreate certificates. Apple documents that the default profile uses the data-protection keychain unless an explicit --keychain is supplied."
            }
            Start-Sleep -Seconds $retrySeconds
            continue
        }
        if (Test-MacNotaryTransientServiceRecoveryRequired -Details $details) {
            Write-Warning "Apple notarization is temporarily unavailable during $Operation. Retrying this idempotent operation in 60 seconds; no submit is repeated. Ctrl+C remains the explicit operator stop."
            if (-not [string]::IsNullOrWhiteSpace($details)) { Write-Warning $details }
            Start-Sleep -Seconds 60
            continue
        }
        return $result
    }
}

function Test-MacNotarizationRequired {
    $value = Get-EnvironmentText 'MACOS_REQUIRE_NOTARIZATION'
    if (-not $value) { return $false }
    return @('1','true','yes','on') -contains $value.ToLowerInvariant()
}
function Sign-MacBundle([string]$AppPath) {
    if (-not $isMacHost) { return }
    Resolve-MacDistributionSigning
    $codesign = Get-ExternalCommandPath 'codesign'
    if (-not $codesign) { throw 'codesign is required to prepare macOS application bundles.' }

    if ($script:MacApplicationSigningIdentity) {
        $fileCommand = Get-ExternalCommandPath 'file'
        if (-not $fileCommand) { throw "The macOS 'file' utility is required for Developer ID signing." }
        $entitlementsPath = Join-Path ([IO.Path]::GetTempPath()) ("$ProductName-notarization-" + [Guid]::NewGuid().ToString('N') + '.entitlements.plist')
        $entitlements = @'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>com.apple.security.cs.allow-jit</key>
  <true/>
</dict>
</plist>
'@
        Write-Utf8NoBom $entitlementsPath $entitlements
        try {
            $appHost = [IO.Path]::GetFullPath((Join-Path $AppPath "Contents/Resources/app/$ExecutableName"))
            $machOFiles = [Collections.Generic.List[IO.FileInfo]]::new()
            foreach ($item in Get-ChildItem -LiteralPath $AppPath -File -Recurse -ErrorAction Stop) {
                $description = [string](& $fileCommand $item.FullName 2>$null)
                if ($LASTEXITCODE -eq 0 -and $description -match 'Mach-O') { $machOFiles.Add($item) }
            }
            foreach ($item in @($machOFiles | Sort-Object { $_.FullName.Length } -Descending)) {
                $signArguments = @('--force','--options','runtime','--timestamp','--sign',$script:MacApplicationSigningIdentity)
                if ([IO.Path]::GetFullPath($item.FullName) -eq $appHost) { $signArguments += @('--entitlements',$entitlementsPath) }
                $signArguments += $item.FullName
                & $codesign @signArguments 2>&1 | ForEach-Object { Write-Host $_ }
                if ($LASTEXITCODE -ne 0) { throw "Developer ID codesign failed for nested Mach-O component $($item.FullName)" }
            }

            # Frameworks, plug-ins and other nested code containers own signatures in addition to
            # the individual Mach-O files they contain. Sign these deepest-first before the app.
            $nestedCodeBundles = @(
                Get-ChildItem -LiteralPath $AppPath -Directory -Recurse -ErrorAction Stop |
                    Where-Object { $_.Extension -in @('.framework','.bundle','.plugin','.xpc','.app') } |
                    Sort-Object { $_.FullName.Length } -Descending
            )
            foreach ($bundle in $nestedCodeBundles) {
                & $codesign --force --options runtime --timestamp --sign $script:MacApplicationSigningIdentity $bundle.FullName 2>&1 | ForEach-Object { Write-Host $_ }
                if ($LASTEXITCODE -ne 0) { throw "Developer ID codesign failed for nested code bundle $($bundle.FullName)" }
            }

            # Sign the enclosing bundle last. The .NET apphost above owns the JIT entitlement;
            # the small Contents/MacOS shell launcher does not need it.
            & $codesign --force --options runtime --timestamp --sign $script:MacApplicationSigningIdentity $AppPath 2>&1 | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0) { throw "Developer ID codesign failed for $AppPath" }
            & $codesign --verify --deep --strict --verbose=2 $AppPath 2>&1 | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0) { throw "Developer ID signature verification failed for $AppPath" }
            foreach ($item in $machOFiles) {
                & $codesign --verify --strict $item.FullName 2>&1 | ForEach-Object { Write-Host $_ }
                if ($LASTEXITCODE -ne 0) { throw "Developer ID signature verification failed for nested Mach-O component $($item.FullName)" }
            }
            Write-Host "Developer ID signed and verified $($machOFiles.Count) Mach-O component(s), $($nestedCodeBundles.Count) nested code bundle(s), and the $ProductName app bundle." -ForegroundColor Green
        }
        finally { Remove-Item -LiteralPath $entitlementsPath -Force -ErrorAction SilentlyContinue }
        return
    }

    & $codesign --force --deep --sign - --timestamp=none $AppPath 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { throw "Ad-hoc codesign failed for $AppPath" }
}
function Sign-MacDiskImage([string]$DiskImagePath) {
    if (-not $isMacHost -or -not (Test-Path -LiteralPath $DiskImagePath -PathType Leaf)) { return }
    Resolve-MacDistributionSigning
    if (-not $script:MacApplicationSigningIdentity) {
        if (Test-MacNotarizationRequired) { throw "Developer ID Application identity is required to sign disk image $DiskImagePath" }
        return
    }
    $codesign = Get-ExternalCommandPath 'codesign'
    if (-not $codesign) { throw 'codesign is required to sign macOS disk images.' }
    & $codesign --force --timestamp --sign $script:MacApplicationSigningIdentity $DiskImagePath 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { throw "Developer ID disk-image signing failed for $DiskImagePath" }
    & $codesign --verify --strict --verbose=2 $DiskImagePath 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { throw "Developer ID disk-image signature verification failed for $DiskImagePath" }
    Write-Host "Developer ID signed disk image: $DiskImagePath" -ForegroundColor Green
}
function Get-MacNotaryObjectPropertyValue([object]$InputObject,[string]$PropertyName) {
    if ($null -eq $InputObject -or [string]::IsNullOrWhiteSpace($PropertyName)) { return $null }
    if ($InputObject -is [Collections.IDictionary]) {
        if ($InputObject.Contains($PropertyName)) { return $InputObject[$PropertyName] }
        return $null
    }
    $property = $InputObject.PSObject.Properties[$PropertyName]
    if ($null -eq $property) { return $null }
    return $property.Value
}
function Get-MacNotaryObjectPropertyText([object]$InputObject,[string]$PropertyName) {
    $value = Get-MacNotaryObjectPropertyValue -InputObject $InputObject -PropertyName $PropertyName
    if ($null -eq $value) { return '' }
    return [string]$value
}
function Set-MacNotaryObjectPropertyValue([object]$InputObject,[string]$PropertyName,[object]$Value) {
    if ($null -eq $InputObject -or [string]::IsNullOrWhiteSpace($PropertyName)) { return }
    if ($InputObject -is [Collections.IDictionary]) {
        $InputObject[$PropertyName] = $Value
        return
    }
    $property = $InputObject.PSObject.Properties[$PropertyName]
    if ($null -eq $property) {
        $InputObject | Add-Member -NotePropertyName $PropertyName -NotePropertyValue $Value -Force
        return
    }
    $property.Value = $Value
}
function Get-MacNotaryStatePath([string]$ArtifactPath) {
    return "$ArtifactPath.notary-state.json"
}
function Remove-MacNotaryState([string]$ArtifactPath) {
    Remove-Item -LiteralPath (Get-MacNotaryStatePath $ArtifactPath) -Force -ErrorAction SilentlyContinue
}
function Get-MacNotaryWaitTimeout {
    $configured = Get-EnvironmentText 'MACOS_NOTARY_WAIT_TIMEOUT'
    if (-not $configured) { return '2h' }
    if ($configured -notmatch '^\d+(?:[smh])?$') { throw "MACOS_NOTARY_WAIT_TIMEOUT must be an integer optionally followed by s, m, or h; received '$configured'." }
    return $configured
}
function ConvertTo-MacNotaryTimeoutSeconds([string]$Timeout) {
    if ([string]::IsNullOrWhiteSpace($Timeout) -or $Timeout -notmatch '^(\d+)([smh]?)$') {
        throw "Invalid macOS notarization wait timeout '$Timeout'."
    }
    $value = [int64]$Matches[1]
    $multiplier = switch ($Matches[2]) {
        'h' { 3600L }
        'm' { 60L }
        default { 1L }
    }
    $seconds = $value * $multiplier
    if ($seconds -lt 1 -or $seconds -gt [int]::MaxValue) { throw "macOS notarization wait timeout '$Timeout' is outside the supported range." }
    return [int]$seconds
}
function Get-MacNotaryPollIntervalSeconds {
    $configured = Get-EnvironmentText 'MACOS_NOTARY_POLL_INTERVAL_SECONDS'
    if (-not $configured) { return 20 }
    $seconds = 0
    if (-not [int]::TryParse($configured, [ref]$seconds) -or $seconds -lt 5 -or $seconds -gt 300) {
        throw "MACOS_NOTARY_POLL_INTERVAL_SECONDS must be an integer from 5 through 300; received '$configured'."
    }
    return $seconds
}
function ConvertFrom-MacNotaryJsonOutput([object[]]$Output,[string]$Operation) {
    $text = @($Output | ForEach-Object { [string]$_ }) -join "`n"
    $first = $text.IndexOf('{')
    $last = $text.LastIndexOf('}')
    if ($first -lt 0 -or $last -lt $first) { throw "Apple notarytool $Operation did not return JSON: $text" }
    try { return ($text.Substring($first, $last - $first + 1) | ConvertFrom-Json) }
    catch { throw "Apple notarytool $Operation returned invalid JSON: $($_.Exception.Message)" }
}
function Save-MacNotaryStateObject([string]$ArtifactPath,[object]$State) {
    $statePath = Get-MacNotaryStatePath $ArtifactPath
    $State | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $statePath -Encoding utf8
    return $statePath
}
function New-MacNotaryPendingState([string]$ArtifactPath,[string[]]$BaselineSubmissionIds,[DateTime]$SubmitStartedAtUtc) {
    $state = [ordered]@{
        schema = 2
        artifactName = [IO.Path]::GetFileName($ArtifactPath)
        artifactSha256 = (Get-FileHash -LiteralPath $ArtifactPath -Algorithm SHA256).Hash.ToLowerInvariant()
        phase = 'submit-pending'
        submissionId = ''
        submitStartedAtUtc = $SubmitStartedAtUtc.ToUniversalTime().ToString('O')
        submittedAtUtc = ''
        acceptedAtUtc = ''
        completedAtUtc = ''
        baselineSubmissionIds = @($BaselineSubmissionIds)
    }
    Save-MacNotaryStateObject -ArtifactPath $ArtifactPath -State $state | Out-Null
    return $state
}
function Save-MacNotarySubmittedState([string]$ArtifactPath,[object]$ExistingState,[string]$SubmissionId,[string]$SubmittedAtUtc) {
    $state = if ($null -eq $ExistingState) {
        [ordered]@{
            schema = 2
            artifactName = [IO.Path]::GetFileName($ArtifactPath)
            artifactSha256 = (Get-FileHash -LiteralPath $ArtifactPath -Algorithm SHA256).Hash.ToLowerInvariant()
            phase = 'submitted'
            submissionId = $SubmissionId
            submitStartedAtUtc = [DateTime]::UtcNow.ToString('O')
            submittedAtUtc = $SubmittedAtUtc
            acceptedAtUtc = ''
            completedAtUtc = ''
            baselineSubmissionIds = @()
        }
    } else {
        [ordered]@{
            schema = 2
            artifactName = (Get-MacNotaryObjectPropertyText -InputObject $ExistingState -PropertyName 'artifactName')
            artifactSha256 = (Get-MacNotaryObjectPropertyText -InputObject $ExistingState -PropertyName 'artifactSha256')
            phase = 'submitted'
            submissionId = $SubmissionId
            submitStartedAtUtc = (Get-MacNotaryObjectPropertyText -InputObject $ExistingState -PropertyName 'submitStartedAtUtc')
            submittedAtUtc = $SubmittedAtUtc
            acceptedAtUtc = (Get-MacNotaryObjectPropertyText -InputObject $ExistingState -PropertyName 'acceptedAtUtc')
            completedAtUtc = (Get-MacNotaryObjectPropertyText -InputObject $ExistingState -PropertyName 'completedAtUtc')
            baselineSubmissionIds = @(Get-MacNotaryObjectPropertyValue -InputObject $ExistingState -PropertyName 'baselineSubmissionIds')
        }
    }
    if ([string]::IsNullOrWhiteSpace([string]$state['artifactName'])) { $state['artifactName'] = [IO.Path]::GetFileName($ArtifactPath) }
    if ([string]::IsNullOrWhiteSpace([string]$state['artifactSha256'])) { $state['artifactSha256'] = (Get-FileHash -LiteralPath $ArtifactPath -Algorithm SHA256).Hash.ToLowerInvariant() }
    if ([string]::IsNullOrWhiteSpace([string]$state['submitStartedAtUtc'])) { $state['submitStartedAtUtc'] = [DateTime]::UtcNow.ToString('O') }
    Save-MacNotaryStateObject -ArtifactPath $ArtifactPath -State $state | Out-Null
    return $state
}
function Set-MacNotaryStatePhase([string]$ArtifactPath,[object]$State,[ValidateSet('submitted','accepted','complete')][string]$Phase) {
    if ($null -eq $State) { return $null }
    Set-MacNotaryObjectPropertyValue -InputObject $State -PropertyName 'phase' -Value $Phase
    if ($Phase -eq 'accepted') {
        Set-MacNotaryObjectPropertyValue -InputObject $State -PropertyName 'acceptedAtUtc' -Value ([DateTime]::UtcNow.ToString('O'))
    }
    elseif ($Phase -eq 'complete') {
        Set-MacNotaryObjectPropertyValue -InputObject $State -PropertyName 'completedAtUtc' -Value ([DateTime]::UtcNow.ToString('O'))
    }
    Save-MacNotaryStateObject -ArtifactPath $ArtifactPath -State $State | Out-Null
    return $State
}
function Save-MacNotaryState([string]$ArtifactPath,[string]$SubmissionId) {
    # Compatibility entry point for state written by older release code.
    $state = Save-MacNotarySubmittedState -ArtifactPath $ArtifactPath -ExistingState $null -SubmissionId $SubmissionId -SubmittedAtUtc ([DateTime]::UtcNow.ToString('O'))
    return (Get-MacNotaryStatePath $ArtifactPath)
}

function Get-MacNotaryState([string]$ArtifactPath) {
    $statePath = Get-MacNotaryStatePath $ArtifactPath
    if (-not (Test-Path -LiteralPath $statePath -PathType Leaf) -or -not (Test-Path -LiteralPath $ArtifactPath -PathType Leaf)) { return $null }
    try { $state = Get-Content -LiteralPath $statePath -Raw -Encoding UTF8 | ConvertFrom-Json }
    catch {
        Write-Warning "Ignoring unreadable notarization state '$statePath': $($_.Exception.Message)"
        Remove-Item -LiteralPath $statePath -Force -ErrorAction SilentlyContinue
        return $null
    }
    $stateArtifactSha256 = Get-MacNotaryObjectPropertyText -InputObject $state -PropertyName 'artifactSha256'
    if ([string]::IsNullOrWhiteSpace($stateArtifactSha256)) {
        Remove-Item -LiteralPath $statePath -Force -ErrorAction SilentlyContinue
        return $null
    }
    $currentHash = (Get-FileHash -LiteralPath $ArtifactPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if (-not [string]::Equals($currentHash, $stateArtifactSha256, [StringComparison]::OrdinalIgnoreCase)) {
        Write-Host "Discarding stale notarization state for $([IO.Path]::GetFileName($ArtifactPath)) because the artifact bytes changed." -ForegroundColor DarkCyan
        Remove-Item -LiteralPath $statePath -Force -ErrorAction SilentlyContinue
        return $null
    }
    $phase = Get-MacNotaryObjectPropertyText -InputObject $state -PropertyName 'phase'
    $submissionId = Get-MacNotaryObjectPropertyText -InputObject $state -PropertyName 'submissionId'
    if ([string]::IsNullOrWhiteSpace($phase)) {
        # Schema 1 compatibility: an ID plus matching hash means the artifact was submitted.
        if ([string]::IsNullOrWhiteSpace($submissionId)) {
            Remove-Item -LiteralPath $statePath -Force -ErrorAction SilentlyContinue
            return $null
        }
        $state = Save-MacNotarySubmittedState -ArtifactPath $ArtifactPath -ExistingState $state -SubmissionId $submissionId -SubmittedAtUtc ([DateTime]::UtcNow.ToString('O'))
    }
    elseif ($phase -ne 'submit-pending' -and [string]::IsNullOrWhiteSpace($submissionId)) {
        Write-Warning "Ignoring notarization state '$statePath' because phase '$phase' requires a submission ID."
        return $null
    }
    return $state
}

function Get-MacNotaryHistoryEntries([string[]]$NotaryArguments) {
    $arguments = @('history') + @($NotaryArguments) + @('--output-format','json','--no-progress')
    $result = Invoke-MacNotaryToolWithCredentialRecovery -Operation 'submission-history query' -Arguments $arguments
    if ($result.ExitCode -ne 0) { throw "Apple notarytool could not query submission history: $($result.Details)" }
    $historyObject = ConvertFrom-MacNotaryJsonOutput -Output $result.Output -Operation 'history'
    $history = Get-MacNotaryObjectPropertyValue -InputObject $historyObject -PropertyName 'history'
    if ($null -eq $history) { return @() }
    return @($history)
}
function Get-MacNotaryHistoryIds([object[]]$Entries) {
    $ids = [Collections.Generic.List[string]]::new()
    foreach ($entry in @($Entries)) {
        $id = Get-MacNotaryObjectPropertyText -InputObject $entry -PropertyName 'id'
        if (-not [string]::IsNullOrWhiteSpace($id) -and -not $ids.Contains($id)) { $ids.Add($id) }
    }
    return @($ids)
}
function ConvertFrom-MacNotaryCreatedDate([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return [DateTime]::MinValue }
    $parsed = [DateTime]::MinValue
    if ([DateTime]::TryParse($Value, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind, [ref]$parsed)) {
        return $parsed.ToUniversalTime()
    }
    return [DateTime]::MinValue
}
function Find-MacNotarySubmissionForPendingState([string]$ArtifactPath,[object]$State,[string[]]$NotaryArguments) {
    $entries = @(Get-MacNotaryHistoryEntries -NotaryArguments $NotaryArguments)
    $artifactName = [IO.Path]::GetFileName($ArtifactPath)
    $startedText = Get-MacNotaryObjectPropertyText -InputObject $State -PropertyName 'submitStartedAtUtc'
    $startedAt = ConvertFrom-MacNotaryCreatedDate $startedText
    if ($startedAt -eq [DateTime]::MinValue) { $startedAt = [DateTime]::UtcNow.AddMinutes(-30) }
    $windowStart = $startedAt.AddMinutes(-2)
    $baseline = @()
    $baselineValue = Get-MacNotaryObjectPropertyValue -InputObject $State -PropertyName 'baselineSubmissionIds'
    if ($null -ne $baselineValue) { $baseline = @($baselineValue | ForEach-Object { [string]$_ }) }
    $candidates = [Collections.Generic.List[object]]::new()
    foreach ($entry in $entries) {
        $name = Get-MacNotaryObjectPropertyText -InputObject $entry -PropertyName 'name'
        $id = Get-MacNotaryObjectPropertyText -InputObject $entry -PropertyName 'id'
        if ([string]::IsNullOrWhiteSpace($id) -or -not [string]::Equals($name, $artifactName, [StringComparison]::Ordinal)) { continue }
        if ($baseline -contains $id) { continue }
        $created = ConvertFrom-MacNotaryCreatedDate (Get-MacNotaryObjectPropertyText -InputObject $entry -PropertyName 'createdDate')
        if ($created -lt $windowStart) { continue }
        $candidates.Add([pscustomobject]@{ Id = $id; Created = $created; Status = (Get-MacNotaryObjectPropertyText -InputObject $entry -PropertyName 'status') })
    }
    if ($candidates.Count -eq 0) { return $null }
    $ordered = @($candidates | Sort-Object -Property Created -Descending)
    if ($ordered.Count -gt 1) {
        Write-Warning "Apple history contains $($ordered.Count) new submissions named '$artifactName' after this artifact-local submit started. Adopting the newest ID $($ordered[0].Id) and never issuing another upload for this pending transaction."
    }
    return $ordered[0]
}
function Get-MacNotaryReconcileSeconds {
    $value = Get-EnvironmentText 'MACOS_NOTARY_SUBMIT_RECONCILE_SECONDS'
    if (-not $value) { return 600 }
    $seconds = 0
    if (-not [int]::TryParse($value, [ref]$seconds) -or $seconds -lt 30 -or $seconds -gt 3600) {
        throw "MACOS_NOTARY_SUBMIT_RECONCILE_SECONDS must be an integer from 30 through 3600; received '$value'."
    }
    return $seconds
}
function Resolve-MacNotaryPendingSubmission([string]$ArtifactPath,[object]$State,[string[]]$NotaryArguments,[switch]$WaitForVisibility) {
    $deadline = if ($WaitForVisibility) { [DateTime]::UtcNow.AddSeconds((Get-MacNotaryReconcileSeconds)) } else { [DateTime]::UtcNow }
    do {
        $candidate = Find-MacNotarySubmissionForPendingState -ArtifactPath $ArtifactPath -State $State -NotaryArguments $NotaryArguments
        if ($null -ne $candidate) {
            $submittedAt = if ($candidate.Created -eq [DateTime]::MinValue) { [DateTime]::UtcNow.ToString('O') } else { $candidate.Created.ToString('O') }
            $resolved = Save-MacNotarySubmittedState -ArtifactPath $ArtifactPath -ExistingState $State -SubmissionId $candidate.Id -SubmittedAtUtc $submittedAt
            Write-Host "Recovered Apple submission $($candidate.Id) for $([IO.Path]::GetFileName($ArtifactPath)) from history; no duplicate upload was issued." -ForegroundColor Green
            return $resolved
        }
        if ([DateTime]::UtcNow -ge $deadline) { break }
        Write-Warning "The submit result for $([IO.Path]::GetFileName($ArtifactPath)) is ambiguous and no new matching history entry is visible yet. Waiting 15 seconds and querying history again; the artifact will NOT be resubmitted while this transaction is unresolved."
        Start-Sleep -Seconds 15
    } while ($true)
    return $null
}
function Get-MacNotarySubmissionInfo([string]$SubmissionId,[string[]]$NotaryArguments) {
    $arguments = @('info',$SubmissionId) + @($NotaryArguments) + @('--output-format','json','--no-progress')
    $result = Invoke-MacNotaryToolWithCredentialRecovery -Operation "status query for submission $SubmissionId" -Arguments $arguments
    if ($result.ExitCode -ne 0) { throw "Apple notarytool could not query submission ${SubmissionId}: $($result.Details)" }
    return ConvertFrom-MacNotaryJsonOutput -Output $result.Output -Operation "info $SubmissionId"
}

function Save-MacNotaryFailureLog([string]$ArtifactPath,[string]$SubmissionId,[string[]]$NotaryArguments) {
    $logPath = "$ArtifactPath.notary-log.json"
    $arguments = @('log',$SubmissionId) + @($NotaryArguments) + @($logPath)
    $result = Invoke-MacNotaryToolWithCredentialRecovery -Operation "failure-log download for submission $SubmissionId" -Arguments $arguments
    if ($result.ExitCode -eq 0 -and (Test-Path -LiteralPath $logPath -PathType Leaf)) {
        Write-Warning "Apple notarization diagnostics were saved to $logPath"
        return $logPath
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$result.Details)) { Write-Warning $result.Details }
    return $null
}
function Wait-MacNotarySubmission([string]$ArtifactPath,[string]$SubmissionId,[string[]]$NotaryArguments) {
    $checkpoint = Get-MacNotaryWaitTimeout
    $checkpointSeconds = ConvertTo-MacNotaryTimeoutSeconds $checkpoint
    $pollIntervalSeconds = Get-MacNotaryPollIntervalSeconds
    $nextCheckpoint = [DateTime]::UtcNow.AddSeconds($checkpointSeconds)
    Write-Host "Waiting for Apple notarization submission $SubmissionId for $([IO.Path]::GetFileName($ArtifactPath)). Only idempotent info queries are retried; the artifact upload will never be repeated by this wait loop." -ForegroundColor Cyan

    while ($true) {
        $info = Get-MacNotarySubmissionInfo -SubmissionId $SubmissionId -NotaryArguments $NotaryArguments
        $status = Get-MacNotaryObjectPropertyText -InputObject $info -PropertyName 'status'
        if ([string]::IsNullOrWhiteSpace($status)) {
            Write-Warning "Apple notarytool info returned valid JSON without a status for submission $SubmissionId. Retrying in $pollIntervalSeconds seconds instead of aborting the release."
            Start-Sleep -Seconds $pollIntervalSeconds
            continue
        }
        if ([string]::Equals($status, 'Accepted', [StringComparison]::OrdinalIgnoreCase)) {
            $state = Get-MacNotaryState $ArtifactPath
            if ($null -ne $state) { Set-MacNotaryStatePhase -ArtifactPath $ArtifactPath -State $state -Phase accepted | Out-Null }
            Write-Host "Apple notarization accepted submission $SubmissionId for $([IO.Path]::GetFileName($ArtifactPath))." -ForegroundColor Green
            return
        }
        if (-not [string]::Equals($status, 'In Progress', [StringComparison]::OrdinalIgnoreCase)) {
            Save-MacNotaryFailureLog -ArtifactPath $ArtifactPath -SubmissionId $SubmissionId -NotaryArguments $NotaryArguments | Out-Null
            throw "Apple notarization submission $SubmissionId finished with terminal status '$status' for $ArtifactPath."
        }
        if ([DateTime]::UtcNow -ge $nextCheckpoint) {
            Write-Warning "Apple notarization submission $SubmissionId for $([IO.Path]::GetFileName($ArtifactPath)) is still In Progress after another $checkpoint. Continuing only status polling; no upload or package build is repeated."
            $nextCheckpoint = [DateTime]::UtcNow.AddSeconds($checkpointSeconds)
        }
        Start-Sleep -Seconds $pollIntervalSeconds
    }
}

function Ensure-MacNotarySubmissionAccepted([string]$ArtifactPath) {
    if (-not (Get-ExternalCommandPath 'xcrun')) { throw 'xcrun is required for Apple notarization.' }
    $notaryArgs = @(Get-MacNotaryArguments)
    $state = Get-MacNotaryState $ArtifactPath

    if ($null -ne $state) {
        $phase = Get-MacNotaryObjectPropertyText -InputObject $state -PropertyName 'phase'
        $submissionId = Get-MacNotaryObjectPropertyText -InputObject $state -PropertyName 'submissionId'
        if ([string]::Equals($phase, 'submit-pending', [StringComparison]::OrdinalIgnoreCase) -and [string]::IsNullOrWhiteSpace($submissionId)) {
            Write-Host "Reconciling the pending Apple submit transaction for unchanged artifact $([IO.Path]::GetFileName($ArtifactPath)) before any new upload is allowed." -ForegroundColor Cyan
            $resolved = Resolve-MacNotaryPendingSubmission -ArtifactPath $ArtifactPath -State $state -NotaryArguments $notaryArgs -WaitForVisibility
            if ($null -eq $resolved) {
                throw "Apple submission state for $ArtifactPath remains ambiguous after history reconciliation. No duplicate upload was attempted. Keep the artifact and its .notary-state.json file, then rerun later; the next run will reconcile this exact artifact before it can submit anything new."
            }
            $state = $resolved
            $submissionId = Get-MacNotaryObjectPropertyText -InputObject $state -PropertyName 'submissionId'
        }
        if (-not [string]::IsNullOrWhiteSpace($submissionId)) {
            Write-Host "Resuming Apple notarization submission $submissionId for unchanged artifact $([IO.Path]::GetFileName($ArtifactPath)); no upload is repeated." -ForegroundColor Cyan
            Wait-MacNotarySubmission -ArtifactPath $ArtifactPath -SubmissionId $submissionId -NotaryArguments $notaryArgs
            return $submissionId
        }
    }

    # Per-artifact transaction boundary. First obtain an idempotent history snapshot so an ambiguous
    # submit can be reconciled against the exact set of submissions that existed before this upload.
    $artifactName = [IO.Path]::GetFileName($ArtifactPath)
    Write-Host "Preparing artifact-local Apple notarization transaction for $artifactName..." -ForegroundColor Cyan
    $baselineEntries = @(Get-MacNotaryHistoryEntries -NotaryArguments $notaryArgs)
    $baselineIds = @(Get-MacNotaryHistoryIds -Entries $baselineEntries)
    $submitStartedAtUtc = [DateTime]::UtcNow
    $state = New-MacNotaryPendingState -ArtifactPath $ArtifactPath -BaselineSubmissionIds $baselineIds -SubmitStartedAtUtc $submitStartedAtUtc

    Write-Host "Submitting $artifactName to Apple notary service exactly once for this transaction..." -ForegroundColor Cyan
    $submitArguments = @('submit',$ArtifactPath) + @($notaryArgs) + @('--output-format','json','--no-progress')
    $submitResult = Invoke-MacNotaryToolOnce -Operation "single upload of $artifactName" -Arguments $submitArguments
    $submissionId = ''
    if ($submitResult.ExitCode -eq 0) {
        try {
            $submit = ConvertFrom-MacNotaryJsonOutput -Output $submitResult.Output -Operation 'submit'
            $submissionId = Get-MacNotaryObjectPropertyText -InputObject $submit -PropertyName 'id'
        }
        catch {
            Write-Warning "Apple submit exited successfully but its local response could not be parsed. The transaction will be reconciled through history rather than uploaded again: $($_.Exception.Message)"
        }
    }
    else {
        Write-Warning "Apple submit returned a non-zero local result for $artifactName. Because submit is non-idempotent, this release will not retry the upload blindly."
        if (-not [string]::IsNullOrWhiteSpace([string]$submitResult.Details)) { Write-Warning $submitResult.Details }
    }

    if ([string]::IsNullOrWhiteSpace($submissionId)) {
        $resolved = Resolve-MacNotaryPendingSubmission -ArtifactPath $ArtifactPath -State $state -NotaryArguments $notaryArgs -WaitForVisibility
        if ($null -eq $resolved) {
            throw "Apple submit for $ArtifactPath had an ambiguous local result and no matching new history entry became visible during reconciliation. No second upload was attempted. The hash-bound pending state is preserved for a later rerun."
        }
        $state = $resolved
        $submissionId = Get-MacNotaryObjectPropertyText -InputObject $state -PropertyName 'submissionId'
    }
    else {
        $state = Save-MacNotarySubmittedState -ArtifactPath $ArtifactPath -ExistingState $state -SubmissionId $submissionId -SubmittedAtUtc ([DateTime]::UtcNow.ToString('O'))
        Write-Host "Apple upload completed; submission $submissionId was persisted immediately in $(Get-MacNotaryStatePath $ArtifactPath)." -ForegroundColor Green
    }

    Wait-MacNotarySubmission -ArtifactPath $ArtifactPath -SubmissionId $submissionId -NotaryArguments $notaryArgs
    return $submissionId
}

function Complete-MacArtifactLocalValidation([string]$ArtifactPath,[ValidateSet('dmg','pkg')][string]$Kind,[switch]$Quiet) {
    $xcrun = Get-ExternalCommandPath 'xcrun'
    if (-not $xcrun) { return $false }
    if ($Quiet) { & $xcrun stapler validate $ArtifactPath *> $null }
    else { & $xcrun stapler validate $ArtifactPath 2>&1 | ForEach-Object { Write-Host $_ } }
    if ($LASTEXITCODE -ne 0) { return $false }

    if ($Kind -eq 'pkg') {
        $pkgutil = Get-ExternalCommandPath 'pkgutil'
        if (-not $pkgutil) { return $false }
        if ($Quiet) { & $pkgutil --check-signature $ArtifactPath *> $null }
        else { & $pkgutil --check-signature $ArtifactPath 2>&1 | ForEach-Object { Write-Host $_ } }
        if ($LASTEXITCODE -ne 0) { return $false }
    }
    else {
        $hdiutil = Get-ExternalCommandPath 'hdiutil'
        $codesign = Get-ExternalCommandPath 'codesign'
        if (-not $hdiutil -or -not $codesign) { return $false }
        if ($Quiet) { & $hdiutil verify $ArtifactPath *> $null } else { & $hdiutil verify $ArtifactPath 2>&1 | ForEach-Object { Write-Host $_ } }
        if ($LASTEXITCODE -ne 0) { return $false }
        if ($Quiet) { & $codesign --verify --strict --verbose=2 $ArtifactPath *> $null } else { & $codesign --verify --strict --verbose=2 $ArtifactPath 2>&1 | ForEach-Object { Write-Host $_ } }
        if ($LASTEXITCODE -ne 0) { return $false }
    }
    return $true
}
function Test-MacDistributionArtifactReady([string]$ArtifactPath,[ValidateSet('dmg','pkg')][string]$Kind) {
    if ($ForceRebuildArtifacts -or -not $isMacHost -or -not (Test-Path -LiteralPath $ArtifactPath -PathType Leaf)) { return $false }
    $xcrun = Get-ExternalCommandPath 'xcrun'
    if (-not $xcrun) { return $false }

    if (Complete-MacArtifactLocalValidation -ArtifactPath $ArtifactPath -Kind $Kind -Quiet) {
        $state = Get-MacNotaryState $ArtifactPath
        if ($null -ne $state) { Set-MacNotaryStatePhase -ArtifactPath $ArtifactPath -State $state -Phase complete | Out-Null }
        Write-Host "Reusing already signed, notarized, stapled, and validated macOS $Kind artifact: $ArtifactPath" -ForegroundColor Green
        return $true
    }

    # An unchanged artifact with a state file is always resumed/reconciled before rebuild. This is
    # the per-package no-duplicate boundary: pending/submitted/accepted state blocks a fresh upload.
    $state = Get-MacNotaryState $ArtifactPath
    if ($null -ne $state -and (Test-MacNotaryCredentialsAvailable)) {
        Ensure-MacNotarySubmissionAccepted -ArtifactPath $ArtifactPath | Out-Null
    }
    else {
        # Legacy recovery for artifacts notarized by an older release script that did not persist
        # state through completion. A successful staple proves Apple already has a ticket.
        & $xcrun stapler staple $ArtifactPath *> $null
        if ($LASTEXITCODE -ne 0) { return $false }
    }

    if (-not (Complete-MacArtifactLocalValidation -ArtifactPath $ArtifactPath -Kind $Kind -Quiet)) { return $false }
    $state = Get-MacNotaryState $ArtifactPath
    if ($null -ne $state) { Set-MacNotaryStatePhase -ArtifactPath $ArtifactPath -State $state -Phase complete | Out-Null }
    Write-Host "Recovered and reused already notarized macOS $Kind artifact: $ArtifactPath" -ForegroundColor Green
    return $true
}


function Complete-MacDistributionArtifact([string]$ArtifactPath,[ValidateSet('dmg','pkg')][string]$Kind) {
    if (-not $isMacHost -or -not (Test-Path -LiteralPath $ArtifactPath -PathType Leaf)) { return }
    Resolve-MacDistributionSigning

    $hasRequiredSigningIdentity = if ($Kind -eq 'pkg') { [bool]$script:MacInstallerSigningIdentity } else { [bool]$script:MacApplicationSigningIdentity }
    $hasNotaryCredentials = Test-MacNotaryCredentialsAvailable
    if (-not $hasRequiredSigningIdentity -or -not $hasNotaryCredentials) {
        $reason = if (-not $hasRequiredSigningIdentity) { 'the required Developer ID signing identity is unavailable' } else { 'notary credentials are unavailable' }
        $message = "$([IO.Path]::GetFileName($ArtifactPath)) is NOT notarized because $reason. A quarantined Internet/WhatsApp/browser download can be blocked by Gatekeeper on another Mac."
        if (Test-MacNotarizationRequired) { throw "$message Configure Apple Developer ID signing and notarytool credentials or unset MACOS_REQUIRE_NOTARIZATION for local-only artifacts." }
        if (-not $script:MacNotaryWarningIssued) {
            Write-Warning $message
            Write-Warning 'Recommended Apple-ID setup: `xcrun notarytool store-credentials <profile> --apple-id <APPLE_ID> --team-id <TEAM_ID>` and enter the app-specific password when prompted; then set MACOS_NOTARY_KEYCHAIN_PROFILE=<profile>. API-key authentication remains supported through APPLE_NOTARY_KEY_* variables.'
            $script:MacNotaryWarningIssued = $true
        }
        return
    }

    $xcrun = Get-ExternalCommandPath 'xcrun'
    if (-not $xcrun) { throw 'xcrun is required for Apple notarization.' }
    $submissionId = Ensure-MacNotarySubmissionAccepted -ArtifactPath $ArtifactPath
    & $xcrun stapler staple $ArtifactPath 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { throw "Apple notarization ticket stapling failed for $ArtifactPath" }
    if (-not (Complete-MacArtifactLocalValidation -ArtifactPath $ArtifactPath -Kind $Kind)) {
        throw "macOS $Kind validation failed after notarization/stapling: $ArtifactPath"
    }
    $state = Get-MacNotaryState $ArtifactPath
    if ($null -ne $state) { Set-MacNotaryStatePhase -ArtifactPath $ArtifactPath -State $state -Phase complete | Out-Null }

    $spctl = Get-ExternalCommandPath 'spctl'
    if ($Kind -eq 'pkg') {
        if ($spctl) {
            $assessment = @(& $spctl --assess --type install --verbose=4 $ArtifactPath 2>&1 | ForEach-Object { [string]$_ })
            if ($LASTEXITCODE -eq 0) {
                $assessment | ForEach-Object { Write-Host $_ }
                Write-Host "Gatekeeper installer assessment accepted $([IO.Path]::GetFileName($ArtifactPath))." -ForegroundColor Green
            } else {
                $assessment | ForEach-Object { Write-Warning $_ }
                Write-Warning "Local spctl installer assessment rejected $([IO.Path]::GetFileName($ArtifactPath)) even though notarization, stapling, and pkgutil signature validation succeeded. Test the downloaded PKG on a clean Mac before publishing."
            }
        }
    } else {
        if ($spctl) {
            & $spctl --assess --type open --context 'context:primary-signature' --verbose=4 $ArtifactPath 2>&1 | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0) { throw "Gatekeeper disk-image assessment failed after notarization: $ArtifactPath" }
        }
    }
    Write-Host "Signed, notarized, stapled, and validated macOS $Kind artifact: $ArtifactPath (submission $submissionId)." -ForegroundColor Green
}

function New-MacLauncher([string]$Destination,[string]$BinaryRelativePath) {
    $template = @'
#!/bin/sh
set -u
HERE=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
APP=$(CDPATH= cd -- "$HERE/../Resources/app" && pwd)
BIN="$APP/__EXECUTABLE__"
PRODUCT="__PRODUCT__"
LOG_DIR="$HOME/Library/Logs/$PRODUCT"
LOG_FILE="$LOG_DIR/launcher.log"
USER_DATA_DIR="$HOME/Library/Application Support/$PRODUCT"
USER_CACHE_DIR="$HOME/Library/Caches/$PRODUCT"
SHOW_CONSOLE="${LOCALGPT_SHOW_CONSOLE:-1}"

read_endpoint() {
  for f in \
    "$HOME/Library/Application Support/$PRODUCT/runtime/server.json" \
    "$HOME/.local/share/$PRODUCT/runtime/server.json"
  do
    [ -f "$f" ] || continue
    owner_pid=$(sed -nE 's/.*"[Pp]rocess[Ii]d"[[:space:]]*:[[:space:]]*([0-9]+).*/\1/p' "$f" | head -n 1)
    if [ -n "${owner_pid:-}" ] && ! kill -0 "$owner_pid" 2>/dev/null; then
      rm -f "$f" 2>/dev/null || true
      continue
    fi
    url=$(sed -nE 's/.*"([Bb]ase[Uu]rl|[Uu]rl)"[[:space:]]*:[[:space:]]*"([^\"]+)".*/\2/p' "$f" | head -n 1)
    case "${url:-}" in
      http://127.0.0.1:*|http://localhost:*|https://127.0.0.1:*|https://localhost:*) printf '%s' "$url"; return 0 ;;
    esac
  done
  return 1
}
endpoint_responds() {
  candidate="$1"
  probe="${candidate%/}/health"
  if [ -x /usr/bin/curl ]; then
    /usr/bin/curl --silent --show-error --fail --connect-timeout 1 --max-time 2 --output /dev/null "$probe" >/dev/null 2>&1
    return $?
  fi
  return 1
}
ensure_writable_dir() {
  target="$1"
  /bin/mkdir -p "$target" 2>/dev/null || return 1
  probe="$target/.write-test-$$"
  ( umask 077; : >"$probe" ) 2>/dev/null || return 1
  /bin/rm -f "$probe" 2>/dev/null || true
  return 0
}
repair_user_directory() {
  target="$1"
  [ -x /usr/bin/osascript ] || return 1
  uid=$(/usr/bin/id -u)
  gid=$(/usr/bin/id -g)
  /usr/bin/osascript - "$PRODUCT" "$target" "$uid" "$gid" <<'APPLESCRIPT' >/dev/null 2>&1
on run argv
  set productName to item 1 of argv
  set targetPath to item 2 of argv
  set userId to item 3 of argv
  set groupId to item 4 of argv
  set response to display dialog (productName & " needs write access to its per-user data folder:" & return & return & targetPath & return & return & "The application bundle in /Applications remains read-only. Only this user-data folder will be repaired.") buttons {"Cancel", "Repair Access"} default button "Repair Access" with icon caution
  if button returned of response is not "Repair Access" then error number -128
  do shell script ("/bin/mkdir -p " & quoted form of targetPath & " && /usr/sbin/chown -R " & userId & ":" & groupId & " " & quoted form of targetPath & " && /bin/chmod -R u+rwX " & quoted form of targetPath) with administrator privileges
end run
APPLESCRIPT
}
ensure_user_storage() {
  for target in "$USER_DATA_DIR" "$USER_DATA_DIR/runtime" "$LOG_DIR" "$USER_CACHE_DIR"; do
    if ensure_writable_dir "$target"; then
      continue
    fi
    printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') User-data write probe failed for $target; requesting a scoped permission repair." >>"$LOG_FILE" 2>/dev/null || true
    repair_user_directory "$target" || return 1
    ensure_writable_dir "$target" || return 1
  done
  : >>"$LOG_FILE" 2>/dev/null || return 1
  return 0
}
open_startup_terminal() {
  case "$SHOW_CONSOLE" in
    0|false|FALSE|no|NO|off|OFF) return 0 ;;
  esac
  helper="$LOG_DIR/startup-watch.command"
  cat >"$helper" <<EOF
#!/bin/sh
clear
printf '%s\n' '$PRODUCT console' 'Application output is mirrored into this launcher log for the same visible/debuggable startup experience used on Windows and Linux.' 'The browser opens automatically when the local server is ready. Close this Terminal window when you no longer need the live log.' ''
exec /usr/bin/tail -n 120 -f '$LOG_FILE'
EOF
  chmod +x "$helper" 2>/dev/null || true
  if /usr/bin/open -a Terminal "$helper" >/dev/null 2>&1; then
    return 0
  fi
  if [ -x /usr/bin/osascript ]; then
    if /usr/bin/osascript - "$helper" <<'APPLESCRIPT' >/dev/null 2>&1
on run argv
  tell application "Terminal"
    activate
    do script quoted form of (item 1 of argv)
  end tell
end run
APPLESCRIPT
    then
      return 0
    fi
  fi
  printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') Could not open Terminal for the visible $PRODUCT console; startup will continue and retry once if it remains slow." >>"$LOG_FILE" 2>/dev/null || true
  return 1
}
detect_hardware_architecture() {
  if [ -x /usr/sbin/sysctl ]; then
    apple_silicon=$(/usr/sbin/sysctl -n hw.optional.arm64 2>/dev/null || true)
    if [ "$apple_silicon" = "1" ]; then
      printf '%s' 'arm64'
      return 0
    fi
  fi
  if [ -x /usr/bin/uname ]; then
    /usr/bin/uname -m 2>/dev/null || printf '%s' 'unknown'
    return 0
  fi
  printf '%s' 'unknown'
}
detect_translation_state() {
  if [ -x /usr/sbin/sysctl ]; then
    translated=$(/usr/sbin/sysctl -n sysctl.proc_translated 2>/dev/null || true)
    case "${translated:-0}" in
      1) printf '%s' '1'; return 0 ;;
    esac
  fi
  printf '%s' '0'
}
ensure_native_launcher_process() {
  hardware=$(detect_hardware_architecture)
  process_arch=$(/usr/bin/uname -m 2>/dev/null || printf '%s' 'unknown')
  translated=$(detect_translation_state)
  printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') Launcher architecture: hardware=$hardware process=$process_arch translated=$translated." >>"$LOG_FILE" 2>/dev/null || true

  if [ "$hardware" = "arm64" ] && [ "$translated" = "1" ] && [ "${LOCALGPT_NATIVE_REEXEC:-0}" != "1" ] && [ -x /usr/bin/arch ]; then
    printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') The launcher was started through Rosetta on Apple Silicon; re-executing the same launcher with the native arm64 system shell." >>"$LOG_FILE" 2>/dev/null || true
    export LOCALGPT_NATIVE_REEXEC=1
    exec /usr/bin/arch -arm64 /bin/sh "$0" "$@"
  fi
}
verify_runtime_architecture() {
  [ -x /usr/bin/file ] || return 0
  hardware=$(detect_hardware_architecture)
  process_arch=$(/usr/bin/uname -m 2>/dev/null || printf '%s' 'unknown')
  translated=$(detect_translation_state)
  description=$(/usr/bin/file "$BIN" 2>/dev/null || true)
  manifest="$APP/../native-architecture-manifest.txt"
  printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') Runtime architecture check: hardware=$hardware process=$process_arch translated=$translated; $BIN => $description" >>"$LOG_FILE" 2>/dev/null || true

  case "$hardware" in
    arm64)
      case "$description" in
        *arm64*) return 0 ;;
        *)
          show_failure "Architecture mismatch on Apple Silicon (process=$process_arch translated=$translated). Exact runtime: $BIN => $description. Package inventory: $manifest. Install the osx-arm64 package; if this still fails, report the manifest so the exact native dependency can be identified."
          return 1
          ;;
      esac
      ;;
    x86_64)
      case "$description" in
        *x86_64*) return 0 ;;
        *)
          show_failure "Architecture mismatch on an Intel Mac (process=$process_arch). Exact runtime: $BIN => $description. Package inventory: $manifest. Install the osx-x64 package."
          return 1
          ;;
      esac
      ;;
  esac

  printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') Hardware architecture could not be classified; startup continues because the package-time Mach-O manifest already validated the bundle." >>"$LOG_FILE" 2>/dev/null || true
  return 0
}

terminate_stale_processes() {
  [ -x /usr/bin/pgrep ] || return 0
  stale_pids=$(/usr/bin/pgrep -f "$BIN" 2>/dev/null || true)
  [ -n "${stale_pids:-}" ] || return 0
  for stale_pid in $stale_pids; do
    [ "$stale_pid" = "$$" ] && continue
    printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') Stopping stale $PRODUCT process $stale_pid that has no responding runtime endpoint." >>"$LOG_FILE" 2>/dev/null || true
    /bin/kill -TERM "$stale_pid" 2>/dev/null || true
  done
  sleep 1
  for stale_pid in $stale_pids; do
    [ "$stale_pid" = "$$" ] && continue
    if /bin/kill -0 "$stale_pid" 2>/dev/null; then
      /bin/kill -KILL "$stale_pid" 2>/dev/null || true
    fi
  done
}

show_failure() {
  reason="$1"
  printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') $reason" >>"$LOG_FILE" 2>/dev/null || printf '%s\n' "$reason" >&2
  if [ -x /usr/bin/osascript ]; then
    /usr/bin/osascript - "$PRODUCT" "$reason" "$LOG_FILE" <<'APPLESCRIPT' >/dev/null 2>&1 || true
on run argv
  set productName to item 1 of argv
  set reasonText to item 2 of argv
  set logPath to item 3 of argv
  display alert (productName & " could not start") message (reasonText & return & return & "Startup log: " & logPath) as critical buttons {"OK"} default button "OK"
end run
APPLESCRIPT
  fi
  /usr/bin/open -R "$LOG_FILE" >/dev/null 2>&1 || true
}

if ! ensure_user_storage; then
  show_failure "$PRODUCT cannot write to its per-user Application Support/Logs/Cache directories. Repair the ownership when prompted, or fix those user folders and start the application again."
  exit 1
fi

ensure_native_launcher_process "$@"

if open_startup_terminal; then
  terminal_opened=1
else
  terminal_opened=0
fi

if url=$(read_endpoint 2>/dev/null); then
  if endpoint_responds "$url"; then
    /usr/bin/open "$url" >/dev/null 2>&1 || true
    exit 0
  fi
fi

terminate_stale_processes

if [ ! -x "$BIN" ]; then
  show_failure "The packaged application executable is missing or is not executable: $BIN"
  exit 1
fi
if ! verify_runtime_architecture; then
  exit 1
fi

cd "$APP" || { show_failure "The packaged application directory could not be opened: $APP"; exit 1; }
printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') Starting $PRODUCT from $BIN with an automatically selected loopback port (macOS port 5000 is commonly occupied by AirPlay Receiver)." >>"$LOG_FILE"
"$BIN" --port 0 >>"$LOG_FILE" 2>&1 &
pid=$!

i=0
while [ $i -lt 600 ]; do
  if url=$(read_endpoint 2>/dev/null); then
    if endpoint_responds "$url"; then
      printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') $PRODUCT ready at $url (pid $pid)" >>"$LOG_FILE"
      /usr/bin/open "$url" >/dev/null 2>&1 || true
      exit 0
    fi
  fi
  if ! kill -0 "$pid" 2>/dev/null; then
    wait "$pid" 2>/dev/null
    code=$?
    show_failure "$PRODUCT exited during startup with code $code."
    if [ "$code" -eq 0 ]; then exit 1; fi
    exit "$code"
  fi
  if [ $i -eq 40 ] && [ $terminal_opened -eq 0 ]; then
    printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') Startup is taking longer than 20 seconds; opening a Terminal log helper while $PRODUCT continues starting." >>"$LOG_FILE"
    open_startup_terminal
    terminal_opened=1
  fi
  i=$((i+1))
  sleep 0.5
done

printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') $PRODUCT did not publish a healthy local endpoint within 5 minutes; terminating launcher-owned process $pid so the next start cannot inherit a stale database/port lock." >>"$LOG_FILE"
if /bin/kill -0 "$pid" 2>/dev/null; then
  /bin/kill -TERM "$pid" 2>/dev/null || true
  sleep 2
fi
if /bin/kill -0 "$pid" 2>/dev/null; then
  /bin/kill -KILL "$pid" 2>/dev/null || true
fi
show_failure "$PRODUCT did not publish a healthy local HTTP endpoint within 5 minutes. The stuck process was terminated; inspect the launcher log and start the application again."
exit 1
'@
    Write-Utf8NoBom $Destination ($template.Replace('__PRODUCT__', $ProductName).Replace('__EXECUTABLE__', $BinaryRelativePath))
    Set-UnixExecutable $Destination
}
function Remove-NonTargetMacRuntimeAssets([string]$AppPath,[string]$RuntimeIdentifier) {
    if (-not $isMacHost) { return }

    $targetRuntime = if ($RuntimeIdentifier.EndsWith('arm64')) { 'osx-arm64' } elseif ($RuntimeIdentifier.EndsWith('x64')) { 'osx-x64' } else { $null }
    if ([string]::IsNullOrWhiteSpace($targetRuntime)) { return }

    $removed = [Collections.Generic.List[string]]::new()
    foreach ($runtimeDirectory in @(Get-ChildItem -LiteralPath $AppPath -Directory -Recurse -ErrorAction SilentlyContinue | Where-Object {
        $isRuntimeChild = $_.Parent -and $_.Parent.Name -eq 'runtimes'
        $isWrongOsxRuntime = $_.Name.StartsWith('osx-', [StringComparison]::OrdinalIgnoreCase) -and
            -not $_.Name.Equals($targetRuntime, [StringComparison]::OrdinalIgnoreCase)
        $isOtherAppleRuntime = $_.Name -match '^(maccatalyst|ios|iossimulator|tvos|tvossimulator)-'
        $isRuntimeChild -and ($isWrongOsxRuntime -or $isOtherAppleRuntime)
    })) {
        $removed.Add($runtimeDirectory.Name)
        Remove-Item -LiteralPath $runtimeDirectory.FullName -Recurse -Force -ErrorAction Stop
    }

    if ($removed.Count -gt 0) {
        $summary = ($removed | Sort-Object -Unique) -join ', '
        Write-Host "Removed $($removed.Count) non-target Apple runtime asset folder(s) from the $RuntimeIdentifier macOS bundle: $summary" -ForegroundColor DarkCyan
    }
}
function Assert-MacBundleArchitecture([string]$AppPath,[string]$RuntimeIdentifier) {
    if (-not $isMacHost) { return }

    $fileCommand = Get-ExternalCommandPath 'file'
    if (-not $fileCommand) { throw "The macOS 'file' utility is required to validate native bundle architecture." }

    $expectedPattern = if ($RuntimeIdentifier.EndsWith('arm64')) { '\barm64e?\b' } elseif ($RuntimeIdentifier.EndsWith('x64')) { '\bx86_64\b' } else { throw "Unsupported macOS runtime identifier for architecture validation: $RuntimeIdentifier" }
    $machOCount = 0
    $mismatches = [Collections.Generic.List[string]]::new()
    $inventory = [Collections.Generic.List[string]]::new()

    foreach ($item in Get-ChildItem -LiteralPath $AppPath -File -Recurse -ErrorAction Stop) {
        $description = [string](& $fileCommand $item.FullName 2>$null)
        if ($LASTEXITCODE -ne 0 -or $description -notmatch 'Mach-O') { continue }

        $machOCount++
        $relative = Get-RelativePathPortable -BasePath $AppPath -TargetPath $item.FullName
        $entry = "$relative => $description"
        $inventory.Add($entry)
        if ($description -notmatch $expectedPattern) {
            $mismatches.Add($entry)
        }
    }

    $manifestPath = Join-Path $AppPath 'Contents/Resources/native-architecture-manifest.txt'
    $manifestLines = @(
        "$ProductName $Version native architecture manifest",
        "RID: $RuntimeIdentifier",
        "Expected Mach-O architecture: $expectedPattern",
        "Generated: $([DateTimeOffset]::UtcNow.ToString('O'))",
        ''
    ) + @($inventory)
    Write-Utf8NoBom $manifestPath (($manifestLines -join [Environment]::NewLine) + [Environment]::NewLine)

    if ($machOCount -eq 0) {
        throw "No Mach-O payload was found in $AppPath. Refusing to ship a macOS application whose native architecture cannot be verified. Manifest: $manifestPath"
    }
    if ($mismatches.Count -gt 0) {
        $details = ($mismatches | Select-Object -First 20) -join [Environment]::NewLine
        throw "The $RuntimeIdentifier bundle contains native component(s) without the required architecture. Exact offending file(s):$([Environment]::NewLine)$details$([Environment]::NewLine)Full architecture inventory: $manifestPath"
    }

    Write-Host "Validated $machOCount Mach-O component(s) in $ProductName.app for $RuntimeIdentifier; no incompatible Intel/ARM-only payload was found." -ForegroundColor Green
    Write-Host "Native architecture manifest: $manifestPath" -ForegroundColor DarkCyan
}

function New-Dmg([string]$AppPath,[string]$Destination) {
    if (-not $isMacHost -or -not (Get-Command hdiutil -ErrorAction SilentlyContinue)) {
        Write-Warning "DMG materialization is a native macOS finishing step; $Destination was not produced on this host."
        return $false
    }

    $appName = [IO.Path]::GetFileName($AppPath)
    $volumeName = "$ProductName $Version"
    $stage = Join-Path ([IO.Path]::GetTempPath()) ("dmg-stage-" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage -Force | Out-Null
    try {
        Copy-Item -LiteralPath $AppPath -Destination (Join-Path $stage $appName) -Recurse -Force
        New-Item -ItemType SymbolicLink -Path (Join-Path $stage 'Applications') -Target '/Applications' | Out-Null

        # Keep the branded background in the image as an asset, but never automate Finder during
        # a release. Finder AppleEvents can block a headless build for minutes and return -1712.
        if (-not [string]::IsNullOrWhiteSpace($DmgBackgroundPath)) {
            if (-not (Test-Path -LiteralPath $DmgBackgroundPath -PathType Leaf)) { throw "DMG background was not found: $DmgBackgroundPath" }
            $backgroundDirectory = Join-Path $stage '.background'
            New-Item -ItemType Directory -Path $backgroundDirectory -Force | Out-Null
            Copy-Item -LiteralPath $DmgBackgroundPath -Destination (Join-Path $backgroundDirectory 'background.png') -Force
        }

        Remove-Item -LiteralPath $Destination -Force -ErrorAction SilentlyContinue
        & hdiutil create -volname $volumeName -srcfolder $stage -ov -format UDZO -imagekey zlib-level=9 $Destination | Out-Null
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $Destination -PathType Leaf)) {
            throw "hdiutil failed while creating $Destination"
        }

        # Verify the compressed image without mounting it or opening Finder.
        & hdiutil verify $Destination | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Remove-Item -LiteralPath $Destination -Force -ErrorAction SilentlyContinue
            throw "hdiutil verification failed for $Destination"
        }

        Sign-MacDiskImage $Destination
        & hdiutil verify $Destination | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Remove-Item -LiteralPath $Destination -Force -ErrorAction SilentlyContinue
            throw "hdiutil verification failed after Developer ID signing for $Destination"
        }
        Write-Host "Created, verified, and distribution-signed headless DMG with $appName and Applications alias: $Destination" -ForegroundColor Green
        return $true
    }
    finally {
        Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
    }
}
function New-MacPkg([string]$AppPath,[string]$Destination) {
    if (-not $isMacHost) { return $false }
    $pkgbuild = Get-ExternalCommandPath 'pkgbuild'
    if (-not $pkgbuild) {
        Write-Warning "pkgbuild is unavailable; $Destination was not produced. The DMG and TAR.GZ remain available."
        return $false
    }

    $pkgutil = Get-ExternalCommandPath 'pkgutil'
    if (-not $pkgutil) {
        Write-Warning "pkgutil is unavailable; a PKG cannot be payload-validated, so $Destination was not produced."
        return $false
    }

    $identifier = "io.github.michi0403.$($ProductName.ToLowerInvariant())"
    $appName = [IO.Path]::GetFileName($AppPath)
    $pkgRoot = Join-Path ([IO.Path]::GetTempPath()) ("pkg-root-" + [Guid]::NewGuid().ToString('N'))
    $applicationsRoot = Join-Path $pkgRoot 'Applications'
    New-Item -ItemType Directory -Path $applicationsRoot -Force | Out-Null
    try {
        # Root-mode packaging makes the payload layout explicit:
        # /Applications/<Product>.app/Contents/... instead of relying on component inference.
        Copy-Item -LiteralPath $AppPath -Destination (Join-Path $applicationsRoot $appName) -Recurse -Force
        $stagedInfoPlist = Join-Path $applicationsRoot "$appName/Contents/Info.plist"
        $stagedMacOsRoot = Join-Path $applicationsRoot "$appName/Contents/MacOS"
        if (-not (Test-Path -LiteralPath $stagedInfoPlist -PathType Leaf) -or -not (Test-Path -LiteralPath $stagedMacOsRoot -PathType Container)) {
            throw "macOS PKG staging does not contain a complete application bundle: $applicationsRoot/$appName"
        }

        Remove-Item -LiteralPath $Destination -Force -ErrorAction SilentlyContinue
        Resolve-MacDistributionSigning
        $pkgBuildArguments = @(
            '--root', $pkgRoot,
            '--identifier', $identifier,
            '--version', $Version,
            '--install-location', '/',
            '--ownership', 'recommended'
        )
        if ($script:MacInstallerSigningIdentity) { $pkgBuildArguments += @('--sign', $script:MacInstallerSigningIdentity) }
        $pkgBuildArguments += $Destination
        & $pkgbuild @pkgBuildArguments 2>&1 | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $Destination -PathType Leaf)) {
            Write-Warning "pkgbuild failed while creating $Destination. The DMG and TAR.GZ remain available."
            Remove-Item -LiteralPath $Destination -Force -ErrorAction SilentlyContinue
            return $false
        }

        $payloadLines = @(& $pkgutil --payload-files $Destination 2>&1 | ForEach-Object { [string]$_ })
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "pkgutil could not inspect $Destination. The unverified PKG was removed."
            Remove-Item -LiteralPath $Destination -Force -ErrorAction SilentlyContinue
            return $false
        }

        $normalizedPayload = @(
            $payloadLines |
                ForEach-Object { ([string]$_).Trim().TrimStart([char[]]"./\") } |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        )
        $bundlePrefix = "Applications/$appName/"
        $hasBundle = @($normalizedPayload | Where-Object { $_ -eq "Applications/$appName" -or $_.StartsWith($bundlePrefix, [StringComparison]::Ordinal) }).Count -gt 0
        $hasInfoPlist = @($normalizedPayload | Where-Object { $_ -eq "${bundlePrefix}Contents/Info.plist" }).Count -gt 0
        $hasMacOsPayload = @($normalizedPayload | Where-Object { $_.StartsWith("${bundlePrefix}Contents/MacOS/", [StringComparison]::Ordinal) }).Count -gt 0
        if (-not $hasBundle -or -not $hasInfoPlist -or -not $hasMacOsPayload) {
            Write-Warning "PKG payload validation failed for $Destination; expected /Applications/$appName/Contents layout was not present."
            Remove-Item -LiteralPath $Destination -Force -ErrorAction SilentlyContinue
            return $false
        }

        Write-Host "Validated PKG payload root /Applications/$appName with Info.plist and executable content." -ForegroundColor Green
        return $true
    }
    finally {
        Remove-Item -LiteralPath $pkgRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
function New-AppImage([string]$Source,[string]$Destination) {
    $tool = $null
    if ($isLinuxHost) {
        # appimagetool itself runs on the host architecture, but ARCH selects the runtime embedded
        # into the resulting AppImage. This allows an x64 WSL/Linux host to finish an arm64 AppDir.
        $tool = Get-ExternalCommandPath 'appimagetool'
    }

    $engine = $null
    if (-not $tool -and $UseContainerFallback -and ($isLinuxHost -or $isMacHost)) {
        $engine = Get-ExternalCommandPath 'docker'
        if (-not $engine) { $engine = Get-ExternalCommandPath 'podman' }
    }

    if (-not $tool -and -not $engine) {
        if ($isMacHost) {
            Complete-OptionalPackageFailure 'AppImage' "AppImage finishing needs Linux. macOS can cross-publish the Linux payload, but finishing needs a Linux builder or an explicitly enabled Docker/Podman fallback (-UseContainerFallback)."
            return $false
        }
        Complete-OptionalPackageFailure 'AppImage' "appimagetool is unavailable. Install it on Linux/WSL or opt into an already-installed Docker/Podman engine with -UseContainerFallback."
        return $false
    }

    $appDir = Join-Path ([IO.Path]::GetTempPath()) ("appimage-" + [Guid]::NewGuid().ToString('N') + '.AppDir')
    New-Item -ItemType Directory -Path $appDir -Force | Out-Null
    try {
        Copy-Item -Path (Join-Path $Source '*') -Destination $appDir -Recurse -Force
        $appRun = Join-Path $appDir 'AppRun'
        $appRunTemplate = @'
#!/bin/sh
set -u
HERE=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
PRODUCT="__PRODUCT__"
DATA_HOME="${XDG_DATA_HOME:-$HOME/.local/share}"
STATE_HOME="${XDG_STATE_HOME:-$HOME/.local/state}"
CACHE_HOME="${XDG_CACHE_HOME:-$HOME/.cache}"
for target in "$DATA_HOME/$PRODUCT" "$STATE_HOME/$PRODUCT" "$CACHE_HOME/$PRODUCT"; do
  mkdir -p "$target" 2>/dev/null || { printf '%s\n' "$PRODUCT needs write access to $target" >&2; exit 73; }
  probe="$target/.write-test-$$"
  ( umask 077; : >"$probe" ) 2>/dev/null || { printf '%s\n' "$PRODUCT cannot write to $target. Fix ownership/permissions for this per-user directory and try again." >&2; exit 73; }
  rm -f "$probe" 2>/dev/null || true
done
export XDG_DATA_HOME="$DATA_HOME" XDG_STATE_HOME="$STATE_HOME" XDG_CACHE_HOME="$CACHE_HOME"
exec "$HERE/__EXECUTABLE__" "$@"
'@
        Write-Utf8NoBom $appRun ($appRunTemplate.Replace('__EXECUTABLE__', $ExecutableName).Replace('__PRODUCT__', $ProductName))
        Set-UnixExecutable $appRun
        Set-UnixExecutable (Join-Path $appDir $ExecutableName)
        $desktop = Join-Path $appDir "$ProductName.desktop"
        $iconLine = ''
        if (-not [string]::IsNullOrWhiteSpace($MacIconSource) -and (Test-Path -LiteralPath $MacIconSource -PathType Leaf)) {
            Copy-Item -LiteralPath $MacIconSource -Destination (Join-Path $appDir "$ProductName.png") -Force
            $iconLine = "Icon=$ProductName`n"
        }
        Write-Utf8NoBom $desktop "[Desktop Entry]`nType=Application`nName=$ProductName`nExec=$ExecutableName`n${iconLine}Terminal=true`nCategories=Utility;`n"
        $appImageArch = if ($Rid.EndsWith('arm64')) { 'aarch64' } else { 'x86_64' }
        if ($tool) {
            $hadArch = Test-Path Env:ARCH
            $previousArch = $env:ARCH
            $hadExtractAndRun = Test-Path Env:APPIMAGE_EXTRACT_AND_RUN
            $previousExtractAndRun = $env:APPIMAGE_EXTRACT_AND_RUN
            try {
                $env:ARCH = $appImageArch
                if (-not [string]::IsNullOrWhiteSpace($env:WSL_DISTRO_NAME) -or -not [string]::IsNullOrWhiteSpace($env:WSL_INTEROP)) {
                    # WSL commonly has no FUSE device. appimagetool supports extract-and-run mode.
                    $env:APPIMAGE_EXTRACT_AND_RUN = '1'
                }
                & $tool $appDir $Destination | Out-Host
                if ($LASTEXITCODE -ne 0) { throw 'appimagetool failed.' }
            }
            finally {
                if ($hadArch) { $env:ARCH = $previousArch } else { Remove-Item Env:ARCH -ErrorAction SilentlyContinue }
                if ($hadExtractAndRun) { $env:APPIMAGE_EXTRACT_AND_RUN = $previousExtractAndRun } else { Remove-Item Env:APPIMAGE_EXTRACT_AND_RUN -ErrorAction SilentlyContinue }
            }
        } else {
            $image = if ($env:APPIMAGETOOL_CONTAINER_IMAGE) { $env:APPIMAGETOOL_CONTAINER_IMAGE } else { 'ghcr.io/appimage/appimagetool:continuous' }
            $parent = Split-Path -Parent $appDir
            $leaf = Split-Path -Leaf $appDir
            $outLeaf = [IO.Path]::GetFileName($Destination)
            $platform = if ($Rid.EndsWith('arm64')) { 'linux/arm64' } else { 'linux/amd64' }
            & $engine run --rm --privileged --platform $platform -e "ARCH=$appImageArch" -v "${parent}:/work" $image "/work/$leaf" "/work/$outLeaf" | Out-Host
            if ($LASTEXITCODE -ne 0) { throw 'Containerized appimagetool failed.' }
            Move-Item -LiteralPath (Join-Path $parent $outLeaf) -Destination $Destination -Force
        }
        return $true
    } catch {
        Complete-OptionalPackageFailure 'AppImage' $_.Exception.Message
        return $false
    } finally { Remove-Item -LiteralPath $appDir -Recurse -Force -ErrorAction SilentlyContinue }
}
function New-Rpm([string]$Source,[string]$Destination,[string]$Architecture) {
    $rpmbuild = $null
    if ($isLinuxHost -or $isMacHost) { $rpmbuild = Resolve-RpmBuildPath }

    $engine = $null
    if (-not $rpmbuild -and $UseContainerFallback -and ($isLinuxHost -or $isMacHost)) {
        $engine = Get-ExternalCommandPath 'docker'
        if (-not $engine) { $engine = Get-ExternalCommandPath 'podman' }
    }

    if (-not $rpmbuild -and -not $engine) {
        if ($isMacHost) {
            $brew = Get-ExternalCommandPath 'brew'
            $hint = if ($brew) { " The build normally provisions it with 'brew install rpm'; inspect the Homebrew error above if provisioning failed." } else { ' Install Homebrew and then install the rpm formula, or use a Linux builder.' }
            Complete-OptionalPackageFailure 'RPM' "rpmbuild is unavailable on macOS.$hint"
            return $false
        }
        Complete-OptionalPackageFailure 'RPM' "rpmbuild is unavailable. Install rpm-build/rpm tooling on Linux or opt into an already-installed Docker/Podman engine with -UseContainerFallback."
        return $false
    }

    $rpmTarget = "$Architecture-unknown-linux"
    try {
        if ($engine) {
            $image = if ($env:RPMBUILD_CONTAINER_IMAGE) { $env:RPMBUILD_CONTAINER_IMAGE } else { 'fedora:42' }
            $work = Join-Path ([IO.Path]::GetTempPath()) ("rpm-container-" + [Guid]::NewGuid().ToString('N'))
            New-Item -ItemType Directory -Path $work -Force | Out-Null
            try {
                Copy-Item -Path (Join-Path $Source '*') -Destination $work -Recurse -Force
                $script = @"
set -eu
dnf -y install rpm-build >/dev/null
mkdir -p /root/rpmbuild/{BUILD,RPMS,SOURCES,SPECS,SRPMS}
cp -a /work /root/rpmbuild/SOURCES/payload
cat >/root/rpmbuild/SPECS/package.spec <<'EOF'
Name: $($ProductName.ToLowerInvariant())
Version: $Version
Release: 1
Summary: $ProductName
License: Open Source
BuildArch: $Architecture
%description
$ProductName
%install
mkdir -p %{buildroot}/opt/$($ProductName.ToLowerInvariant()) %{buildroot}/usr/bin
cp -a /root/rpmbuild/SOURCES/payload/. %{buildroot}/opt/$($ProductName.ToLowerInvariant())/
printf '#!/bin/sh\nexec /opt/$($ProductName.ToLowerInvariant())/$ExecutableName "`$@"\n' > %{buildroot}/usr/bin/$($ProductName.ToLowerInvariant())
chmod 0755 %{buildroot}/usr/bin/$($ProductName.ToLowerInvariant()) %{buildroot}/opt/$($ProductName.ToLowerInvariant())/$ExecutableName
%files
/opt/$($ProductName.ToLowerInvariant())
/usr/bin/$($ProductName.ToLowerInvariant())
EOF
rpmbuild --target $rpmTarget -bb /root/rpmbuild/SPECS/package.spec >/dev/null
cp /root/rpmbuild/RPMS/*/*.rpm /out/package.rpm
"@
                $out = Split-Path -Parent $Destination
                $platform = if ($Rid.EndsWith('arm64')) { 'linux/arm64' } else { 'linux/amd64' }
                & $engine run --rm --platform $platform -v "${work}:/work:ro" -v "${out}:/out" $image sh -lc $script | Out-Host
                if ($LASTEXITCODE -ne 0) { throw 'Containerized RPM packaging failed.' }
                Move-Item -LiteralPath (Join-Path $out 'package.rpm') -Destination $Destination -Force
                return $true
            } finally { Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue }
        }

        $top = Join-Path ([IO.Path]::GetTempPath()) ("rpmbuild-" + [Guid]::NewGuid().ToString('N'))
        foreach ($d in 'BUILD','RPMS','SOURCES','SPECS','SRPMS') { New-Item -ItemType Directory -Path (Join-Path $top $d) -Force | Out-Null }
        try {
            $payload = Join-Path $top 'SOURCES/payload'
            New-Item -ItemType Directory -Path $payload -Force | Out-Null
            Copy-Item -Path (Join-Path $Source '*') -Destination $payload -Recurse -Force
            $lower = $ProductName.ToLowerInvariant()
            $spec = Join-Path $top 'SPECS/package.spec'
            $specText = @"
Name: $lower
Version: $Version
Release: 1
Summary: $ProductName
License: Open Source
BuildArch: $Architecture
%description
$ProductName
%install
mkdir -p %{buildroot}/opt/$lower %{buildroot}/usr/bin
cp -a $payload/. %{buildroot}/opt/$lower/
printf '#!/bin/sh\nexec /opt/$lower/$ExecutableName "`$@"\n' > %{buildroot}/usr/bin/$lower
chmod 0755 %{buildroot}/usr/bin/$lower %{buildroot}/opt/$lower/$ExecutableName
%files
/opt/$lower
/usr/bin/$lower
"@
            Write-Utf8NoBom $spec $specText
            & $rpmbuild --target $rpmTarget --define "_topdir $top" -bb $spec | Out-Host
            if ($LASTEXITCODE -ne 0) { throw 'rpmbuild failed.' }
            $rpm = Get-ChildItem (Join-Path $top 'RPMS') -Filter '*.rpm' -File -Recurse | Select-Object -First 1
            if (-not $rpm) { throw 'rpmbuild produced no RPM.' }
            Copy-Item -LiteralPath $rpm.FullName -Destination $Destination -Force
            return $true
        } finally { Remove-Item -LiteralPath $top -Recurse -Force -ErrorAction SilentlyContinue }
    } catch {
        Complete-OptionalPackageFailure 'RPM' $_.Exception.Message
        return $false
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$artifacts = [Collections.Generic.List[string]]::new()
$arch = if ($Rid.EndsWith('arm64')) { 'arm64' } else { 'x64' }
$base = "$ProductName-$Version-$Rid-$($Mode.ToLowerInvariant())"

if ($ProbeExistingArtifactsOnly) {
    if (-not $Rid.StartsWith('osx-') -or $ForceRebuildArtifacts -or -not $isMacHost) { return }
    $existingTar = Resolve-ExistingReleaseArtifactPath (Join-Path $OutputDirectory "$base.tar.gz")
    $existingDmg = Resolve-ExistingReleaseArtifactPath (Join-Path $OutputDirectory "$base.dmg")
    $existingPkg = Resolve-ExistingReleaseArtifactPath (Join-Path $OutputDirectory "$base.pkg")
    $tarReady = (Test-Path -LiteralPath $existingTar -PathType Leaf) -and (Get-Item -LiteralPath $existingTar).Length -gt 1048576
    $dmgReady = Test-MacDistributionArtifactReady $existingDmg 'dmg'
    $pkgReady = Test-MacDistributionArtifactReady $existingPkg 'pkg'
    if ($tarReady -and $dmgReady -and $pkgReady) {
        Write-Host "The complete $Rid Full macOS release already exists and passed local notarization/staple validation; skipping publish/sign/package work for this RID." -ForegroundColor Green
        $existingTar
        $existingDmg
        $existingPkg
    }
    return
}

if ($Rid.StartsWith('osx-')) {
    $app = Join-Path $OutputDirectory "$ProductName.app"
    Remove-Item -LiteralPath $app -Recurse -Force -ErrorAction SilentlyContinue
    $resources = Join-Path $app 'Contents/Resources/app'; $macos = Join-Path $app 'Contents/MacOS'
    New-Item -ItemType Directory -Path $resources,$macos -Force | Out-Null
    Copy-Item -Path (Join-Path $PayloadDirectory '*') -Destination $resources -Recurse -Force
    Remove-NonTargetMacRuntimeAssets $app $Rid
    Set-UnixExecutable (Join-Path $resources $ExecutableName)
    Write-DependencyHelper (Join-Path $resources 'install-dependencies.sh')
    New-MacLauncher (Join-Path $macos $ProductName) $ExecutableName
    $bundleIcon = New-MacBundleIcon $app
    $iconPlist = if ([string]::IsNullOrWhiteSpace($bundleIcon)) { '' } else { "<key>CFBundleIconFile</key><string>$bundleIcon</string>" }
    $launchArchitecture = if ($Rid.EndsWith('arm64')) { 'arm64' } else { 'x86_64' }
    $nativeExecutionPlist = if ($Rid.EndsWith('arm64')) { '<key>LSRequiresNativeExecution</key><true/>' } else { '' }
    $infoPlist = @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict><key>CFBundleName</key><string>$ProductName</string><key>CFBundleDisplayName</key><string>$ProductName</string><key>CFBundleIdentifier</key><string>io.github.michi0403.$($ProductName.ToLowerInvariant())</string><key>CFBundleVersion</key><string>$Version</string><key>CFBundleShortVersionString</key><string>$Version</string><key>CFBundleExecutable</key><string>$ProductName</string><key>CFBundlePackageType</key><string>APPL</string><key>LSArchitecturePriority</key><array><string>$launchArchitecture</string></array>$nativeExecutionPlist$iconPlist<key>NSHighResolutionCapable</key><true/></dict></plist>
"@
    Write-Utf8NoBom (Join-Path $app 'Contents/Info.plist') $infoPlist
    Assert-MacBundleArchitecture $app $Rid
    Sign-MacBundle $app
    $tarDestination = Join-Path $OutputDirectory "$base.tar.gz"
    $tar = if ($ForceRebuildArtifacts) { $tarDestination } else { Resolve-ExistingReleaseArtifactPath $tarDestination }
    if (-not $ForceRebuildArtifacts -and (Test-Path -LiteralPath $tar -PathType Leaf) -and (Get-Item -LiteralPath $tar).Length -gt 1048576) {
        Write-Host "Reusing existing TAR.GZ for $Rid Full: $tar" -ForegroundColor Green
    }
    else {
        $tar = $tarDestination
        Invoke-PackagingTool @('tar','--source',$app,'--output',$tar,'--root',"$ProductName.app",'--executable',"Contents/MacOS/$ProductName",'--executable',"Contents/Resources/app/$ExecutableName",'--executable','Contents/Resources/app/install-dependencies.sh')
    }
    Add-Artifact $artifacts $tar

    $dmgDestination = Join-Path $OutputDirectory "$base.dmg"
    $dmg = if ($ForceRebuildArtifacts) { $dmgDestination } else { Resolve-ExistingReleaseArtifactPath $dmgDestination }
    if (Test-MacDistributionArtifactReady $dmg 'dmg') {
        Add-Artifact $artifacts $dmg
    }
    else {
        $dmg = $dmgDestination
        if (New-Dmg $app $dmg) {
            Complete-MacDistributionArtifact $dmg 'dmg'
            Add-Artifact $artifacts $dmg
        }
    }

    $pkgDestination = Join-Path $OutputDirectory "$base.pkg"
    $pkg = if ($ForceRebuildArtifacts) { $pkgDestination } else { Resolve-ExistingReleaseArtifactPath $pkgDestination }
    if (Test-MacDistributionArtifactReady $pkg 'pkg') {
        Add-Artifact $artifacts $pkg
    }
    else {
        $pkg = $pkgDestination
        if (New-MacPkg $app $pkg) {
            Complete-MacDistributionArtifact $pkg 'pkg'
            Add-Artifact $artifacts $pkg
        }
    }
}
elseif ($Rid.StartsWith('linux-')) {
    $workingPayload = Join-Path $OutputDirectory ('.payload-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $workingPayload -Force | Out-Null
    try {
        Copy-Item -Path (Join-Path $PayloadDirectory '*') -Destination $workingPayload -Recurse -Force
        Set-UnixExecutable (Join-Path $workingPayload $ExecutableName)
        Write-DependencyHelper (Join-Path $workingPayload 'install-dependencies.sh')
        $tar = Join-Path $OutputDirectory "$base.tar.gz"
        Invoke-PackagingTool @('tar','--source',$workingPayload,'--output',$tar,'--root',"$ProductName-$Version",'--executable',$ExecutableName,'--executable','install-dependencies.sh')
        Add-Artifact $artifacts $tar
        $debArch = if ($Rid.EndsWith('arm64')) { 'arm64' } else { 'amd64' }
        $deb = Join-Path $OutputDirectory "$base.deb"
        $debArgs = @('deb','--source',$workingPayload,'--output',$deb,'--package',$ProductName.ToLowerInvariant(),'--version',$Version,'--architecture',$debArch,'--executable',$ExecutableName,'--description',"$ProductName $Mode")
        if ($DependencyPolicy -eq 'PublisherStudio') { $debArgs += @('--dependency','ffmpeg') }
        Invoke-PackagingTool $debArgs; Add-Artifact $artifacts $deb
        $rpm = Join-Path $OutputDirectory "$base.rpm"
        $rpmArch = if ($Rid.EndsWith('arm64')) { 'aarch64' } else { 'x86_64' }
        if (New-Rpm $workingPayload $rpm $rpmArch) { Add-Artifact $artifacts $rpm }
        $appImage = Join-Path $OutputDirectory "$base.AppImage"
        if (New-AppImage $workingPayload $appImage) { Add-Artifact $artifacts $appImage }
    } finally { Remove-Item -LiteralPath $workingPayload -Recurse -Force -ErrorAction SilentlyContinue }
} else { throw "Native packaging only accepts Linux/macOS RIDs: $Rid" }
$artifacts
