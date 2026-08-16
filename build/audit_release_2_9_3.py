#!/usr/bin/env python3
"""Regression audit retaining LocalGPT 2.9.3 all-member calibration and supplied-seed preservation contracts in current source."""
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[1]
def read(rel): return (ROOT/rel).read_text(encoding='utf-8')
def require(rel, needle):
    if needle not in read(rel): raise AssertionError(f"{rel}: missing {needle!r}")
def forbid(rel, needle):
    if needle in read(rel): raise AssertionError(f"{rel}: forbidden {needle!r}")
try:
    for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj']:
        require(rel,'<Version>2.9.7</Version>')
    protocol='src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj'
    require(protocol,'<Version>2.1.1</Version>'); require(protocol,'<PackageVersion>2.1.1</PackageVersion>')

    cfg='src/LocalGPT/Services/CouncilTeamConfigurationService.cs'
    require(cfg,'private const int CurrentSeedVersion = 24;')
    require(cfg,'"SystemBenchmarkCalibration"')
    require(cfg,'if (row is { IsSystemSeed: true })')
    require(cfg,'CreateUniqueUserCopyKey'); require(cfg,'CloneAsUserOwnedDefinition')
    require(cfg,'if (row.IsSystemSeed && row.IsUserModified)')
    require(cfg,'Recovered supplied Council seed'); require(cfg,'Preserved supplied Council seed')

    seed='src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs'
    require(seed,'DisplayName = "Initial Hardware Calibration Benchmark"')
    require(seed,'Role = "Benchmark Subject"')
    require(seed,'AiSelectionMode = CouncilRoleAiSelectionMode.AllSelected')
    for step in ['"benchmark-task-design"','"benchmark-calibration"','"benchmark-curation"','"benchmark-coverage"','"benchmark-performance"','"benchmark-profiles"']:
        require(seed,step)
    require(seed,'"SystemBenchmarkCalibration"'); require(seed,'requiresHumanCheckpoint: true')
    require(seed,'Representative sampling, duplicate social/measurement task rounds and role takeover are forbidden')
    forbid(seed,'Step("benchmark-readiness"')

    models='src/LocalGPT/BusinessObjects/ProviderModelBenchmarkModels.cs'
    require(models,'public bool OwnLiveSession { get; set; } = true;')
    require(models,'public Action<string>? ProgressMessage { get; set; }')

    provider='src/LocalGPT/Services/ProviderModelBenchmarkService.cs'
    require(provider,'request.ProgressMessage?.Invoke(normalized);')
    require(provider,'if (request.OwnLiveSession)')
    require(provider,'enableAutomaticTools: false')

    calibration='src/LocalGPT/Services/CouncilBenchmarkCalibrationService.cs'
    require(calibration,'MaxProfilesPerModel = 4')
    require(calibration,'MaxTasks = 1')
    require(calibration,'TaskDefinitions = [taskPack]')
    require(calibration,'ProfileMode = ProviderModelBenchmarkProfileMode.EvenlySpaced')
    require(calibration,'StopWhenImprovementStalls = false')
    require(calibration,'IncludeCouncilReview = false')
    require(calibration,'OwnLiveSession = false')
    require(calibration,'No representative sampling or size-bracket extrapolation is allowed')
    require(calibration,'SaveBenchmarkProfileSetAsync')
    require(calibration,'Coverage gate: PASS'); require(calibration,'Coverage gate: PARTIAL')

    presets='src/LocalGPT/Services/HardwarePerformancePresetService.cs'
    require(presets,'SaveBenchmarkProfileSetAsync')
    for tier in ['Low','Middle','High','Expert']: require(presets,f'(Name: "{tier}"')
    require(presets,'BuildBenchmarkTierRoute')
    require(presets,'item.SourceRunId == sourceRunId && item.Name == normalizedName')

    multi='src/LocalGPT/Services/MultiModelCouncilService.cs'
    require(multi,'case "SystemBenchmarkCalibration":')
    require(multi,'benchmarkCalibration.RunAsync')
    require(multi,'ModelName = "LocalGPT Benchmark Engine"')
    require(multi,'Targets = result.ModelSelections.ToList()')
    require(multi,'completed normally after')

    require('src/LocalGPT/Services/FirstRunOnboardingService.cs','Calibrate installed models first')
    require('src/LocalGPT/Services/LocalGptCatalogService.cs','Run the recommended first-install model calibration')
    require('src/LocalGPT/Services/LocalGptCatalogService.cs','Do not sample representatives')
    print('LocalGPT 2.9.3 deterministic benchmark calibration/seed-preservation regression audit passed under 2.9.7.')
except Exception as exc:
    print(f'LocalGPT 2.9.3 regression audit failed: {exc}',file=sys.stderr); raise SystemExit(1)
