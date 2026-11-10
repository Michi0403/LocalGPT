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
if ($project.Project.PropertyGroup.Version -ne '7.0.5') { throw 'LocalGPT next application version must be 7.0.5.' }
Write-Host 'LocalGPT Council/Hyperlight/repository staging and dedicated traffic file source contracts passed.' -ForegroundColor Green
