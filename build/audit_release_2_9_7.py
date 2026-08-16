#!/usr/bin/env python3
"""Source-only compile-contract regression audit for LocalGPT 2.9.7 under current 2.9.9 source."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8")

def require(rel: str, needle: str) -> None:
    if needle not in read(rel):
        raise AssertionError(f"{rel}: missing {needle!r}")

def forbid(rel: str, needle: str) -> None:
    if needle in read(rel):
        raise AssertionError(f"{rel}: forbidden {needle!r}")

try:
    for rel in [
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
    ]:
        require(rel, "<Version>2.9.9</Version>")

    inbox = "src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor"
    forbid(inbox, "outcome.Succeeded")
    require(inbox, "outcome.Status, Vocabulary.Get().DeferredCompleted")
    require(inbox, "Approval is not reported as successful execution.")

    multi = "src/LocalGPT/Services/MultiModelCouncilService.cs"
    forbid(multi, "team.KnowledgeReferences")
    require(multi, "request.ExternalProjectContextJson")
    require(multi, "External project knowledge/context: supplied for this request")
    require(multi, "team.PreferredCapabilities.Any(item => item.Contains(\"knowledge\"")

    outcome = "src/LocalGPT/BusinessObjects/HumanCollaborationModels.cs"
    require(outcome, "public sealed record DeferredDxAiExecutionOutcome(")
    require(outcome, "string Status,")
    require(outcome, "string ResultStatus,")
    forbid(outcome, "bool Succeeded")

    team = "src/LocalGPT/BusinessObjects/OrganicCouncilModels.cs"
    require(team, "public sealed class OrganicCouncilTeamDefinition")
    require(team, "public List<string> PreferredCapabilities { get; set; } = [];")

    protocol = "src/LocalGPT.WireProtocolVersion/LocalGPT.WireProtocolVersion.csproj"
    require(protocol, "<Version>2.1.1</Version>")
    require(protocol, "<PackageVersion>2.1.1</PackageVersion>")

    print("LocalGPT 2.9.7 compile-contract regression audit passed under 2.9.9.")
except Exception as exc:
    print(f"LocalGPT 2.9.7 compile-contract regression audit failed under 2.9.9: {exc}", file=sys.stderr)
    raise SystemExit(1)
