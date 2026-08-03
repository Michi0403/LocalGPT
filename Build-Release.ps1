param(
    [ValidateSet("all", "win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")]
    [string]$Runtime = "all",
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [string]$WireProtocolVersion = "2.1.0",
    [string]$WireProtocolPackageUrl = "",
    [switch]$UseBundledWireProtocolPackage,
    [switch]$IncludeWindowsWrapper
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Join-Path $root "LocalGPTWebviewWrapper"
$artifacts = Join-Path $root "artifacts\release"
$packageDirectory = Join-Path $root "packages"
$appProject = Join-Path $solutionRoot "LocalGPT\LocalGPT.csproj"
$setupProject = Join-Path $solutionRoot "LocalGPTInstallerConsole\LocalGPTInstallerConsole.csproj"
$wrapperProject = Join-Path $solutionRoot "LocalGPTWebviewWrapper\LocalGPTWebviewWrapper.csproj"
$wireProject = Join-Path $solutionRoot "LocalGPT.WireProtocolVersion\LocalGPT.WireProtocolVersion.csproj"
$documentationScript = Join-Path $root "build\Build-Documentation.ps1"
$wirePackageName = "LocalGPT.WireProtocolVersion.$WireProtocolVersion.nupkg"
$wirePackage = Join-Path $packageDirectory $wirePackageName
$localApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
$sharedWirePackageDirectory = if ([string]::IsNullOrWhiteSpace($localApplicationData)) { $null } else { Join-Path $localApplicationData "LocalGPT\NuGet" }
$documentationCacheRoot = Join-Path $artifacts ".documentation-cache"
$documentationPrepared = $false

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
        [Parameter(Mandatory)][string]$Version
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
    if ([string]$status.documentationMode -ne "docfx") { throw "Published LocalGPT documentation did not use the DocFX modern site." }
    if ([string]$status.pdfMode -notin @("html-browser-print", "docfx-pdf-plugin")) { throw "Published LocalGPT documentation does not contain the complete HTML-backed documentation PDF." }
    if ([string]$status.pdfMode -eq "html-browser-print" -and [int]$status.pdfSourcePageCount -lt 10) { throw "The LocalGPT documentation PDF did not include the expected HTML page set." }
    if (-not ([bool]$status.completeApiReference)) { throw "Published LocalGPT documentation is missing the complete XML-generated API reference." }
    if ([int]$status.apiYamlCount -le 1 -or [int]$status.apiHtmlCount -le 1) { throw "Published LocalGPT documentation contains an incomplete API graph." }
    if ([long]$status.pdfBytes -lt 65536) { throw "Published LocalGPT documentation contains an unexpectedly small PDF." }
    if ([int]$status.pdfCandidateCount -lt 1 -or [string]::IsNullOrWhiteSpace([string]$status.pdfGeneratedSourcePath)) { throw "Published LocalGPT documentation did not record a real documentation PDF source." }

    Write-Host "Verified complete LocalGPT $Version DocFX modern HTML and HTML-backed PDF documentation in $DocumentationRoot" -ForegroundColor Green
}

$appVersion = Resolve-ProjectVersion -ProjectPath $appProject

function Prepare-LocalGptDocumentation {
    if ($script:documentationPrepared) { return }

    if (-not (Test-Path -LiteralPath $documentationScript -PathType Leaf)) {
        throw "Documentation build script not found: $documentationScript"
    }

    $appProjectDirectory = Split-Path -Parent $appProject
    $neutralOutputRoot = Join-Path $appProjectDirectory "bin\$Configuration\net10.0"
    $documentationAssembly = Join-Path $neutralOutputRoot "LocalGPT.dll"
    $documentationXml = Join-Path $neutralOutputRoot "LocalGPT.xml"
    $documentationOutput = Join-Path $neutralOutputRoot "wwwroot\help-docs"
    $packageGraphProperties = @(
        "-p:UseLocalWireProtocolProject=false",
        "-p:LocalGptWireProtocolVersion=$WireProtocolVersion",
        "-p:LocalGptWireProtocolPackageDirectory=$packageDirectory",
        "-p:RestoreAdditionalProjectSources=$packageDirectory",
        "-p:RuntimeIdentifier=",
        "-p:RuntimeIdentifiers="
    )

    Write-Host "Building the RID-neutral LocalGPT assembly once for shared release documentation..." -ForegroundColor Cyan
    Invoke-DotNet -Arguments (@("restore", $appProject, "--disable-parallel", "--force-evaluate") + $packageGraphProperties) -FailureMessage "RID-neutral LocalGPT restore for documentation failed."
    Invoke-DotNet -Arguments (@("build", $appProject, "-c", $Configuration, "--no-restore", "-maxcpucount:1", "-p:BuildProjectReferences=false", "-p:BuildLocalGptDocumentation=false") + $packageGraphProperties) -FailureMessage "RID-neutral LocalGPT build for documentation failed."

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

    Assert-LocalGptDocumentationPayload -DocumentationRoot $documentationOutput -Version $appVersion
    Remove-Item -LiteralPath $script:documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $script:documentationCacheRoot -Force | Out-Null
    Copy-Item -Path (Join-Path $documentationOutput "*") -Destination $script:documentationCacheRoot -Recurse -Force
    $script:documentationPrepared = $true
    Write-Host "Cached one verified documentation payload for all RID publishes." -ForegroundColor Green
}

function Resolve-PublishProfilePath {
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$ProfileName
    )

    $projectDirectory = Split-Path -Parent $ProjectPath
    $profilePath = Join-Path $projectDirectory "Properties\PublishProfiles\$ProfileName.pubxml"
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

function Publish-Runtime {
    param([Parameter(Mandatory)][string]$Rid)

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

    $publishedDocumentationRoot = Join-Path $appFolder "wwwroot\help-docs"
    if ($script:documentationPrepared) {
        if (-not (Test-Path -LiteralPath $script:documentationCacheRoot -PathType Container)) {
            throw "The shared LocalGPT documentation cache is missing: $script:documentationCacheRoot"
        }
        Remove-Item -LiteralPath $publishedDocumentationRoot -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Path $publishedDocumentationRoot -Force | Out-Null
        Copy-Item -Path (Join-Path $script:documentationCacheRoot "*") -Destination $publishedDocumentationRoot -Recurse -Force
        Write-Host "Reused the verified complete documentation payload for $Rid." -ForegroundColor Cyan
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

    Compress-Archive -Path $appFolder -DestinationPath $appZip -CompressionLevel Optimal -Force
    Compress-Archive -Path $setupFolder -DestinationPath $setupZip -CompressionLevel Optimal -Force
    Write-Host "Created $appZip" -ForegroundColor Green
    Write-Host "Created $setupZip" -ForegroundColor Green
}

New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
Remove-Item -LiteralPath $documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
Ensure-WireProtocolPackage
Copy-Item $wirePackage (Join-Path $artifacts $wirePackageName) -Force
if ($sharedWirePackageDirectory) {
    New-Item -ItemType Directory -Path $sharedWirePackageDirectory -Force | Out-Null
    Copy-Item $wirePackage (Join-Path $sharedWirePackageDirectory $wirePackageName) -Force
    Write-Host "Updated shared LocalGPT protocol package cache: $sharedWirePackageDirectory" -ForegroundColor Green
}

Prepare-LocalGptDocumentation

$runtimes = if ($Runtime -eq "all") {
    @("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")
} else {
    @($Runtime)
}

try {
    foreach ($rid in $runtimes) { Publish-Runtime $rid }
}
finally {
    Remove-Item -LiteralPath $documentationCacheRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Release output: $artifacts" -ForegroundColor Green
Write-Host "Protocol package: $(Join-Path $artifacts $wirePackageName)" -ForegroundColor Green
