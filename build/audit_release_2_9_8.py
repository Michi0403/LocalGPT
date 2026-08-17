#!/usr/bin/env python3
"""Source-only regression audit for LocalGPT 2.9.8 configurable Council policy and live-stream synchronization."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

def read(rel: str) -> str:
    path = ROOT / rel
    if rel.endswith('.cs'):
        stem = path.with_suffix('')
        parts = sorted(stem.parent.glob(stem.name + '*.cs'))
        if parts:
            return '\n'.join(part.read_text(encoding='utf-8', errors='replace') for part in parts)
    if rel.endswith('.razor'):
        stem = path.with_suffix('')
        parts = ([path] if path.is_file() else []) + sorted(stem.parent.glob(stem.name + '*.razor.cs'))
        if parts:
            return '\n'.join(part.read_text(encoding='utf-8', errors='replace') for part in parts)
    return path.read_text(encoding='utf-8', errors='replace')

def require(rel: str, needle: str) -> None:
    if needle not in read(rel):
        raise AssertionError(f"{rel}: missing {needle!r}")

def forbid(rel: str, needle: str) -> None:
    if needle in read(rel):
        raise AssertionError(f"{rel}: forbidden {needle!r}")

try:
    for rel in [
        'src/LocalGPT/LocalGPT.csproj',
        'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
        'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
    ]:
        require(rel, '<Version>3.0.7</Version>')

    require('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj', '<Version>2.1.1</Version>')
    require('src/LocalGPT/Services/CouncilTeamConfigurationService.cs', 'private const int CurrentSeedVersion = 26;')

    # Parameterless functions such as localgpt.time_state.now accept the correct empty-object call.
    registry = 'src/LocalGPT/Services/DxAiFunctionRegistry.cs'
    require(registry, 'normalizedParameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null')
    require(registry, 'foreach (var proposedProperty in value.EnumerateObject())')
    forbid(registry, 'var unexpected = value.EnumerateObject().FirstOrDefault')

    # Ollama receives one canonical tool per transport-safe name.
    ollama = 'src/LocalGPT/Services/OllamaThinkingChatClient.cs'
    require(ollama, 'ValidateUniqueAutomaticToolNames(functions);')
    require(ollama, 'Automatic DXFunction transport names are ambiguous')

    # Rich participant lanes remain first-class and use the newest in-memory live state before an attached snapshot.
    chat = 'src/LocalGPT/Components/Pages/Chat.razor'
    require(chat, 'CouncilLiveSessions.GetParticipantActivities(runId)')
    require(chat, 'CouncilLiveSessions.GetTranscript(runId)')
    require(chat, 'localgpt-live-participant-card')
    require(chat, 'Thinking, functions and provider stream')
    require(chat, 'Completed answer')

    live = 'src/LocalGPT/Services/CouncilLiveSessionService.cs'
    require(live, 'TranscriptTrimTargetCharacters = 1_750_000')
    require(live, 'ParticipantActivityTrimTargetCharacters = 280_000')
    require(live, 'GetParticipantActivities(Guid runId)')
    require(live, 'GetTranscript(Guid runId)')

    multi = 'src/LocalGPT/Services/MultiModelCouncilService.cs'
    require(multi, 'orderedPresentationBuffer')
    require(multi, 'additional model work, not delayed UI rendering')
    require(multi, 'the runtime request itself owns unload semantics')
    forbid(multi, 'RequestOllamaUnloadAsync(')
    require(multi, 'roleComplianceRetryCount: definition.RoleComplianceRetryCount')
    require(multi, 'finalAnswerRecoveryEnabled: definition.FinalAnswerRecoveryEnabled')
    require(multi, 'finalAnswerRecoveryMaxOutputTokens: definition.FinalAnswerRecoveryMaxOutputTokens')

    # User-configurable team lifecycle and function policy are available in the frontend.
    ui = 'src/LocalGPT/Components/Pages/CouncilTeams.razor'
    for needle in [
        'Automatic/native functions allowed by this team',
        'Reset selected from template',
        'Delete selected preset',
        'Member result recovery',
        'SetAutomaticFunctionPolicy',
    ]:
        require(ui, needle)

    print('LocalGPT 2.9.8 configurable Council policy/live-stream synchronization audit passed.')
except Exception as exc:
    print(f'LocalGPT 2.9.8 source audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
