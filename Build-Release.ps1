param(
    [ValidateSet("all", "win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")]
    [string]$Runtime = "all",
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [string]$WireProtocolVersion = "2.1.1",
    [string]$WireProtocolPackageUrl = "",
    [switch]$UseBundledWireProtocolPackage,
    [switch]$IncludeWindowsWrapper,
    [switch]$AllowMissingDevExpressLicense
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $root 'build/Assert-PowerShellCompatibility.ps1')
& (Join-Path $root 'build/Initialize-BuildPrerequisites.ps1') -AllowMissingDevExpressLicense:$AllowMissingDevExpressLicense
& (Join-Path $root 'build/Assert-CrossPlatformBoundaries.ps1')
Write-Host "Refreshing reviewed LocalGPT frontend SHA-256 inventory before the ordered CLI build..." -ForegroundColor DarkCyan
& (Join-Path $root 'build/Update-JavaScriptDiagnosticsManifest.ps1')
& (Join-Path $root 'build/Assert-JavaScriptDiagnostics.ps1')
Write-Host "Clearing repository-local bin/obj build state for the authoritative release build..." -ForegroundColor Cyan
Get-ChildItem (Join-Path $root "src") -Directory -Recurse -Force |
    Where-Object { $_.Name -in @("bin", "obj") } |
    Sort-Object FullName -Descending |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
$solutionRoot = Join-Path $root "src"
$artifacts = Join-Path $root "artifacts/release"
$packageDirectory = Join-Path $root "packages"
$appProject = Join-Path $solutionRoot "LocalGPT/LocalGPT.csproj"
$setupProject = Join-Path $solutionRoot "LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj"
$wrapperProject = Join-Path $solutionRoot "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj"
$wireProject = Join-Path $solutionRoot "LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj"
$documentationScript = Join-Path $root "build/Build-Documentation.ps1"
$pagesSnapshotScript = Join-Path $root "build/Update-GitHubPagesSnapshot.ps1"
$pagesSnapshotArchive = Join-Path $root ".github/pages/localgpt-kawaii-docs.zip"
$wirePackageName = "LocalGPT.WireProtocolVersion.$WireProtocolVersion.nupkg"
$wirePackage = Join-Path $packageDirectory $wirePackageName
$localApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
$sharedWirePackageDirectory = if ([string]::IsNullOrWhiteSpace($localApplicationData)) { $null } else { Join-Path $localApplicationData "LocalGPT/NuGet" }
$documentationCacheRoot = Join-Path $artifacts ".documentation-cache"
$documentationPrepared = $false
$releaseZipPaths = New-Object 'System.Collections.Generic.List[string]'
$releasePackagingVersion = '1.0.0'
$releasePackagingPackageName = "LocalGPT.ReleasePackaging.$releasePackagingVersion.nupkg"
$releasePackagingPackage = Join-Path $packageDirectory $releasePackagingPackageName
$releasePackagingTool = $null
$nativeReleasePackagingScript = Join-Path $root 'build/NativeReleasePackaging.ps1'

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments, [Parameter(Mandatory)][string]$FailureMessage)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
}

function Resolve-ProjectVersion {
    param([Parameter(Mandatory)][string]$ProjectPath)

    [xml]$project = Get-Content -LiteralPath $ProjectPath -Raw
    $versions = @(
        $project.Project.PropertyGroup |
            ForEach-Object { [string]$_.Version } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    if ($versions.Count -eq 0) { throw "Project version was not found in $ProjectPath" }
    return $versions[0]
}

function Assert-LocalGptDocumentationPayload {
    param(
        [Parameter(Mandatory)][string]$DocumentationRoot,
        [Parameter(Mandatory)][string]$Version,
        [switch]$RequirePhysicalPdf
    )
    $requiredArtifacts = @(
        (Join-Path $DocumentationRoot "index.html"),
        (Join-Path $DocumentationRoot "documentation-status.json"),
        (Join-Path $DocumentationRoot "LocalGPT.xml")
    )
    if ($RequirePhysicalPdf) { $requiredArtifacts += (Join-Path $DocumentationRoot "LocalGPT-$Version.pdf") }
    foreach ($requiredArtifact in $requiredArtifacts) {
        if (-not (Test-Path -LiteralPath $requiredArtifact -PathType Leaf)) {
            throw "Published LocalGPT documentation is incomplete: $requiredArtifact"
        }
    }

    $statusPath = Join-Path $DocumentationRoot "documentation-status.json"
    $status = Get-Content -LiteralPath $statusPath -Raw | ConvertFrom-Json
    if (-not [string]::Equals([string]$status.version, $Version, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Published LocalGPT documentation version '$($status.version)' does not match application version '$Version'."
    }
    $versionedPdfs = @(Get-ChildItem -LiteralPath $DocumentationRoot -File -Filter 'LocalGPT-*.pdf' -ErrorAction SilentlyContinue)
    $versionedPdfNames = @($versionedPdfs | ForEach-Object { $_.Name })
    $versionedPdfDisplay = if ($versionedPdfNames.Count -eq 0) { '<none>' } else { $versionedPdfNames -join ', ' }
    if ($RequirePhysicalPdf) {
        if ($versionedPdfs.Count -ne 1 -or -not [string]::Equals($versionedPdfs[0].Name, "LocalGPT-$Version.pdf", [StringComparison]::OrdinalIgnoreCase)) {
            throw "Published LocalGPT documentation must contain exactly one current versioned PDF (LocalGPT-$Version.pdf). Found: $versionedPdfDisplay"
        }
    }
    elseif ($versionedPdfs.Count -ne 0) {
        throw "Runtime HTML documentation must not duplicate the standalone release PDF. Found: $versionedPdfDisplay"
    }
    $apiIndex = Join-Path $DocumentationRoot 'api/index.html'
    if (-not (Test-Path -LiteralPath $apiIndex -PathType Leaf)) { throw "Published LocalGPT documentation is missing api/index.html: $apiIndex" }
    $physicalApiHtmlCount = @(Get-ChildItem -LiteralPath (Join-Path $DocumentationRoot 'api') -Filter '*.html' -File -Recurse -ErrorAction SilentlyContinue).Count
    if ($physicalApiHtmlCount -le 1) { throw "Published LocalGPT documentation API directory is physically incomplete ($physicalApiHtmlCount HTML file(s))." }
    if ([string]$status.documentationMode -ne "docfx") { throw "Published LocalGPT documentation did not use the DocFX modern site." }
    if ([string]$status.pdfMode -notin @("html-browser-print", "docfx-pdf-plugin")) { throw "Published LocalGPT documentation does not contain the complete HTML-backed documentation PDF." }
    if ([string]$status.pdfMode -eq "html-browser-print" -and [int]$status.pdfSourcePageCount -lt 10) { throw "The LocalGPT documentation PDF did not include the expected HTML page set." }
    if ([string]$status.pdfMode -eq "html-browser-print" -and [int]$status.apiHtmlCount -gt 0 -and [int]$status.pdfSourcePageCount -lt [int]$status.apiHtmlCount) { throw "The LocalGPT documentation PDF omitted generated API pages." }
    if (-not ([bool]$status.completeApiReference)) { throw "Published LocalGPT documentation is missing the complete XML-generated API reference." }
    if (-not ([bool]$status.htmlPreflightValidated)) { throw "Published LocalGPT documentation did not pass the pre-PDF HTML accessibility/link preflight." }
    if ([int]$status.unresolvedAssemblyReferenceCount -ne 0) { throw "Published LocalGPT documentation contains unresolved assembly references: $($status.unresolvedAssemblyReferences -join ', ')" }
    if ([int]$status.apiYamlCount -le 1 -or [int]$status.apiHtmlCount -le 1) { throw "Published LocalGPT documentation contains an incomplete API graph." }
    if ([long]$status.pdfBytes -lt 1048576) { throw "Published LocalGPT documentation contains an unexpectedly small PDF." }
    if ([int]$status.pdfCandidateCount -lt 1 -or [string]::IsNullOrWhiteSpace([string]$status.pdfGeneratedSourcePath)) { throw "Published LocalGPT documentation did not record a real documentation PDF source." }
    if (-not $RequirePhysicalPdf) {
        if ([bool]$status.pdfAvailable) { throw "Runtime documentation status must declare pdfAvailable=false when the standalone PDF is not embedded." }
        if (-not [string]::Equals([string]$status.releasePdfFileName, "LocalGPT-$Version.pdf", [StringComparison]::OrdinalIgnoreCase)) { throw "Runtime documentation did not preserve the standalone release PDF identity." }
        if ([long]$status.releasePdfBytes -lt 1048576) { throw "Runtime documentation did not preserve the standalone release PDF size metadata." }
    }

    $payloadLabel = if ($RequirePhysicalPdf) { "modern HTML and HTML-backed PDF" } else { "modern HTML with standalone release-PDF metadata" }
    Write-Host "Verified complete LocalGPT $Version DocFX $payloadLabel documentation in $DocumentationRoot" -ForegroundColor Green
}

$appVersion = Resolve-ProjectVersion -ProjectPath $appProject

function Prepare-LocalGptDocumentation {
    if ($script:documentationPrepared) { return }

    if (-not (Test-Path -LiteralPath $documentationScript -PathType Leaf)) {
        throw "Documentation build script not found: $documentationScript"
    }

    $appProjectDirectory = Split-Path -Parent $appProject
    $neutralOutputRoot = Join-Path $appProjectDirectory "bin/$Configuration/net10.0"
    $documentationAssembly = Join-Path $neutralOutputRoot "LocalGPT.dll"
    $documentationXml = Join-Path $neutralOutputRoot "LocalGPT.xml"
    $documentationOutput = Join-Path $neutralOutputRoot "wwwroot/help-docs"
    # Documentation is produced from the authoritative source-project graph. The release package is still
    # packed and delivered for package-mode consumers, but rebuilding that same mutable local package
    # version through NuGet can reuse a stale global-packages entry. That failure presents as hundreds of
    # missing LocalGPT.WireProtocol types even though packing itself succeeded.
    $documentationBuildProperties = @(
        "-p:UseLocalWireProtocolProject=true",
        "-p:RuntimeIdentifier=",
        "-p:RuntimeIdentifiers=",
        "-p:BuildLocalGptDocumentation=false",
        "-p:SeedLocalGptGitHubPagesSnapshotOnBuild=false",
        "-p:CopyLocalLockFileAssemblies=true"
    )

    Write-Host "Building the RID-neutral LocalGPT assembly once for shared release documentation..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments (@("restore", $appProject, "--disable-parallel", "--force-evaluate") + $documentationBuildProperties) -FailureMessage "RID-neutral LocalGPT restore for documentation failed."
    Invoke-DotNet -Arguments (@("build", $appProject, "-c", $Configuration, "--no-restore", "-maxcpucount:1", "-p:BuildProjectReferences=false", "-p:BuildLocalGptDocumentation=false") + $documentationBuildProperties) -FailureMessage "RID-neutral LocalGPT build for documentation failed."

    if (-not (Test-Path -LiteralPath $documentationAssembly -PathType Leaf)) { throw "Documentation assembly not found: $documentationAssembly" }
    if (-not (Test-Path -LiteralPath $documentationXml -PathType Leaf)) { throw "Documentation XML not found: $documentationXml" }

    Write-Host "Generating the complete LocalGPT documentation once for all runtime packages..." -ForegroundColor Cyan
    & $documentationScript `
        -RepositoryRoot $root `
        -AssemblyPath $documentationAssembly `
        -XmlDocumentationPath $documentationXml `
        -Version $appVersion `
        -OutputWebRoot $documentationOutput `
        -RequirePdf

    Assert-LocalGptDocumentationPayload -DocumentationRoot $documentationOutput -Version $appVersion -RequirePhysicalPdf
    if (-not (Test-Path -LiteralPath $pagesSnapshotScript -PathType Leaf)) { throw "GitHub Pages snapshot script not found: $pagesSnapshotScript" }
    Write-Host "Validating and seeding the LocalGPT $appVersion GitHub Pages snapshot from the release documentation payload..." -ForegroundColor Cyan
    & $pagesSnapshotScript -DocumentationRoot $documentationOutput -OutputArchive $pagesSnapshotArchive
    if (-not (Test-Path -LiteralPath $pagesSnapshotArchive -PathType Leaf)) { throw "LocalGPT GitHub Pages snapshot update failed to create $pagesSnapshotArchive." }
    Remove-Item -LiteralPath $script:documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $script:documentationCacheRoot -Force | Out-Null
    Copy-Item -Path (Join-Path $documentationOutput "*") -Destination $script:documentationCacheRoot -Recurse -Force
    $script:documentationPrepared = $true
    Write-Host "Cached one verified documentation payload for all RID publishes." -ForegroundColor Green
}

function Copy-LocalGptRuntimeDocumentation {
    param(
        [Parameter(Mandatory)][string]$SourceRoot,
        [Parameter(Mandatory)][string]$DestinationRoot,
        [Parameter(Mandatory)][string]$Version
    )

    $pdfName = "LocalGPT-$Version.pdf"
    Remove-Item -LiteralPath $DestinationRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
    foreach ($entry in Get-ChildItem -LiteralPath $SourceRoot -Force) {
        if (-not $entry.PSIsContainer -and [string]::Equals($entry.Name, $pdfName, [StringComparison]::OrdinalIgnoreCase)) { continue }
        Copy-Item -LiteralPath $entry.FullName -Destination (Join-Path $DestinationRoot $entry.Name) -Recurse -Force
    }

    $statusPath = Join-Path $DestinationRoot "documentation-status.json"
    $status = Get-Content -LiteralPath $statusPath -Raw | ConvertFrom-Json
    $releasePdfBytes = [long]$status.pdfBytes
    $status | Add-Member -NotePropertyName releasePdfFileName -NotePropertyValue $pdfName -Force
    $status | Add-Member -NotePropertyName releasePdfBytes -NotePropertyValue $releasePdfBytes -Force
    $status | Add-Member -NotePropertyName runtimePdfPublished -NotePropertyValue $false -Force
    $status | Add-Member -NotePropertyName pdfAvailable -NotePropertyValue $false -Force
    $status | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $statusPath -Encoding utf8

    $releaseUrl = "https://github.com/Michi0403/LocalGPT/releases/latest"
    foreach ($htmlFile in Get-ChildItem -LiteralPath $DestinationRoot -Filter '*.html' -File -Recurse -ErrorAction SilentlyContinue) {
        $html = [IO.File]::ReadAllText($htmlFile.FullName)
        if ($html.IndexOf($pdfName, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $escapedPdf = [regex]::Escape($pdfName)
            $html = [regex]::Replace($html, '(?i)href=["''](?:\.\./|\./)?' + $escapedPdf + '["'']', 'href="' + $releaseUrl + '"')
            [IO.File]::WriteAllText($htmlFile.FullName, $html, [Text.UTF8Encoding]::new($false))
        }
    }
}

function Resolve-PublishProfilePath {
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$ProfileName
    )

    $projectDirectory = Split-Path -Parent $ProjectPath
    $profilePath = Join-Path $projectDirectory "Properties/PublishProfiles/$ProfileName.pubxml"
    if (-not (Test-Path -LiteralPath $profilePath)) {
        throw "Publish profile not found: $profilePath"
    }

    return $profilePath
}

function Resolve-ProfilePublishFolder {
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$ProfileName
    )

    $profilePath = Resolve-PublishProfilePath -ProjectPath $ProjectPath -ProfileName $ProfileName
    [xml]$profile = Get-Content -LiteralPath $profilePath -Raw
    $propertyGroups = @($profile.Project.PropertyGroup)
    $publishDirectory = @(
        $propertyGroups |
            ForEach-Object { $_.PublishDir } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }
    ) | Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace([string]$publishDirectory)) {
        $publishDirectory = @(
            $propertyGroups |
                ForEach-Object { $_.PublishUrl } |
                Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }
        ) | Select-Object -First 1
    }

    if ([string]::IsNullOrWhiteSpace([string]$publishDirectory)) {
        throw "Publish profile does not define PublishDir or PublishUrl: $profilePath"
    }

    $projectDirectory = Split-Path -Parent $ProjectPath
    $resolved = if ([IO.Path]::IsPathRooted([string]$publishDirectory)) {
        [string]$publishDirectory
    } else {
        Join-Path $projectDirectory ([string]$publishDirectory)
    }

    return [IO.Path]::GetFullPath($resolved)
}

function Resolve-ReleaseProfile {
    param([Parameter(Mandatory)][string]$Rid)
    switch ($Rid) {
        "win-x64"     { return @{ AppAsset = "winx64.zip";     SetupAsset = "setupwinx64.zip";     AppProfile = "winx64";     SetupProfile = "winx64";     WrapperProfile = "winx64" } }
        "win-x86"     { return @{ AppAsset = "winx86.zip";     SetupAsset = "setupwinx86.zip";     AppProfile = "winx86";     SetupProfile = "winx86";     WrapperProfile = "winx86" } }
        "win-arm64"   { return @{ AppAsset = "winarm64.zip";   SetupAsset = "setupwinarm64.zip";   AppProfile = "winarm64";   SetupProfile = "winarm64";   WrapperProfile = "winarm64" } }
        "linux-x64"   { return @{ AppAsset = "linuxx64.zip";   SetupAsset = "setuplinuxx64.zip";   AppProfile = "linuxx64";   SetupProfile = "linuxx64";   WrapperProfile = $null } }
        "linux-arm64" { return @{ AppAsset = "linuxarm64.zip"; SetupAsset = "setuplinuxarm64.zip"; AppProfile = "linuxarm64"; SetupProfile = "linuxarm64"; WrapperProfile = $null } }
        "osx-x64"     { return @{ AppAsset = "macosx64.zip";   SetupAsset = "setupmacosx64.zip";   AppProfile = "macosx64";   SetupProfile = "macosx64";   WrapperProfile = $null } }
        "osx-arm64"   { return @{ AppAsset = "macosarm64.zip"; SetupAsset = "setupmacosarm64.zip"; AppProfile = "macosarm64"; SetupProfile = "macosarm64"; WrapperProfile = $null } }
        default { throw "Unsupported release runtime: $Rid" }
    }
}


function New-PortableReleaseArchive {
    param(
        [Parameter(Mandatory)][string]$SourceDirectory,
        [Parameter(Mandatory)][string]$DestinationPath,
        [Parameter(Mandatory)][string]$RootFolderName,
        [string[]]$UnixExecutableRelativePaths = @(),
        [switch]$WriteUnixPermissions
    )

    $sourceRoot = [IO.Path]::GetFullPath($SourceDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
        throw "Release archive source directory does not exist: $sourceRoot"
    }
    if ([string]::IsNullOrWhiteSpace($RootFolderName) -or $RootFolderName.IndexOfAny([char[]]"/\\") -ge 0) {
        throw "Release archive wrapper must be one directory name: $RootFolderName"
    }

    $files = @(Get-ChildItem -LiteralPath $sourceRoot -File -Recurse | Sort-Object FullName)
    if ($files.Count -eq 0) { throw "Release archive source is empty: $sourceRoot" }

    $unixExecutableSet = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($unixExecutablePath in @($UnixExecutableRelativePaths)) {
        if ([string]::IsNullOrWhiteSpace($unixExecutablePath)) { continue }
        $normalizedExecutablePath = $unixExecutablePath.TrimStart([char[]]"\/").Replace('\', '/')
        if ($normalizedExecutablePath.Split('/') -contains '..') { throw "Unsafe Unix executable path: $unixExecutablePath" }
        [void]$unixExecutableSet.Add($normalizedExecutablePath)
    }
    $unixRegularFileAttributes = [int]-2119958528 # 0100644 << 16
    $unixExecutableFileAttributes = [int]-2115174400 # 0100755 << 16

    $destination = [IO.Path]::GetFullPath($DestinationPath)
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    $temporaryArchive = "$destination.$([Guid]::NewGuid().ToString('N')).tmp"

    Add-Type -AssemblyName System.IO.Compression -ErrorAction SilentlyContinue
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
    try {
        $archive = [IO.Compression.ZipFile]::Open($temporaryArchive, [IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach ($file in $files) {
                $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart([char[]]"\/").Replace('\', '/')
                if ([string]::IsNullOrWhiteSpace($relative) -or $relative.Split('/') -contains '..') {
                    throw "Unsafe release archive source path: $($file.FullName)"
                }
                $entryName = "$RootFolderName/$relative"
                if ($entryName.Contains('\')) {
                    throw "Portable ZIP entries may not contain Windows path separators: $entryName"
                }

                $entry = $archive.CreateEntry($entryName, [IO.Compression.CompressionLevel]::Optimal)
                $entry.LastWriteTime = [DateTimeOffset]::new(1980, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
                if ($WriteUnixPermissions) {
                    $entry.ExternalAttributes = if ($unixExecutableSet.Contains($relative)) { $unixExecutableFileAttributes } else { $unixRegularFileAttributes }
                }
                $input = [IO.File]::Open($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
                try {
                    $output = $entry.Open()
                    try { $input.CopyTo($output) }
                    finally { $output.Dispose() }
                }
                finally { $input.Dispose() }
            }
        }
        finally { $archive.Dispose() }

        $verification = [IO.Compression.ZipFile]::OpenRead($temporaryArchive)
        try {
            $entries = @($verification.Entries | Where-Object { -not [string]::IsNullOrWhiteSpace($_.Name) })
            if ($entries.Count -ne $files.Count) {
                throw "Release archive entry count $($entries.Count) does not match source file count $($files.Count): $temporaryArchive"
            }
            if ($WriteUnixPermissions) {
                foreach ($unixExecutablePath in $unixExecutableSet) {
                    $expectedEntryName = "$RootFolderName/$unixExecutablePath"
                    $executableEntry = $entries | Where-Object { $_.FullName -eq $expectedEntryName } | Select-Object -First 1
                    if ($null -eq $executableEntry) { throw "Unix executable entry is missing from release archive: $expectedEntryName" }
                    $permissionBits = (($executableEntry.ExternalAttributes -shr 16) -band 511)
                    if ($permissionBits -ne 493) { throw "Unix executable entry '$expectedEntryName' does not carry mode 0755 (actual permission bits: $permissionBits)." }
                }
            }
            foreach ($entry in $entries) {
                if ($entry.FullName.Contains('\')) {
                    throw "Release archive is not POSIX/ZIP portable because it contains a backslash entry: $($entry.FullName)"
                }
                if (-not $entry.FullName.StartsWith("$RootFolderName/", [StringComparison]::Ordinal)) {
                    throw "Release archive entry '$($entry.FullName)' escapes expected wrapper '$RootFolderName'."
                }
            }
        }
        finally { $verification.Dispose() }

        Remove-Item -LiteralPath $destination -Force -ErrorAction SilentlyContinue
        [IO.File]::Move($temporaryArchive, $destination)
    }
    finally {
        Remove-Item -LiteralPath $temporaryArchive -Force -ErrorAction SilentlyContinue
    }
}

function Test-VersionDirectoryName {
    param([Parameter(Mandatory)][string]$Name)
    return $Name -match '^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$'
}

function Complete-ReleaseBundle {
    param(
        [Parameter(Mandatory)][string]$Version,
        [Parameter(Mandatory)][string[]]$ReleaseZipPaths,
        [Parameter(Mandatory)][string]$DocumentationPdfPath,
        [Parameter(Mandatory)][string]$WindowsX64SetupExecutablePath,
        [Parameter(Mandatory)][string]$ReadmePath,
        [Parameter(Mandatory)][string]$LicensePath,
        [Parameter(Mandatory)][string]$WireProtocolPackagePath,
        [Parameter(Mandatory)][string]$ReleasePackagingPackagePath,
        [Parameter(Mandatory)][string]$SetupIconPath,
        [Parameter(Mandatory)][bool]$RequireWindowsX64Setup
    )

    $versionDirectory = Join-Path $artifacts $Version
    if (Test-Path -LiteralPath $versionDirectory) {
        throw "Release bundle '$versionDirectory' already exists. Existing version directories are never overwritten."
    }

    $uniqueZipPaths = @($ReleaseZipPaths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
    if ($uniqueZipPaths.Count -eq 0) { throw "No release ZIPs were produced for release $Version." }
    foreach ($zipPath in $uniqueZipPaths) {
        if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) { throw "Expected release ZIP is missing: $zipPath" }
    }
    foreach ($requiredFile in @($DocumentationPdfPath, $ReadmePath, $LicensePath, $WireProtocolPackagePath, $ReleasePackagingPackagePath, $SetupIconPath)) {
        if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) { throw "Required upload-ready release file is missing: $requiredFile" }
    }
    if ($RequireWindowsX64Setup -and -not (Test-Path -LiteralPath $WindowsX64SetupExecutablePath -PathType Leaf)) {
        throw "Windows x64 setup executable is required for the full release bundle but is missing: $WindowsX64SetupExecutablePath"
    }

    $stagingDirectory = Join-Path $artifacts (".release-bundle-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null
    try {
        foreach ($zipPath in $uniqueZipPaths) {
            Copy-Item -LiteralPath $zipPath -Destination (Join-Path $stagingDirectory ([IO.Path]::GetFileName($zipPath))) -Force
        }
        Copy-Item -LiteralPath $DocumentationPdfPath -Destination (Join-Path $stagingDirectory ([IO.Path]::GetFileName($DocumentationPdfPath))) -Force
        Copy-Item -LiteralPath $ReadmePath -Destination (Join-Path $stagingDirectory ([IO.Path]::GetFileName($ReadmePath))) -Force
        Copy-Item -LiteralPath $LicensePath -Destination (Join-Path $stagingDirectory ([IO.Path]::GetFileName($LicensePath))) -Force
        Copy-Item -LiteralPath $WireProtocolPackagePath -Destination (Join-Path $stagingDirectory ([IO.Path]::GetFileName($WireProtocolPackagePath))) -Force
        Copy-Item -LiteralPath $ReleasePackagingPackagePath -Destination (Join-Path $stagingDirectory ([IO.Path]::GetFileName($ReleasePackagingPackagePath))) -Force
        Copy-Item -LiteralPath $SetupIconPath -Destination (Join-Path $stagingDirectory "LocalGPT.ico") -Force
        if (Test-Path -LiteralPath $WindowsX64SetupExecutablePath -PathType Leaf) {
            Copy-Item -LiteralPath $WindowsX64SetupExecutablePath -Destination (Join-Path $stagingDirectory ([IO.Path]::GetFileName($WindowsX64SetupExecutablePath))) -Force
        }

        New-Item -ItemType Directory -Path $versionDirectory -Force | Out-Null
        foreach ($file in Get-ChildItem -LiteralPath $stagingDirectory -File) {
            Move-Item -LiteralPath $file.FullName -Destination (Join-Path $versionDirectory $file.Name)
        }

        $checksumPath = Join-Path $versionDirectory 'SHA256SUMS.txt'
        $checksumLines = foreach ($file in Get-ChildItem -LiteralPath $versionDirectory -File | Sort-Object Name) {
            if ($file.Name -eq 'SHA256SUMS.txt') { continue }
            $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            "$hash  $($file.Name)"
        }
        [IO.File]::WriteAllLines($checksumPath, [string[]]$checksumLines, (New-Object Text.UTF8Encoding($false)))

        foreach ($zipPath in $uniqueZipPaths) {
            Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
        }

        foreach ($directory in Get-ChildItem -LiteralPath $artifacts -Directory -Force) {
            if ($directory.FullName -eq $versionDirectory) { continue }
            if (Test-VersionDirectoryName -Name $directory.Name) { continue }
            Remove-Item -LiteralPath $directory.FullName -Recurse -Force -ErrorAction Stop
        }

        Write-Host "Upload-ready release bundle: $versionDirectory" -ForegroundColor Green
    }
    finally {
        Remove-Item -LiteralPath $stagingDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Ensure-WireProtocolPackage {
    New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null

    if ($UseBundledWireProtocolPackage) {
        if (-not (Test-Path $wirePackage)) {
            throw "The bundled RID-neutral LocalGPT 1-Wire package is missing: $wirePackage"
        }
        return
    }

    if (-not [string]::IsNullOrWhiteSpace($WireProtocolPackageUrl)) {
        Write-Host "Downloading LocalGPT.WireProtocolVersion $WireProtocolVersion..." -ForegroundColor Cyan
        $temporary = "$wirePackage.download"
        Remove-Item $temporary -Force -ErrorAction SilentlyContinue
        Invoke-WebRequest -Uri $WireProtocolPackageUrl -OutFile $temporary -UseBasicParsing
        if (-not (Test-Path $temporary)) { throw "The protocol package download did not create a file." }
        Move-Item $temporary $wirePackage -Force
        return
    }

    Write-Host "Packing the RID-neutral LocalGPT 1-Wire protocol once..." -ForegroundColor Cyan
    Remove-Item $wirePackage -Force -ErrorAction SilentlyContinue
    Invoke-DotNet -Arguments @(
        "pack", $wireProject,
        "-c", $Configuration,
        "-o", $packageDirectory,
        "-p:PackageVersion=$WireProtocolVersion",
        "-p:GeneratePackageOnBuild=false",
        "-p:Platform=AnyCPU",
        "-p:PlatformTarget=AnyCPU",
        "-p:RuntimeIdentifier=",
        "-p:RuntimeIdentifiers="
    ) -FailureMessage "LocalGPT 1-Wire package creation failed."

    if (-not (Test-Path $wirePackage)) {
        throw "The expected protocol package was not produced: $wirePackage"
    }
}


function Publish-UnixRuntime {
    param([Parameter(Mandatory)][string]$Rid)
    if ($Rid.StartsWith('win-')) { throw "Publish-UnixRuntime received Windows RID $Rid." }
    if (-not $script:releasePackagingTool) { throw 'Release packaging tool was not prepared.' }
    if (-not (Test-Path -LiteralPath $script:nativeReleasePackagingScript -PathType Leaf)) { throw "Native packaging script is missing: $script:nativeReleasePackagingScript" }

    foreach ($mode in @('Full','Light')) {
        $selfContained = if ($mode -eq 'Full') { 'true' } else { 'false' }
        $publishFolder = Join-Path $artifacts ("staging/$Rid/$($mode.ToLowerInvariant())")
        Remove-Item -LiteralPath $publishFolder -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Path $publishFolder -Force | Out-Null
        Write-Host "Publishing LocalGPT $Rid $mode application payload (no setup console)..." -ForegroundColor Cyan
        Invoke-DotNet -Arguments @(
            'restore', $appProject, '-r', $Rid, '--disable-parallel', '--force-evaluate'
        ) -FailureMessage "LocalGPT application restore failed for $Rid $mode."
        Invoke-DotNet -Arguments @(
            'publish', $appProject, '-c', $Configuration, '-r', $Rid, '--no-restore', '-o', $publishFolder,
            '--self-contained', $selfContained,
            '-p:PublishSingleFile=false',
            '-p:BuildLocalGptDocumentation=false',
            '-p:SeedLocalGptGitHubPagesSnapshotOnBuild=false',
            '-p:RequireLocalGptDocumentationPdf=false'
        ) -FailureMessage "LocalGPT application publish failed for $Rid $mode."
        $appExecutable = 'LocalGPT'
        if (-not (Test-Path -LiteralPath (Join-Path $publishFolder $appExecutable) -PathType Leaf)) { throw "Published LocalGPT apphost is missing for $Rid $mode." }
        Copy-LocalGptRuntimeDocumentation -SourceRoot $script:documentationCacheRoot -DestinationRoot (Join-Path $publishFolder 'wwwroot/help-docs') -Version $appVersion
        Assert-LocalGptDocumentationPayload -DocumentationRoot (Join-Path $publishFolder 'wwwroot/help-docs') -Version $appVersion
        $protocolDirectory = Join-Path $publishFolder 'protocol'; New-Item -ItemType Directory -Path $protocolDirectory -Force | Out-Null
        Copy-Item -LiteralPath $wirePackage -Destination (Join-Path $protocolDirectory $wirePackageName) -Force
        $nativeArtifacts = & $script:nativeReleasePackagingScript -ProductName 'LocalGPT' -ExecutableName $appExecutable -Version $appVersion -Rid $Rid -Mode $mode -PayloadDirectory $publishFolder -OutputDirectory $artifacts -PackagingTool $script:releasePackagingTool -DependencyPolicy LocalGPT
        foreach ($artifact in @($nativeArtifacts)) { if (-not [string]::IsNullOrWhiteSpace([string]$artifact)) { $script:releaseZipPaths.Add([string]$artifact) } }
    }
}

function Publish-Runtime {
    param([Parameter(Mandatory)][string]$Rid)

    if (-not $Rid.StartsWith('win-')) { Publish-UnixRuntime -Rid $Rid; return }

    $profile = Resolve-ReleaseProfile $Rid
    $appFolder = Resolve-ProfilePublishFolder -ProjectPath $appProject -ProfileName $profile.AppProfile
    $setupFolder = Resolve-ProfilePublishFolder -ProjectPath $setupProject -ProfileName $profile.SetupProfile
    $appZip = Join-Path $artifacts $profile.AppAsset
    $setupZip = Join-Path $artifacts $profile.SetupAsset

    Remove-Item $appFolder, $setupFolder, $appZip, $setupZip -Recurse -Force -ErrorAction SilentlyContinue

    $appExecutable = if ($Rid.StartsWith("win-")) { "LocalGPT.exe" } else { "LocalGPT" }
    $setupExecutable = if ($Rid.StartsWith("win-")) { "LocalGPTInstallerConsole.exe" } else { "LocalGPTInstallerConsole" }

    $buildDocumentation = "false"
    $requireDocumentationPdf = "false"
    Write-Host "Publishing LocalGPT application through profile $($profile.AppProfile)..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments @(
        "publish", $appProject,
        "-c", $Configuration,
        "-p:PublishProfile=$($profile.AppProfile)",
        "-p:BuildLocalGptDocumentation=$buildDocumentation",
        "-p:SeedLocalGptGitHubPagesSnapshotOnBuild=false",
        "-p:RequireLocalGptDocumentationPdf=$requireDocumentationPdf"
    ) -FailureMessage "LocalGPT application publish failed for $Rid."

    Write-Host "Publishing LocalGPT setup through profile $($profile.SetupProfile)..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments @(
        "publish", $setupProject,
        "-c", $Configuration,
        "-p:PublishProfile=$($profile.SetupProfile)"
    ) -FailureMessage "LocalGPT setup publish failed for $Rid."

    if (-not (Test-Path (Join-Path $appFolder $appExecutable))) {
        throw "Published LocalGPT executable not found in the publish-profile output: $(Join-Path $appFolder $appExecutable)"
    }
    if (-not (Test-Path (Join-Path $setupFolder $setupExecutable))) {
        throw "Published LocalGPT setup executable not found in the publish-profile output: $(Join-Path $setupFolder $setupExecutable)"
    }

    $publishedDocumentationRoot = Join-Path $appFolder "wwwroot/help-docs"
    if ($script:documentationPrepared) {
        if (-not (Test-Path -LiteralPath $script:documentationCacheRoot -PathType Container)) {
            throw "The shared LocalGPT documentation cache is missing: $script:documentationCacheRoot"
        }
        Copy-LocalGptRuntimeDocumentation -SourceRoot $script:documentationCacheRoot -DestinationRoot $publishedDocumentationRoot -Version $appVersion
        Write-Host "Reused the verified HTML documentation payload for $Rid without duplicating the standalone release PDF." -ForegroundColor Cyan
    }

    Assert-LocalGptDocumentationPayload -DocumentationRoot $publishedDocumentationRoot -Version $appVersion
    $requiredSetupFiles = @(
        "Default.cmd", "Install.cmd", "Update.cmd", "Start.cmd", "Start-NoBrowser.cmd",
        "Install-Ollama.cmd", "Pull-Models-Slim.cmd", "Pull-Models-RTX3060.cmd",
        "Pull-Models-Full.cmd", "Setup-Learning-Base.cmd", "Import-Recommended.cmd", "Uninstall.cmd"
    )
 
    $protocolSetupDirectory = Join-Path $setupFolder "protocol"
    New-Item -ItemType Directory -Path $protocolSetupDirectory -Force | Out-Null
    Copy-Item $wirePackage (Join-Path $protocolSetupDirectory $wirePackageName) -Force

    if ($IncludeWindowsWrapper -and $profile.WrapperProfile) {
        $wrapperFolder = Resolve-ProfilePublishFolder -ProjectPath $wrapperProject -ProfileName $profile.WrapperProfile
        Remove-Item $wrapperFolder -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Publishing the optional WinUI wrapper through profile $($profile.WrapperProfile)..." -ForegroundColor Cyan
        Invoke-DotNet -Arguments @(
            "publish", $wrapperProject,
            "-c", $Configuration,
            "-p:PublishProfile=$($profile.WrapperProfile)"
        ) -FailureMessage "WinUI wrapper publish failed for $Rid."
        Copy-Item (Join-Path $wrapperFolder "*") $appFolder -Recurse -Force
    }

    # Final release-boundary check: optional wrapper/publish steps must not reintroduce stale documentation.
    Assert-LocalGptDocumentationPayload -DocumentationRoot $publishedDocumentationRoot -Version $appVersion
    $appRootFolderName = Split-Path -Leaf $appFolder
    $setupRootFolderName = Split-Path -Leaf $setupFolder
    New-PortableReleaseArchive -SourceDirectory $appFolder -DestinationPath $appZip -RootFolderName $appRootFolderName -WriteUnixPermissions:(!$Rid.StartsWith("win-")) -UnixExecutableRelativePaths @($appExecutable)
    New-PortableReleaseArchive -SourceDirectory $setupFolder -DestinationPath $setupZip -RootFolderName $setupRootFolderName -WriteUnixPermissions:(!$Rid.StartsWith("win-")) -UnixExecutableRelativePaths @($setupExecutable)
    $script:releaseZipPaths.Add($appZip)
    $script:releaseZipPaths.Add($setupZip)
    Write-Host "Created portable ZIP $appZip" -ForegroundColor Green
    Write-Host "Created portable ZIP $setupZip" -ForegroundColor Green
}

New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
Remove-Item -LiteralPath $documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
Ensure-WireProtocolPackage
$releasePackagingTool = & (Join-Path $root 'build/Ensure-ReleasePackagingPackage.ps1') -Configuration $Configuration -Version $releasePackagingVersion
if (-not (Test-Path -LiteralPath $releasePackagingPackage -PathType Leaf)) { throw "Release-packaging package preparation did not produce $releasePackagingPackage" }
Copy-Item $wirePackage (Join-Path $artifacts $wirePackageName) -Force
Copy-Item $releasePackagingPackage (Join-Path $artifacts $releasePackagingPackageName) -Force
if ($sharedWirePackageDirectory) {
    New-Item -ItemType Directory -Path $sharedWirePackageDirectory -Force | Out-Null
    Copy-Item $wirePackage (Join-Path $sharedWirePackageDirectory $wirePackageName) -Force
    Copy-Item $releasePackagingPackage (Join-Path $sharedWirePackageDirectory $releasePackagingPackageName) -Force
    Write-Host "Updated shared LocalGPT protocol/release-packaging package cache: $sharedWirePackageDirectory" -ForegroundColor Green
}

Prepare-LocalGptDocumentation

$runtimes = if ($Runtime -eq "all") {
    @("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")
} else {
    @($Runtime)
}

try {
    foreach ($rid in $runtimes) { Publish-Runtime $rid }

    $documentationPdf = Join-Path $documentationCacheRoot "LocalGPT-$appVersion.pdf"
    $winX64Profile = Resolve-ReleaseProfile -Rid "win-x64"
    $winX64SetupFolder = Resolve-ProfilePublishFolder -ProjectPath $setupProject -ProfileName $winX64Profile.SetupProfile
    $winX64SetupExecutable = Join-Path $winX64SetupFolder "LocalGPTInstallerConsole.exe"
    $requireWinX64Setup = @($runtimes) -contains "win-x64"
    $licensePath = Join-Path $root "LICENSE.MD"
    if (-not (Test-Path -LiteralPath $licensePath -PathType Leaf)) { $licensePath = Join-Path $root "LICENSE" }

    Complete-ReleaseBundle `
        -Version $appVersion `
        -ReleaseZipPaths @($releaseZipPaths) `
        -DocumentationPdfPath $documentationPdf `
        -WindowsX64SetupExecutablePath $winX64SetupExecutable `
        -ReadmePath (Join-Path $root "README.md") `
        -LicensePath $licensePath `
        -WireProtocolPackagePath $wirePackage `
        -ReleasePackagingPackagePath $releasePackagingPackage `
        -SetupIconPath (Join-Path $root "src/LocalGPT/wwwroot/favicon.ico") `
        -RequireWindowsX64Setup $requireWinX64Setup
}
finally {
    Remove-Item -LiteralPath $documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
}

$releaseBundle = Join-Path $artifacts $appVersion
Write-Host "Release output: $releaseBundle" -ForegroundColor Green
Write-Host "Protocol package cache: $(Join-Path $artifacts $wirePackageName)" -ForegroundColor Green
