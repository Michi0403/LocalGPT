param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$TargetRuntimeIdentifier = '',
    [ValidateSet('Developer','Release')][string]$Mode = 'Developer'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Runtime images share the driver protocol of this pinned hluk release. Do not fetch "latest".
$version = 'v0.19.1'
$runtimeIds = @('python', 'dotnet-jit', 'powershell')
$runtimeTemplates = @{ python = 'python'; 'dotnet-jit' = 'dotnet'; powershell = 'powershell' }
$repo = 'hyperlight-dev/hyperlight-unikraft'
$headers = @{ 'User-Agent' = 'LocalGPT-Hyperlight-Guest-Provisioner'; 'Accept' = 'application/vnd.github+json' }

function Get-HostRuntimeIdentifier {
    $architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) { return "win-$architecture" }
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)) { return "osx-$architecture" }
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Linux)) { return "linux-$architecture" }
    return "unknown-$architecture"
}

function Assert-PackageComplete([string]$Root) {
    $manifest = Join-Path $Root 'hyperlight/runtimes/manifest.json'
    if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) { return $false }
    try {
        $metadata = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
        if ($metadata.Version -ne $version -or $metadata.TargetRuntimeIdentifier -ne $effectiveTarget) { return $false }
        $binary = Join-Path $Root ('hyperlight/bin/' + $executableName)
        if (-not (Test-Path -LiteralPath $binary -PathType Leaf) -or (Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash -ne $metadata.BinarySha256) { return $false }
        foreach ($id in $runtimeIds) {
            $image = Join-Path $Root ('hyperlight/runtimes/' + $id + '.cpio')
            if (-not (Test-Path -LiteralPath $image -PathType Leaf)) { return $false }
            if ((Get-FileHash -LiteralPath $image -Algorithm SHA256).Hash -ne $metadata.Images.$id) { return $false }
        }
        return $true
    } catch { return $false }
}

$hostRid = Get-HostRuntimeIdentifier
$effectiveTarget = if ([string]::IsNullOrWhiteSpace($TargetRuntimeIdentifier)) { $hostRid } else { $TargetRuntimeIdentifier.Trim() }
$executableName = if ($effectiveTarget.StartsWith('win-', [StringComparison]::Ordinal)) { 'hluk.exe' } else { 'hluk' }
$unsupported = @('win-x86', 'osx-x64')
if ($effectiveTarget -in $unsupported) {
    Write-Warning "Hyperlight Unikraft $version has no supported $effectiveTarget guest host. LocalGPT remains buildable, but sandbox execution is unavailable for this RID; no native execution fallback."
    return
}
if ($effectiveTarget -notin @('win-x64','win-arm64','linux-x64','linux-arm64','osx-arm64')) {
    Write-Warning "Unrecognized Hyperlight guest target $effectiveTarget. LocalGPT remains buildable; no unverified sandbox binary is installed."
    return
}
if ($effectiveTarget -ne $hostRid) {
    $message = "Hyperlight guest rootfs and CLI provisioning for $effectiveTarget must run on a native $effectiveTarget build host (current host $hostRid)."
    Write-Warning "$message Cross-target LocalGPT publishing continues with sandbox unavailable rather than altering the existing release toolchain."
    return
}
$root = [IO.Path]::GetFullPath($OutputDirectory)
if (Assert-PackageComplete $root) {
    Write-Host "Hyperlight $version Python, .NET and PowerShell guest images already verified in $root"
    return
}

$cacheBase = if ($env:LOCALAPPDATA) { Join-Path $env:LOCALAPPDATA 'LocalGPT/hyperlight-build-cache' }
    elseif ($env:XDG_CACHE_HOME) { Join-Path $env:XDG_CACHE_HOME 'LocalGPT/hyperlight-build-cache' }
    else { Join-Path ([Environment]::GetFolderPath('UserProfile')) '.cache/LocalGPT/hyperlight-build-cache' }
$cacheBase = [IO.Path]::GetFullPath($cacheBase)
$toolDir = Join-Path $cacheBase ('tools/' + $version + '/' + $effectiveTarget)
$tool = Join-Path $toolDir $executableName
$toolReceipt = Join-Path $toolDir 'hluk.sha256'
$downloadDir = Join-Path $cacheBase ('downloads/' + $version)
$cachePath = Join-Path $cacheBase ('images/' + $version + '/' + $effectiveTarget)
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('localgpt-hluk-provision-' + [guid]::NewGuid().ToString('N'))
$oldCache = [Environment]::GetEnvironmentVariable('HLUK_CACHE_DIR')
try {
    [IO.Directory]::CreateDirectory($toolDir) | Out-Null
    [IO.Directory]::CreateDirectory($downloadDir) | Out-Null
    [IO.Directory]::CreateDirectory($cachePath) | Out-Null
    [IO.Directory]::CreateDirectory($temporary) | Out-Null
    # Do not execute remotely downloaded installer scripts. Select a pinned release asset,
    # verify its SHA-256 against the upstream release inventory, then extract locally.
    $verifiedTool = (Test-Path -LiteralPath $tool -PathType Leaf) -and (Test-Path -LiteralPath $toolReceipt -PathType Leaf)
    if ($verifiedTool) {
        $verifiedTool = ((Get-FileHash -LiteralPath $tool -Algorithm SHA256).Hash -eq ((Get-Content -LiteralPath $toolReceipt -Raw).Trim()))
    }
    if (-not $verifiedTool) {
        $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/tags/$version" -Headers $headers
        $target = switch ($effectiveTarget) {
            'win-x64' { 'x86_64-pc-windows-msvc' }
            'win-arm64' { 'aarch64-pc-windows-msvc' }
            'linux-x64' { 'x86_64-unknown-linux-musl' }
            'linux-arm64' { 'aarch64-unknown-linux-musl' }
            'osx-arm64' { 'aarch64-apple-darwin' }
        }
        $baseName = "hluk-$version-$target"
        $matches = @($release.assets | Where-Object { $_.name -in @("$baseName.tar.gz", "$baseName.zip") })
        if ($matches.Count -ne 1) { throw "No unique, supported $effectiveTarget hluk release binary asset found for $version." }
        $asset = $matches[0]
        $checksumFile = Join-Path $downloadDir 'SHA256SUMS'
        $archive = Join-Path $downloadDir $asset.name
        Write-Host "Downloading pinned Hyperlight $version binary for $effectiveTarget; SHA-256 verification is mandatory."
        Invoke-WebRequest -Uri $asset.browser_download_url -Headers $headers -OutFile $archive -UseBasicParsing
        $checksumAssets = @($release.assets | Where-Object { $_.name -eq 'SHA256SUMS' })
        $expected = $null
        if ($checksumAssets.Count -eq 1) {
            Invoke-WebRequest -Uri $checksumAssets[0].browser_download_url -Headers $headers -OutFile $checksumFile -UseBasicParsing
            $line = @(Get-Content -LiteralPath $checksumFile | Where-Object { $_ -match ('\s\*?' + [regex]::Escape($asset.name) + '$') })
            if ($line.Count -ne 1) { throw "SHA256SUMS does not contain a unique checksum for $($asset.name)." }
            $checksumMatch = [regex]::Match($line[0], '^([a-fA-F0-9]{64})\s+')
            if (-not $checksumMatch.Success) { throw "SHA256SUMS contains an invalid checksum for $($asset.name)." }
            $expected = $checksumMatch.Groups[1].Value.ToUpperInvariant()
        }
        else {
            $digestProperty = $asset.PSObject.Properties['digest']
            if ($null -ne $digestProperty -and $digestProperty.Value) {
                $digestMatch = [regex]::Match([string]$digestProperty.Value, '^sha256:([0-9a-fA-F]{64})$')
                if ($digestMatch.Success) { $expected = $digestMatch.Groups[1].Value.ToUpperInvariant() }
            }
        }
        if (-not $expected) { throw "Neither the upstream SHA256SUMS nor a release asset SHA-256 digest is available for $($asset.name)." }
        if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $expected) { throw "SHA-256 verification failed for $($asset.name)." }
        if ($asset.name.EndsWith('.zip', [StringComparison]::Ordinal)) {
            Expand-Archive -LiteralPath $archive -DestinationPath $temporary -Force
        } else {
            & tar -xzf $archive -C $temporary
            if ($LASTEXITCODE -ne 0) { throw 'Could not extract the verified Hyperlight release archive.' }
        }
        $extracted = @(Get-ChildItem -LiteralPath $temporary -Recurse -File | Where-Object { $_.Name -eq $executableName })
        if ($extracted.Count -ne 1) { throw 'Verified Hyperlight archive did not contain exactly one expected executable.' }
        Copy-Item -LiteralPath $extracted[0].FullName -Destination $tool -Force
        if (-not $effectiveTarget.StartsWith('win-', [StringComparison]::Ordinal)) { & chmod 755 $tool; if ($LASTEXITCODE -ne 0) { throw 'chmod failed on the Hyperlight executable.' } }
        if ($effectiveTarget -eq 'osx-arm64') {
            $entitlements = Join-Path $temporary 'hyperlight-entitlements.plist'
            [IO.File]::WriteAllText($entitlements, '<?xml version="1.0" encoding="UTF-8"?><plist version="1.0"><dict><key>com.apple.security.hypervisor</key><true/></dict></plist>')
            & codesign -f -s - --entitlements $entitlements $tool
            if ($LASTEXITCODE -ne 0) { throw 'Hypervisor.framework entitlement signing of hluk failed.' }
        }
        (Get-FileHash -LiteralPath $tool -Algorithm SHA256).Hash | Set-Content -LiteralPath $toolReceipt -Encoding Ascii
    }
    $toolVersion = (& $tool --version 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0 -or $toolVersion -notmatch [regex]::Escape($version.TrimStart('v'))) {
        throw "Installed hluk must match the pinned driver protocol $version; found $toolVersion"
    }
    [Environment]::SetEnvironmentVariable('HLUK_CACHE_DIR', $cachePath)
    foreach ($id in $runtimeIds) {
        Write-Host "Ensuring pinned $id Hyperlight guest runtime image is available."
        $rootfs = Join-Path $cachePath ('rootfs/ghcr.io/hyperlight-dev/hyperlight-unikraft/' + $id + '/initrd-' + $version + '.cpio')
        if (-not (Test-Path -LiteralPath $rootfs -PathType Leaf)) {
            $template = $runtimeTemplates[$id]
            $projectName = 'provision-' + $id
            Push-Location $temporary
            try {
                & $tool init $projectName --template $template --image-version ($version.TrimStart('v'))
                if ($LASTEXITCODE -ne 0) { throw "hluk init did not download the $id guest runtime image." }
            } finally { Pop-Location }
        }
        if (-not (Test-Path -LiteralPath $rootfs -PathType Leaf) -or (Get-Item -LiteralPath $rootfs).Length -lt 1024) {
            throw "Hyperlight $id rootfs could not be located in the verified OCI cache at $rootfs."
        }
    }
    $binOutput = Join-Path $root 'hyperlight/bin'
    $imageOutput = Join-Path $root 'hyperlight/runtimes'
    [IO.Directory]::CreateDirectory($binOutput) | Out-Null
    [IO.Directory]::CreateDirectory($imageOutput) | Out-Null
    Copy-Item -LiteralPath $tool -Destination (Join-Path $binOutput $executableName) -Force
    $imageHashes = @{}
    foreach ($id in $runtimeIds) {
        $rootfs = Join-Path $cachePath ('rootfs/ghcr.io/hyperlight-dev/hyperlight-unikraft/' + $id + '/initrd-' + $version + '.cpio')
        $destination = Join-Path $imageOutput ($id + '.cpio')
        Copy-Item -LiteralPath $rootfs -Destination $destination -Force
        $imageHashes[$id] = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
    }
    $metadata = @{
        Version = $version
        TargetRuntimeIdentifier = $effectiveTarget
        BinarySha256 = (Get-FileHash -LiteralPath (Join-Path $binOutput $executableName) -Algorithm SHA256).Hash
        Images = $imageHashes
    }
    $manifestPath = Join-Path $imageOutput 'manifest.json'
    $metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
    Write-Host "Provisioned and verified Python, .NET JIT and PowerShell Hyperlight $version runtimes for $effectiveTarget in $root"
}
catch {
    $message = "LocalGPT Hyperlight guest provisioning did not complete: $($_.Exception.Message) No untrusted source executes on the host."
    if ($Mode -eq 'Release') { throw $message }
    Write-Warning "$message The normal development build remains available, but the sandbox is not provisioned."
}
finally {
    [Environment]::SetEnvironmentVariable('HLUK_CACHE_DIR', $oldCache)
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force -Recurse -ErrorAction SilentlyContinue }
}
