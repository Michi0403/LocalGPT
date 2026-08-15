#!/usr/bin/env python3
"""Static source audit for LocalGPT 2.8.3 benchmark/rejoin/build-guard repair."""
from pathlib import Path
import sys
root = Path(__file__).resolve().parents[1]

def text(rel):
    p = root / rel
    if not p.is_file():
        raise AssertionError(f"missing {rel}")
    return p.read_text(encoding="utf-8")

def require(rel, *needles):
    value = text(rel)
    missing = [n for n in needles if n not in value]
    if missing:
        raise AssertionError(f"{rel} missing: {', '.join(missing)}")

try:
    require(
        "src/LocalGPT/Services/ProviderModelBenchmarkService.cs",
        "WebUtility.HtmlDecode(normalized)",
        "CommentHandling = JsonCommentHandling.Skip",
        "AllowTrailingCommas = true",
        "JsonDocument.ParseValue(ref reader)",
    )
    require(
        "src/LocalGPT/wwwroot/js/localgpt-reconnect.js",
        "recoverFromBackForwardCache",
        "event?.persisted",
        "event.stopImmediatePropagation?.()",
        "location.replace(target.href)",
        "window.addEventListener('pageshow', recoverFromBackForwardCache, { capture: true })",
    )
    require(
        "src/LocalGPT/Services/CouncilTextService.cs",
        "DecodeHumanVisibleText",
        "WebUtility.HtmlDecode(value)",
    )
    if "WebUtility.HtmlDecode" in text("src/LocalGPT/Components/Pages/ModelCouncil.razor"):
        raise AssertionError("ModelCouncil still owns direct HtmlDecode")
    if "WebUtility.HtmlDecode" in text("src/LocalGPT/Components/Layout/CouncilSpoolerPanel.razor"):
        raise AssertionError("CouncilSpoolerPanel still owns direct HtmlDecode")
    preset_docs = text("src/LocalGPT/Services/HardwarePerformancePresetDxAiFunctions.cs")
    get_start = preset_docs.index("public sealed class GetHardwarePerformancePresetFunction")
    get_preamble = preset_docs[max(0, get_start - 700):get_start]
    if '<param name="presets">' in get_preamble:
        raise AssertionError("stale GetHardwarePerformancePresetFunction XML param documentation remains")
    for rel in (
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    ):
        require(rel, "<Version>2.8.7</Version>")
    print("LocalGPT 2.8.3 benchmark/rejoin/build-guard source audit passed.")
except AssertionError as exc:
    print(f"LocalGPT 2.8.3 benchmark/rejoin/build-guard source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)
