#!/usr/bin/env python3
"""Regression audit retaining LocalGPT 2.9.5 configurable all-members team preflight in current source."""
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
        require(rel,'<Version>2.9.9</Version>')
    protocol='src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj'
    require(protocol,'<Version>2.1.1</Version>'); require(protocol,'<PackageVersion>2.1.1</PackageVersion>')

    models='src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs'
    for needle in ['public enum CouncilAllMembersReadinessPreflightMode','LegacyWorkflowDefault','Disabled','RoleAwareProbe','AllMembersReadinessPreflightMode','IncludeAllMembersReadinessPreflightInWorkflowContext','AllMembersReadinessPreflightMaxOutputTokens','AllMembersReadinessPreflightPromptTemplate']:
        require(models,needle)
    page='src/LocalGPT/Components/Pages/CouncilTeams.razor'
    for needle in ['All-members readiness preflight','Preflight mode','Role-aware probe for every selected member','Include all preflight member output in later workflow model context','can be very large on big Councils']:
        require(page,needle)
    cfg='src/LocalGPT/Services/CouncilTeamConfigurationService.cs'
    require(cfg,'private const int CurrentSeedVersion = 25;')
    require(cfg,'CouncilAllMembersReadinessPreflightMode.LegacyWorkflowDefault')
    require(cfg,'Math.Clamp(team.AllMembersReadinessPreflightMaxOutputTokens, 32, 2048)')
    seed='src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs'
    require(seed,'DisplayName = "Initial Hardware Calibration Benchmark"')
    require(seed,'AllMembersReadinessPreflightMode = CouncilAllMembersReadinessPreflightMode.Disabled')
    require(seed,'IncludeAllMembersReadinessPreflightInWorkflowContext = false')
    require(seed,'Users may enable the team-level role-aware preflight explicitly in Council Teams')
    forbid(seed,'Step("benchmark-readiness"')
    multi='src/LocalGPT/Services/MultiModelCouncilService.cs'
    for needle in ['RunConfiguredAllMembersReadinessPreflightAsync','BuildConfiguredAllMembersReadinessPrompt','phase: "Team preflight"','role: "All-members readiness preflight"','Do not execute the user\'s original request','GetCouncilWorkflowContextSteps','!IsConfiguredAllMembersReadinessPreflightStep(step)','AllMembersReadinessPreflightMode == CouncilAllMembersReadinessPreflightMode.RoleAwareProbe','AllMembersReadinessPreflightMode == CouncilAllMembersReadinessPreflightMode.LegacyWorkflowDefault']:
        require(multi,needle)
    print('LocalGPT 2.9.5 configurable all-members team preflight regression audit passed under 2.9.9.')
except Exception as exc:
    print(f'LocalGPT 2.9.5 regression audit failed: {exc}',file=sys.stderr); raise SystemExit(1)
