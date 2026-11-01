param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath($RepositoryRoot)
$findings = New-Object 'System.Collections.Generic.List[string]'

function Assert-SourceTokens([string]$relative, [string[]]$required) {
    $file = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        $findings.Add(('{0}(1,1): error LGAI6001: Missing architecture component.' -f $relative))
        return
    }
    $text = [IO.File]::ReadAllText($file)
    foreach ($token in $required) {
        if ($text.IndexOf($token, [StringComparison]::Ordinal) -lt 0) {
            $findings.Add(('{0}(1,1): error LGAI6001: Missing recovery contract: {1}' -f $relative, $token))
        }
    }
}

Assert-SourceTokens 'src/LocalGPT/Services/AiHelpEscalatorService.cs' @(
    'class AiHelpEscalatorService', 'IAiHelpEscalatorService', 'ValidateTaskId(report.TaskId)',
    'state.Failures++', 'state.Repeated', 'state.CuratorUsed = true;',
    'report.Status', 'File.Move(temporary, file, true)',
    'state.LastDecision.Type is "await_user" or "halt" or "completed"',
    'human.AuthorizeOrEnqueueAsync', 'QuestionScope: "Member"', 'GateMode: "NextPhase"',
    'CheckHumanResponseAsync', 'IAiHelpCuratorService', 'scopeFactory.CreateScope()',
    'db.HumanCollaborationRequests.AsNoTracking()', 'TryGetValue(taskId', 'LastDecision.HumanRequestId',
    'report.Status == "success"',
    'state.RecentAttemptIds.Contains(attemptId)',
    'item.Status != vocabulary.Get().HumanStatusAnswered'
)
Assert-SourceTokens 'src/LocalGPT/Services/AiHelpCuratorService.cs' @(
    'class AiHelpCuratorService', 'IProviderModelRuntimeService',
    'HelpEscalatorCuratorSystemPrompt', 'enableAutomaticTools: false',
    'HelpEscalatorCuratorModelKey', 'GetCandidatesAsync', 'cancellationToken',
    'IDxAiFunctionRegistry functions', 'registeredRequiredParameters', 'registeredOptionalParameters',
    '!string.Equals(model.SelectionKey, report.FailedModelSelectionKey',
    '"retry" or "ask_user" or "cannot_resolve"'
)
Assert-SourceTokens 'src/LocalGPT/Services/AiHelpEscalatorDxAiFunctions.cs' @(
    'class ReportAiHelpAttemptFunction', '"ai.help.report_attempt"',
    'class InspectAiHelpAttemptFunction', '"ai.help.status"',
    'IsCoordinationOnly: true', 'SupportsAutomaticInvocation: true',
    '"required":["taskId","task","action","status"]',
    '"required":["taskId"]'
)
Assert-SourceTokens 'src/LocalGPT/Controller/AiHelpEscalationController.cs' @(
    '[ApiController]', '[Route("api/ai/help")]', '[HttpPost("attempt")]',
    '[HttpGet("status")]', '[HttpPost("human-response")]',
    'IAiHelpEscalatorService'
)
Assert-SourceTokens 'src/LocalGPT/BusinessObjects/AiHelpEscalationModels.cs' @(
    'class AiHelpAttemptReport', 'class AiHelpTaskState', 'class AiHelpRecoveryDecision',
    'class AiHelpFailureEvidence'
)
Assert-SourceTokens 'src/LocalGPT/Program.ServiceRegistration.cs' @(
    'AddSingleton<IAiHelpEscalatorService, AiHelpEscalatorService>()',
    'AddScoped<IAiHelpCuratorService, AiHelpCuratorService>()',
    'typeof(IDxAiFunctionHandler).IsAssignableFrom(type.AsType())'
)
Assert-SourceTokens 'src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs' @(
    'HelpEscalatorGlobalInstruction', 'HelpEscalatorCuratorSystemPrompt',
    'HelpEscalatorAlternativeInstruction', 'HelpEscalatorUnguidedFailureLimit',
    'HelpEscalatorMaximumRetainedTasks', 'HelpEscalatorCuratorModelKey',
    'ai.help.report_attempt', 'ai.help.status', 'await_user'
)
Assert-SourceTokens 'src/LocalGPT/Services/GoalExecutionService.cs' @('IAiHelpEscalatorService aiHelpEscalator')
Assert-SourceTokens 'src/LocalGPT/Services/GoalExecutionService.DurableExecution.cs' @(
    'aiHelpEscalator.ReportAttemptAsync', 'GoalStepRecoveryExhausted', 'receipt.ExternalEffectsPossible ? GoalExecutionState.NeedsApproval'
)
Assert-SourceTokens 'src/LocalGPT/Services/MultiModelCouncilService.cs' @(
    'IAiHelpEscalatorService aiHelpEscalator', 'this.aiHelpEscalator = aiHelpEscalator;'
)
Assert-SourceTokens 'src/LocalGPT/Services/MultiModelCouncilService.RoundRecovery.cs' @(
    'aiHelpEscalator.ReportAttemptAsync', 'Status = "no_progress"', 'CouncilMemberRecoveryExhausted'
)
Assert-SourceTokens 'src/LocalGPT/Services/CouncilAutomaticFunctionPolicyService.cs' @(
    'strictTeam || strictStep', 'ai.help.report_attempt', 'ai.help.status'
)
Assert-SourceTokens 'src/LocalGPT/Services/CouncilTextService.CouncilTranscriptText.cs' @('HelpEscalatorGlobalInstruction')
Assert-SourceTokens 'src/LocalGPT/Services/AiContextBootstrapService.cs' @('HelpEscalatorGlobalInstruction')
Assert-SourceTokens 'src/LocalGPT/Services/GoalExecutionService.SemanticPlanning.cs' @('HelpEscalatorGlobalInstruction')
Assert-SourceTokens 'src/LocalGPT/Services/GoalExecutionService.KeyaccountManager.cs' @('HelpEscalatorGlobalInstruction')
Assert-SourceTokens 'src/LocalGPT/Services/UploadProcessingRecommendationService.cs' @('HelpEscalatorGlobalInstruction')
Assert-SourceTokens 'Directory.Build.targets' @('AssertLocalGptAiHelpEscalation', 'Assert-AiHelpEscalator.ps1')

$handler = Join-Path $root 'src/LocalGPT/Services/AiHelpEscalatorDxAiFunctions.cs'
if (Test-Path -LiteralPath $handler -PathType Leaf) {
    $text = [IO.File]::ReadAllText($handler)
    $begin = $text.IndexOf('"ai.help.report_attempt"', [StringComparison]::Ordinal)
    $end = $text.IndexOf('"ai.help.status"', [StringComparison]::Ordinal)
    if ($begin -ge 0 -and $end -gt $begin) {
        $reportDescriptor = $text.Substring($begin, $end - $begin)
        foreach ($forbidden in @('RequiresHumanConfirmation: true', '"negativePrompt"')) {
            if ($reportDescriptor.IndexOf($forbidden, [StringComparison]::Ordinal) -ge 0) {
                $findings.Add(('src/LocalGPT/Services/AiHelpEscalatorDxAiFunctions.cs(1,1): error LGAI6001: Invalid escalation schema/approval requirement: {0}' -f $forbidden))
            }
        }
    }
}

if ($findings.Count -gt 0) {
    foreach ($finding in $findings) { Write-Output $finding }
    throw ('AI Help Escalator source contract failed with {0} finding(s).' -f $findings.Count)
}
Write-Output 'AI Help Escalator: controller, owner service, curator, human inbox, automatic DX functions and globally seeded prompts validated.'
