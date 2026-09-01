param(
    [Parameter(Mandatory)][string]$ProductName,
    [Parameter(Mandatory)][string]$ExecutableName,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$Rid,
    [Parameter(Mandatory)][ValidateSet('Full','Light')][string]$Mode,
    [Parameter(Mandatory)][string]$PayloadDirectory,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][string]$PackagingTool,
    [ValidateSet('LocalGPT','PublisherStudio')][string]$DependencyPolicy = 'LocalGPT',
    [switch]$UseContainerFallback,
    [switch]$ProvisionHomebrewTools,
    [switch]$RequireOptionalPackages,
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
    if ($rpmbuild -or -not $isMacHost -or -not $ProvisionHomebrewTools) { return $rpmbuild }
    $brew = Get-ExternalCommandPath 'brew'
    if (-not $brew) {
        Write-Warning "Homebrew is not installed, so macOS cannot provision rpmbuild automatically. TAR.GZ and DEB can still be produced."
        return $null
    }
    Write-Host "Provisioning Homebrew rpm for Linux RPM materialization..." -ForegroundColor Cyan
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
function Write-DependencyHelper([string]$Destination) {
    if ($DependencyPolicy -eq 'LocalGPT') {
@'
#!/bin/sh
set -eu
printf '%s\n' 'Optional local AI runtime setup:' '  1) Ollama' '  2) LM Studio / llmster' '  3) Skip'
printf 'Choice [3]: '
read choice || choice=3
case "${choice:-3}" in
  1) printf '%s\n' 'Install Ollama from its official distribution, then configure the provider in LocalGPT.' ;;
  2) printf '%s\n' 'Install LM Studio / llmster from its official distribution, then configure the provider in LocalGPT.' ;;
  *) printf '%s\n' 'Skipped. LocalGPT does not redistribute an AI runtime.' ;;
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
function Sign-MacBundleAdHoc([string]$AppPath) {
    if (-not $isMacHost) { return }
    $codesign = Get-ExternalCommandPath 'codesign'
    if (-not $codesign) {
        Write-Warning "codesign is unavailable; $ProductName.app will remain unsigned."
        return
    }
    & $codesign --force --deep --sign - --timestamp=none $AppPath 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) {
        throw "Ad-hoc codesign failed for $AppPath"
    }
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
mkdir -p "$LOG_DIR"

read_endpoint() {
  for f in \
    "$HOME/Library/Application Support/$PRODUCT/runtime/server.json" \
    "$HOME/.local/share/$PRODUCT/runtime/server.json"
  do
    [ -f "$f" ] || continue
    pid=$(sed -nE 's/.*"[Pp]rocess[Ii]d"[[:space:]]*:[[:space:]]*([0-9]+).*/\1/p' "$f" | head -n 1)
    if [ -n "${pid:-}" ] && ! kill -0 "$pid" 2>/dev/null; then
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
show_failure() {
  reason="$1"
  printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') $reason" >>"$LOG_FILE"
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

if url=$(read_endpoint 2>/dev/null); then
  /usr/bin/open "$url" >/dev/null 2>&1 || true
  exit 0
fi

if [ ! -x "$BIN" ]; then
  show_failure "The packaged application executable is missing or is not executable: $BIN"
  exit 1
fi

cd "$APP" || { show_failure "The packaged application directory could not be opened: $APP"; exit 1; }
printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') Starting $PRODUCT from $BIN" >>"$LOG_FILE"
"$BIN" >>"$LOG_FILE" 2>&1 &
pid=$!

i=0
while [ $i -lt 120 ]; do
  if url=$(read_endpoint 2>/dev/null); then
    printf '%s\n' "$(date '+%Y-%m-%d %H:%M:%S') $PRODUCT ready at $url (pid $pid)" >>"$LOG_FILE"
    /usr/bin/open "$url" >/dev/null 2>&1 || true
    exit 0
  fi
  if ! kill -0 "$pid" 2>/dev/null; then
    wait "$pid" 2>/dev/null
    code=$?
    show_failure "$PRODUCT exited during startup with code $code."
    if [ "$code" -eq 0 ]; then exit 1; fi
    exit "$code"
  fi
  i=$((i+1))
  sleep 0.25
done

show_failure "$PRODUCT is still running but did not publish its local web address within 30 seconds."
exit 1
'@
    Write-Utf8NoBom $Destination ($template.Replace('__PRODUCT__', $ProductName).Replace('__EXECUTABLE__', $BinaryRelativePath))
    Set-UnixExecutable $Destination
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

        Write-Host "Created and verified headless DMG with $appName and Applications alias: $Destination" -ForegroundColor Green
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
        & $pkgbuild `
            --root $pkgRoot `
            --identifier $identifier `
            --version $Version `
            --install-location '/' `
            --ownership recommended `
            $Destination 2>&1 | ForEach-Object { Write-Host $_ }
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
HERE=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
exec "$HERE/__EXECUTABLE__" "$@"
'@
        Write-Utf8NoBom $appRun ($appRunTemplate.Replace('__EXECUTABLE__', $ExecutableName))
        Set-UnixExecutable $appRun
        Set-UnixExecutable (Join-Path $appDir $ExecutableName)
        $desktop = Join-Path $appDir "$ProductName.desktop"
        $iconLine = ''
        if (-not [string]::IsNullOrWhiteSpace($MacIconSource) -and (Test-Path -LiteralPath $MacIconSource -PathType Leaf)) {
            Copy-Item -LiteralPath $MacIconSource -Destination (Join-Path $appDir "$ProductName.png") -Force
            $iconLine = "Icon=$ProductName`n"
        }
        Write-Utf8NoBom $desktop "[Desktop Entry]`nType=Application`nName=$ProductName`nExec=$ExecutableName`n${iconLine}Terminal=false`nCategories=Utility;`n"
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
            $hint = if ($brew) { " Install it with 'brew install rpm', or pass -ProvisionHomebrewTools to let this build invoke Homebrew." } else { ' Install Homebrew and then install the rpm formula, or use a Linux builder.' }
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

if ($Rid.StartsWith('osx-')) {
    $app = Join-Path $OutputDirectory "$ProductName.app"
    Remove-Item -LiteralPath $app -Recurse -Force -ErrorAction SilentlyContinue
    $resources = Join-Path $app 'Contents/Resources/app'; $macos = Join-Path $app 'Contents/MacOS'
    New-Item -ItemType Directory -Path $resources,$macos -Force | Out-Null
    Copy-Item -Path (Join-Path $PayloadDirectory '*') -Destination $resources -Recurse -Force
    Set-UnixExecutable (Join-Path $resources $ExecutableName)
    Write-DependencyHelper (Join-Path $resources 'install-dependencies.sh')
    New-MacLauncher (Join-Path $macos $ProductName) $ExecutableName
    $bundleIcon = New-MacBundleIcon $app
    $iconPlist = if ([string]::IsNullOrWhiteSpace($bundleIcon)) { '' } else { "<key>CFBundleIconFile</key><string>$bundleIcon</string>" }
    $infoPlist = @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict><key>CFBundleName</key><string>$ProductName</string><key>CFBundleDisplayName</key><string>$ProductName</string><key>CFBundleIdentifier</key><string>io.github.michi0403.$($ProductName.ToLowerInvariant())</string><key>CFBundleVersion</key><string>$Version</string><key>CFBundleShortVersionString</key><string>$Version</string><key>CFBundleExecutable</key><string>$ProductName</string><key>CFBundlePackageType</key><string>APPL</string>$iconPlist<key>NSHighResolutionCapable</key><true/></dict></plist>
"@
    Write-Utf8NoBom (Join-Path $app 'Contents/Info.plist') $infoPlist
    Sign-MacBundleAdHoc $app
    $tar = Join-Path $OutputDirectory "$base.tar.gz"
    Invoke-PackagingTool @('tar','--source',$app,'--output',$tar,'--root',"$ProductName.app",'--executable',"Contents/MacOS/$ProductName",'--executable',"Contents/Resources/app/$ExecutableName",'--executable','Contents/Resources/app/install-dependencies.sh')
    Add-Artifact $artifacts $tar
    $dmg = Join-Path $OutputDirectory "$base.dmg"
    if (New-Dmg $app $dmg) { Add-Artifact $artifacts $dmg }
    $pkg = Join-Path $OutputDirectory "$base.pkg"
    if (New-MacPkg $app $pkg) { Add-Artifact $artifacts $pkg }
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
