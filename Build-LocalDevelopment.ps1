param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [ValidateSet("x64", "x86", "arm64")]
    [string]$Platform = "x64",
    [switch]$UseWireProtocolPackage,
    [switch]$Clean,
    [switch]$AllowMissingDevExpressLicense
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $root 'build/Assert-PowerShellCompatibility.ps1')
if ($null -eq (Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1)) {
    throw 'dotnet was not found on PATH. Install the repository-required .NET SDK and reopen the terminal before running this build script.'
}
& (Join-Path $root 'build/Initialize-DevExpressLicense.ps1') -Require:(-not $AllowMissingDevExpressLicense)
Write-Host "Refreshing reviewed LocalGPT frontend SHA-256 inventory before the ordered CLI build..." -ForegroundColor DarkCyan
& (Join-Path $root 'build/Update-JavaScriptDiagnosticsManifest.ps1')
& (Join-Path $root 'build/Assert-JavaScriptDiagnostics.ps1')
Write-Host "Clearing repository-local obj restore state before the ordered CLI build..." -ForegroundColor DarkCyan
Get-ChildItem (Join-Path $root "src") -Directory -Recurse -Force |
    Where-Object { $_.Name -eq "obj" } |
    Sort-Object FullName -Descending |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
$solutionRoot = Join-Path $root "src"
$wireProject = Join-Path $solutionRoot "LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj"
$appProject = Join-Path $solutionRoot "LocalGPT/LocalGPT.csproj"
$setupProject = Join-Path $solutionRoot "LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj"
$wrapperProject = Join-Path $solutionRoot "LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj"
$documentationScript = Join-Path $root "build/Build-Documentation.ps1"
$pagesSnapshotScript = Join-Path $root "build/Update-GitHubPagesSnapshot.ps1"
$pagesSnapshotArchive = Join-Path $root ".github/pages/localgpt-kawaii-docs.zip"
$packageDirectory = Join-Path $root "packages"
$wireVersion = "2.1.1"
$wirePackage = Join-Path $packageDirectory "LocalGPT.WireProtocolVersion.$wireVersion.nupkg"
$packageRestoreCache = Join-Path $root "artifacts/development/.nuget-packages"
$useProject = if ($UseWireProtocolPackage) { "false" } else { "true" }
$appOutputRoot = Join-Path (Split-Path -Parent $appProject) "bin/$Configuration/net10.0"
$documentationRoot = Join-Path $appOutputRoot "wwwroot/help-docs"
$requireDocumentationPdf = $true

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

function Assert-LocalGptDocumentation {
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
            throw "The LocalGPT development build did not produce required documentation: $requiredArtifact"
        }
    }

    $statusPath = Join-Path $DocumentationRoot "documentation-status.json"
    $status = Get-Content -LiteralPath $statusPath -Raw | ConvertFrom-Json
    if ([string]$status.documentationMode -ne "docfx") { throw "The LocalGPT development build used static documentation instead of the DocFX modern site." }
    if ([string]$status.pdfMode -notin @("html-browser-print", "docfx-pdf-plugin")) { throw "The LocalGPT development build did not produce the complete HTML-backed documentation PDF." }
    if ([string]$status.pdfMode -eq "html-browser-print" -and [int]$status.pdfSourcePageCount -lt 10) { throw "The LocalGPT documentation PDF did not include the expected HTML page set." }
    if (-not ([bool]$status.completeApiReference)) { throw "The LocalGPT development documentation does not contain the complete XML-generated API reference." }
    if ([int]$status.unresolvedAssemblyReferenceCount -ne 0) { throw "The LocalGPT development documentation contains unresolved assembly references: $($status.unresolvedAssemblyReferences -join ', ')" }
    if ([int]$status.apiYamlCount -le 1 -or [int]$status.apiHtmlCount -le 1) { throw "The LocalGPT development documentation API graph is incomplete." }
    if ([long]$status.pdfBytes -lt 65536) { throw "The LocalGPT development PDF is unexpectedly small and is not accepted as complete." }
    if ([int]$status.pdfCandidateCount -lt 1 -or [string]::IsNullOrWhiteSpace([string]$status.pdfGeneratedSourcePath)) { throw "The LocalGPT development build did not record a real documentation PDF source." }

    Write-Host "Verified complete LocalGPT $Version DocFX modern HTML, XML API reference and HTML-backed PDF documentation." -ForegroundColor Green
}

$appVersion = Resolve-ProjectVersion -ProjectPath $appProject

if ($Clean) {
    Get-ChildItem $solutionRoot -Directory -Recurse -Force |
        Where-Object { $_.Name -in @("bin", "obj") } |
        Sort-Object FullName -Descending |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}

New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
$wireBuildProperties = @(
    "-p:Platform=AnyCPU",
    "-p:PlatformTarget=AnyCPU",
    "-p:RuntimeIdentifier=",
    "-p:RuntimeIdentifiers="
)

Write-Host "Restoring and building the RID-neutral protocol project..." -ForegroundColor Cyan
Invoke-DotNet -Arguments (@("restore", $wireProject, "--disable-parallel", "--force-evaluate") + $wireBuildProperties) -FailureMessage "Wire protocol restore failed."
Invoke-DotNet -Arguments (@("build", $wireProject, "-c", $Configuration, "--no-restore", "-maxcpucount:1") + $wireBuildProperties) -FailureMessage "Wire protocol build failed."

Remove-Item -LiteralPath $wirePackage -Force -ErrorAction SilentlyContinue
Write-Host "Packing the RID-neutral protocol for package-mode consumers..." -ForegroundColor Cyan
Invoke-DotNet -Arguments (@("pack", $wireProject, "-c", $Configuration, "--no-build", "-o", $packageDirectory, "-p:PackageVersion=$wireVersion", "-maxcpucount:1") + $wireBuildProperties) -FailureMessage "Wire protocol package creation failed."
if (-not (Test-Path -LiteralPath $wirePackage)) { throw "Expected wire protocol package was not produced: $wirePackage" }

if ($UseWireProtocolPackage) {
    # Local packages are intentionally rebuilt with the stable public version. Use an isolated restore
    # cache and evict only this package/version so NuGet cannot reuse an older 2.1.1 extraction.
    $cachedWirePackage = Join-Path $packageRestoreCache "localgpt.wireprotocolversion/$wireVersion"
    Remove-Item -LiteralPath $cachedWirePackage -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $packageRestoreCache -Force | Out-Null
}

$appProperties = @(
    "-p:UseLocalWireProtocolProject=$useProject",
    "-p:LocalGptWireProtocolVersion=$wireVersion",
    "-p:LocalGptWireProtocolPackageDirectory=$packageDirectory",
    "-p:RestoreAdditionalProjectSources=$packageDirectory",
    "-p:CopyLocalLockFileAssemblies=true"
)
if ($UseWireProtocolPackage) { $appProperties += "-p:RestorePackagesPath=$packageRestoreCache" }
Write-Host "Restoring and building LocalGPT..." -ForegroundColor Cyan
Invoke-DotNet -Arguments (@("restore", $appProject, "--disable-parallel", "--force-evaluate") + $appProperties) -FailureMessage "LocalGPT application restore failed."
Invoke-DotNet -Arguments (@("build", $appProject, "-c", $Configuration, "--no-restore", "-maxcpucount:1", "-p:BuildProjectReferences=false", "-p:BuildLocalGptDocumentation=false", "-p:SeedLocalGptGitHubPagesSnapshotOnBuild=false") + $appProperties) -FailureMessage "LocalGPT application build failed."

Write-Host "Restoring and building the installer..." -ForegroundColor Cyan
Invoke-DotNet -Arguments @("restore", $setupProject, "--disable-parallel", "--force-evaluate") -FailureMessage "LocalGPT installer restore failed."
Invoke-DotNet -Arguments @("build", $setupProject, "-c", $Configuration, "--no-restore", "-maxcpucount:1") -FailureMessage "LocalGPT installer build failed."

# Keep the selected dependency graph for the complete development run. The protocol package is still
# created above for package consumers, and -UseWireProtocolPackage explicitly selects that graph. A
# second unconditional package-mode rebuild reused the mutable 2.1.1 NuGet cache and could replace an
# already successful source-project build with hundreds of false missing-type errors.

$documentationAssembly = Join-Path $appOutputRoot "LocalGPT.dll"
$documentationXml = Join-Path $appOutputRoot "LocalGPT.xml"
if (-not (Test-Path -LiteralPath $documentationScript -PathType Leaf)) { throw "Documentation build script not found: $documentationScript" }
if (-not (Test-Path -LiteralPath $documentationAssembly -PathType Leaf)) { throw "Documentation assembly not found: $documentationAssembly" }
if (-not (Test-Path -LiteralPath $documentationXml -PathType Leaf)) { throw "Documentation XML not found: $documentationXml" }

Write-Host "Generating LocalGPT documentation once from the completed RID-neutral application build..." -ForegroundColor Cyan
& $documentationScript `
    -RepositoryRoot $root `
    -AssemblyPath $documentationAssembly `
    -XmlDocumentationPath $documentationXml `
    -Version $appVersion `
    -OutputWebRoot $documentationRoot `
    -RequirePdf:$requireDocumentationPdf
Assert-LocalGptDocumentation -DocumentationRoot $documentationRoot -Version $appVersion
if (-not (Test-Path -LiteralPath $pagesSnapshotScript -PathType Leaf)) { throw "GitHub Pages snapshot script not found: $pagesSnapshotScript" }
Write-Host "Validating and seeding the LocalGPT $appVersion GitHub Pages snapshot from the completed documentation build..." -ForegroundColor Cyan
& $pagesSnapshotScript -DocumentationRoot $documentationRoot -OutputArchive $pagesSnapshotArchive
if (-not (Test-Path -LiteralPath $pagesSnapshotArchive -PathType Leaf)) { throw "LocalGPT GitHub Pages snapshot update failed to create $pagesSnapshotArchive." }

$wrapperProperties = @(
    "-p:Platform=$Platform",
    "-p:UseLocalWireProtocolProject=$useProject",
    "-p:LocalGptWireProtocolVersion=$wireVersion",
    "-p:LocalGptWireProtocolPackageDirectory=$packageDirectory",
    "-p:RestoreAdditionalProjectSources=$packageDirectory"
)
if ($UseWireProtocolPackage) { $wrapperProperties += "-p:RestorePackagesPath=$packageRestoreCache" }
Write-Host "Restoring and building the optional WinUI wrapper..." -ForegroundColor Cyan
Invoke-DotNet -Arguments (@("restore", $wrapperProject, "--disable-parallel", "--force-evaluate") + $wrapperProperties) -FailureMessage "LocalGPT WinUI wrapper restore failed."
# The WinUI XAML compiler resolves protocol types through LocalGPT's transitive project reference.
# Release publish builds that graph normally; the previous development-only BuildProjectReferences=false
# skipped the architecture-specific protocol target and left the compiler looking for
# bin\$Platform\$Configuration\net10.0\LocalGPT.WireProtocolVersion.dll. Keep documentation
# disabled for this dependency pass, but allow MSBuild to materialize the complete reference graph.
Invoke-DotNet -Arguments (@("build", $wrapperProject, "-c", $Configuration, "--no-restore", "-maxcpucount:1", "-p:BuildProjectReferences=true", "-p:BuildLocalGptDocumentation=false", "-p:SeedLocalGptGitHubPagesSnapshotOnBuild=false") + $wrapperProperties) -FailureMessage "LocalGPT WinUI wrapper build failed."

$dependencyMode = if ($UseWireProtocolPackage) { "package" } else { "source project" }
Write-Host "LocalGPT development build completed in protocol -> app ($dependencyMode graph) -> installer -> documentation -> wrapper order." -ForegroundColor Green
