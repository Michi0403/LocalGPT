#!/usr/bin/env python3
"""Static release audit for LocalGPT 3.0.8 benchmark coverage and browser recovery."""
from pathlib import Path
import hashlib
import sys

root = Path(__file__).resolve().parents[1]
checks = 0

def read(rel):
    return (root / rel).read_text(encoding="utf-8-sig", errors="strict")

def require(rel, *needles):
    global checks
    value = read(rel)
    missing = [needle for needle in needles if needle not in value]
    if missing:
        raise AssertionError(f"{rel} missing: {', '.join(missing)}")
    checks += len(needles)

try:
    for rel in (
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    ):
        require(rel, "<Version>3.0.8</Version>")
    require("src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj", "<Version>2.1.1</Version>")

    setup = read("src/LocalGPT/Services/InitialSetupAssistantService.cs")
    start = setup.index("CreateBenchmarkTeamAsync")
    benchmark_team = setup[start:start + 7000]
    if ".Take(128)" in benchmark_team or ".Take(4)" in benchmark_team:
        raise AssertionError("initial benchmark-team creation still truncates selected model membership")
    for needle in ("usePreferred ? preferred : selected", "role.AssignedModelKeys = pool.ToList()"):
        if needle not in benchmark_team:
            raise AssertionError(f"benchmark-team creation missing: {needle}")
        checks += 1

    require(
        "src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.BenchmarkTemplates.cs",
        "ONE consolidated benchmark suite",
        "never four model packs",
        "ONE deterministic LocalGPT measurement phase",
        "Do not create per-model packs, model quartets, batches or representative groups.",
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.WorkflowDefinitionExecution.cs",
        "exactBenchmarkTargets",
        "Model-generated sampling, quartets and representative packs are ignored.",
        "calibration.RequestedTargetCount != exactBenchmarkTargets.Count",
    )
    require(
        "src/LocalGPT/Services/CouncilBenchmarkCalibrationService.cs",
        "One deterministic all-model measurement phase",
        "missingBenchmarkTargets",
        "RequestedTargetCount",
    )
    require(
        "src/LocalGPT/Components/App.razor",
        "ssr: { disableDomPreservation: false }",
    )
    require(
        "src/LocalGPT/wwwroot/js/localgpt-reconnect.js",
        "interactiveShellLooksUsable",
        "scheduleResumeHealthCheck",
        "hiddenAfterInteractiveReady",
        "globalThis.Blazor?.reconnect",
        "server-side Council or benchmark work",
    )

    for rel, stale_names in (
        ("src/LocalGPT/Services/MinecraftDatapackService.cs", ("catalog", "patterns", "text", "jsonText", "serviceLogger")),
        ("src/LocalGPT/Services/MinecraftProjectService.cs", ("jsonText", "patterns", "datapackService", "catalog", "serviceLogger")),
    ):
        value = read(rel)
        class_pos = value.index("public sealed partial class")
        preamble = value[max(0, class_pos - 1800):class_pos]
        for name in stale_names:
            if f'<param name="{name}">' in preamble:
                raise AssertionError(f"stale class XML param remains in {rel}: {name}")
            checks += 1

    maintenance = read("src/LocalGPT/Services/ProjectMaintenanceService.BuildReview.cs")
    normalize_pos = maintenance.index("NormalizeCompilerSearchRoots")
    normalize_preamble = maintenance[max(0, normalize_pos - 1200):normalize_pos]
    for name in ("customRoots", "cancellationToken"):
        if f'<param name="{name}">' in normalize_preamble:
            raise AssertionError(f"stale NormalizeCompilerSearchRoots XML param remains: {name}")
        checks += 1

    js_path = root / "src/LocalGPT/wwwroot/js/localgpt-reconnect.js"
    digest = hashlib.sha256(js_path.read_bytes()).hexdigest()
    manifest = read("build/javascript-diagnostics-files.sha256")
    expected = f"{digest}  src/LocalGPT/wwwroot/js/localgpt-reconnect.js"
    if expected not in manifest:
        raise AssertionError("LocalGPT reconnect JavaScript SHA-256 manifest is stale")
    checks += 1

    render_files = [p for p in (root / "src/LocalGPT").rglob("*.razor") if "@rendermode" in p.read_text(encoding="utf-8", errors="ignore")]
    if len(render_files) != 20:
        raise AssertionError(f"expected 20 explicit @rendermode files, found {len(render_files)}")
    checks += 1

    print(f"LocalGPT 3.0.8 release source audit passed: {checks} checks.")
except (AssertionError, ValueError) as exc:
    print(f"LocalGPT 3.0.8 release source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)
