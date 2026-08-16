#!/usr/bin/env python3
"""Source-only regression audit for LocalGPT 2.9.5 configurable all-members team preflight."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")

def require(rel, needle):
    if needle not in read(rel):
        raise AssertionError(f"{rel}: missing {needle!r}")

def forbid(rel, needle):
    if needle in read(rel):
        raise AssertionError(f"{rel}: forbidden {needle!r}")

try:
    for rel in [
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
    ]:
        require(rel, "<Version>2.9.5</Version>")

    protocol = "src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj"
    require(protocol, "<Version>2.1.1</Version>")
    require(protocol, "<PackageVersion>2.1.1</PackageVersion>")

    models = "src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs"
    require(models, "public enum CouncilAllMembersReadinessPreflightMode")
    require(models, "LegacyWorkflowDefault")
    require(models, "Disabled")
    require(models, "RoleAwareProbe")
    require(models, "AllMembersReadinessPreflightMode")
    require(models, "IncludeAllMembersReadinessPreflightInWorkflowContext")
    require(models, "AllMembersReadinessPreflightMaxOutputTokens")
    require(models, "AllMembersReadinessPreflightPromptTemplate")

    page = "src/LocalGPT/Components/Pages/CouncilTeams.razor"
    require(page, "All-members readiness preflight")
    require(page, "Preflight mode")
    require(page, "Role-aware probe for every selected member")
    require(page, "Include all preflight member output in later workflow model context")
    require(page, "can be very large on big Councils")

    cfg = "src/LocalGPT/Services/CouncilTeamConfigurationService.cs"
    require(cfg, "private const int CurrentSeedVersion = 23;")
    require(cfg, "CouncilAllMembersReadinessPreflightMode.LegacyWorkflowDefault")
    require(cfg, "Math.Clamp(team.AllMembersReadinessPreflightMaxOutputTokens, 32, 2048)")

    seed = "src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs"
    require(seed, 'DisplayName = "Initial Hardware Calibration Benchmark"')
    require(seed, "AllMembersReadinessPreflightMode = CouncilAllMembersReadinessPreflightMode.Disabled")
    require(seed, "IncludeAllMembersReadinessPreflightInWorkflowContext = false")
    require(seed, "Users may enable the team-level role-aware preflight explicitly in Council Teams")
    forbid(seed, 'Step("benchmark-readiness"')

    multi = "src/LocalGPT/Services/MultiModelCouncilService.cs"
    require(multi, "RunConfiguredAllMembersReadinessPreflightAsync")
    require(multi, "BuildConfiguredAllMembersReadinessPrompt")
    require(multi, 'phase: "Team preflight"')
    require(multi, 'role: "All-members readiness preflight"')
    require(multi, "Do not execute the user\'s original request")
    require(multi, "GetCouncilWorkflowContextSteps")
    require(multi, "!IsConfiguredAllMembersReadinessPreflightStep(step)")
    require(multi, "AllMembersReadinessPreflightMode == CouncilAllMembersReadinessPreflightMode.RoleAwareProbe")
    require(multi, "AllMembersReadinessPreflightMode == CouncilAllMembersReadinessPreflightMode.LegacyWorkflowDefault")

    print("LocalGPT 2.9.5 configurable all-members team preflight source audit passed.")
except Exception as exc:
    print(f"LocalGPT 2.9.5 source audit failed: {exc}", file=sys.stderr)
    raise SystemExit(1)
