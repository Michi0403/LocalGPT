#!/usr/bin/env python3
"""Source-only audit of first-class Council X-Rounds and single-consumer heartbeat restarts."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MODELS = (ROOT / 'src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs').read_text(encoding='utf-8-sig')
RUN_MODELS = (ROOT / 'src/LocalGPT/BusinessObjects/MultiModelCouncilModels.cs').read_text(encoding='utf-8-sig')
X_MODELS = (ROOT / 'src/LocalGPT/BusinessObjects/CouncilXRoundModels.cs').read_text(encoding='utf-8-sig')
X_INTERFACE = (ROOT / 'src/LocalGPT/Interfaces/ICouncilXRoundService.cs').read_text(encoding='utf-8-sig')
X_SERVICE = (ROOT / 'src/LocalGPT/Services/CouncilXRoundService.cs').read_text(encoding='utf-8-sig')
X_FUNCTIONS = (ROOT / 'src/LocalGPT/Services/CouncilXRoundDxAiFunctions.cs').read_text(encoding='utf-8-sig')
COUNCIL = (ROOT / 'src/LocalGPT/Services/MultiModelCouncilService.cs').read_text(encoding='utf-8-sig')
TEAM_SERVICE = (ROOT / 'src/LocalGPT/Services/CouncilTeamConfigurationService.cs').read_text(encoding='utf-8-sig')
TEAM_UI = (ROOT / 'src/LocalGPT/Components/Pages/CouncilTeams.razor').read_text(encoding='utf-8-sig')
CHAT_UI = (ROOT / 'src/LocalGPT/Components/Pages/Chat.razor').read_text(encoding='utf-8-sig')
CHAT_CSS = (ROOT / 'src/LocalGPT/Components/Pages/Chat.razor.css').read_text(encoding='utf-8-sig')
LIVE_MODELS = (ROOT / 'src/LocalGPT/BusinessObjects/CouncilLiveSessionModels.cs').read_text(encoding='utf-8-sig')
LIVE_INTERFACE = (ROOT / 'src/LocalGPT/Interfaces/ICouncilLiveSessionService.cs').read_text(encoding='utf-8-sig')
LIVE_SERVICE = (ROOT / 'src/LocalGPT/Services/CouncilLiveSessionService.cs').read_text(encoding='utf-8-sig')
CHAT_RENDERER = (ROOT / 'src/LocalGPT/Services/Formatting/ChatContentRenderer.cs').read_text(encoding='utf-8-sig')
RUN_CONFIGURATION_MODELS = (ROOT / 'src/LocalGPT/BusinessObjects/CouncilRunConfigurationModels.cs').read_text(encoding='utf-8-sig')
RUN_STATE_MODELS = (ROOT / 'src/LocalGPT/BusinessObjects/CouncilServiceStateModels.cs').read_text(encoding='utf-8-sig')
RUN_CONFIGURATION_SERVICE = (ROOT / 'src/LocalGPT/Services/CouncilRunConfigurationService.cs').read_text(encoding='utf-8-sig')
HUMAN_INTERFACE = (ROOT / 'src/LocalGPT/Interfaces/IHumanCollaborationService.cs').read_text(encoding='utf-8-sig')
HUMAN_SERVICE = (ROOT / 'src/LocalGPT/Services/HumanCollaborationService.cs').read_text(encoding='utf-8-sig')
PROGRAM = (ROOT / 'src/LocalGPT/Program.cs').read_text(encoding='utf-8-sig')
README = (ROOT / 'README.md').read_text(encoding='utf-8-sig')
GUIDE = (ROOT / 'docs/guide/chat-and-council.md').read_text(encoding='utf-8-sig')

failures: list[str] = []

def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        failures.append(f'missing {label}: {needle}')

def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        failures.append(f'forbidden {label}: {needle}')

for field in [
    'XFunctionsEnabled', 'XCanRevisit', 'XCanReturnText', 'XCanStartSingleModel',
    'XCanStartCouncil', 'XMaximumTransitions', 'XRequiresHumanApproval',
    'XDefaultTargetStepKey', 'XChildCouncilTeamKey', 'XMaximumChildCouncilDepth', 'XChildModelName',
]:
    require(MODELS, field, f'workflow X-Round field {field}')

for action in ['ReconsiderStep', 'ReexecuteStep', 'ReturnText', 'StartSingleModel', 'StartCouncil']:
    require(X_MODELS, action, f'X-Round action {action}')

for name in [
    'council.x.status', 'council.x.revisit', 'council.x.return_text',
    'council.x.start_single_model', 'council.x.start_council',
]:
    require(X_FUNCTIONS, f'"{name}"', f'DI-backed X-Round function {name}')
    require(TEAM_UI, name, f'Council Teams X-Round function disclosure {name}')

require(PROGRAM, 'AddSingleton<ICouncilXRoundService, CouncilXRoundService>()', 'X-Round runtime DI registration')
require(X_INTERFACE, 'TryConsumeTransitionBudget', 'transition-budget contract')
require(X_SERVICE, 'ConcurrentDictionary<string, int> transitionCounts', 'per-run/source transition accounting')
require(TEAM_SERVICE, 'step.XMaximumTransitions = Math.Clamp', 'persisted transition budget normalization')
require(TEAM_SERVICE, 'step.XMaximumChildCouncilDepth = Math.Clamp', 'child-Council depth normalization')
require(TEAM_SERVICE, 'enables X-Round DXFunctions while DX/organic function requests are disabled', 'X-Round/DX validation')
require(TEAM_UI, 'X-Rounds / revisable control flow', 'Council Teams X-Round editor')
require(TEAM_UI, 'Human must approve every accepted X control request', 'Council Teams human gate')
require(TEAM_UI, 'Gatekeeper', 'Council Teams Gatekeeper X preset')
require(TEAM_UI, 'Reactive revisit', 'Council Teams reactive-revisit X preset')
require(TEAM_UI, 'Derived single model', 'Council Teams derived single-model X preset')
require(TEAM_UI, 'Derived Council', 'Council Teams derived Council X preset')
require(TEAM_UI, '_editor.WorkflowSteps.Take(stepIndex + 1)', 'Council Teams revisit target chooser only offers current/earlier steps')

require(COUNCIL, 'workflowRevisions', 'immutable workflow revision tracking')
require(COUNCIL, 'roundStep.WorkflowRevision = Math.Max(1, workflowRevision);', 'step revision annotation')
require(COUNCIL, 'roundStep.XRoundCause = xRoundCause ?? string.Empty;', 'step causal annotation')
require(RUN_MODELS, 'public int WorkflowRevision { get; set; } = 1;', 'workflow revision output model')
require(RUN_MODELS, 'public string XRoundCause { get; set; } = string.Empty;', 'workflow cause output model')
require(COUNCIL, 'suppressOrganicFunctions: nextExecutionIsReconsideration', 'reasoning-only reconsider dispatch')
require(COUNCIL, 'var effectiveAllowDxFunctions = definition.CanUseOrganicFunctions && !suppressOrganicFunctions;', 'reconsider side-effect suppression')
require(COUNCIL, 'RunXRoundSingleModelAsync', 'single-model derived X subtask')
require(COUNCIL, 'RunXRoundChildCouncilAsync', 'child-Council derived X subtask')
require(COUNCIL, 'WaitForXRoundApprovalAsync', 'human X transition gate')
require(COUNCIL, "cannot jump forward across workflow gates", 'X revisit forward-gate safeguard')
require(COUNCIL, 'XRoundChildDepth = request.XRoundChildDepth + 1', 'child-Council depth propagation')
require(COUNCIL, 'SaveToMemory = false', 'derived child Council memory isolation')

# One direct live user contribution may interrupt exactly one currently active participant.
# It remains queued until PrepareHumanHeartbeatAsync drains it, so later participants/rounds
# still receive it as shared heartbeat context.
require(HUMAN_INTERFACE, 'SetPreferredDirectUserMessageConsumer', 'foreground heartbeat consumer contract')
require(HUMAN_INTERFACE, 'ClearPreferredDirectUserMessageConsumer', 'foreground heartbeat consumer clear contract')
require(HUMAN_INTERFACE, 'TryClaimDirectUserMessage', 'single-consumer heartbeat claim contract')
require(HUMAN_SERVICE, 'directUserMessageClaims', 'heartbeat claim state')
require(HUMAN_SERVICE, 'preferredDirectUserMessageConsumers', 'foreground heartbeat consumer state')
require(HUMAN_SERVICE, 'TryClaimDirectUserMessage(Guid contributionId, Guid councilRunId, string consumerKey)', 'heartbeat claim implementation')
require(COUNCIL, 'humanCollaboration.TryClaimDirectUserMessage(contribution.Id, councilRunId, consumerKey)', 'event-path heartbeat claim')
require(COUNCIL, 'humanCollaboration.TryClaimDirectUserMessage(item.Id, councilRunId, consumerKey)', 'queued-path heartbeat claim')
require(COUNCIL, 'humanCollaboration.SetPreferredDirectUserMessageConsumer(councilRunId, consumerKey)', 'ordered stream owns immediate heartbeat')
require(COUNCIL, 'humanCollaboration.ClearPreferredDirectUserMessageConsumer(councilRunId, consumerKey)', 'ordered stream releases immediate heartbeat')
require(COUNCIL, 'waiting for this member\'s one Council turn', 'pre-registered equivalent live participant lanes')
require(COUNCIL, 'ordered transcript integration may follow later', 'live result visible before ordered integration')
require(LIVE_MODELS, 'string FinalContent', 'authoritative completed participant result snapshot')
require(LIVE_INTERFACE, 'SetParticipantActivityResult', 'completed participant result service contract')
require(LIVE_SERVICE, 'activity.FinalContent = finalContent?.Trim() ?? string.Empty;', 'completed participant result storage')
require(COUNCIL, 'liveCouncilSessions.SetParticipantActivityResult(', 'participant completion publishes authoritative result before ordered transcript')
require(CHAT_UI, 'Completed answer', 'completed live-lane answer rendering')
require(CHAT_UI, 'Result ready · expand', 'completed remote/live result discoverability')
require(CHAT_UI, 'Thinking, functions and provider stream', 'live-lane stream history remains available')
require(CHAT_CSS, '.localgpt-live-result-ready', 'completed live result visual affordance')
require(CHAT_RENDERER, 'RepairCommonProseSpacing', 'conservative model prose spacing repair')
require(CHAT_RENDERER, 'ProseLabelBoundaryRegex', 'label/number spacing repair')
require(CHAT_RENDERER, "!line.Contains('`')", 'inline-code spacing protection')
require(COUNCIL, 'Readable streamed prose', 'Council prompt word-boundary contract')
require(COUNCIL, 'PrepareHumanHeartbeatAsync', 'shared later heartbeat context path')
require(COUNCIL, 'Queued direct user messages for the current Council heartbeat', 'same-round future-participant heartbeat context')
require(COUNCIL, 'without restarting the model', 'non-restart heartbeat delivery for participants that have not started yet')
require(COUNCIL, 'exactly one currently', 'single active-model heartbeat restart contract')

require(CHAT_UI, 'Parallel models per AI host', 'fine-grained per-host concurrency UI')
require(CHAT_UI, 'CouncilEditorMaxParallelModels', 'run/future concurrency editor binding')
require(RUN_CONFIGURATION_MODELS, 'int MaxParallelModels', 'run snapshot concurrency setting')
require(RUN_STATE_MODELS, 'public int MaxParallelModels { get; set; }', 'run state concurrency setting')
require(RUN_CONFIGURATION_SERVICE, 'state.MaxParallelModels = Math.Max(1, maxParallelModels);', 'live run concurrency update')
require(COUNCIL, 'maxParallelModels = Math.Max(1, runConfiguration.MaxParallelModels);', 'phase gate reads current run concurrency')
require(CHAT_UI, 'Model response timeout (seconds)', 'fine-grained model timeout UI')
require(CHAT_UI, 'CouncilEditorModelTimeoutSeconds', 'run/future model timeout editor binding')
require(RUN_CONFIGURATION_MODELS, 'int ModelTimeoutSeconds', 'run snapshot model timeout setting')
require(RUN_STATE_MODELS, 'public int ModelTimeoutSeconds { get; set; }', 'run state model timeout setting')
require(RUN_CONFIGURATION_SERVICE, 'state.ModelTimeoutSeconds = Math.Clamp(modelTimeoutSeconds, 30, 1800);', 'live run model timeout update')
require(COUNCIL, 'modelTimeoutSeconds = Math.Clamp(runConfiguration.ModelTimeoutSeconds, 30, 1800);', 'phase reads current model timeout')
require(COUNCIL, 'modelTimeoutSeconds = Math.Clamp(currentRunConfiguration.ModelTimeoutSeconds, 30, 1800);', 'participant reads live-updated model timeout')
forbid((ROOT / 'src/LocalGPT/Services/ModelPresetService.cs').read_text(encoding='utf-8-sig'), 'Math.Clamp(preset.MaxParallelModels, 1, 8)', 'obsolete eight-model preset clamp')

require(README, 'https://michi0403.github.io/LocalGPT/', 'visible LocalGPT GitHub Pages URL')
require(README, 'https://michi0403.github.io/BlazorPublisher/', 'visible PublisherStudio GitHub Pages URL')
require(GUIDE, '## X-Rounds: revisable Council control flow', 'X-Round guide')
require(GUIDE, 'The immediate restart claim is single-consumer per Council run.', 'heartbeat guide')

if failures:
    print('Council X-Round/heartbeat source audit failed:')
    for failure in failures:
        print(f'  - {failure}')
    raise SystemExit(1)

print(
    'Council X-Round/heartbeat source audit passed: Council Teams owns bounded revisable control-flow policy; '
    'five DI-backed X functions cover status, revisit, text return, one-model and child-Council subtasks; '
    'reconsider suppresses side effects while reexecute keeps normal policy; transcript revisions remain immutable; '
    'human/transition/depth gates are present; each direct live user message can immediately restart only the ordered foreground participant while remaining available to later Council heartbeat context; '
    'live lanes pre-register every provider-qualified participant and retain each authoritative completed answer before ordered transcript integration; '
    'streamed prose has conservative display-only spacing repair that excludes code; and the Chat UI exposes per-host plus per-road concurrency controls.'
)
