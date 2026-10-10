#!/usr/bin/env python3
"""Static regression contracts for source-revision toolchain authority. No SDK/build required."""
from pathlib import Path
root = Path(__file__).resolve().parents[1] / 'src' / 'LocalGPT'
def read(rel): return (root / rel).read_text(encoding='utf-8')
workspace = read('Services/ChatUploadWorkspaceService.cs')
teams = read('Services/OrganicCouncilBlueprintSeedDataService.cs')
seeding = read('Services/CouncilTeamConfigurationService.Seeding.cs')
team_config = read('Services/CouncilTeamConfigurationService.cs')
gate = read('Services/MultiModelCouncilService.WorkspaceAndResearchGates.cs')
workflow = read('Services/MultiModelCouncilService.WorkflowDefinitionExecution.cs')
compiler = read('Services/OrganicCouncilBlueprintSeedDataService.BuildOrchestrationTemplates.cs')
checks = {
 'frameworks are parsed from exact csproj XML': 'System.Xml.Linq.XDocument.Load(path)' in workspace and 'TargetFrameworks' in workspace,
 'frameworks grouped per original archive': 'projects.GroupBy(' in workspace and 'archiveGroup.Key' in workspace,
 'global.json is inventoried and its sdk version is reported': '"global.json"' in workspace and 'sdk.version=' in workspace,
 'global.json rollForward is reported': 'sdk.rollForward=' in workspace,
 'global.json allowPrerelease is reported': 'sdk.allowPrerelease=' in workspace,
 'python project and interpreter metadata inventoried': all(x in workspace for x in ['"pyproject.toml"','".python-version"','"pyvenv.cfg"', '"Pipfile.lock"']),
 'python version metadata constrained': 'requires-python' in workspace and 'python_version' in workspace,
 'python dependency lockfiles are inventoried': all(x in workspace for x in ['"poetry.lock"','"uv.lock"','"requirements.txt"']),
 'other language version manifests are inventoried': all(x in workspace for x in ['"go.mod"','"Cargo.toml"','"rust-toolchain.toml"','"package.json"','"CMakePresets.json"']),
 'Java and Gradle version manifests inventoried': '"pom.xml"' in workspace and '"gradle.properties"' in workspace,
 'toolchain declarations are per extracted archive root': 'file.RelativePath.StartsWith("extracted/"' in workspace and 'GroupBy(file => string.Join("/", file.RelativePath.Split(\'/\').Take(2))' in workspace,
 'source metadata distinct from installed compiler': 'An installed runtime does not establish an installed SDK' in workspace,
 'current revision is never silently migrated': 'A migration is a NEW revision' in workspace and 'never an in-place replacement' in gate,
 'research judge receives deterministic local Knowledge/regex receipt': 'BuildLocalResearchEvidenceReceiptAsync(cancellationToken)' in workflow and 'GetEntriesAsync(cancellationToken: cancellationToken)' in gate and 'runtimePolicy.GetSnapshot()' in gate,
 'local catalog check cannot impersonate curated regex check': 'the independent Knowledge/curated regex catalog still requires localgpt.regex.list/get' in gate,
 'research preflight failures are surfaced as unverified': 'Mandatory local research preflight: UNVERIFIED' in gate,
 'research judge explicitly checks source, local knowledge and regex': 'RESEARCH JUDGE EVIDENCE CHECKLIST' in teams and 'regex catalog (list/get)' in teams,
 'research judge has actual knowledge tool permissions': '"localgpt.learning.snapshot", "localgpt.knowledge.list", "localgpt.knowledge.freshness.report", "localgpt.regex.list", "localgpt.regex.get"' in teams,
 'research judge can inspect installed tools': '"toolchain.installation.list"' in teams,
 'research judge can inspect local remote-source cache': 'localgpt.knowledge.remote.inspect' in teams,
 'external source fetch still needs human approval': 'human-approval-gated localgpt.knowledge.remote.import' in teams and 'existing approval/feedback' in teams,
 'missing toolchain requires human resource question': 'human.collaboration.request' in teams and 'BLOCKING resource question' in teams,
 'current revision pin is part of every seed team': 'team.ArchitectureContracts.Add(versionRevisionBoundary)' in seeding,
 'user edited seed copies preserved': 'row.IsSystemSeed && row.IsUserModified' in seeding and 'CloneAsUserOwnedDefinition' in seeding,
 'system teams seed version bumped': 'CurrentSeedVersion = 43' in team_config,
 'current project revision parent is approved separately': 'project.revision.save' in seeding and 'parent revision' in seeding,
 'model fallback is not forwarded as trusted evidence': 'GuardCurrentRevisionToolchainClaims(stageAnswer, sourceAuthority, result, request)' in workflow,
 'guard verifies source-backed frameworks': '### Authoritative source project frameworks' in gate and '.csproj`:' in gate,
 'guard rejects affirmative fallback and non-standard claims': 'fallback to' in gate and 'non-standard .NET' in gate,
 'guard does not automatically approve a revision': 'human.collaboration.request' in gate and 'project.revision.save' in gate,
 'existing compiler workflow rejects same-revision downgrade': 'NEVER silently change the current project' in compiler,
 'version bumped': '<Version>5.4.5</Version>' in read('LocalGPT.csproj'),
}
for k,v in checks.items():
 if not v: print('FAIL:',k)
if not all(checks.values()): raise SystemExit(1)
print(f'Council version/revision authority source audit passed: {len(checks)} checks.')
