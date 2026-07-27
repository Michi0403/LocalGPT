Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Fail([string]$Message) { throw "Project maintenance architecture validation failed: $Message" }
$root = Split-Path -Parent $PSScriptRoot
$files = @{
    Models = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\BusinessObjects\ProjectMaintenanceModels.cs'
    ProjectModels = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\BusinessObjects\LocalGptProjectModels.cs'
    RevisionModels = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\BusinessObjects\ProjectArchitectureModels.cs'
    Context = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\BusinessObjects\EFCore\LocalGptMemoryDbContext.cs'
    Service = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Services\ProjectMaintenanceService.cs'
    CodeGeneration = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Services\CodeGenerationWorkflowService.cs'
    Interface = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Interfaces\IProjectMaintenanceService.cs'
    Controller = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Controller\ProjectMaintenanceController.cs'
    Functions = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Services\ProjectMaintenanceDxAiFunctions.cs'
    Page = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Components\Pages\ProjectMaintenance.razor'
    Migration = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Migrations\20260727190000_AddProjectMaintenanceWorkspaces.cs'
    IdentityMigration = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Migrations\20260727193000_FixProjectTrackedFileRevisionIdentity.cs'
    Snapshot = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Migrations\LocalGptMemoryDbContextModelSnapshot.cs'
    Program = Join-Path $root 'LocalGPTWebviewWrapper\LocalGPT\Program.cs'
    LocalizationGuard = Join-Path $root 'build\Assert-LocalizationIntegrity.ps1'
    GitSourceVisibilityGuard = Join-Path $root 'build\Assert-GitSourceVisibility.ps1'
}
foreach ($entry in $files.GetEnumerator()) {
    if (-not (Test-Path -LiteralPath $entry.Value -PathType Leaf)) { Fail "Missing $($entry.Key): $($entry.Value)" }
}
$content = @{}
foreach ($entry in $files.GetEnumerator()) { $content[$entry.Key] = Get-Content -LiteralPath $entry.Value -Raw -Encoding UTF8 }

foreach ($token in @(
    'ProjectWorkspaceRoot','ProjectCompilerInstallation','LocalGptProjectTrackedFile','ProjectBuildVerification',
    'StableFileKey','AbsolutePath','ProjectRelativePath','WorkspaceRelativePath','SolutionPath','ProjectFilePath',
    'StructureRegex','ContentFormatRegex','ContentHash','SourceSnapshotHash','SnapshotArchivePath',
    'TestsExecuted','TestsSucceeded','SourceChangedDuringVerification','EvidenceManifestPath')) {
    if ($content.Models -notmatch [regex]::Escape($token)) { Fail "Required model/path/evidence field is missing: $token" }
}
foreach ($token in @('ProjectType','SolutionSearchPattern','FileIncludePattern','FileExcludePattern')) {
    if ($content.ProjectModels -notmatch [regex]::Escape($token)) { Fail "Project discovery metadata is missing: $token" }
}
foreach ($token in @('SourceRootPath','SolutionPath','CompileVerified','CouncilVerified','ReadyForTesting')) {
    if ($content.RevisionModels -notmatch [regex]::Escape($token)) { Fail "Revision maintenance field is missing: $token" }
}
foreach ($token in @('ProjectWorkspaceRoots','ProjectCompilerInstallations','LocalGptProjectTrackedFiles','ProjectBuildVerifications')) {
    if ($content.Context -notmatch [regex]::Escape($token)) { Fail "DbContext set/configuration is missing: $token" }
    if ($content.Migration -notmatch [regex]::Escape($token)) { Fail "Migration table is missing: $token" }
}
if ($content.Context -notmatch 'ProjectId,\s*item\.RevisionId,\s*item\.ProjectRelativePath') { Fail 'Tracked-file uniqueness must include the revision id.' }
if ($content.IdentityMigration -notmatch 'IX_LocalGptProjectTrackedFiles_ProjectId_RevisionId_ProjectRelativePath') { Fail 'Revision-aware tracked-file index repair migration is missing.' }
if ($content.Snapshot -notmatch 'HasIndex\("ProjectId",\s*"RevisionId",\s*"ProjectRelativePath"\)\.IsUnique') { Fail 'EF model snapshot does not contain the revision-aware tracked-file identity.' }
if ($content.Program -notmatch 'AddScoped<IProjectMaintenanceService,\s*ProjectMaintenanceService>') { Fail 'Project maintenance service is not registered.' }

foreach ($token in @(
    'ResolveWorkspaceAsync','DiscoverCompilerInstallationsAsync','ValidateCompilerInstallationAsync','ScanProjectFilesAsync',
    'SaveTrackedFilePatternAsync','RegisterRevisionWorkspaceAsync','RunBuildVerificationAsync',
    'RecordCouncilBuildReviewAsync','ApproveRevisionReadyForTestAsync')) {
    if ($content.Interface -notmatch [regex]::Escape($token) -or $content.Service -notmatch [regex]::Escape($token)) { Fail "Project maintenance operation is incomplete: $token" }
}
foreach ($token in @(
    'RequireConfirmation(request.UserConfirmed','item.RevisionId == request.RevisionId',
    'request.RevisionId?.ToString("N") ?? "base"','CaptureTrackedSourceStateAsync',
    'requireStoredHashMatch: true','SourceHashBefore','SourceHashAfter','SourceChangedDuringVerification',
    'if (!verification.BuildSucceeded','if (!verification.CouncilReviewSucceeded)',
    'if (!string.Equals(currentState.Hash, verification.SourceSnapshotHash','CreateEntryFromFile','LocalGPT-Revisions')) {
    if ($content.Service -notmatch [regex]::Escape($token)) { Fail "Approval/hash/snapshot safeguard is missing: $token" }
}
foreach ($token in @('/usr/share/dotnet','\.dotnet','/usr/lib/jvm','Microsoft Visual Studio','EnvironmentVariablesJson')) {
    if ($content.Service -notmatch $token) { Fail "Cross-platform/custom compiler discovery safeguard is missing: $token" }
}
foreach ($token in @('CopyTrackedProjectIntoWorkspaceAsync','ComputeFileHashAsync','file.ContentHash','did not preserve the approved file bytes','RegisterRevisionWorkspaceAsync','ScanProjectFilesAsync')) {
    if ($content.CodeGeneration -notmatch [regex]::Escape($token)) { Fail "Lossless isolated CodeDOM maintenance safeguard is missing: $token" }
}
foreach ($name in @(
    'project.maintenance.get','project.revision.workspace.register','project.files.scan','project.file.patterns.save',
    'project.revision.build.verify','project.revision.council-review','project.revision.ready.approve')) {
    if ($content.Functions -notmatch [regex]::Escape($name)) { Fail "Required DXFunction is missing: $name" }
}
if ($content.Functions -notmatch 'RequiresHumanConfirmation:\s*true' -or $content.Functions -notmatch 'ApprovalRequiredBeforeCompletion:\s*true') { Fail 'Build and release DXFunctions must retain explicit human approval gates.' }
foreach ($route in @('compilers/discover','revisions/{revisionId:guid}/workspace','projects/{projectId:guid}/scan','files/{trackedFileId:guid}/patterns','projects/{projectId:guid}/verify','council-review','approve-ready')) {
    if ($content.Controller -notmatch [regex]::Escape($route)) { Fail "Controller route is missing: $route" }
}
foreach ($token in @('@page "/project-maintenance"','Revision source root','Structure regex','Compiler environment JSON','Run build verification','Approve ready for testing')) {
    if ($content.Page -notmatch [regex]::Escape($token)) { Fail "Project maintenance setup page is incomplete: $token" }
}
if ($content.Service -notmatch 'ILogger<ProjectMaintenanceService>' -or $content.Service -notmatch '\.LogInformation\(' -or $content.Service -notmatch '\bcatch\b') { Fail 'Project maintenance service logging/catch contract is incomplete.' }
if ($content.LocalizationGuard -notmatch 'case-insensitive duplicate keys' -or $content.LocalizationGuard -notmatch 'ConvertFrom-Json' -or $content.LocalizationGuard -notmatch '\[char\]0x2420') { Fail 'Localization duplicate-key/encoding guard is missing.' }
if ($content.GitSourceVisibilityGuard -notmatch 'git check-ignore' -or $content.GitSourceVisibilityGuard -notmatch 'Required .gitignore protection rule') { Fail 'Git source visibility guard is missing.' }
Write-Host 'Project maintenance architecture validation passed.'
