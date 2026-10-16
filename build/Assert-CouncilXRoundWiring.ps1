$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$required = @(
    @{ Path = 'src/LocalGPT/Services/MultiModelCouncilService.RunOrchestration.cs'; Tokens = @('LoadContinuationConversationAsync','ContinuedFromConversationId') }
    @{ Path = 'src/LocalGPT/Services/CouncilRuntimeService.PromptAndTextRuntime.cs'; Tokens = @('CreateUpdate') }
    @{ Path = 'src/LocalGPT/Services/CouncilRuntimeService.cs'; Tokens = @('NotRunLane') }
    @{ Path = 'src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs'; Tokens = @('RunUiActionAsync') }
    # Source-read readiness requires native callable tools as well as the broker, not a null
    # list which Ollama intentionally interprets as broker-only metadata.
    @{ Path = 'src/LocalGPT/Services/CouncilAutomaticFunctionPolicyService.cs'; Tokens = @('var exposed = preferred', '"dxai.tools.discover", "dxai.tools.preview", "dxai.tools.invoke"', 'strictTeam || strictStep', 'IsStrict: true', '!IsBrokerTool(name)', 'exposed,') }
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
if ($findings.Count -gt 0) { foreach ($finding in $findings) { Write-Error $finding }; throw ('Source contract audit failed: ' + $findings.Count + ' finding(s).') }
Write-Host 'Maintained PowerShell source contract audit passed.'
