#!/usr/bin/env python3
"""Source-only regression audit for LocalGPT 2.8.8 optional same-role coordination."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]


def read(rel):
    path = root / rel
    if not path.is_file():
        raise AssertionError(f"missing {rel}")
    return path.read_text(encoding="utf-8")


def require(rel, *needles):
    text = read(rel)
    missing = [needle for needle in needles if needle not in text]
    if missing:
        raise AssertionError(f"{rel} missing {missing}")


try:
    for rel in [
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
    ]:
        require(rel, "<Version>2.9.5</Version>")
        match = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", read(rel))
        if not match or int(match.group(2)) > 9 or int(match.group(3)) > 9:
            raise AssertionError(f"version-slot policy failed for {rel}")

    require("Directory.Build.props", "<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>")
    require("src/LocalGPT/Services/CouncilTextService.cs", "using System.Text.RegularExpressions;")

    require(
        "src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs",
        "enum CouncilRoleResultSynthesisMemberMode",
        "DeterministicRandomRoleMember",
        "AssignedRoleMember",
        "public bool EnableRolePeerReview { get; set; }",
        "public bool SummarizeRoleResults { get; set; }",
        "public CouncilRoleResultSynthesisMemberMode RoleResultSynthesisMemberMode",
        "public string RoleResultSynthesisModelName { get; set; } = string.Empty;",
    )

    require(
        "src/LocalGPT/Components/Pages/CouncilTeams.razor",
        "Role-member coordination",
        "Peer-review role-member answers, report usefulness and vote for the strongest role result",
        "Summarize multiple role-member answers into one final result of this role",
        "Random assigned role member (stable per run)",
        "One selected role member",
        "WorkflowModelCandidates(step)",
        "{{ExecutingRoleMember}}",
        "{{RolePeerMembers}}",
    )

    require(
        "src/LocalGPT/Services/CouncilTeamConfigurationService.cs",
        "CouncilRoleResultSynthesisMemberMode.DeterministicRandomRoleMember",
        "step.RoleResultSynthesisModelName = step.RoleResultSynthesisModelName?.Trim() ?? string.Empty;",
        "uses a selected role-result summarizer but no provider-qualified role member is selected",
        "but that model is not bound to role",
    )

    service = read("src/LocalGPT/Services/MultiModelCouncilService.cs")
    required_service = [
        "BuildConfiguredRolePeerReviewPrompt(",
        "BuildConfiguredRoleSynthesisPrompt(",
        "SelectConfiguredRoleSynthesisParticipant(",
        "BuildConfiguredRoleEvidence(",
        "definition.EnableRolePeerReview && usablePrimaryAiSteps.Count >= 2",
        "definition.SummarizeRoleResults && usablePrimaryAiSteps.Count >= 2",
        'allowDxFunctions: false,',
        'Peer usefulness — <exact provider-qualified peer identity>: <0-100>%',
        'Role vote: <exact provider-qualified role member identity>',
        'benchmark candidate lists, tool arguments, earlier-role outputs, or the transcript are task SUBJECTS',
        'Model names mentioned in the user request, benchmark targets, prior transcript, tool arguments, or another role\'s output are task data',
        '{{ExecutingRoleMember}}',
        '{{RolePeerMembers}}',
        'role-synthesis',
        'System.Security.Cryptography.SHA256.HashData',
        'step.SummarizeRoleResults &&',
        'step.RoleResultSynthesisMemberMode == CouncilRoleResultSynthesisMemberMode.AssignedRoleMember',
    ]
    missing = [needle for needle in required_service if needle not in service]
    if missing:
        raise AssertionError(f"MultiModelCouncilService.cs missing {missing}")

    # Backward compatibility: both new coordination switches are bools with default false and
    # every extra execution branch is gated by one of those switches plus >=2 usable AI role members.
    models = read("src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs")
    if "public bool EnableRolePeerReview { get; set; } = true" in models:
        raise AssertionError("peer review must remain opt-in")
    if "public bool SummarizeRoleResults { get; set; } = true" in models:
        raise AssertionError("role synthesis must remain opt-in")
    if service.count("allowDxFunctions: false,") < 2:
        raise AssertionError("both coordination phases must explicitly suppress DXFunctions")

    modes = []
    for path in (root / "src/LocalGPT").rglob("*.razor"):
        for line in path.read_text(encoding="utf-8").splitlines():
            if "@rendermode" in line:
                modes.append((str(path.relative_to(root)), line.strip()))
    if len(modes) != 19:
        raise AssertionError(f"expected 19 LocalGPT rendermode directives, found {len(modes)}")

    print("LocalGPT 2.8.8 optional same-role coordination source audit passed.")
except (AssertionError, OSError) as exc:
    print(f"LocalGPT 2.8.8 source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)
