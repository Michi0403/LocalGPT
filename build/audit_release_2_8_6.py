#!/usr/bin/env python3
"""Source-only regression audit for LocalGPT 2.8.6 trace and Council durability."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]

def read(rel):
    base = globals().get("root") or globals().get("ROOT")
    path = base / rel
    if rel.endswith(".cs"):
        stem = path.with_suffix("")
        parts = sorted(stem.parent.glob(stem.name + "*.cs"))
        if parts:
            return "\n".join(part.read_text(encoding="utf-8", errors="replace") for part in parts)
    if rel.endswith(".razor"):
        stem = path.with_suffix("")
        parts = ([path] if path.is_file() else []) + sorted(stem.parent.glob(stem.name + "*.razor.cs"))
        if parts:
            return "\n".join(part.read_text(encoding="utf-8", errors="replace") for part in parts)
    if not path.is_file():
        raise AssertionError(f"missing {rel}")
    return path.read_text(encoding="utf-8", errors="replace")

def require(rel, *needles):
    text = read(rel)
    missing = [needle for needle in needles if needle not in text]
    if missing:
        raise AssertionError(f"{rel} missing {missing}")

try:
    for rel in [
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
    ]:
        require(rel, "<Version>3.0.4</Version>")
    require("Directory.Build.props", "<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>")

    require(
        "src/LocalGPT/BusinessObjects/OllamaProtocolModels.cs",
        '[JsonPropertyName("think")]',
        "public bool? Think { get; set; }",
    )
    require(
        "src/LocalGPT/Services/OllamaThinkingChatClient.cs",
        "Think = councilRuntime.OllamaThinkingChatClientShouldSkipExplicitThinking",
        "OllamaThinkingChatClientRememberExplicitThinkingRejected",
        "BuildOllamaFunctionCallTrace(call)",
        "BuildOllamaFunctionResultTrace",
        '<details class=\\\"council-step\\\" open><summary>Function call',
        '<details class=\\\"council-step\\\" open><summary>Function result',
        "automaticToolTrace.Append",
        "yield return CreateStreamingUpdate(BuildOllamaFunctionCallTrace(call));",
        "foreach (var trace in streamedToolResults)",
    )
    fallback = read("src/LocalGPT/Services/OllamaThinkingChatClient.cs")
    if "minimalResponse.IsSuccessStatusCode" not in fallback or "Do not poison the cache" not in fallback:
        raise AssertionError("Ollama thinking compatibility fallback is not conservatively cached")

    require(
        "src/LocalGPT/Services/CouncilRuntimeService.cs",
        "BuildUserVisibleProviderTrace(ChatResponseUpdate update",
        'typeName.Contains("Reasoning"',
        'typeName.Contains("FunctionCall"',
        'key.Contains("reasoning"',
        'key.Contains("tool_call"',
        '<details class=\\\"council-step\\\" open><summary>Function call',
        '<details class=\\\"council-step\\\" open><summary>Function result',
    )
    require(
        "src/LocalGPT/Services/CompositeChatClient.cs",
        "_councilRuntime.BuildUserVisibleProviderTrace(update, _logger)",
        "yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(trace)]);",
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.cs",
        "councilRuntime.BuildUserVisibleProviderTrace(update, logger)",
        "result.LogPath = await WriteLogAsync(result, CancellationToken.None, logger)",
        "temporaryPath = $\"{path}.{Guid.NewGuid():N}.tmp\";",
        "System.IO.File.Move(temporaryPath, path, overwrite: true);",
        "SaveToMemoryAsync(failedRequest, failedResult, null, CancellationToken.None)",
    )
    require(
        "src/LocalGPT/Services/CouncilTextService.cs",
        ".Matches(content)",
        ".Cast<Match>()",
        "WebUtility.HtmlDecode(match.Groups[\"thinking\"].Value).Trim()",
    )
    require(
        "src/LocalGPT/Services/Formatting/ChatResponseFormatter.cs",
        '<details class=\\\"model-thinking open\\\" open><summary>Model thinking</summary>',
    )
    require(
        "src/LocalGPT/Services/Formatting/ChatContentRenderer.cs",
        '<details class=\\\"model-thinking\\\" open>',
        'Every provider-supplied thinking block stays expanded.',
    )
    require(
        "src/LocalGPT/Services/CouncilChatClient.cs",
        '.AppendLine("<details class=\\\"model-thinking open\\\" open>")',
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.cs",
        '.AppendLine("<details class=\\\"model-thinking open\\\" open>")',
    )
    require(
        "src/LocalGPT/Components/Pages/Chat.razor",
        '<details open data-localgpt-panel-key="@($"former-thought-{thought.ConversationId:N}")">',
    )
    require(
        "src/LocalGPT/Services/OneWire/OneWireExecutionServices.cs",
        "SaveToMemory = true,",
        "ICouncilLiveSessionService",
        "liveSessions.Begin(",
        "CreateLinkedTokenSource(cancellationToken, liveCancellation)",
        "liveSessions.Complete(request.RunId);",
    )

    # Version slot rule: application versions may not contain a two-digit minor/patch slot.
    for rel in [
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
    ]:
        match = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", read(rel))
        if not match or int(match.group(2)) > 9 or int(match.group(3)) > 9:
            raise AssertionError(f"version-slot policy failed for {rel}")

    modes=[]
    for path in (root / "src/LocalGPT").rglob("*.razor"):
        for line in path.read_text(encoding="utf-8").splitlines():
            if "@rendermode" in line:
                modes.append((str(path.relative_to(root)), line.strip()))
    if len(modes) != 20:
        raise AssertionError(f"expected 20 LocalGPT rendermode directives, found {len(modes)}")

    print("LocalGPT 2.8.6 reasoning/function trace and Council durability source audit passed.")
except (AssertionError, OSError) as exc:
    print(f"LocalGPT 2.8.6 source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)
