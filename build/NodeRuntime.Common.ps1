Set-StrictMode -Version Latest

function Get-LocalGptNodeHostDescriptor {
    $runningOnWindows = [IO.Path]::DirectorySeparatorChar -eq '\'
    $platform = if ($runningOnWindows) { 'win' } else { 'linux' }

    if (-not $runningOnWindows) {
        try {
            if ([Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::OSX)) {
                $platform = 'darwin'
            }
            elseif ([Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Linux)) {
                $platform = 'linux'
            }
        }
        catch {
            if (Test-Path -LiteralPath '/System/Library/CoreServices/SystemVersion.plist' -PathType Leaf) {
                $platform = 'darwin'
            }
        }
    }

    $architecture = ''
    try {
        $architecture = [Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
    }
    catch { }

    if ([string]::IsNullOrWhiteSpace($architecture)) {
        if ($runningOnWindows) {
            $architecture = ([string]$env:PROCESSOR_ARCHITECTURE).ToLowerInvariant()
        }
        else {
            $uname = Get-Command uname -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($null -ne $uname) {
                $unamePath = if (-not [string]::IsNullOrWhiteSpace([string]$uname.Source)) { [string]$uname.Source } else { [string]$uname.Path }
                try { $architecture = ([string](& $unamePath -m 2>$null)).Trim().ToLowerInvariant() }
                catch { }
            }
        }
    }

    switch -Regex ($architecture) {
        '^(arm64|aarch64)$' { $nodeArchitecture = 'arm64'; break }
        '^(x64|amd64|x86_64)$' { $nodeArchitecture = 'x64'; break }
        '^(x86|i[3-6]86)$' {
            if ($platform -ne 'win') { throw "Node.js bootstrap does not support 32-bit $platform hosts." }
            $nodeArchitecture = 'x86'
            break
        }
        default { throw "Unsupported host architecture '$architecture' for the LocalGPT Node.js bootstrap." }
    }

    return [pscustomobject]@{
        Platform = $platform
        Architecture = $nodeArchitecture
        RunningOnWindows = $runningOnWindows
    }
}

function Get-LocalGptDocumentationToolCacheRoot {
    param(
        [AllowEmptyString()][string]$FallbackRoot = ''
    )

    $sharedCacheRoot = [string]$env:FUTURE2_DOCUMENTATION_CACHE_ROOT
    if (-not [string]::IsNullOrWhiteSpace($sharedCacheRoot)) {
        return Join-Path ([IO.Path]::GetFullPath($sharedCacheRoot)) 'LocalGPT/DocumentationTools'
    }

    $localApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    if (-not [string]::IsNullOrWhiteSpace($localApplicationData)) {
        return Join-Path $localApplicationData 'LocalGPT/DocumentationTools'
    }

    $homePath = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
    if (-not [string]::IsNullOrWhiteSpace($homePath)) {
        return Join-Path $homePath '.local/share/LocalGPT/DocumentationTools'
    }

    if (-not [string]::IsNullOrWhiteSpace($FallbackRoot)) {
        return Join-Path ([IO.Path]::GetFullPath($FallbackRoot)) 'runtime'
    }

    throw 'Unable to determine a writable per-user cache directory for LocalGPT documentation tools.'
}

function Get-LocalGptNodeDistributionDescriptor {
    param(
        [Parameter(Mandatory)][string]$Version
    )

    $hostInfo = Get-LocalGptNodeHostDescriptor
    $rootName = "node-v$Version-$($hostInfo.Platform)-$($hostInfo.Architecture)"
    switch ($hostInfo.Platform) {
        'win' {
            $archiveName = "$rootName.zip"
            $archiveFormat = 'zip'
            $executableRelativePath = 'node.exe'
        }
        'darwin' {
            $archiveName = "$rootName.tar.gz"
            $archiveFormat = 'tar.gz'
            $executableRelativePath = 'bin/node'
        }
        'linux' {
            $archiveName = "$rootName.tar.gz"
            $archiveFormat = 'tar.gz'
            $executableRelativePath = 'bin/node'
        }
        default {
            throw "Unsupported host platform '$($hostInfo.Platform)' for the LocalGPT Node.js bootstrap."
        }
    }

    return [pscustomobject]@{
        Platform = $hostInfo.Platform
        Architecture = $hostInfo.Architecture
        RunningOnWindows = $hostInfo.RunningOnWindows
        RootName = $rootName
        ArchiveName = $archiveName
        ArchiveFormat = $archiveFormat
        ExecutableRelativePath = $executableRelativePath
    }
}

function Get-LocalGptNodeInfo {
    param(
        [Parameter(Mandatory)][string]$Path,
        [int]$MinimumMajor = 20,
        [bool]$Provisioned = $false,
        [AllowEmptyString()][string]$Platform = '',
        [AllowEmptyString()][string]$Architecture = ''
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    try {
        $versionOutput = @(& $Path --version 2>$null)
        $exitCode = [int]$LASTEXITCODE
        if ($exitCode -ne 0 -or $versionOutput.Count -eq 0) { return $null }

        $versionText = ([string]($versionOutput | Select-Object -First 1)).Trim()
        $versionMatch = [regex]::Match($versionText, '^v?(?<major>\d+)\.')
        if (-not $versionMatch.Success) { return $null }

        $major = [int]$versionMatch.Groups['major'].Value
        if ($major -lt $MinimumMajor) { return $null }

        return [pscustomobject]@{
            Path = [IO.Path]::GetFullPath($Path)
            Version = $versionText
            Major = $major
            Provisioned = $Provisioned
            Platform = $Platform
            Architecture = $Architecture
        }
    }
    catch {
        return $null
    }
}

function Set-LocalGptNodeProcessEnvironment {
    param(
        [Parameter(Mandatory)][psobject]$NodeInfo
    )

    [Environment]::SetEnvironmentVariable('PLAYWRIGHT_NODEJS_PATH', [string]$NodeInfo.Path, [EnvironmentVariableTarget]::Process)
    $nodeDirectory = Split-Path -Parent ([string]$NodeInfo.Path)
    $pathEntries = @(([string][Environment]::GetEnvironmentVariable('PATH', [EnvironmentVariableTarget]::Process)) -split [IO.Path]::PathSeparator)
    $alreadyPresent = $false
    foreach ($entry in $pathEntries) {
        if ([string]::IsNullOrWhiteSpace($entry)) { continue }
        try {
            if ([string]::Equals([IO.Path]::GetFullPath($entry), [IO.Path]::GetFullPath($nodeDirectory), [StringComparison]::OrdinalIgnoreCase)) {
                $alreadyPresent = $true
                break
            }
        }
        catch { }
    }
    if (-not $alreadyPresent) {
        $currentPath = [string][Environment]::GetEnvironmentVariable('PATH', [EnvironmentVariableTarget]::Process)
        $updatedPath = if ([string]::IsNullOrWhiteSpace($currentPath)) { $nodeDirectory } else { "$nodeDirectory$([IO.Path]::PathSeparator)$currentPath" }
        [Environment]::SetEnvironmentVariable('PATH', $updatedPath, [EnvironmentVariableTarget]::Process)
    }
}

function Find-LocalGptNodeRuntime {
    param(
        [Parameter(Mandatory)][string]$CacheRoot,
        [Parameter(Mandatory)][string]$Version,
        [int]$MinimumMajor = 20
    )

    $distribution = Get-LocalGptNodeDistributionDescriptor -Version $Version
    $provisionedRoot = Join-Path $CacheRoot $distribution.RootName
    $provisionedExecutable = Join-Path $provisionedRoot $distribution.ExecutableRelativePath
    $candidates = [System.Collections.Generic.List[string]]::new()

    if (-not [string]::IsNullOrWhiteSpace($env:PLAYWRIGHT_NODEJS_PATH)) {
        $candidates.Add($env:PLAYWRIGHT_NODEJS_PATH)
    }

    $nodeCommand = Get-Command node -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $nodeCommand) {
        $commandPath = if (-not [string]::IsNullOrWhiteSpace([string]$nodeCommand.Source)) { [string]$nodeCommand.Source } else { [string]$nodeCommand.Path }
        if (-not [string]::IsNullOrWhiteSpace($commandPath)) { $candidates.Add($commandPath) }
    }

    if ($distribution.Platform -eq 'win') {
        if (-not [string]::IsNullOrWhiteSpace($env:ProgramFiles)) { $candidates.Add((Join-Path $env:ProgramFiles 'nodejs/node.exe')) }
        $programFilesX86 = [Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
        if (-not [string]::IsNullOrWhiteSpace($programFilesX86)) { $candidates.Add((Join-Path $programFilesX86 'nodejs/node.exe')) }
        $localApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
        if (-not [string]::IsNullOrWhiteSpace($localApplicationData)) { $candidates.Add((Join-Path $localApplicationData 'Programs/nodejs/node.exe')) }
    }
    elseif ($distribution.Platform -eq 'darwin') {
        $candidates.Add('/opt/homebrew/bin/node')
        $candidates.Add('/usr/local/bin/node')
    }
    else {
        $candidates.Add('/usr/local/bin/node')
        $candidates.Add('/usr/bin/node')
        $homePath = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
        if (-not [string]::IsNullOrWhiteSpace($homePath)) { $candidates.Add((Join-Path $homePath '.local/bin/node')) }
    }
    $candidates.Add($provisionedExecutable)

    $visited = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        try { $fullPath = [IO.Path]::GetFullPath($candidate) }
        catch { continue }
        if (-not $visited.Add($fullPath)) { continue }
        $isProvisioned = [string]::Equals($fullPath, [IO.Path]::GetFullPath($provisionedExecutable), [StringComparison]::OrdinalIgnoreCase)
        $nodeInfo = Get-LocalGptNodeInfo -Path $fullPath -MinimumMajor $MinimumMajor -Provisioned $isProvisioned -Platform $distribution.Platform -Architecture $distribution.Architecture
        if ($null -ne $nodeInfo) { return $nodeInfo }
    }

    return $null
}

function Get-LocalGptNodeArchiveHash {
    param(
        [Parameter(Mandatory)][string]$ManifestPath,
        [Parameter(Mandatory)][string]$ArchiveName
    )

    if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) { return $null }
    $escapedArchive = [regex]::Escape($ArchiveName)
    foreach ($line in @(Get-Content -LiteralPath $ManifestPath -ErrorAction Stop)) {
        $match = [regex]::Match([string]$line, "^(?<hash>[0-9a-fA-F]{64})\s+\*?$escapedArchive\s*$")
        if ($match.Success) { return $match.Groups['hash'].Value.ToLowerInvariant() }
    }
    return $null
}

function Install-LocalGptNodeRuntime {
    param(
        [Parameter(Mandatory)][string]$CacheRoot,
        [Parameter(Mandatory)][string]$Version,
        [int]$MinimumMajor = 20
    )

    $distribution = Get-LocalGptNodeDistributionDescriptor -Version $Version
    New-Item -ItemType Directory -Path $CacheRoot -Force | Out-Null

    $provisionedRoot = Join-Path $CacheRoot $distribution.RootName
    $provisionedExecutable = Join-Path $provisionedRoot $distribution.ExecutableRelativePath
    $existing = Get-LocalGptNodeInfo -Path $provisionedExecutable -MinimumMajor $MinimumMajor -Provisioned $true -Platform $distribution.Platform -Architecture $distribution.Architecture
    if ($null -ne $existing) {
        Set-LocalGptNodeProcessEnvironment -NodeInfo $existing
        return $existing
    }

    $archivePath = Join-Path $CacheRoot $distribution.ArchiveName
    $archiveDownloadPath = "$archivePath.download"
    $manifestName = "node-v$Version-SHASUMS256.txt"
    $manifestPath = Join-Path $CacheRoot $manifestName
    $manifestDownloadPath = "$manifestPath.download"
    $extractRoot = Join-Path $CacheRoot (".node-v$Version-$($distribution.Platform)-$($distribution.Architecture)-extract")
    $releaseRoot = "https://nodejs.org/download/release/v$Version"

    $expectedHash = Get-LocalGptNodeArchiveHash -ManifestPath $manifestPath -ArchiveName $distribution.ArchiveName
    if ([string]::IsNullOrWhiteSpace($expectedHash)) {
        Write-Host "Downloading the official Node.js v$Version checksum manifest for $($distribution.Platform)-$($distribution.Architecture)..." -ForegroundColor Cyan
        Remove-Item -LiteralPath $manifestDownloadPath -Force -ErrorAction SilentlyContinue
        try {
            try { [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12 } catch { }
            Invoke-WebRequest -Uri "$releaseRoot/SHASUMS256.txt" -OutFile $manifestDownloadPath -UseBasicParsing
            Move-Item -LiteralPath $manifestDownloadPath -Destination $manifestPath -Force
        }
        catch {
            Remove-Item -LiteralPath $manifestDownloadPath -Force -ErrorAction SilentlyContinue
            throw "Node.js v$Version checksum manifest could not be downloaded: $($_.Exception.Message)"
        }
        $expectedHash = Get-LocalGptNodeArchiveHash -ManifestPath $manifestPath -ArchiveName $distribution.ArchiveName
    }

    if ([string]::IsNullOrWhiteSpace($expectedHash)) {
        throw "The official Node.js v$Version checksum manifest does not contain '$($distribution.ArchiveName)'."
    }

    if (Test-Path -LiteralPath $archivePath -PathType Leaf) {
        $archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
        if (-not [string]::Equals($archiveHash, $expectedHash, [StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $archivePath -Force
        }
    }

    if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) {
        Write-Host "Node.js $MinimumMajor+ was not found. Downloading verified Node.js v$Version ($($distribution.Platform)-$($distribution.Architecture)) for LocalGPT documentation tooling..." -ForegroundColor Cyan
        Remove-Item -LiteralPath $archiveDownloadPath -Force -ErrorAction SilentlyContinue
        try {
            Invoke-WebRequest -Uri "$releaseRoot/$($distribution.ArchiveName)" -OutFile $archiveDownloadPath -UseBasicParsing
            $downloadHash = (Get-FileHash -LiteralPath $archiveDownloadPath -Algorithm SHA256).Hash.ToLowerInvariant()
            if (-not [string]::Equals($downloadHash, $expectedHash, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Node.js archive checksum mismatch. Expected $expectedHash but received $downloadHash."
            }
            Move-Item -LiteralPath $archiveDownloadPath -Destination $archivePath -Force
        }
        catch {
            Remove-Item -LiteralPath $archiveDownloadPath -Force -ErrorAction SilentlyContinue
            throw "Node.js v$Version could not be provisioned for LocalGPT documentation tooling: $($_.Exception.Message)"
        }
    }

    Remove-Item -LiteralPath $extractRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null
    try {
        if ($distribution.ArchiveFormat -eq 'zip') {
            Expand-Archive -LiteralPath $archivePath -DestinationPath $extractRoot -Force
        }
        else {
            $tarCommand = Get-Command tar -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($null -eq $tarCommand) {
                throw "The 'tar' command is required to extract $($distribution.ArchiveName) on $($distribution.Platform)."
            }
            $tarArguments = if ($distribution.ArchiveFormat -eq 'tar.gz') {
                @('-xzf', $archivePath, '-C', $extractRoot)
            }
            else {
                @('-xJf', $archivePath, '-C', $extractRoot)
            }
            $tarPath = if (-not [string]::IsNullOrWhiteSpace([string]$tarCommand.Source)) { [string]$tarCommand.Source } else { [string]$tarCommand.Path }
            & $tarPath @tarArguments
            if ($LASTEXITCODE -ne 0) { throw "tar exited with code $LASTEXITCODE while extracting $($distribution.ArchiveName)." }
        }

        $expandedRoot = Join-Path $extractRoot $distribution.RootName
        $expandedNode = Join-Path $expandedRoot $distribution.ExecutableRelativePath
        if (-not (Test-Path -LiteralPath $expandedNode -PathType Leaf)) {
            throw "The verified Node.js archive did not contain '$($distribution.ExecutableRelativePath)'."
        }

        Remove-Item -LiteralPath $provisionedRoot -Recurse -Force -ErrorAction SilentlyContinue
        Move-Item -LiteralPath $expandedRoot -Destination $provisionedRoot -Force
    }
    finally {
        Remove-Item -LiteralPath $extractRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    if (-not $distribution.RunningOnWindows) {
        $chmod = Get-Command chmod -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -ne $chmod) {
            $chmodPath = if (-not [string]::IsNullOrWhiteSpace([string]$chmod.Source)) { [string]$chmod.Source } else { [string]$chmod.Path }
            & $chmodPath '+x' $provisionedExecutable 2>$null
        }
    }

    $installed = Get-LocalGptNodeInfo -Path $provisionedExecutable -MinimumMajor $MinimumMajor -Provisioned $true -Platform $distribution.Platform -Architecture $distribution.Architecture
    if ($null -eq $installed) {
        throw "The provisioned Node.js runtime could not be executed: $provisionedExecutable"
    }

    Set-LocalGptNodeProcessEnvironment -NodeInfo $installed
    return $installed
}

function Resolve-LocalGptNodeRuntime {
    param(
        [Parameter(Mandatory)][string]$CacheRoot,
        [Parameter(Mandatory)][string]$Version,
        [int]$MinimumMajor = 20,
        [int]$MaximumPreferredMajor = 22,
        [switch]$AllowProvisioning,
        [switch]$PreferCompatibleLts
    )

    $nodeInfo = Find-LocalGptNodeRuntime -CacheRoot $CacheRoot -Version $Version -MinimumMajor $MinimumMajor

    # Reuse any already-installed Node.js runtime that satisfies the actual minimum.
    # PreferCompatibleLts must never mean "install a second Node.js" merely because the
    # developer has a newer supported runtime on PATH. Provision only when no usable Node exists.
    if ($null -ne $nodeInfo) {
        Set-LocalGptNodeProcessEnvironment -NodeInfo $nodeInfo
        if ($PreferCompatibleLts -and $nodeInfo.Major -gt $MaximumPreferredMajor) {
            Write-Host "Using existing Node.js $($nodeInfo.Version); no additional Node.js runtime will be provisioned." -ForegroundColor DarkGray
        }
        return $nodeInfo
    }

    if ($AllowProvisioning) {
        return Install-LocalGptNodeRuntime -CacheRoot $CacheRoot -Version $Version -MinimumMajor $MinimumMajor
    }

    return $null
}
