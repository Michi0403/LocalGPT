#!/usr/bin/env python3
"""Source-only regression audit for LocalGPT 2.9.9 Council live-lane and architecture repair."""
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

def method_slice(rel: str, signature: str, next_marker: str) -> str:
    text = read(rel)
    start = text.index(signature)
    end = text.index(next_marker, start)
    return text[start:end]

try:
    for rel in [
        'src/LocalGPT/LocalGPT.csproj',
        'src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj',
        'src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj',
    ]:
        require(rel, '<Version>3.0.4</Version>')

    require('src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj', '<Version>2.1.1</Version>')

    # The 2.9.8 build break is repaired at the architecture boundary, not by weakening the audit.
    ui = 'src/LocalGPT/Components/Pages/CouncilTeams.razor'
    require(ui, '@rendermode InteractiveServer')
    require(ui, 'CouncilText.FormatInlineNameList(_editor.AllowedAutomaticFunctions)')
    require(ui, 'CouncilText.FormatMultilineNameList(step.AllowedAutomaticFunctions)')
    require(ui, 'CouncilText.ParseUserEditableNameList(args.Value?.ToString())')
    forbid(ui, ".Split(new[] { '\\r', '\\n', ',', ';' }")
    forbid(ui, 'string.Join(", ", _editor.AllowedAutomaticFunctions)')
    forbid(ui, 'string.Join(Environment.NewLine, step.AllowedAutomaticFunctions)')

    text_service = 'src/LocalGPT/Services/CouncilTextService.cs'
    require(text_service, 'public List<string> ParseUserEditableNameList(string? value)')
    require(text_service, 'public string FormatInlineNameList(IEnumerable<string>? values)')
    require(text_service, 'public string FormatMultilineNameList(IEnumerable<string>? values)')

    # Single/custom workflow steps now use the same rich producer-side participant lane as parallel phases.
    multi = 'src/LocalGPT/Services/MultiModelCouncilService.cs'
    configured = method_slice(
        multi,
        '        private async Task RunConfiguredParticipantAsync(',
        '\n        /// <summary>\n        /// Performs select configured workflow participant')
    for needle in [
        'BeginParticipantActivity',
        'SetParticipantActivityStatus',
        'AppendParticipantActivity',
        'SetParticipantActivityResult',
        'CompleteParticipantActivity',
        'orderedPresentationBuffer',
        'orderedPresentationBuffer.Length < 8192',
        'participantStreamUpdate,',
        'RunParticipantAsync(',
        '.ConfigureAwait(false)',
    ]:
        if needle not in configured:
            raise AssertionError(f'{multi}: configured participant path missing {needle!r}')
    if 'modelTimeoutSeconds,\n                            request.StreamUpdate,' in configured:
        raise AssertionError(f'{multi}: configured participant still bypasses the rich live lane')

    # Renderer-affine awaits remain explicitly opted into their UI context; service orchestration stays free-threaded.
    require('src/LocalGPT/Components/Pages/Chat.razor', 'ConfigureAwait(true)')

    # Native Ollama tool metadata validates transport-name uniqueness before it is sent to the provider.
    ollama = 'src/LocalGPT/Services/OllamaThinkingChatClient.cs'
    automatic_tools = method_slice(
        ollama,
        '    private List<OllamaToolDefinition>? BuildAutomaticTools()',
        '\n    /// <summary>\n    /// Retrieves automatic functions')
    if automatic_tools.index('ValidateUniqueAutomaticToolNames(functions);') > automatic_tools.index('return functions.Select'):
        raise AssertionError(f'{ollama}: transport collision validation occurs after tool construction')
    require(ollama, 'Automatic DXFunction transport names are ambiguous')

    # The benchmark preflight clock remains a true parameterless function and empty JSON is normalized safely.
    require('src/LocalGPT/Services/TimeAndStateDxAiFunction.cs', '"localgpt.time_state.now"')
    require('src/LocalGPT/Services/TimeAndStateDxAiFunction.cs', 'ParameterSchemaJson: "{\\"type\\":\\"object\\",\\"properties\\":{},\\"additionalProperties\\":false}"')
    require('src/LocalGPT/Services/DxAiFunctionRegistry.cs', 'normalizedParameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null')
    require('src/LocalGPT/Services/DxAiFunctionRegistry.cs', 'foreach (var proposedProperty in value.EnumerateObject())')

    # Remove the warning seen in the user's Windows compile log.
    previous_step = method_slice(
        multi,
        '        private string BuildConfiguredWorkflowPreviousStep(',
        '\n        /// <summary>\n        /// Builds configured workflow transcript')
    if '<param name="team">' in previous_step:
        raise AssertionError(f'{multi}: stale XML param documentation for team remains')

    print('LocalGPT 2.9.9 Council live-lane/architecture source audit passed.')
except Exception as exc:
    print(f'LocalGPT 2.9.9 source audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
