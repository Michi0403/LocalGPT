#!/usr/bin/env python3
"""Source-only regression audit for LocalGPT 2.9.4 role-task authority and benchmark task execution."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")

def require(rel, needle):
    text = read(rel)
    if needle not in text:
        raise AssertionError(f"{rel}: missing {needle!r}")

def forbid(rel, needle):
    text = read(rel)
    if needle in text:
        raise AssertionError(f"{rel}: forbidden {needle!r}")

try:
    for rel in [
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
    ]:
        require(rel, "<Version>2.9.4</Version>")

    protocol = "src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj"
    require(protocol, "<Version>2.1.1</Version>")
    require(protocol, "<PackageVersion>2.1.1</PackageVersion>")

    cfg = "src/LocalGPT/Services/CouncilTeamConfigurationService.cs"
    require(cfg, "private const int CurrentSeedVersion = 22;")
    require(cfg, "if (row.IsSystemSeed && row.IsUserModified)")
    require(cfg, "Recovered supplied Council seed")

    seed = "src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs"
    require(seed, 'DisplayName = "Initial Hardware Calibration Benchmark"')
    require(seed, '"benchmark-task-design"')
    require(seed, '"benchmark-subject-execution"')
    require(seed, '"benchmark-calibration"')
    require(seed, 'Role = "Task Curator"')
    require(seed, 'Role = "Benchmark Subject"')
    require(seed, 'Role = "Code Curator"')
    require(seed, 'AiSelectionMode = CouncilRoleAiSelectionMode.AllSelected')
    require(seed, "TASK PACK TO EXECUTE:")
    require(seed, "Execute every numbered task exactly once, in order.")
    require(seed, "The original user request is background context only")
    require(seed, "C# correctness")
    require(seed, "Provider identity")
    require(seed, "Structured settings")
    require(seed, "Accessibility/practical UI reasoning")
    require(seed, "includePriorTranscript: false")
    forbid(seed, '"benchmark-readiness"')

    calibration = "src/LocalGPT/Services/CouncilBenchmarkCalibrationService.cs"
    require(calibration, "MaxProfilesPerModel = 4")
    require(calibration, "MaxTasks = 4")
    require(calibration, "ProfileMode = ProviderModelBenchmarkProfileMode.EvenlySpaced")
    require(calibration, "StopWhenImprovementStalls = false")

    provider = "src/LocalGPT/Services/ProviderModelBenchmarkService.cs"
    for task_name in ["C# correctness", "Provider identity", "Structured settings", "Accessibility"]:
        require(provider, f'new("{task_name}"')

    multi = "src/LocalGPT/Services/MultiModelCouncilService.cs"
    require(multi, "CURRENT WORKFLOW ROLE TASK — AUTHORITATIVE")
    require(multi, "Your assigned responsibility is: {{RoleResponsibility}}")
    require(multi, "BACKGROUND USER REQUEST — CONTEXT ONLY")
    require(multi, "PRIOR COUNCIL EVIDENCE — INPUT ONLY")
    require(multi, "Perform only the CURRENT WORKFLOW ROLE TASK now.")
    require(multi, "do not answer the overall user request in place of your assigned role output")
    require(multi, 'case "SystemBenchmarkCalibration":')

    print("LocalGPT 2.9.4 role-task authority/benchmark execution source audit passed.")
except Exception as exc:
    print(f"LocalGPT 2.9.4 source audit failed: {exc}", file=sys.stderr)
    raise SystemExit(1)
