Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))

function Assert-Source([string]$RelativePath, [string[]]$Patterns) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing reviewed source $RelativePath" }
    $text = [IO.File]::ReadAllText($path)
    foreach ($pattern in $Patterns) {
        if (-not [regex]::IsMatch($text, $pattern)) { throw "Missing source contract $pattern in $RelativePath" }
    }
}

Assert-Source 'src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.BuildOrchestrationTemplates.cs' @(
    'compiler-intake.*?', 'canUseOrganicFunctions: true',
    'council\.repository\.workspace\.stage', 'session\.sandbox\.run', 'session\.sandbox\.runs',
    'localgpt\.console\.execute', 'localgpt\.console\.history', 'project\.workspace\.file\.read')
Assert-Source 'src/LocalGPT/Services/RepositoryWorkspacePromotionDxAiFunctions.cs' @(
    'RequiresHumanConfirmation: true', 'request\.UserConfirmed', 'catalog\.ArchiveRoots\.Contains',
    'FileAttributes\.ReparsePoint', 'File\.Copy\(path, copyPath, overwrite: false\)',
    '1024L \* 1024 \* 1024', 'Directory\.Delete\(target, recursive: true\)')
Assert-Source 'src/LocalGPT/Services/SessionSandboxDxAiFunctions.cs' @(
    'RequiresHumanConfirmation: true', 'SupportsAutomaticInvocation: false',
    'session\.sandbox\.runs', 'session\.sandbox\.run', 'sandboxes\.Start\(',
    'request\.UserConfirmed', 'sandboxes\.ListRuns')
Assert-Source 'src/LocalGPT/Services/HyperlightUnikraftSessionSandboxProvider.cs' @('start\.Environment\.Clear', '--initrd', 'VerifyBundledRuntime')
Assert-Source 'src/LocalGPT/Services/OneWire/OneWireTrafficJournal.cs' @(
    'WriteFile\(entry, traffic\)', 'JsonSerializer\.Serialize\(entry\)',
    'File\.AppendAllText', 'File\.Move\(path, path \+ "\.previous", overwrite: true\)', 'fileSync')
Assert-Source 'src/LocalGPT/BusinessObjects/OneWireTrafficOptions.cs' @('FileLoggingEnabled', 'FilePath', 'MaximumFileBytes')
$config = Get-Content -LiteralPath (Join-Path $root 'src/LocalGPT/appsettings.json') -Raw | ConvertFrom-Json
if ($config.OneWire.Traffic.FileLoggingEnabled -ne $true -or [long]$config.OneWire.Traffic.MaximumFileBytes -lt 1048576) {
    throw 'LocalGPT 1-Wire disk journal is not enabled with a bounded rotation policy.'
}
$project = [xml](Get-Content -LiteralPath (Join-Path $root 'src/LocalGPT/LocalGPT.csproj') -Raw)
$versionNode = $project.SelectSingleNode('/Project/PropertyGroup/Version[normalize-space(.) != ""]')
if ($null -eq $versionNode -or [string]$versionNode.InnerText -notmatch '^[0-9]+\.[0-9]\.[0-9]$') {
    throw 'LocalGPT application version must have single-digit minor and patch slots.'
}
Assert-Source 'src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.BuildOrchestrationTemplates.cs' @(
    'RequiredCompletedFunctions = \["council\.development\.artifact\.verify"\]',
    'council\.artifact_workspace\.codedom\.preview', 'council\.artifact_workspace\.codedom\.write',
    'council\.artifact_workspace\.sandbox\.run',
    'council\.artifact_workspace_file\.read', 'council\.artifact_workspace_file\.write')
Assert-Source 'src/LocalGPT/Services/ArtifactWorkspaceDxAiFunctions.cs' @(
    '"council\.artifact_workspace_file\.read"',
    '"council\.artifact_workspace_file\.write"',
    '/api/dxai/functions/council\.artifact_workspace_file\.read/invoke',
    '/api/dxai/functions/council\.artifact_workspace_file\.write/invoke')
Assert-Source 'src/LocalGPT/Services/CouncilDevelopmentDxAiFunctions.cs' @(
    'council\.artifact_workspace\.codedom\.preview', 'council\.artifact_workspace\.codedom\.write',
    'council\.artifact_workspace\.sandbox\.run', 'council\.development\.artifact\.verify',
    'codeDom\.GenerateCSharpSource', 'codeDom\.GenerateCSharpSandboxProgram',
    'request\.UserConfirmed', 'sandboxes\.Start\(', 'SourceExtension',
    'ExpectedSourceSha256', 'SourceSha256',
    'reviews\.GetReviewAsync', 'stages\.VerifyStageArchiveAsync')
Assert-Source 'src/LocalGPT/Services/CouncilDevelopmentWorkspaceService.cs' @(
    'RecordStageBaselineAsync', 'VerifyStageArchiveAsync', 'SHA256\.HashDataAsync',
    'No file bytes changed', 'archive\.Entries', 'archivedHash', 'source ZIP')
Assert-Source 'src/LocalGPT/Services/RepositoryWorkspacePromotionDxAiFunctions.cs' @('developmentWorkspaces\.RecordStageBaselineAsync')
Assert-Source 'src/LocalGPT/Services/ArtifactWorkspaceDxAiFunctions.cs' @('SourceSha256 = Convert\.ToHexString\(SHA256\.HashData')
Assert-Source 'src/LocalGPT/Program.ServiceRegistration.cs' @('ICouncilDevelopmentWorkspaceService, CouncilDevelopmentWorkspaceService')
Write-Host 'LocalGPT Council/CodeDOM/Hyperlight/verified repository workflow and traffic file source contracts passed.' -ForegroundColor Green
