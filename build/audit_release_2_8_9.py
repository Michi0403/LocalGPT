#!/usr/bin/env python3
"""Source-only regression audit for LocalGPT 2.8.9 Council rejoin/circuit recovery."""
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
        match = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", read(rel))
        if not match or int(match.group(2)) > 9 or int(match.group(3)) > 9:
            raise AssertionError(f"version-slot policy failed for {rel}")

    require("Directory.Build.props", "<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>")
    require("src/LocalGPT/Services/CouncilTextService.cs", "using System.Text.RegularExpressions;")

    chat = read("src/LocalGPT/Components/Pages/Chat.razor")
    for needle in [
        "CouncilLiveSessionAttachmentSnapshot? attachedLiveCouncilSnapshot;",
        "readonly SemaphoreSlim liveCouncilAttachGate = new(1, 1);",
        "LiveCouncilTranscript(liveCouncilMessage.RunId, context.Content)",
        "Task<bool> AttachToLiveCouncilSessionAsync(Guid runId, bool reloadChatControl = false)",
        "var shouldReloadChatControl = reloadChatControl || firstAttachmentToRun;",
        "if (shouldReloadChatControl",
        "MergeAuthoritativeLiveCouncilMessage(captured);",
        "live Council run(s) available to rejoin",
        "CouncilLiveSessions.GetAttachmentSnapshot(runId)",
        "snapshot.IsRunning ? string.Empty : CouncilLiveSessions.GetTranscript(runId)",
    ]:
        if needle not in chat:
            raise AssertionError(f"Chat.razor missing {needle}")

    attach_start = chat.index("private async Task<bool> AttachToLiveCouncilSessionAsync")
    attach_end = chat.index("[JSInvokable]", attach_start)
    attach = chat[attach_start:attach_end]
    if attach.count("DxAiChat.LoadMessages(") != 1:
        raise AssertionError("live Council attach path must bind DxAIChat exactly once and only for initial join/rejoin")
    if "readComposerDraft" not in attach or "restoreComposerDraft" not in attach:
        raise AssertionError("initial rejoin must preserve the current composer draft")

    require(
        "src/LocalGPT/Program.cs",
        "options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);",
        "options.KeepAliveInterval = TimeSpan.FromSeconds(15);",
        "options.HandshakeTimeout = TimeSpan.FromSeconds(30);",
    )

    # Preserve the additive 2.8.8 role coordination feature and its opt-in defaults.
    require(
        "src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs",
        "public bool EnableRolePeerReview { get; set; }",
        "public bool SummarizeRoleResults { get; set; }",
    )
    models = read("src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs")
    if "public bool EnableRolePeerReview { get; set; } = true" in models:
        raise AssertionError("peer review must remain opt-in")
    if "public bool SummarizeRoleResults { get; set; } = true" in models:
        raise AssertionError("role synthesis must remain opt-in")

    modes = []
    for path in (root / "src/LocalGPT").rglob("*.razor"):
        for line in path.read_text(encoding="utf-8").splitlines():
            if "@rendermode" in line:
                modes.append((str(path.relative_to(root)), line.strip()))
    if len(modes) != 20:
        raise AssertionError(f"expected 20 LocalGPT rendermode directives, found {len(modes)}")

    print("LocalGPT 2.8.9 Council rejoin/circuit recovery source audit passed.")
except (AssertionError, OSError, ValueError) as exc:
    print(f"LocalGPT 2.8.9 source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)
