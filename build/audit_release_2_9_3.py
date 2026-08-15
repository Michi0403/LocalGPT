#!/usr/bin/env python3
"""Source-only regression audit for LocalGPT 2.9.3 deterministic first-run benchmark calibration and seed preservation."""
from pathlib import Path
import sys

ROOT=Path(__file__).resolve().parents[1]

def read(rel):
    return (ROOT/rel).read_text(encoding="utf-8")

def require(rel, needle):
    text=read(rel)
    if needle not in text:
        raise AssertionError(f"{rel}: missing {needle!r}")

def forbid(rel, needle):
    text=read(rel)
    if needle in text:
        raise AssertionError(f"{rel}: forbidden {needle!r}")

try:
    for rel in [
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
    ]:
        require(rel, "<Version>2.9.3</Version>")
    require("src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj", "<Version>2.1.1</Version>")
    require("src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj", "<PackageVersion>2.1.1</PackageVersion>")

    cfg="src/LocalGPT/Services/CouncilTeamConfigurationService.cs"
    require(cfg, "private const int CurrentSeedVersion = 21;")
    require(cfg, '"SystemBenchmarkCalibration"')
    require(cfg, "if (row is { IsSystemSeed: true })")
    require(cfg, "CreateUniqueUserCopyKey")
    require(cfg, "CloneAsUserOwnedDefinition")
    require(cfg, "if (row.IsSystemSeed && row.IsUserModified)")
    require(cfg, "Recovered supplied Council seed")
    require(cfg, "Preserved supplied Council seed")

    seed="src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs"
    require(seed, 'DisplayName = "Initial Hardware Calibration Benchmark"')
    require(seed, 'Role = "Benchmark Subject"')
    require(seed, 'AiSelectionMode = CouncilRoleAiSelectionMode.AllSelected')
    require(seed, '"benchmark-readiness"')
    require(seed, '"benchmark-calibration"')
    require(seed, '"SystemBenchmarkCalibration"')
    require(seed, "requiresHumanCheckpoint: true")
    require(seed, '"benchmark-coverage"')
    require(seed, '"benchmark-performance"')
    require(seed, '"benchmark-profiles"')
    require(seed, "enableRolePeerReview: true")
    require(seed, "summarizeRoleResults: true")
    require(seed, "representative sampling and size-bracket extrapolation are forbidden")

    ui="src/LocalGPT/Components/Pages/CouncilTeams.razor"
    require(ui, '("SystemBenchmarkCalibration", "LocalGPT all-member benchmark calibration engine")')

    models="src/LocalGPT/BusinessObjects/ProviderModelBenchmarkModels.cs"
    require(models, "public bool OwnLiveSession { get; set; } = true;")
    require(models, "public Action<string>? ProgressMessage { get; set; }")

    provider="src/LocalGPT/Services/ProviderModelBenchmarkService.cs"
    require(provider, "request.ProgressMessage?.Invoke(normalized);")
    require(provider, "if (request.OwnLiveSession)")
    require(provider, "standalone live session ownership is {OwnLiveSession}")

    calibration="src/LocalGPT/Services/CouncilBenchmarkCalibrationService.cs"
    require(calibration, "MaxProfilesPerModel = 4")
    require(calibration, "ProfileMode = ProviderModelBenchmarkProfileMode.EvenlySpaced")
    require(calibration, "StopWhenImprovementStalls = false")
    require(calibration, "IncludeCouncilReview = false")
    require(calibration, "OwnLiveSession = false")
    require(calibration, "No representative sampling or size-bracket extrapolation is allowed")
    require(calibration, "SaveBenchmarkProfileSetAsync")
    require(calibration, "Coverage gate: PASS")
    require(calibration, "Coverage gate: PARTIAL")

    presets="src/LocalGPT/Services/HardwarePerformancePresetService.cs"
    require(presets, "SaveBenchmarkProfileSetAsync")
    for tier in ["Low", "Middle", "High", "Expert"]:
        require(presets, f'(Name: "{tier}"')
    require(presets, "BuildBenchmarkTierRoute")
    require(presets, "item.SourceRunId == sourceRunId && item.Name == normalizedName")

    multi="src/LocalGPT/Services/MultiModelCouncilService.cs"
    require(multi, 'case "SystemBenchmarkCalibration":')
    require(multi, "benchmarkCalibration.RunAsync")
    require(multi, 'ModelName = "LocalGPT Benchmark Engine"')
    require(multi, "Targets = result.ModelSelections.ToList()")
    require(multi, "Council benchmark workflow")
    require(multi, "completed normally after")

    onboarding="src/LocalGPT/Services/FirstRunOnboardingService.cs"
    require(onboarding, "Calibrate installed models first")
    catalog="src/LocalGPT/Services/LocalGptCatalogService.cs"
    require(catalog, "Run the recommended first-install model calibration")
    require(catalog, "Do not sample representatives")

    print("LocalGPT 2.9.3 deterministic benchmark calibration/seed-preservation source audit passed.")
except Exception as exc:
    print(f"LocalGPT 2.9.3 source audit failed: {exc}", file=sys.stderr)
    raise SystemExit(1)
