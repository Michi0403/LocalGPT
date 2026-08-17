#!/usr/bin/env python3
"""Checks that Council runtime behavior policy is persisted/user-editable rather than hidden in orchestration code."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

def read(rel: str) -> str:
    path = ROOT / rel
    if rel.endswith('.cs'):
        stem = path.with_suffix('')
        parts = sorted(stem.parent.glob(stem.name + '*.cs'))
        if parts:
            return '\n'.join(part.read_text(encoding='utf-8') for part in parts)
    if rel.endswith('.razor'):
        stem = path.with_suffix('')
        parts = ([path] if path.is_file() else []) + sorted(stem.parent.glob(stem.name + '*.razor.cs'))
        if parts:
            return '\n'.join(part.read_text(encoding='utf-8') for part in parts)
    return path.read_text(encoding='utf-8')

def require(rel: str, needle: str) -> None:
    if needle not in read(rel):
        raise AssertionError(f"{rel}: missing {needle!r}")

def forbid(rel: str, needle: str) -> None:
    if needle in read(rel):
        raise AssertionError(f"{rel}: hidden runtime policy remains: {needle!r}")

try:
    require('AGENTS.md', 'User-observable application behavior and policy must be owned by serializable BusinessObjects')
    require('docs/architecture/project-data.md', 'User-observable behavior policy is configuration data.')

    models = 'src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs'
    for needle in [
        'public enum CouncilAutomaticFunctionPolicyMode',
        'public List<string> AllowedAutomaticFunctions { get; set; } = [];',
        'public CouncilAutomaticFunctionPolicyMode AutomaticFunctionPolicyMode { get; set; }',
        'public int RoleComplianceRetryCount { get; set; } = 1;',
        'public bool FinalAnswerRecoveryEnabled { get; set; } = true;',
        'public int FinalAnswerRecoveryMaxOutputTokens { get; set; } = 8192;',
        'public string AllowedAutomaticFunctionsJson { get; set; } = "[]";',
        'public bool IsDeleted { get; set; }',
    ]:
        require(models, needle)

    policy = 'src/LocalGPT/Services/CouncilAutomaticFunctionPolicyService.cs'
    for needle in [
        'CouncilAutomaticFunctionPolicyMode.TeamAllowList',
        'CouncilAutomaticFunctionPolicyMode.ExactAllowList',
        'CouncilAutomaticFunctionPolicyMode.AllPolicyApproved',
        'CouncilAutomaticFunctionPolicyMode.Disabled',
        'team.AllowedAutomaticFunctions',
        'step.AllowedAutomaticFunctions',
    ]:
        require(policy, needle)
    require('src/LocalGPT/Interfaces/ICouncilAutomaticFunctionPolicyService.cs', 'CouncilAutomaticFunctionPolicyResolution Resolve(')
    require('src/LocalGPT/Program.cs', 'AddScoped<ICouncilAutomaticFunctionPolicyService, CouncilAutomaticFunctionPolicyService>()')

    multi = 'src/LocalGPT/Services/MultiModelCouncilService.cs'
    require(multi, 'councilAutomaticFunctionPolicy.Resolve(team, definition, suppressOrganicFunctions)')
    require(multi, 'automaticFunctionPolicy.AutomaticFunctionAllowList')
    require(multi, 'automaticFunctionPolicy.Description')
    forbid(multi, 'automaticFunctionAllowList: definition.AllowedAutomaticFunctions')
    # Canonical benchmark function names are allowed in resettable seed templates, not hidden in runtime orchestration.
    for function_name in [
        'localgpt.hardware.performance.presets.get',
        'localgpt.hardware.performance.presets.list',
        'localgpt.knowledge.list',
        'localgpt.onboarding.status',
        'localgpt.time_state.now',
    ]:
        forbid(multi, f'"{function_name}"')

    config = 'src/LocalGPT/Services/CouncilTeamConfigurationService.cs'
    for needle in [
        'private const int CurrentSeedVersion = 25;',
        'GetDefaultTemplatesAsync',
        'DeleteAsync',
        'ResetToTemplateAsync',
        'AllowedAutomaticFunctionsJson',
        'row.IsDeleted = true;',
        'if (row.IsDeleted)',
        'NormalizeAutomaticFunctionPolicy',
    ]:
        require(config, needle)

    ui = 'src/LocalGPT/Components/Pages/CouncilTeams.razor'
    for needle in [
        'Automatic/native functions allowed by this team',
        "Use this team's allow-list",
        "Use this step's exact allow-list",
        'All registered functions allowed by LocalGPT safety policy',
        'Delete selected preset',
        'Reset selected from template',
        'GetDefaultTemplatesAsync',
        'Role-compliance corrective retries',
        'Final-answer recovery max output tokens',
    ]:
        require(ui, needle)

    db = 'src/LocalGPT/Services/Persistence/DatabaseMigrationCompatibilityService.cs'
    for needle in [
        'AllowedAutomaticFunctionsJson',
        'IsDeleted',
        'AllMembersReadinessPreflightMode',
        'AllMembersReadinessPreflightPromptTemplate',
    ]:
        require(db, needle)

    seed = 'src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs'
    # Resettable templates may ship useful starting lists; they must not be the runtime policy owner.
    require(seed, 'AllowedAutomaticFunctions =')
    require(seed, '"localgpt.time_state.now"')

    print('Configurable Council behavior-policy audit passed.')
except Exception as exc:
    print(f'Configurable Council behavior-policy audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
