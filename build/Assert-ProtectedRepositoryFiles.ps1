[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $PSScriptRoot 'protected-files.sha256'

function Get-NormalizedTextSha256([string]$path) {
    $text = [IO.File]::ReadAllText($path)
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $encoding = [Text.UTF8Encoding]::new($false)
    $bytes = $encoding.GetBytes($normalized)
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha256.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

$expectedFiles = @(
    'AGENTS.md',
    'CLAUDE.md',
    'llms.txt',
    'SECURITY.md',
    'global.json',
    '.claude/settings.json',
    '.github/copilot-instructions.md',
    '.github/CODEOWNERS',
    '.github/workflows/source-hygiene.yml',
    'docs/ARCHITECTURE.md',
    'docs/ARCHITECTURE_FOR_AI.md',
    'docs/COMPONENT_SAFETY_AND_SHORT_TERM_MEMORY.md',
    'docs/COMPILER_VALIDATION_AND_GENERATION_RULES.md',
    'docs/HUMAN_AI_COLLABORATION.md',
    'docs/PEACEFUL_USE_COVENANT.md',
    'docs/RELEASE_PROCESS.md',
    'docs/SECURE_MAINTENANCE.md',
    'build/README.md',
    'build/RepositoryValidation.Common.ps1',
    'build/Assert-CSharpSyntax.ps1',
    'build/Assert-ProjectClosure.ps1',
    'build/Assert-ComponentSafety.ps1',
    'build/Assert-InteractiveServerRenderModes.ps1',
    'build/Assert-AsyncContinuationPolicy.ps1',
    'build/async-continuation-baseline.json',
    'build/Assert-MethodDiagnostics.ps1',
    'build/Assert-ApplicationStaticPolicy.ps1',
    'build/Assert-TextServiceOwnership.ps1',
    'build/Assert-IteratorExceptionPolicy.ps1',
    'build/method-diagnostics-baseline.json',
    'build/application-static-baseline.json',
    'build/text-service-ownership-baseline.json',
    'build/iterator-exception-baseline.json',
    'build/Assert-WorkflowContracts.ps1',
    'build/Assert-HumanCollaboration.ps1',
    'build/Assert-SourceFormatting.ps1',
    'build/Invoke-RepositoryValidation.ps1',
    'build/New-VerifiedSourcePackage.ps1',
    'build/Assert-ProtectedRepositoryFiles.ps1',
    'build/Protect-GovernanceFiles.ps1',
    'docs/THEME_RUNTIME_ARCHITECTURE.md',
    'build/Assert-ThemeArchitecture.ps1',
    'docs/DATABASE_FIRST_PROJECT_ARCHITECTURE.md',
    'docs/OPEN_TASKS.md',
    'build/Assert-ArchitectureTasks.ps1',
    'docs/EF_MIGRATION_SNAPSHOT_ARCHITECTURE.md',
    'build/Assert-EfSnapshotArchitecture.ps1',
    'docs/DATABASE_MIGRATION_BOOTSTRAP.md',
    'build/Assert-DatabaseMigrationBootstrap.ps1',
    'docs/SERVICE_LIFECYCLE_AND_ASYNC_ARCHITECTURE.md',
    'build/Assert-ServiceArchitecture.ps1',
    '.gitignore',
    'Directory.Build.targets',
    'Build-LocalDevelopment.ps1',
    'Build-Release.ps1',
    'build/Assert-GitSourceVisibility.ps1',
    'build/Assert-RuntimeValueOwnership.ps1',
    'build/runtime-value-ownership-baseline.json',
    'docs/RUNTIME_VALUE_OWNERSHIP.md',
    'LocalGPTWebviewWrapper/LocalGPT/Interfaces/ICouncilTextPatternDataService.cs',
    'LocalGPTWebviewWrapper/LocalGPT/Interfaces/ISystemVariableDefinitionService.cs',
    'LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/CouncilTextPatternDataService.cs',
    'LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/InitialDataCatalog.cs',
    'LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/SystemVariableDefinitionService.cs',
    'LocalGPTWebviewWrapper/LocalGPT/Services/CouncilTextService.cs',
    'LocalGPTWebviewWrapper/LocalGPT/Services/CouncilRuntimeService.cs',
    'LocalGPTWebviewWrapper/LocalGPT/Program.cs',
    'build/Assert-JavaScriptDiagnostics.ps1',
    'docs/JAVASCRIPT_RUNTIME_DIAGNOSTICS.md',
    'tests/final23_javascript_runtime_fix.py',
    'tests/localgpt_render_async_contracts.py',
    'LocalGPTWebviewWrapper/LocalGPT/Components/App.razor',
    'LocalGPTWebviewWrapper/LocalGPT/Components/InteractiveStartupMarker.razor',
    'LocalGPTWebviewWrapper/LocalGPT/Components/Layout/HumanCollaborationInbox.razor',
    'LocalGPTWebviewWrapper/LocalGPT/Components/Layout/CouncilSpoolerPanel.razor',
    'LocalGPTWebviewWrapper/LocalGPT/Components/Layout/ThemeJsChangeDispatcher.cs',
    'LocalGPTWebviewWrapper/LocalGPT/wwwroot/css/localgpt-theme-contract.css',
    'build/Assert-PublishConfiguration.ps1',
    'build/Assert-InstallerWorkflow.ps1',
    'LocalGPTWebviewWrapper/LocalGPT/LocalGPT.csproj',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Program.cs',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Properties/launchSettings.json',
    'LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Default.cmd',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Install.cmd',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Update.cmd',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Start.cmd',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Start-NoBrowser.cmd',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Install-Ollama.cmd',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Pull-Models-Slim.cmd',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Pull-Models-RTX3060.cmd',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Pull-Models-Full.cmd',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Setup-Learning-Base.cmd',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Import-Recommended.cmd',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Uninstall.cmd',
    'LocalGPTWebviewWrapper/LocalGPT/Properties/PublishProfiles/winx64.pubxml',
    'LocalGPTWebviewWrapper/LocalGPT/Properties/PublishProfiles/winx86.pubxml',
    'LocalGPTWebviewWrapper/LocalGPT/Properties/PublishProfiles/winarm64.pubxml',
    'LocalGPTWebviewWrapper/LocalGPT/Properties/PublishProfiles/linuxx64.pubxml',
    'LocalGPTWebviewWrapper/LocalGPT/Properties/PublishProfiles/linuxarm64.pubxml',
    'LocalGPTWebviewWrapper/LocalGPT/Properties/PublishProfiles/macosx64.pubxml',
    'LocalGPTWebviewWrapper/LocalGPT/Properties/PublishProfiles/macosarm64.pubxml',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Properties/PublishProfiles/winx64.pubxml',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Properties/PublishProfiles/winx86.pubxml',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Properties/PublishProfiles/winarm64.pubxml',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Properties/PublishProfiles/linuxx64.pubxml',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Properties/PublishProfiles/linuxarm64.pubxml',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Properties/PublishProfiles/macosx64.pubxml',
    'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Properties/PublishProfiles/macosarm64.pubxml',
    'LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/Properties/PublishProfiles/winx64.pubxml',
    'LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/Properties/PublishProfiles/winx86.pubxml',
    'LocalGPTWebviewWrapper/LocalGPTWebviewWrapper/Properties/PublishProfiles/winarm64.pubxml'
)

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'Protected-file hash manifest is missing.'
}

$manifest = @{}
foreach ($line in Get-Content -LiteralPath $manifestPath) {
    $trimmed = $line.Trim()
    if (-not $trimmed -or $trimmed.StartsWith('#')) { continue }
    if ($trimmed -notmatch '^([0-9a-fA-F]{64})\s{2}(.+)$') {
        throw "Invalid protected-file manifest line: $line"
    }
    $relative = $Matches[2].Replace('\', '/')
    if ($manifest.ContainsKey($relative)) { throw "Duplicate protected-file manifest entry: $relative" }
    $manifest[$relative] = $Matches[1].ToLowerInvariant()
}

$errors = [System.Collections.Generic.List[string]]::new()
foreach ($relative in $expectedFiles) {
    if ($relative -eq 'build/protected-files.sha256') {
        if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
            $errors.Add("Missing protected file: $relative")
        }
        continue
    }

    if (-not $manifest.ContainsKey($relative)) {
        $errors.Add("Protected file is absent from the hash manifest: $relative")
        continue
    }

    $path = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $errors.Add("Missing protected file: $relative")
        continue
    }

    $actual = Get-NormalizedTextSha256 $path
    if ($actual -ne $manifest[$relative]) {
        $errors.Add("Protected file changed: $relative")
    }
}

$unexpected = $manifest.Keys | Where-Object { $_ -notin $expectedFiles }
foreach ($relative in $unexpected) {
    $errors.Add("Unexpected protected-file manifest entry: $relative")
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Protected repository files match the reviewed SHA-256 manifest.'
