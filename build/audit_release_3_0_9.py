#!/usr/bin/env python3
"""Static release audit for LocalGPT 3.1.0 benchmark/live-lane/build-contract repair."""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
checks = 0

def read(rel: str) -> str:
    return (root / rel).read_text(encoding="utf-8-sig", errors="strict")

def require(rel: str, *needles: str) -> None:
    global checks
    value = read(rel)
    missing = [needle for needle in needles if needle not in value]
    if missing:
        raise AssertionError(f"{rel} missing: {', '.join(missing)}")
    checks += len(needles)

def forbid(rel: str, *needles: str) -> None:
    global checks
    value = read(rel)
    found = [needle for needle in needles if needle in value]
    if found:
        raise AssertionError(f"{rel} still contains forbidden release behavior: {', '.join(found)}")
    checks += len(needles)

try:
    for rel in (
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    ):
        require(rel, "<Version>3.1.0</Version>")
    require("src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj", "<Version>2.1.1</Version>")
    require("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/3.1.0")

    # The 3.0.8 source intentionally preserved prerendered DOM while Blazor attaches; the build guards must match it.
    require("src/LocalGPT/Components/App.razor", "ssr: { disableDomPreservation: false }")
    require("build/Assert-OperationalDiagnostics.ps1", "disableDomPreservation:\\s*false")
    require("build/Assert-InteractiveServerRenderModes.ps1", "ssr: { disableDomPreservation: false }")
    forbid("build/Assert-OperationalDiagnostics.ps1", "disableDomPreservation:\\s*true")
    forbid("build/Assert-InteractiveServerRenderModes.ps1", "ssr: { disableDomPreservation: true }")

    # Five-point all-model initial calibration, caller/runtime configured rather than developer-machine ceilings.
    require(
        "src/LocalGPT/BusinessObjects/CouncilBenchmarkCalibrationModels.cs",
        "public int ProfileCount { get; set; } = 5;",
        "public int MinimumContextTokens { get; set; } = 2048;",
        "public int MinimumOutputTokens { get; set; } = 128;",
        "public int StopAfterConsecutiveProfileFailures { get; set; }",
        "Low, Normal, High, Expert and Max",
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.WorkflowDefinitionExecution.cs",
        "ProfileCount = 5",
        "MinimumContextTokens = catalog.MinContextTokens",
        "MinimumOutputTokens = catalog.MinOutputTokens",
        "MaximumContextTokens = catalog.MaxContextTokens",
        "StopAfterConsecutiveProfileFailures = 0",
        "exactBenchmarkTargets",
        "Model-generated sampling, quartets and representative packs are ignored.",
    )
    require(
        "src/LocalGPT/Services/CouncilBenchmarkCalibrationService.cs",
        "Math.Clamp(request.MaximumContextTokens, catalog.MinContextTokens, catalog.MaxContextTokens)",
        "Math.Clamp(request.MaximumOutputTokens, catalog.MinOutputTokens, catalog.MaxOutputTokens)",
        'return ["Low", "Normal", "High", "Expert", "Max"]',
        "ProfileNames = [.. profileNames]",
        "BeginParticipantActivity",
        "AppendParticipantActivity",
        "SetParticipantActivityResult",
        "CompleteParticipantActivity",
        "Actual provider calls observed",
        "provider calls ",
        "compliant tasks ",
        "advisory website values are not benchmark scores",
        "remaining subjects will continue",
        "missingBenchmarkTargets",
    )
    forbid(
        "src/LocalGPT/Services/CouncilBenchmarkCalibrationService.cs",
        "MaxProfilesPerModel = 4",
        "StopAfterConsecutiveProfileFailures = 2",
        "Math.Clamp(request.MaximumContextTokens, 4096, 32768)",
        "Math.Clamp(request.MaximumOutputTokens, 512, 1536)",
    )

    require(
        "src/LocalGPT/BusinessObjects/ProviderModelBenchmarkModels.cs",
        "public List<string> ProfileNames { get; set; } = [];",
        "public int AttemptCount { get; set; }",
    )
    require(
        "src/LocalGPT/Services/ProviderModelBenchmarkService.ProfileExecution.cs",
        "taskResult.AttemptCount = attempt + 1;",
    )
    require(
        "src/LocalGPT/Services/ProviderModelBenchmarkService.cs",
        "private readonly LocalGptCatalogService catalog;",
        "Math.Clamp(request.MaximumContextTokens, catalog.MinContextTokens, catalog.MaxContextTokens)",
        "Math.Clamp(request.MaximumOutputTokens, catalog.MinOutputTokens, catalog.MaxOutputTokens)",
        "Math.Clamp(request.StopAfterConsecutiveProfileFailures, 0, maxProfiles)",
    )
    require(
        "src/LocalGPT/Services/ProviderModelBenchmarkService.TasksAndProfiles.cs",
        "minimumContextBound",
        "minimumOutputBound",
        "request.ProfileNames.Count > index",
    )

    require(
        "src/LocalGPT/Services/HardwarePerformancePresetService.cs",
        'var tiers = new[] { "Low", "Normal", "High", "Expert", "Max" };',
        "profile.ProfileName.Equals(tier, StringComparison.OrdinalIgnoreCase)",
        "no target completed that exact provider profile point",
    )
    forbid("src/LocalGPT/Services/HardwarePerformancePresetService.cs", 'Name: "Middle"')

    require(
        "src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.BenchmarkTemplates.cs",
        "Five bounded token profile points",
        "Low, Normal, High, Expert and Max",
        "ONE deterministic LocalGPT measurement phase",
        "at five bounded parameter points",
        "The four curator sections are executed together in one bounded provider turn at each of five profile points",
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.RoleSynthesis.cs",
        "There is no inactivity timeout; the Council remains alive until the questions are answered or the run is explicitly stopped.",
        "liveCouncilSessions.Touch(result.RunId)",
    )

    # Constructor docs are attached to constructors, eliminating the 3.0.7 CS1572 drift while satisfying docs coverage.
    for rel, names in (
        ("src/LocalGPT/Services/MinecraftDatapackService.cs", ("catalog", "patterns", "text", "jsonText", "serviceLogger")),
        ("src/LocalGPT/Services/MinecraftProjectService.cs", ("jsonText", "patterns", "datapackService", "catalog", "serviceLogger")),
    ):
        value = read(rel)
        ctor = value.index(f"public {Path(rel).stem}(")
        preamble = value[max(0, ctor - 1800):ctor]
        for name in names:
            if f'<param name="{name}">' not in preamble:
                raise AssertionError(f"constructor XML documentation missing in {rel}: {name}")
            checks += 1

    # No accidental render-mode removal.
    render_files = [p for p in (root / "src/LocalGPT").rglob("*.razor") if "@rendermode" in p.read_text(encoding="utf-8", errors="ignore")]
    if len(render_files) != 20:
        raise AssertionError(f"expected 20 explicit @rendermode files, found {len(render_files)}")
    checks += 1

    setup = read("src/LocalGPT/Services/InitialSetupAssistantService.cs")
    start = setup.index("CreateBenchmarkTeamAsync")
    benchmark_team = setup[start:start + 7000]
    if ".Take(128)" in benchmark_team or ".Take(4)" in benchmark_team:
        raise AssertionError("initial benchmark-team creation still truncates selected model membership")
    checks += 2

    print(f"LocalGPT 3.1.0 release source audit passed: {checks} checks.")
except (AssertionError, ValueError) as exc:
    print(f"LocalGPT 3.1.0 release source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)
