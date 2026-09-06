param(
    [ValidateSet("all", "all-rids", "win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")]
    [string]$Runtime = "all",
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [string]$WireProtocolVersion = "2.1.1",
    [string]$WireProtocolPackageUrl = "",
    [switch]$UseBundledWireProtocolPackage,
    [switch]$IncludeWindowsWrapper,
    [switch]$UseContainerPackaging,
    [switch]$ProvisionNativePackagingTools,
    [switch]$RequireOptionalNativePackages,
    [switch]$AllowUnsignedMacPackages,
    [string]$DocumentationCacheRoot = "",
    [switch]$DisableDocumentationToolProvisioning,
    [string]$ReleaseOutputRoot = "",
    [switch]$ForceRebuildArtifacts,
    [switch]$AllowMissingDevExpressLicense,
    [ValidateSet("Auto", "Off", "Require")]
    [string]$WslLinux = "Auto",
    [string]$WslDistribution = "",
    [ValidateSet("IfStarted", "Always", "Never")]
    [string]$WslShutdown = "IfStarted",
    [switch]$ProvisionWslBuildTools,
    [switch]$KeepWslBuildTree,
    [switch]$WslChildBuild,
    [switch]$SkipReleaseBundle,
    [string]$PreparedDocumentationRoot = ""
)

$ErrorActionPreference = "Stop"
# File-provider progress is deliberately suppressed. Recursive Remove-Item progress races with
# DocFX/Spectre rendering and can report impossible file/byte totals in the shared terminal.
$ProgressPreference = "SilentlyContinue"
Set-StrictMode -Version Latest

function Initialize-BuildConsoleEncoding {
    if ([Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)) {
        $utf8 = New-Object Text.UTF8Encoding($false)
        [Console]::InputEncoding = $utf8
        [Console]::OutputEncoding = $utf8
        $global:OutputEncoding = $utf8
    }
}
Initialize-BuildConsoleEncoding

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not [string]::IsNullOrWhiteSpace($DocumentationCacheRoot)) {
    $env:FUTURE2_DOCUMENTATION_CACHE_ROOT = [IO.Path]::GetFullPath($DocumentationCacheRoot)
}
$nodeRuntimeCommonScript = Join-Path $root 'build/NodeRuntime.Common.ps1'
if (-not (Test-Path -LiteralPath $nodeRuntimeCommonScript -PathType Leaf)) { throw "Documentation runtime helper is missing: $nodeRuntimeCommonScript" }
. $nodeRuntimeCommonScript
$wslCommonScript = Join-Path $root 'build/WslRelease.Common.ps1'
if (-not (Test-Path -LiteralPath $wslCommonScript -PathType Leaf)) { throw "WSL release helper is missing: $wslCommonScript" }
. $wslCommonScript
& (Join-Path $root 'build/Assert-PowerShellCompatibility.ps1')
& (Join-Path $root 'build/Initialize-BuildPrerequisites.ps1') -AllowMissingDevExpressLicense:$AllowMissingDevExpressLicense -SkipDocumentationNodeProvisioning:($WslChildBuild -and -not [string]::IsNullOrWhiteSpace($PreparedDocumentationRoot))
& (Join-Path $root 'build/Assert-CrossPlatformBoundaries.ps1')
& (Join-Path $root 'build/Assert-OperationalDiagnostics.ps1')
Write-Host "Refreshing reviewed LocalGPT frontend SHA-256 inventory before the ordered CLI build..." -ForegroundColor DarkCyan
& (Join-Path $root 'build/Update-JavaScriptDiagnosticsManifest.ps1')
& (Join-Path $root 'build/Assert-JavaScriptDiagnostics.ps1')
function Clear-RepositoryReleaseBuildState {
    param([switch]$BestEffort)
    $directories = @(
        Get-ChildItem (Join-Path $root "src") -Directory -Recurse -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -in @("bin", "obj") } |
            Sort-Object FullName -Descending
    )
    foreach ($directory in $directories) {
        if (-not (Test-Path -LiteralPath $directory.FullName)) { continue }
        if ($BestEffort) { Remove-Item -LiteralPath $directory.FullName -Recurse -Force -ErrorAction SilentlyContinue }
        else { Remove-Item -LiteralPath $directory.FullName -Recurse -Force -ErrorAction Stop }
    }
    return $directories.Count
}

Write-Host "Clearing repository-local bin/obj build state for the authoritative release build..." -ForegroundColor Cyan
$clearedBuildStateCount = Clear-RepositoryReleaseBuildState
Write-Host "Cleared $clearedBuildStateCount repository-local bin/obj director$(if ($clearedBuildStateCount -eq 1) { 'y' } else { 'ies' }). Durable documentation caches outside bin/obj were preserved." -ForegroundColor DarkCyan
$solutionRoot = Join-Path $root "src"
$configuredReleaseOutputRoot = if (-not [string]::IsNullOrWhiteSpace($ReleaseOutputRoot)) { $ReleaseOutputRoot } else { [string]$env:FUTURE2_RELEASE_OUTPUT_ROOT }
$artifacts = if (-not [string]::IsNullOrWhiteSpace($configuredReleaseOutputRoot)) { [IO.Path]::GetFullPath($configuredReleaseOutputRoot) } else { Join-Path $root "artifacts/release" }
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
$documentationToolCacheBase = Get-LocalGptDocumentationToolCacheRoot -FallbackRoot (Join-Path $root 'docs/.tools')
$documentationCacheRoot = Join-Path $documentationToolCacheBase 'release-payload/LocalGPT' 
$documentationPrepared = $false
$releaseZipPaths = New-Object 'System.Collections.Generic.List[string]'
$releasePackagingVersion = '1.0.2'
$releasePackagingPackageName = "LocalGPT.ReleasePackaging.$releasePackagingVersion.nupkg"
$releasePackagingPackage = Join-Path $packageDirectory $releasePackagingPackageName
$releasePackagingTool = $null
$nativeReleasePackagingScript = Join-Path $root 'build/NativeReleasePackaging.ps1'

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments, [Parameter(Mandatory)][string]$FailureMessage)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
}

function Get-ReleaseHostFamily {
    if ([Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)) { return 'Windows' }
    if ([Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Linux)) { return 'Linux' }
    if ([Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::OSX)) { return 'macOS' }
    throw 'Unsupported release host. LocalGPT release builds support Windows, Linux, and macOS.'
}

function Get-HostDefaultRuntimes {
    switch (Get-ReleaseHostFamily) {
        'Windows' { return @('win-x64', 'win-x86', 'win-arm64') }
        'Linux'   { return @('linux-x64', 'linux-arm64') }
        'macOS'   { return @('osx-x64', 'osx-arm64', 'linux-x64', 'linux-arm64', 'win-x64', 'win-x86', 'win-arm64') }
    }
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
        (Join-Path $DocumentationRoot "LocalGPT.xml"),
        (Join-Path $DocumentationRoot "LocalGPT-$Version.pdf")
    )
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
    if ($versionedPdfs.Count -ne 1 -or -not [string]::Equals($versionedPdfs[0].Name, "LocalGPT-$Version.pdf", [StringComparison]::OrdinalIgnoreCase)) {
        throw "Published LocalGPT documentation must contain exactly one current embedded PDF (LocalGPT-$Version.pdf). Found: $versionedPdfDisplay"
    }
    $apiIndex = Join-Path $DocumentationRoot 'api/index.html'
    if (-not (Test-Path -LiteralPath $apiIndex -PathType Leaf)) { throw "Published LocalGPT documentation is missing api/index.html: $apiIndex" }
    $physicalApiHtmlCount = @(Get-ChildItem -LiteralPath (Join-Path $DocumentationRoot 'api') -Filter '*.html' -File -Recurse -ErrorAction SilentlyContinue).Count
    if ($physicalApiHtmlCount -le 1) { throw "Published LocalGPT documentation API directory is physically incomplete ($physicalApiHtmlCount HTML file(s))." }
    if ([string]$status.documentationMode -ne "docfx") { throw "Published LocalGPT documentation did not use the DocFX modern site." }
    $browserBackedPdfModes = @("html-browser-print", "html-browser-print-compatibility", "html-browser-chunked")
    if ([string]$status.pdfMode -notin @($browserBackedPdfModes + "docfx-pdf-plugin")) { throw "Published LocalGPT documentation does not contain the complete HTML-backed documentation PDF." }
    $acceptedPdfAccessibilityModes = if ([string]$status.pdfMode -eq "html-browser-print" -and [string]$status.pdfCompressionMode -eq "browser-native") {
        @("tagged-pdf-required")
    } elseif ([string]$status.pdfMode -eq "html-browser-print" -and [string]$status.pdfCompressionMode -eq "cached-validated-pdf") {
        # The durable cache preserves the validated PDF/accessibility result but intentionally records cache reuse
        # rather than the original compression mode. A cached browser-native tagged PDF and a cached post-processed
        # HTML-fallback PDF are therefore both valid, while unknown/unavailable accessibility states remain rejected.
        @("tagged-pdf-required", "html-accessibility-fallback")
    } else {
        @("html-accessibility-fallback")
    }
    if ([string]$status.pdfAccessibilityMode -notin $acceptedPdfAccessibilityModes) { throw "Published LocalGPT documentation has an unexpected PDF accessibility mode '$($status.pdfAccessibilityMode)' for PDF mode '$($status.pdfMode)' and compression mode '$($status.pdfCompressionMode)'." }
    if ([string]$status.pdfMode -in $browserBackedPdfModes -and [int]$status.pdfSourcePageCount -lt 10) { throw "The LocalGPT documentation PDF did not include the expected HTML page set." }
    if ([string]$status.pdfMode -in $browserBackedPdfModes -and [int]$status.apiHtmlCount -gt 0 -and [int]$status.pdfSourcePageCount -lt [int]$status.apiHtmlCount) { throw "The LocalGPT documentation PDF omitted generated API pages." }
    if (-not ([bool]$status.completeApiReference)) { throw "Published LocalGPT documentation is missing the complete XML-generated API reference." }
    if (-not ([bool]$status.htmlPreflightValidated)) { throw "Published LocalGPT documentation did not pass the pre-PDF HTML accessibility/link preflight." }
    if ([int]$status.unresolvedAssemblyReferenceCount -ne 0) { throw "Published LocalGPT documentation contains unresolved assembly references: $($status.unresolvedAssemblyReferences -join ', ')" }
    if ([int]$status.apiYamlCount -le 1 -or [int]$status.apiHtmlCount -le 1) { throw "Published LocalGPT documentation contains an incomplete API graph." }
    if ([long]$status.pdfBytes -lt 1048576) { throw "Published LocalGPT documentation contains an unexpectedly small PDF." }
    if ([int]$status.pdfCandidateCount -lt 1 -or [string]::IsNullOrWhiteSpace([string]$status.pdfGeneratedSourcePath)) { throw "Published LocalGPT documentation did not record a real documentation PDF source." }
    if (-not ([bool]$status.pdfAvailable)) { throw "Runtime documentation status must declare pdfAvailable=true because the compressed handbook is embedded." }
    if (-not ([bool]$status.runtimePdfPublished)) { throw "Runtime documentation status must declare runtimePdfPublished=true because the compressed handbook is embedded." }
    if (-not [string]::Equals([string]$status.releasePdfFileName, "LocalGPT-$Version.pdf", [StringComparison]::OrdinalIgnoreCase)) { throw "Runtime documentation did not preserve the embedded PDF identity." }
    if ([long]$status.releasePdfBytes -lt 1048576) { throw "Runtime documentation did not preserve the embedded PDF size metadata." }
    $physicalPdf = Get-Item -LiteralPath (Join-Path $DocumentationRoot "LocalGPT-$Version.pdf")
    if ([long]$physicalPdf.Length -ne [long]$status.pdfBytes) { throw "Embedded LocalGPT PDF byte size does not match documentation-status.json." }
    if ($null -ne $status.maximumSanePdfBytes -and [long]$physicalPdf.Length -gt [long]$status.maximumSanePdfBytes) { throw "Embedded LocalGPT PDF exceeds the configured sane-size ceiling." }

    Write-Host "Verified complete LocalGPT $Version DocFX modern HTML and compressed embedded PDF documentation in $DocumentationRoot" -ForegroundColor Green
}

$appVersion = Resolve-ProjectVersion -ProjectPath $appProject

function Prepare-LocalGptDocumentation {
    if ($script:documentationPrepared) { return }

    if (-not [string]::IsNullOrWhiteSpace($PreparedDocumentationRoot)) {
        $preparedRoot = [IO.Path]::GetFullPath($PreparedDocumentationRoot)
        if (-not (Test-Path -LiteralPath $preparedRoot -PathType Container)) { throw "Prepared LocalGPT documentation root is missing: $preparedRoot" }
        Assert-LocalGptDocumentationPayload -DocumentationRoot $preparedRoot -Version $appVersion -RequirePhysicalPdf
        Remove-Item -LiteralPath $script:documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Path $script:documentationCacheRoot -Force | Out-Null
        Copy-Item -Path (Join-Path $preparedRoot '*') -Destination $script:documentationCacheRoot -Recurse -Force
        $script:documentationPrepared = $true
        Write-Host "Reused parent-prepared LocalGPT documentation for this Linux release child." -ForegroundColor Green
        return
    }

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

    Write-Host "Running fail-fast LocalGPT HTTP startup smoke test before documentation generation..." -ForegroundColor Cyan
    & (Join-Path $root 'build/Test-LocalGptStartupHealth.ps1') -AssemblyPath $documentationAssembly -TimeoutSeconds 45

    Write-Host "Generating the complete LocalGPT documentation once for all runtime packages..." -ForegroundColor Cyan
    & $documentationScript `
        -RepositoryRoot $root `
        -AssemblyPath $documentationAssembly `
        -XmlDocumentationPath $documentationXml `
        -Version $appVersion `
        -OutputWebRoot $documentationOutput `
        -DocumentationCacheRoot $documentationToolCacheBase `
        -PackagingTool $releasePackagingTool `
        -RequirePdf `
        -DisablePdfToolProvisioning:$DisableDocumentationToolProvisioning

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
        Copy-Item -LiteralPath $entry.FullName -Destination (Join-Path $DestinationRoot $entry.Name) -Recurse -Force
    }

    $pdfPath = Join-Path $DestinationRoot $pdfName
    if (-not (Test-Path -LiteralPath $pdfPath -PathType Leaf)) {
        throw "Embedded LocalGPT documentation PDF is missing after runtime documentation copy: $pdfPath"
    }
    $statusPath = Join-Path $DestinationRoot "documentation-status.json"
    $status = Get-Content -LiteralPath $statusPath -Raw | ConvertFrom-Json
    $releasePdfBytes = [long](Get-Item -LiteralPath $pdfPath).Length
    $status | Add-Member -NotePropertyName releasePdfFileName -NotePropertyValue $pdfName -Force
    $status | Add-Member -NotePropertyName releasePdfBytes -NotePropertyValue $releasePdfBytes -Force
    $status | Add-Member -NotePropertyName runtimePdfPublished -NotePropertyValue $true -Force
    $status | Add-Member -NotePropertyName pdfAvailable -NotePropertyValue $true -Force
    $status | Add-Member -NotePropertyName pdfBytes -NotePropertyValue $releasePdfBytes -Force
    $status | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $statusPath -Encoding utf8
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

function Test-ExistingReleaseBundleComplete {
    param([Parameter(Mandatory)][string]$Version)
    $versionDirectory = Join-Path $artifacts $Version
    $checksumPath = Join-Path $versionDirectory 'SHA256SUMS.txt'
    if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) { return $false }
    $lines = @(Get-Content -LiteralPath $checksumPath -Encoding UTF8 | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($lines.Count -eq 0) { return $false }
    $manifestNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($line in $lines) {
        if ([string]$line -notmatch '^([0-9A-Fa-f]{64})\s+(.+)$') { return $false }
        $expectedHash = $Matches[1].ToLowerInvariant()
        $name = $Matches[2].Trim()
        if ([string]::IsNullOrWhiteSpace($name) -or $name.IndexOfAny([char[]]"/\\") -ge 0) { return $false }
        $path = Join-Path $versionDirectory $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $false }
        $actualHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        if (-not [string]::Equals($expectedHash, $actualHash, [StringComparison]::Ordinal)) { return $false }
        [void]$manifestNames.Add($name)
    }
    $requiredBundleNames = @($wirePackageName, $releasePackagingPackageName, "LocalGPT-$Version.pdf")
    foreach ($requiredBundleName in $requiredBundleNames) {
        if (-not $manifestNames.Contains($requiredBundleName)) { return $false }
    }
    $payloadFiles = @(Get-ChildItem -LiteralPath $versionDirectory -File | Where-Object { $_.Name -ne 'SHA256SUMS.txt' })
    return $manifestNames.Count -eq $payloadFiles.Count
}

function Move-OrReuseReleaseFile {
    param(
        [Parameter(Mandatory)][string]$SourcePath,
        [Parameter(Mandatory)][string]$DestinationDirectory,
        [switch]$Move
    )
    $destination = Join-Path $DestinationDirectory ([IO.Path]::GetFileName($SourcePath))
    $sourceFull = [IO.Path]::GetFullPath($SourcePath)
    $destinationFull = [IO.Path]::GetFullPath($destination)
    if ([string]::Equals($sourceFull, $destinationFull, [StringComparison]::OrdinalIgnoreCase)) {
        if (-not (Test-Path -LiteralPath $destinationFull -PathType Leaf)) { throw "Release file is missing: $destinationFull" }
        return $destinationFull
    }

    if (Test-Path -LiteralPath $destinationFull -PathType Leaf) {
        if (Test-Path -LiteralPath $sourceFull -PathType Leaf) {
            $sourceInfo = Get-Item -LiteralPath $sourceFull
            $destinationInfo = Get-Item -LiteralPath $destinationFull
            if ($sourceInfo.Length -ne $destinationInfo.Length) { throw "Release resume conflict: $sourceFull and $destinationFull have different sizes." }
            $sourceHash = (Get-FileHash -LiteralPath $sourceFull -Algorithm SHA256).Hash
            $destinationHash = (Get-FileHash -LiteralPath $destinationFull -Algorithm SHA256).Hash
            if (-not [string]::Equals($sourceHash, $destinationHash, [StringComparison]::OrdinalIgnoreCase)) { throw "Release resume conflict: $sourceFull and $destinationFull contain different bytes." }
            if ($Move) { Remove-Item -LiteralPath $sourceFull -Force }
        }
        return $destinationFull
    }

    if (-not (Test-Path -LiteralPath $sourceFull -PathType Leaf)) { throw "Required release file is missing: $sourceFull" }
    if ($Move) { Move-Item -LiteralPath $sourceFull -Destination $destinationFull }
    else { Copy-Item -LiteralPath $sourceFull -Destination $destinationFull -Force }
    return $destinationFull
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
    if ($ForceRebuildArtifacts -and (Test-Path -LiteralPath $versionDirectory -PathType Container)) {
        Remove-Item -LiteralPath $versionDirectory -Recurse -Force
    }
    if ((Test-ExistingReleaseBundleComplete -Version $Version) -and -not $ForceRebuildArtifacts) {
        Write-Host "Upload-ready release bundle already exists and all SHA-256 entries validate: $versionDirectory" -ForegroundColor Green
        return
    }
    New-Item -ItemType Directory -Path $versionDirectory -Force | Out-Null

    $uniqueZipPaths = @($ReleaseZipPaths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
    if ($uniqueZipPaths.Count -eq 0) { throw "No release artifacts were produced for release $Version." }

    # Move large generated artifacts directly into the final version directory. If a previous run
    # died halfway through this step, byte-identical files already present there are reused instead
    # of copied again. This keeps peak disk usage low and makes final-bundle assembly crash-resumable.
    foreach ($zipPath in $uniqueZipPaths) {
        Move-OrReuseReleaseFile -SourcePath $zipPath -DestinationDirectory $versionDirectory -Move | Out-Null
    }
    Move-OrReuseReleaseFile -SourcePath $DocumentationPdfPath -DestinationDirectory $versionDirectory -Move | Out-Null
    foreach ($supportFile in @($ReadmePath, $LicensePath, $WireProtocolPackagePath, $ReleasePackagingPackagePath, $SetupIconPath)) {
        if (-not (Test-Path -LiteralPath $supportFile -PathType Leaf)) { throw "Required upload-ready release file is missing: $supportFile" }
        Move-OrReuseReleaseFile -SourcePath $supportFile -DestinationDirectory $versionDirectory | Out-Null
    }
    if ($RequireWindowsX64Setup) {
        if (-not (Test-Path -LiteralPath $WindowsX64SetupExecutablePath -PathType Leaf)) { throw "Windows x64 setup executable is required for the full release bundle but is missing: $WindowsX64SetupExecutablePath" }
        Move-OrReuseReleaseFile -SourcePath $WindowsX64SetupExecutablePath -DestinationDirectory $versionDirectory | Out-Null
    }
    elseif (Test-Path -LiteralPath $WindowsX64SetupExecutablePath -PathType Leaf) {
        Move-OrReuseReleaseFile -SourcePath $WindowsX64SetupExecutablePath -DestinationDirectory $versionDirectory | Out-Null
    }

    $checksumPath = Join-Path $versionDirectory 'SHA256SUMS.txt'
    $checksumLines = foreach ($file in Get-ChildItem -LiteralPath $versionDirectory -File | Sort-Object Name) {
        if ($file.Name -eq 'SHA256SUMS.txt') { continue }
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $($file.Name)"
    }
    [IO.File]::WriteAllLines($checksumPath, [string[]]$checksumLines, (New-Object Text.UTF8Encoding($false)))
    if (-not (Test-ExistingReleaseBundleComplete -Version $Version)) { throw "Release bundle checksum verification failed after assembly: $versionDirectory" }
    Write-Host "Upload-ready release bundle: $versionDirectory" -ForegroundColor Green
}

if (-not $ForceRebuildArtifacts -and -not $SkipReleaseBundle -and $Runtime -eq 'all' -and (Test-ExistingReleaseBundleComplete -Version $appVersion)) {
    Write-Host "Release $appVersion is already complete and SHA-256 verified at $(Join-Path $artifacts $appVersion). Nothing will be rebuilt or resubmitted. Use -ForceRebuildArtifacts to intentionally rebuild it." -ForegroundColor Green
    return
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

    $mode = 'Full'
    $selfContained = 'true'
    $publishFolder = Join-Path $artifacts ("staging/$Rid/$($mode.ToLowerInvariant())")
    if ($Rid.StartsWith('osx-') -and -not $ForceRebuildArtifacts) {
        $existingNativeArtifacts = @(
            & $script:nativeReleasePackagingScript -ProductName 'LocalGPT' -ExecutableName 'LocalGPT' -Version $appVersion -Rid $Rid -Mode $mode -PayloadDirectory $publishFolder -OutputDirectory $artifacts -PackagingTool $script:releasePackagingTool -DependencyPolicy LocalGPT -ProbeExistingArtifactsOnly -MacIconSource (Join-Path $root 'src/LocalGPT/wwwroot/android-chrome-512x512.png') -DmgBackgroundPath (Join-Path $root 'build/assets/LocalGPT-dmg-background.png')
        )
        if ($existingNativeArtifacts.Count -eq 3) {
            foreach ($artifact in $existingNativeArtifacts) { $script:releaseZipPaths.Add([string]$artifact) }
            return
        }
    }
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
        $nativeArtifacts = & $script:nativeReleasePackagingScript -ProductName 'LocalGPT' -ExecutableName $appExecutable -Version $appVersion -Rid $Rid -Mode $mode -PayloadDirectory $publishFolder -OutputDirectory $artifacts -PackagingTool $script:releasePackagingTool -DependencyPolicy LocalGPT -UseContainerFallback:$UseContainerPackaging -ProvisionHomebrewTools:$ProvisionNativePackagingTools -RequireOptionalPackages:$RequireOptionalNativePackages -ForceRebuildArtifacts:$ForceRebuildArtifacts -MacIconSource (Join-Path $root 'src/LocalGPT/wwwroot/android-chrome-512x512.png') -DmgBackgroundPath (Join-Path $root 'build/assets/LocalGPT-dmg-background.png')
        foreach ($artifact in @($nativeArtifacts)) { if (-not [string]::IsNullOrWhiteSpace([string]$artifact)) { $script:releaseZipPaths.Add([string]$artifact) } }
        # Native artifacts are now complete; do not keep another multi-gigabyte documentation-bearing RID tree alive.
        Remove-Item -LiteralPath $publishFolder -Recurse -Force -ErrorAction SilentlyContinue
        $transientMacApp = Join-Path $artifacts 'LocalGPT.app'
        if ($Rid.StartsWith('osx-')) { Remove-Item -LiteralPath $transientMacApp -Recurse -Force -ErrorAction SilentlyContinue }
        Write-Host "Released transient $Rid $mode staging workspace after native package validation." -ForegroundColor DarkCyan
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
        Write-Host "Reused the verified HTML documentation and compressed embedded PDF payload for $Rid." -ForegroundColor Cyan
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
        if ($releaseHost -ne 'Windows') {
            Write-Warning "Skipping the optional WinUI/WebView wrapper for $Rid because it is a Windows-native finishing step. The Windows LocalGPT application and setup payloads are still cross-published from $releaseHost."
        }
        else {
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

$releaseHost = Get-ReleaseHostFamily
$runtimes = if ($Runtime -eq "all") {
    @(Get-HostDefaultRuntimes)
} elseif ($Runtime -eq "all-rids") {
    @("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")
} else {
    @($Runtime)
}

$wslLinuxRuntimes = @()
$wslResolvedDistribution = ''
$wslWasRunningBeforeProbe = $false
$wslEffectiveShutdown = $WslShutdown
$wslExecutable = $null
if ($releaseHost -eq 'Windows' -and -not $WslChildBuild -and $WslLinux -ne 'Off') {
    $wslCandidates = if ($Runtime -eq 'all') {
        @('linux-x64','linux-arm64')
    } else {
        @($runtimes | Where-Object { $_ -in @('linux-x64','linux-arm64') })
    }

    if ($wslCandidates.Count -gt 0) {
        $wslExecutable = Get-WslReleaseExecutable
        if (-not [string]::IsNullOrWhiteSpace($wslExecutable)) {
            $wslResolvedDistribution = Resolve-WslReleaseDistribution -WslExecutable $wslExecutable -RequestedDistribution $WslDistribution
        }

        if (-not [string]::IsNullOrWhiteSpace($wslResolvedDistribution)) {
            $runningBeforeProbe = @(Get-WslReleaseRunningDistributions -WslExecutable $wslExecutable)
            $wslWasRunningBeforeProbe = @($runningBeforeProbe | Where-Object { [string]::Equals($_, $wslResolvedDistribution, [StringComparison]::OrdinalIgnoreCase) }).Count -gt 0
            $wslStatus = Get-WslReleaseBuildStatus -WslExecutable $wslExecutable -Distribution $wslResolvedDistribution
            if ((-not $wslStatus.CoreReady) -and $ProvisionWslBuildTools) {
                Write-Host "Provisioning the existing WSL distribution '$wslResolvedDistribution' because -ProvisionWslBuildTools was requested..." -ForegroundColor Cyan
                & (Join-Path $root 'Setup-WslLinuxBuild.ps1') -Distribution $wslResolvedDistribution -Provision -Shutdown Never
                $wslStatus = Get-WslReleaseBuildStatus -WslExecutable $wslExecutable -Distribution $wslResolvedDistribution
            }
            $wslLicenseReady = $wslStatus.CoreReady -and (Test-WslReleaseDevExpressLicenseAvailable -WslExecutable $wslExecutable -Distribution $wslResolvedDistribution -Status $wslStatus)
            if ($wslStatus.CoreReady -and $wslLicenseReady) {
                $wslLinuxRuntimes = @($wslCandidates)
                $runtimes = @($runtimes | Where-Object { $_ -notin $wslLinuxRuntimes })
                if ($WslShutdown -eq 'IfStarted') { $wslEffectiveShutdown = if ($wslWasRunningBeforeProbe) { 'Never' } else { 'Always' } }
                Write-Host "Ready WSL Linux backend '$wslResolvedDistribution' will build: $($wslLinuxRuntimes -join ', ')." -ForegroundColor Cyan
            }
            else {
                $reason = if (-not $wslStatus.CoreReady) { Get-WslReleaseReadinessMessage $wslStatus } else { 'DevExpress build license is not available through the WSL profile or Windows license bridge.' }
                if (-not $wslWasRunningBeforeProbe) { & $wslExecutable --terminate $wslResolvedDistribution 2>$null | Out-Null }
                if ($WslLinux -eq 'Require') { throw "WSL Linux release was required, but '$wslResolvedDistribution' is not ready: $reason" }
                Write-Host "WSL Linux release backend not used: $reason" -ForegroundColor DarkCyan
                if ($Runtime -eq 'all') { Write-Host 'Continuing with the normal Windows release only. Run .\\Setup-WslLinuxBuild.ps1 -Provision to enable automatic Linux packaging.' -ForegroundColor DarkCyan }
                else { Write-Host 'Explicit Linux RIDs remain in the local runtime list, so the existing cross-publish path is preserved.' -ForegroundColor DarkCyan }
            }
        }
        else {
            if ($WslLinux -eq 'Require') { throw 'WSL Linux release was required, but no usable WSL distribution is installed and initialized.' }
            if ($Runtime -eq 'all') { Write-Host 'No ready WSL distribution was found; continuing with the normal Windows release only.' -ForegroundColor DarkCyan }
            else { Write-Host 'No ready WSL distribution was found; explicit Linux RIDs will use the existing Windows cross-publish path.' -ForegroundColor DarkCyan }
        }
    }
}

$displayRuntimes = @($runtimes) + @($wslLinuxRuntimes | ForEach-Object { "$_ (WSL)" })
Write-Host "Release host $releaseHost selected runtime(s): $($displayRuntimes -join ', ')" -ForegroundColor Cyan
if ($Runtime -eq 'all') {
    Write-Host "Runtime 'all' is host-aware. Use -Runtime all-rids only for an explicit cross-host publish attempt." -ForegroundColor DarkCyan
    if ($releaseHost -eq 'Windows' -and $wslLinuxRuntimes.Count -gt 0) {
        Write-Host 'Windows is the release coordinator: Windows packages are native; Linux self-contained Full packages are delegated headlessly to WSL and imported into the same release bundle.' -ForegroundColor DarkCyan
    }
    if ($releaseHost -eq 'macOS') {
        Write-Host "macOS is the full release coordinator: macOS x64/ARM64, Linux x64/ARM64, and Windows x64/x86/ARM64 application/setup payloads are built in one run." -ForegroundColor DarkCyan
        Write-Host "macOS produces native self-contained Full DMG/PKG/TAR.GZ packages; Linux self-contained Full TAR.GZ/DEB are managed and RPM uses Homebrew rpmbuild when available. AppImage remains a Linux/WSL/container finishing step." -ForegroundColor DarkCyan
        if ($IncludeWindowsWrapper) { Write-Host 'The optional WinUI/WebView wrapper remains Windows-host-only and is skipped on macOS.' -ForegroundColor DarkCyan }
    }
}
& (Join-Path $root 'build/Initialize-MacReleaseTrust.ps1') -ProductName 'LocalGPT' -SelectedRuntimes @($runtimes) -AllowUnsignedMacPackages:$AllowUnsignedMacPackages
$requiresNativePackaging = @($runtimes | Where-Object { -not $_.StartsWith('win-') }).Count -gt 0

New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
Remove-Item -LiteralPath $documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
Ensure-WireProtocolPackage

# Documentation PDF assembly now also uses the shared release-packaging tool, so prepare it on every host.
$releasePackagingToolOutput = @(& (Join-Path $root 'build/Ensure-ReleasePackagingPackage.ps1') -Configuration $Configuration -Version $releasePackagingVersion)
if ($releasePackagingToolOutput.Count -ne 1 -or [string]::IsNullOrWhiteSpace([string]$releasePackagingToolOutput[0])) { throw "Release-packaging tool preparation returned $($releasePackagingToolOutput.Count) pipeline value(s); expected exactly one executable path." }
$releasePackagingTool = [string]$releasePackagingToolOutput[0]
if (-not (Test-Path -LiteralPath $releasePackagingTool -PathType Leaf)) { throw "Prepared release-packaging tool is missing: $releasePackagingTool" }
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

try {
    foreach ($rid in $runtimes) { Publish-Runtime $rid }

    if ($wslLinuxRuntimes.Count -gt 0) {
        $wslArtifacts = @(& (Join-Path $root 'build/Invoke-WslLinuxRelease.ps1') `
            -ProductName LocalGPT `
            -RepositoryRoot $root `
            -OutputDirectory $artifacts `
            -PreparedDocumentationRoot $documentationCacheRoot `
            -Version $appVersion `
            -Runtimes $wslLinuxRuntimes `
            -Configuration $Configuration `
            -Distribution $wslResolvedDistribution `
            -UseContainerPackaging:$UseContainerPackaging `
            -RequireOptionalNativePackages:$RequireOptionalNativePackages `
            -KeepBuildTree:$KeepWslBuildTree `
            -Shutdown $wslEffectiveShutdown)
        foreach ($artifact in $wslArtifacts) { if (-not [string]::IsNullOrWhiteSpace([string]$artifact)) { $script:releaseZipPaths.Add([string]$artifact) } }
    }

    $documentationPdf = Join-Path $documentationCacheRoot "LocalGPT-$appVersion.pdf"
    $winX64Profile = Resolve-ReleaseProfile -Rid "win-x64"
    $winX64SetupFolder = Resolve-ProfilePublishFolder -ProjectPath $setupProject -ProfileName $winX64Profile.SetupProfile
    $winX64SetupExecutable = Join-Path $winX64SetupFolder "LocalGPTInstallerConsole.exe"
    $requireWinX64Setup = @($runtimes) -contains "win-x64"
    $licensePath = Join-Path $root "LICENSE.MD"
    if (-not (Test-Path -LiteralPath $licensePath -PathType Leaf)) { $licensePath = Join-Path $root "LICENSE" }

    if (-not $SkipReleaseBundle) {
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
    else {
        Write-Host 'Skipping the upload-ready version bundle because this is a delegated Linux release child.' -ForegroundColor DarkCyan
    }
}
finally {
    Remove-Item -LiteralPath $documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath (Join-Path $artifacts 'staging') -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath (Join-Path $artifacts 'LocalGPT.app') -Recurse -Force -ErrorAction SilentlyContinue
    $postReleaseBuildStateCount = Clear-RepositoryReleaseBuildState -BestEffort
    if ($postReleaseBuildStateCount -gt 0) {
        Write-Host "Released $postReleaseBuildStateCount repository-local bin/obj build-state director$(if ($postReleaseBuildStateCount -eq 1) { 'y' } else { 'ies' }) after the release attempt." -ForegroundColor DarkCyan
    }
}

$releaseBundle = if ($SkipReleaseBundle) { $artifacts } else { Join-Path $artifacts $appVersion }
Write-Host "Release output: $releaseBundle" -ForegroundColor Green
Write-Host "Protocol package cache: $(Join-Path $artifacts $wirePackageName)" -ForegroundColor Green
Write-Host "Release-packaging package cache: $(Join-Path $artifacts $releasePackagingPackageName)" -ForegroundColor Green
