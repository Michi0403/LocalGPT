[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$errors = [System.Collections.Generic.List[string]]::new()

function Add-Error([string]$message) { $errors.Add($message) }

$protectedGuard = Join-Path $root 'build/Assert-ProtectedRepositoryFiles.ps1'
if (-not (Test-Path -LiteralPath $protectedGuard -PathType Leaf)) {
    Add-Error 'Protected repository file guard is missing.'
}
else {
    & $protectedGuard
    if ($LASTEXITCODE -ne 0) { Add-Error 'Protected repository file validation failed.' }
}

$requiredAgentBoundaryFiles = @(
    'AGENTS.md',
    'CLAUDE.md',
    '.claude/settings.json',
    '.github/copilot-instructions.md',
    '.github/CODEOWNERS'
)
foreach ($relative in $requiredAgentBoundaryFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $relative) -PathType Leaf)) {
        Add-Error "Required agent boundary file is missing: $relative"
    }
}

$allFiles = Get-ChildItem $root -Recurse -Force -File
$agentOverrides = Get-ChildItem $root -Recurse -Force -File -Filter 'AGENTS.override.md' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '[\\/](\.git|\.vs|bin|obj)[\\/]' }
if ($agentOverrides) {
    Add-Error 'Repository AGENTS.override.md files are forbidden because they can silently replace the reviewed governance boundary.'
}

$localClaudeSettings = Join-Path $root '.claude/settings.local.json'
if (Test-Path -LiteralPath $localClaudeSettings) {
    Add-Error 'Tracked or packaged .claude/settings.local.json is forbidden; local overrides must not weaken project governance.'
}

$sourceFiles = $allFiles | Where-Object {
    $_.FullName -notmatch '[\\/](\.git|\.vs|\.cr|bin|obj)[\\/]'
}

$forbiddenImport = $sourceFiles |
    Where-Object { $_.Extension -eq '.cs' } |
    Select-String -SimpleMatch 'using static System.Net.WebRequestMethods;'
if ($forbiddenImport) { Add-Error 'Forbidden static WebRequestMethods import found.' }


$mainSourceRoot = Join-Path $root 'LocalGPTWebviewWrapper/LocalGPT'
$plainStaticsDirectory = Join-Path $mainSourceRoot 'Extensions/PlainStatics'
if (Test-Path $plainStaticsDirectory) {
    Add-Error 'Legacy Extensions/PlainStatics must not be reintroduced. Runtime behavior belongs in injected services.'
}

$legacyStaticTypeNames = @(
    'CouncilChatStaticsGeneral',
    'CouncilChatStringFunctions',
    'GlobalVariableSlopCollectionToRemove',
    'SQLLiteFunctions',
    'SQLLiteTableFunctions',
    'HttpAIStaticsGeneral',
    'DevExpressFunctions',
    'TableFunctions',
    'UrlGenerator',
    'RegExStatics',
    'JsonFunctions'
)
$csharpFiles = $sourceFiles | Where-Object { $_.Extension -eq '.cs' }
foreach ($typeName in $legacyStaticTypeNames) {
    $matches = $csharpFiles | Select-String -SimpleMatch $typeName
    if ($matches) { Add-Error "Legacy static utility type '$typeName' was reintroduced." }
}

$allowedStaticImports = @('using static LocalGPT.Services.LocalGptCatalogService;')
$staticImports = $csharpFiles | Select-String -Pattern '^\s*using\s+static\s+' | ForEach-Object { $_.Line.Trim() }
foreach ($staticImport in $staticImports) {
    if ($staticImport -notin $allowedStaticImports) {
        Add-Error "Static import is not allowlisted: $staticImport. Prefer qualification or an injected service to avoid namespace collisions."
    }
}

$allowedStaticClassFiles = @(
    (Join-Path $mainSourceRoot 'Program.cs'),
    (Join-Path $mainSourceRoot 'Extensions/StringExtensions.cs'),
    (Join-Path $mainSourceRoot 'Extensions/CollectionsExtensions.cs'),
    (Join-Path $root 'LocalGPTWebviewWrapper/LocalGPTInstallerConsole/Program.cs')
)
$staticClasses = $csharpFiles | Select-String -Pattern '^\s*(public|internal|private)?\s*static\s+class\s+'
foreach ($match in $staticClasses) {
    if ($match.Path -notin $allowedStaticClassFiles) {
        Add-Error "Unexpected static class outside the explicit language/composition-root allowlist: $($match.Path):$($match.LineNumber)."
    }
}

$programPath = Join-Path $mainSourceRoot 'Program.cs'
if ((Get-Content $programPath -Raw) -match 'public\s+static\s+int\s+Port') {
    Add-Error 'Program.Port must not return as mutable process-wide state; pass the selected port through startup methods.'
}

$appsettings = Join-Path $root 'LocalGPTWebviewWrapper/LocalGPT/appsettings.json'
if ((Get-Content $appsettings -Raw) -match '"DetailedErrors"\s*:\s*true') {
    Add-Error 'Production appsettings.json must not enable DetailedErrors.'
}

$props = Join-Path $root 'Directory.Build.props'
if (-not (Test-Path $props)) { Add-Error 'Directory.Build.props is missing.' }
else {
    $text = Get-Content $props -Raw
    foreach ($required in '<NuGetAudit>true</NuGetAudit>', '<NuGetAuditMode>all</NuGetAuditMode>', 'NU1903', 'NU1904') {
        if (-not $text.Contains($required)) { Add-Error "Missing dependency audit setting: $required" }
    }
}

$catalog = Join-Path $root 'LocalGPTWebviewWrapper/LocalGPT/Services/Persistence/InitialDataCatalog.cs'
if (-not (Test-Path $catalog)) { Add-Error 'InitialDataCatalog.cs is missing.' }
else {
    $catalogText = Get-Content $catalog -Raw
    if ($catalogText -match 'docs[/\\]\*\.md') {
        Add-Error 'Wildcard Markdown knowledge seeding is forbidden.'
    }
    foreach ($requiredPolicy in 'AGENTS.md', 'SECURITY.md', 'llms.txt', 'PEACEFUL_USE_COVENANT.md', 'SECURE_MAINTENANCE.md') {
        if (-not $catalogText.Contains($requiredPolicy)) {
            Add-Error "Reviewed repository policy is missing from the explicit knowledge allowlist: $requiredPolicy"
        }
    }
    if ($catalogText.Contains('docs/PROJECT_IDENTITY.md')) {
        Add-Error 'Project identity/history must not be injected into runtime model briefings.'
    }
}


$bootstrap = Join-Path $root 'LocalGPTWebviewWrapper/LocalGPT/Services/AiContextBootstrapService.cs'
if (Test-Path $bootstrap) {
    $bootstrapText = Get-Content $bootstrap -Raw
    if ($bootstrapText -match 'Michael Fleischer|Michi0403') {
        Add-Error 'Runtime bootstrap prompts must not personalize model behavior around the maintainer.'
    }
}

$legacyBootstrap = Join-Path $root 'build/Install-OllamaLocalGPTAndModels.ps1'
if (-not (Test-Path $legacyBootstrap)) {
    Add-Error 'The fail-closed legacy bootstrap marker is missing.'
}
else {
    $legacyText = Get-Content $legacyBootstrap -Raw
    if (-not $legacyText.Contains('DisabledLegacyBootstrap')) {
        Add-Error 'Legacy all-in-one bootstrap must remain disabled.'
    }
    if ($legacyText -match 'Invoke-WebRequest|Invoke-RestMethod|Start-Process|SetEnvironmentVariable|Set-ExecutionPolicy|Remove-Item|Expand-Archive') {
        Add-Error 'Disabled legacy bootstrap contains active side-effect commands.'
    }
}


$removedActionHelpers = @(
    'build/Legacy',
    'build/PublishBlazorSolutionAndCreateZips.ps1',
    'build/Start#U2011LocalGPT.ps1',
    'build/blazorPublisher_repo_to_text_generator.ps1',
    'build/localgpt_repo_to_text_generator.ps1',
    'build/tacosportal_repo_to_text_generator.ps1',
    'build/ollamapullallrtx3060.ps1'
)
foreach ($relative in $removedActionHelpers) {
    if (Test-Path (Join-Path $root $relative)) {
        Add-Error "Broad side-effect helper must not be shipped in public source: $relative"
    }
}

$installerLauncherDirectory = Join-Path $root 'LocalGPTWebviewWrapper/LocalGPTInstallerConsole'
$oneClickLaunchers = Get-ChildItem $installerLauncherDirectory -Filter '*.cmd' -File -ErrorAction SilentlyContinue
if ($oneClickLaunchers) {
    Add-Error ('One-click installer/model/delete launchers must not be shipped: ' + (($oneClickLaunchers | Select-Object -ExpandProperty Name) -join ', '))
}


$dxFunctionDoc = Join-Path $root 'docs/DXAI_FUNCTIONS_AND_CHANGE_REVIEWS.md'
if (-not (Test-Path $dxFunctionDoc)) { Add-Error 'DXAIFunction/change-review architecture documentation is missing.' }

$requiredDxFunctionFiles = @(
    'Interfaces/IDxAiFunctionRegistry.cs',
    'Interfaces/ICodeGenerationWorkflowService.cs',
    'Interfaces/ICouncilCodeGenerationPlanService.cs',
    'Services/DxAiFunctionRegistry.cs',
    'Services/CodeGenerationWorkflowService.cs',
    'Services/CouncilCodeGenerationPlanService.cs',
    'Controller/DxAiFunctionsController.cs',
    'Controller/CodeGenerationController.cs'
)
foreach ($relative in $requiredDxFunctionFiles) {
    if (-not (Test-Path (Join-Path $mainSourceRoot $relative))) {
        Add-Error "Required DXAIFunction/change-review component is missing: $relative"
    }
}

$ollamaClientPath = Join-Path $mainSourceRoot 'Services/OllamaThinkingChatClient.cs'
if (Test-Path $ollamaClientPath) {
    $ollamaClientText = Get-Content $ollamaClientPath -Raw
    if ($ollamaClientText -notmatch 'SupportsAutomaticInvocation\s*&&' -or
        $ollamaClientText -notmatch '!function\.RequiresHumanConfirmation') {
        Add-Error 'Automatic Ollama tool discovery must remain limited to explicitly automatic, confirmation-free functions.'
    }
    if ($ollamaClientText -match 'BuildParametersSchema\s*\([^)]*\)\s*=>\s*[^;]*switch') {
        Add-Error 'Ollama parameter schemas must come from DXAIFunction descriptors, not a hard-coded function-name switch.'
    }
}

$workflowPath = Join-Path $mainSourceRoot 'Services/CodeGenerationWorkflowService.cs'
if (Test-Path $workflowPath) {
    $workflowText = Get-Content $workflowPath -Raw
    foreach ($requiredBoundary in 'ApprovalConsumed', 'ExpectedReviewHash', 'UserConfirmedBuild', 'ResolveInsideRoot') {
        if (-not $workflowText.Contains($requiredBoundary)) {
            Add-Error "Code-generation workflow lost required confirmation/path boundary: $requiredBoundary"
        }
    }
}

$forbiddenArtifacts = $allFiles | Where-Object {
    $_.FullName -match '[\\/](\.vs|\.cr|bin|obj)[\\/]' -or
    $_.Name -match '\.(db|db-wal|db-shm|sqlite|sqlite3|pfx|p12|snk|pem|key|cer|crt)$' -or
    $_.Name -match '\.(ttf|otf|woff|woff2|eot)$' -or
    $_.Name -in @('DevExpressLicense.cs', 'devextreme-license.js')
}
if ($forbiddenArtifacts) {
    Add-Error ('Generated/runtime/private artifacts found: ' + (($forbiddenArtifacts | Select-Object -First 10 -ExpandProperty FullName) -join '; '))
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Security policy source checks passed.'
