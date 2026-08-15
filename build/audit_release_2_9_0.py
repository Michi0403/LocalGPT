#!/usr/bin/env python3
"""Source-only regression audit for LocalGPT 2.9.0 rejoin compile repair."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]

def read(rel):
    path = root / rel
    if not path.is_file():
        raise AssertionError(f"missing {rel}")
    return path.read_text(encoding="utf-8")

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
        require(rel, "<Version>2.9.0</Version>")
        match = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", read(rel))
        if not match or int(match.group(2)) > 9 or int(match.group(3)) > 9:
            raise AssertionError(f"version-slot policy failed for {rel}")

    require("Directory.Build.props", "<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>")
    require("src/LocalGPT/Services/CouncilTextService.cs", "using System.Text.RegularExpressions;")

    chat = read("src/LocalGPT/Components/Pages/Chat.razor")
    invalid_shapes = [
        "composerDraft = await InvokeAsync(() =>",
        "composerDraft = await InvokeAsync(async () =>",
    ]
    for invalid in invalid_shapes:
        if invalid in chat:
            raise AssertionError(f"compile-invalid InvokeAsync result assignment remains: {invalid}")

    for needle in [
        "string? capturedComposerDraft = null;",
        "await InvokeAsync(async () =>",
        "capturedComposerDraft = await JS",
        '.InvokeAsync<string>("localGptChatUi.readComposerDraft")',
        ".ConfigureAwait(true);",
        "composerDraft = capturedComposerDraft ?? string.Empty;",
        "AttachToLiveCouncilSessionAsync(Guid runId, bool reloadChatControl = false)",
        "var shouldReloadChatControl = reloadChatControl || firstAttachmentToRun;",
        "MergeAuthoritativeLiveCouncilMessage(captured);",
    ]:
        if needle not in chat:
            raise AssertionError(f"Chat.razor missing {needle}")

    attach_start = chat.index("private async Task AttachToLiveCouncilSessionAsync")
    attach_end = chat.index("[JSInvokable]", attach_start)
    attach = chat[attach_start:attach_end]
    if attach.count("DxAiChat.LoadMessages(") != 1:
        raise AssertionError("2.8.9 single-bind live Council attach invariant regressed")
    if attach.count("readComposerDraft") != 1 or attach.count("restoreComposerDraft") != 1:
        raise AssertionError("rejoin composer draft preservation must remain single-shot")

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
    if len(modes) != 19:
        raise AssertionError(f"expected 19 LocalGPT rendermode directives, found {len(modes)}")

    print("LocalGPT 2.9.0 rejoin compile repair source audit passed.")
except (AssertionError, OSError, ValueError) as exc:
    print(f"LocalGPT 2.9.0 source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)
