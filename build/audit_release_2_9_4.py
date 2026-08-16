#!/usr/bin/env python3
"""Regression audit retaining LocalGPT 2.9.4 role-task authority in current source."""
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[1]
def read(rel): return (ROOT/rel).read_text(encoding='utf-8')
def require(rel,needle):
    if needle not in read(rel): raise AssertionError(f"{rel}: missing {needle!r}")
def forbid(rel,needle):
    if needle in read(rel): raise AssertionError(f"{rel}: forbidden {needle!r}")
try:
    for rel in ['src/LocalGPT/LocalGPT.csproj','src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj','src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj']:
        require(rel,'<Version>2.9.8</Version>')
    protocol='src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj'
    require(protocol,'<Version>2.1.1</Version>'); require(protocol,'<PackageVersion>2.1.1</PackageVersion>')
    cfg='src/LocalGPT/Services/CouncilTeamConfigurationService.cs'
    require(cfg,'private const int CurrentSeedVersion = 25;'); require(cfg,'Recovered supplied Council seed')

    seed='src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs'
    require(seed,'DisplayName = "Initial Hardware Calibration Benchmark"')
    require(seed,'"benchmark-task-design"'); require(seed,'"benchmark-calibration"')
    require(seed,'Role = "Task Curator"'); require(seed,'Role = "Benchmark Subject"'); require(seed,'Role = "Code Curator"')
    require(seed,'AiSelectionMode = CouncilRoleAiSelectionMode.AllSelected')
    require(seed,'Produce exactly four numbered tasks')
    require(seed,'C# correctness'); require(seed,'Provider identity'); require(seed,'Structured settings'); require(seed,'Accessibility/practical UI reasoning')
    require(seed,'includePriorTranscript: false')
    forbid(seed,'"benchmark-subject-execution"')
    forbid(seed,'Step("benchmark-readiness"')

    calibration='src/LocalGPT/Services/CouncilBenchmarkCalibrationService.cs'
    require(calibration,'BuildCuratedTaskPack(request.TaskPackText)')
    require(calibration,'MaxProfilesPerModel = 4'); require(calibration,'MaxTasks = 1')
    require(calibration,'TaskDefinitions = [taskPack]')
    require(calibration,'ProfileMode = ProviderModelBenchmarkProfileMode.EvenlySpaced')

    multi='src/LocalGPT/Services/MultiModelCouncilService.cs'
    require(multi,'CURRENT WORKFLOW ROLE TASK — AUTHORITATIVE')
    require(multi,'BACKGROUND USER REQUEST — CONTEXT ONLY')
    require(multi,'Perform only the CURRENT WORKFLOW ROLE TASK now.')
    require(multi,'do not answer the overall user request in place of your assigned role output')
    require(multi,'ROLE COMPLIANCE: being an AI model is not a reason to decline')
    require(multi,'configured corrective retry attempt(s)')
    require(multi,'case "SystemBenchmarkCalibration":')
    print('LocalGPT 2.9.4 role-task authority regression audit passed under 2.9.8.')
except Exception as exc:
    print(f'LocalGPT 2.9.4 regression audit failed: {exc}',file=sys.stderr); raise SystemExit(1)
