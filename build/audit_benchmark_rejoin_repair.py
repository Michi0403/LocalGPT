#!/usr/bin/env python3
"""Static source audit for LocalGPT 3.0.8 benchmark/rejoin/all-model coverage repair."""
from pathlib import Path
import sys
root = Path(__file__).resolve().parents[1]

def text(rel):
    path = root / rel
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
    if not path.is_file():
        raise AssertionError(f"missing {rel}")
    return path.read_text(encoding='utf-8', errors='replace')

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
        require(rel, "<Version>3.0.8</Version>")
    require(
        "src/LocalGPT/Components/App.razor",
        "disableDomPreservation: false",
    )
    require(
        "src/LocalGPT/wwwroot/js/localgpt-reconnect.js",
        "scheduleResumeHealthCheck",
        "interactiveShellLooksUsable",
        "globalThis.Blazor?.reconnect",
    )
    require(
        "src/LocalGPT/Services/CouncilBenchmarkCalibrationService.cs",
        "missingBenchmarkTargets",
        "RequestedTargetCount",
        "One deterministic all-model measurement phase",
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.WorkflowDefinitionExecution.cs",
        "exactBenchmarkTargets",
        "Model-generated sampling, quartets and representative packs are ignored.",
    )
    require(
        "src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.BenchmarkTemplates.cs",
        "ONE consolidated benchmark suite",
        "never four model packs",
        "ONE deterministic LocalGPT measurement phase",
    )
    print("LocalGPT 3.0.8 benchmark/rejoin/all-model coverage source audit passed.")
except AssertionError as exc:
    print(f"LocalGPT 3.0.8 benchmark/rejoin/all-model coverage source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)
