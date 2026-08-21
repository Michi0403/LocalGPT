#!/usr/bin/env python3
"""Static contract audit for LocalGPT 3.1.10 native DXAiChat attachment repair."""
from __future__ import annotations

from pathlib import Path
import hashlib
import re
import sys

root = Path(__file__).resolve().parents[1]
checks = 0


def read(rel: str) -> str:
    path = root / rel
    if not path.is_file():
        raise AssertionError(f"missing {rel}")
    return path.read_text(encoding="utf-8-sig", errors="strict")


def require(rel: str, *tokens: str) -> None:
    global checks
    data = read(rel)
    for token in tokens:
        checks += 1
        if token not in data:
            raise AssertionError(f"{rel} missing {token!r}")


def forbid(rel: str, *tokens: str) -> None:
    global checks
    data = read(rel)
    for token in tokens:
        checks += 1
        if token in data:
            raise AssertionError(f"{rel} unexpectedly contains {token!r}")


try:
    # This repair must not touch the working 3.1.9 Chat/component geometry.
    for rel, expected in (
        ("src/LocalGPT/Components/Pages/Chat.razor", "0d9ab6ed72f41eebbbf8839c54b5fda9a409d424a1fa11c87d2994352c837569"),
        ("src/LocalGPT/Components/Pages/Chat.razor.css", "2a620187aa41712f53dddab92ee2ab834c4f46fe512925dce94efb387f28b0e4"),
    ):
        checks += 1
        actual = hashlib.sha256((root / rel).read_bytes()).hexdigest()
        if actual != expected:
            raise AssertionError(f"protected 3.1.9 UI file changed: {rel} -> {actual}")

    razor = read("src/LocalGPT/Components/Pages/Chat.razor")
    quick_start = razor.index('<div class="w-100" data-testid="chat-quick-configuration-bar"')
    session_start = razor.index('<details class="chat-session-tools-ribbon"', quick_start)
    quick_block = razor[quick_start:session_start]
    checks += 1
    if quick_block.count("<DxFormLayoutItem") != 3:
        raise AssertionError("3.1.9 quick preset row changed")

    js_rel = "src/LocalGPT/wwwroot/js/localgpt-chat-ui.js"
    require(
        js_rel,
        "const nativeUploadMimeFailureText = 'File has no MIME type. Please ensure that the attached file has an extension.';",
        "function normalizeSelectedUploadFileTypes(event)",
        "String(file.type || '').trim() || 'application/octet-stream'",
        "new File([file], fileName",
        "lastModified: file.lastModified",
        "document.addEventListener('change', diagnostics.guard('localgpt-chat-ui.uploadMime.normalize', normalizeSelectedUploadFileTypes), true);",
        "function rememberNativeSendDraft(host, composer, editor)",
        "const files = [...pendingUploadFiles(composer)];",
        "function maybeRestoreFailedNativeSendDraft(host)",
        "countNativeUploadMimeFailures(host) <= state.failureCount",
        "restorePendingUploadFiles(composer, state.files)",
        "nativeSendDraftRecovery.delete(host);",
    )
    # Keep automatic DxAIChat delivery. Do not replace it with a manual MessageSent pipeline.
    forbid(
        "src/LocalGPT/Components/Pages/Chat.razor",
        "MessageSent=",
        "MessageSending=",
    )
    # The new browser repair must not introduce any layout/style mutation.
    js = read(js_rel)
    repair_start = js.index("function normalizeSelectedUploadFileTypes")
    repair_end = js.index("function cachePendingUploadFiles", repair_start)
    repair = js[repair_start:repair_end]
    for token in ("style.", "classList.add", "classList.remove", "getBoundingClientRect", "scroll"):
        checks += 1
        if token in repair:
            raise AssertionError(f"attachment repair unexpectedly changes UI/layout via {token!r}")

    require(
        "src/LocalGPT/Services/CouncilTextService.LiveCouncilAndKnowledgeText.cs",
        "var mediaType = string.IsNullOrWhiteSpace(dataContent.MediaType)",
        '? "application/octet-stream"',
        ": dataContent.MediaType.Trim();",
        "BuildDataContentFileName(index, mediaType, logger)",
        "mediaType,",
    )
    require(
        "src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs",
        'string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType.Trim()',
    )
    # Existing broad policy remains present; the repair does not tighten upload acceptance.
    require(
        "src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs",
        'new(LocalGptRuntimeCollection.AllowedUploadMimeTypes',
        '"*/*"',
        '"application/octet-stream"',
        '".cmd"',
        '".txt"',
        '".mp4"',
        '".wav"',
    )

    # JavaScript diagnostics manifest must match the edited source after LF normalization.
    normalized = read(js_rel).replace("\r\n", "\n").replace("\r", "\n")
    js_hash = hashlib.sha256(normalized.encode("utf-8")).hexdigest()
    manifest = read("build/javascript-diagnostics-files.sha256")
    checks += 1
    if f"{js_hash}  {js_rel}" not in manifest:
        raise AssertionError("localgpt-chat-ui.js diagnostics manifest hash is stale")

    require("src/LocalGPT/Components/App.razor", "js/localgpt-chat-ui.js?v=3.1.10")

    print(f"LocalGPT 3.1.10 Chat attachment delivery audit passed: {checks} checks.")
except (AssertionError, ValueError) as exc:
    print(f"LocalGPT 3.1.10 Chat attachment delivery audit failed: {exc}", file=sys.stderr)
    raise SystemExit(1)
