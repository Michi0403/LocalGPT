$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$required = @(
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.RunOrchestration.cs'; Tokens = @('LoadContinuationConversationAsync','ContinuedFromConversationId') }
    @{ Path = 'src/LocalGPT/Services/CouncilRuntimeService.PromptAndTextRuntime.cs'; Tokens = @('CreateUpdate') }
    @{ Path = 'src/LocalGPT/Services/CouncilRuntimeService.cs'; Tokens = @('NotRunLane') }
    @{ Path = 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'; Tokens = @('RunUiActionAsync') }
    # Source-read readiness requires the original unrestricted native-function contract,
    # preferred tools plus broker fallback, and exclusive strict lists.
    @{ Path = 'src/LocalGPT/Services/CouncilAutomaticFunctionPolicyService.cs'; Tokens = @('var exposed = preferred', '"dxai.tools.discover", "dxai.tools.preview", "dxai.tools.invoke"', 'strictTeam || strictStep', 'IsStrict: true', '!IsBrokerTool(name)', 'exposed,') }
    @{ Path = 'src/LocalGPT/Services/OllamaThinkingChatClient.Tools.cs'; Tokens = @('automaticFunctionAllowList is null || automaticFunctionAllowList.Contains(function.Name)', 'function.SupportsAutomaticInvocation', 'function.AvailableToAi') }
    @{ Path = 'src/LocalGPT/Services/OllamaThinkingChatClient.ToolExecution.cs'; Tokens = @('var directMatch = functions.FirstOrDefault', 'automaticFunctionAllowList.Contains("dxai.tools.invoke")', 'var transportMatches = eligible', 'return transportMatches.Count == 1') }
    @{ Path = 'src/LocalGPT/Services/ChatUploadWorkspaceDxAiFunctions.cs'; Tokens = @('"chat.upload_workspace_files"', '"chat.upload_workspace_file"', 'Source: "DIHandler"', 'SupportsAutomaticInvocation: true') }
    @{ Path = 'src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.GameProjectTemplates.cs'; Tokens = @('"game-development-baseline"', '"chat.upload_workspace_files"', '"chat.upload_workspace_file"', '"game-development-mechanics"', '"game-development-ai"') }
    @{ Path = 'src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs'; Tokens = @('StrictTeamAllowList = 5', 'StrictStepAllowList = 6', 'bool IsStrict = false') }
    @{ Path = 'src/LocalGPT/Components/Pages/CouncilTeams.razor.cs'; Tokens = @('CouncilAutomaticFunctionPolicyMode.StrictTeamAllowList', 'CouncilAutomaticFunctionPolicyMode.StrictStepAllowList') }
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.WorkflowPrompting.cs'; Tokens = @('Tool initiative:', 'Repair boundary:', 'automaticFunctionPolicy.IsStrict') }
)
$findings = [Collections.Generic.List[string]]::new()
foreach ($entry in $required) {
    $file = Join-Path $root $entry.Path
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { $findings.Add('Missing source: ' + $entry.Path); continue }
    $content = [IO.File]::ReadAllText($file)
    foreach ($token in $entry.Tokens) {
        if ($content.IndexOf($token, [StringComparison]::Ordinal) -lt 0) { $findings.Add($entry.Path + ': missing contract ' + $token) }
    }
}
$nativeToolsSource = [IO.File]::ReadAllText((Join-Path $root 'src/LocalGPT/Services/OllamaThinkingChatClient.Tools.cs'))
if ($nativeToolsSource -match 'automaticFunctionAllowList\s+is\s+null\s*\?\s*function\.Source\s*==\s*"DxFunctionBroker"') {
    $findings.Add('Ollama automatic-tool default regressed to broker-only; 5.5.0 exposed all eligible DXFunctions.')
}
if ($findings.Count -gt 0) { foreach ($finding in $findings) { Write-Error $finding }; throw ('Source contract audit failed: ' + $findings.Count + ' finding(s).') }
Write-Host 'Maintained PowerShell source contract audit passed.'
