#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"


def read(*parts: str) -> str:
    path = ROOT.joinpath(*parts)
    if not path.is_file():
        raise AssertionError(f"Missing required source file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def assert_project_closure() -> int:
    count = 0
    for project in ROOT.rglob("*.csproj"):
        if any(part in {"bin", "obj"} for part in project.parts):
            continue
        root = ET.parse(project).getroot()
        for reference in root.iter("ProjectReference"):
            include = reference.attrib.get("Include", "").replace("\\", "/")
            if not include:
                continue
            target = (project.parent / include).resolve()
            if not target.is_file():
                raise AssertionError(
                    f"Broken ProjectReference: {project.relative_to(ROOT)} -> {include}"
                )
            count += 1
    return count


def assert_no_missing_implicit_sources() -> int:
    # SDK projects include *.cs by default. Guard the specific source-loss regression and all new shared contracts.
    required = [
        APP / "Services" / "LearningRoundService.cs",
        APP / "Services" / "LearningRoundDxAiFunctions.cs",
        APP / "Services" / "RegexDxAiFunctions.cs",
        APP / "Services" / "OneWire" / "OrganicPluginDxAiFunctions.cs",
        APP / "BusinessObjects" / "LearningRoundModels.cs",
        APP / "Interfaces" / "ILearningRoundService.cs",
        ROOT / "LocalGPTWebviewWrapper" / "LocalGPT.WireProtocolVersion" / "OneWireProtocolContracts.cs",
    ]
    missing = [str(path.relative_to(ROOT)) for path in required if not path.is_file()]
    if missing:
        raise AssertionError("Required compilation sources are missing: " + ", ".join(missing))
    return len(required)


def main() -> int:
    program = read("LocalGPTWebviewWrapper", "LocalGPT", "Program.cs")
    chat = read("LocalGPTWebviewWrapper", "LocalGPT", "Components", "Pages", "Chat.razor")
    council_text = read("LocalGPTWebviewWrapper", "LocalGPT", "Services", "CouncilTextService.cs")
    initial_data = read("LocalGPTWebviewWrapper", "LocalGPT", "Services", "Persistence", "InitialDataCatalog.cs")
    regex_dx = read("LocalGPTWebviewWrapper", "LocalGPT", "Services", "RegexDxAiFunctions.cs")
    learning_dx = read("LocalGPTWebviewWrapper", "LocalGPT", "Services", "LearningRoundDxAiFunctions.cs")
    organic_dx = read("LocalGPTWebviewWrapper", "LocalGPT", "Services", "OneWire", "OrganicPluginDxAiFunctions.cs")
    blueprint = read("LocalGPTWebviewWrapper", "LocalGPT", "Services", "OrganicCouncilBlueprintService.cs")
    wire = read("LocalGPTWebviewWrapper", "LocalGPT.WireProtocolVersion", "OneWireProtocolContracts.cs")

    assert "public const int DefaultPort = 5000;" in program
    assert "public static System.Int32 Port => System.Threading.Volatile.Read(ref runtimePort);" in program
    assert not re.search(r"(?<!System\.Threading\.)\bVolatile\.(?:Read|Write)", program)
    assert "options.MultipartBodyLengthLimit = long.MaxValue;" in program
    assert "options.MaximumReceiveMessageSize = null;" in program
    assert "options.Limits.MaxRequestBodySize = null;" in program
    assert "ApplicationStarted.Register" in program
    assert "DeleteRuntimeEndpointFile" in program

    wrapper = read("LocalGPTWebviewWrapper", "LocalGPTWebviewWrapper", "App.xaml.cs")
    assert "Environment.GetCommandLineArgs().Skip(1).ToArray()" in wrapper
    assert "await _webApp.StartAsync();" in wrapper
    assert "StartAsync().ConfigureAwait(false)" not in wrapper
    assert 'GetAsync("/health"' in wrapper

    assert 'var prompt = """' in council_text
    assert 'return $"""' not in council_text[council_text.index('MultiModelCouncilServiceCreateCouncilSystemPrompt'):council_text.index('MultiModelCouncilServiceCreateProposalPrompt')]
    assert '<localgpt-self-assessment>{"modelName"' in council_text
    assert '<localgpt-self-assessment>{{"modelName"' not in council_text
    assert '.Replace("__LOCALGPT_MODEL_NAME__"' in council_text

    upload_match = re.search(r"<DxAIChatFileUploadSettings(?P<body>.*?)\/>", chat, re.S)
    assert upload_match, "DxAIChatFileUploadSettings was not found."
    assert "MaxFileCount" in upload_match.group("body")
    assert "MaxFileSize" in upload_match.group("body")
    assert "FileTypeFilter" not in upload_match.group("body")
    for token in [
        'data-testid="council-team-picker"',
        'data-testid="live-council-participation"',
        "Running Council session",
        "No Council heartbeat is running",
        "Refresh running sessions",
        "Enable human participation",
        "Share without stopping generation",
    ]:
        assert token in chat, f"Chat live-council feature missing: {token}"

    generated_regex_seeds = re.findall(r'new\("builtin\.[^"]+"\s*,', initial_data)
    assert len(generated_regex_seeds) >= 55, f"Only {len(generated_regex_seeds)} builtin regex seeds found."

    catalog_source = read("LocalGPTWebviewWrapper", "LocalGPT", "Services", "LocalGptCatalogService.cs")
    generated_regex_methods = re.findall(
        r"\[GeneratedRegex\(.*?\)\]\s*(?:public|private|internal|protected)\s+static\s+partial\s+Regex\s+(\w+)\s*\(",
        catalog_source,
        re.S,
    )
    seed_keys = set(re.findall(r'new\("builtin\.([^"]+)"', initial_data))

    def kebab_case(name: str) -> str:
        name = re.sub(r"([A-Z]+)([A-Z][a-z])", r"\1-\2", name)
        name = re.sub(r"([a-z0-9])([A-Z])", r"\1-\2", name)
        return name.lower()

    missing_regex_seeds = [name for name in generated_regex_methods if kebab_case(name) not in seed_keys]
    assert not missing_regex_seeds, "GeneratedRegex methods without database seeds: " + ", ".join(missing_regex_seeds)
    for token in [
        'new("LearningRoundPolicy"',
        'builtin.solution-project-reference',
        'builtin.csharp-service-registration',
        'builtin.installer-port-contract',
        'builtin.onewire-capability-key',
        'builtin.localgpt-self-assessment-block',
    ]:
        assert token in initial_data, f"Initial data seed missing: {token}"

    for token in ["localgpt.regex.list", "localgpt.regex.get", "localgpt.regex.upsert", "localgpt.regex.test"]:
        assert token in regex_dx
    for token in ["localgpt.learning.snapshot", "localgpt.learning.maintain"]:
        assert token in learning_dx
    assert 'Key = "learning-round"' in blueprint
    team_service = read("LocalGPTWebviewWrapper", "LocalGPT", "Services", "CouncilTeamConfigurationService.cs")
    assert "private const int CurrentSeedVersion = 5;" in team_service
    assert "ApplyDefinition(row, definition);" in team_service
    assert "class ProposePublisherTextFunction" in organic_dx
    assert 'Name: "publisher.text.proposal.request"' in organic_dx

    assert 'public const string Version = "2.1";' in wire
    assert "public const int MaximumMessageBytes = 8 * 1024 * 1024;" in wire
    assert "public const int MaximumDiscoveryBytes = 32 * 1024;" in wire

    for token in [
        "RequiresHumanInteractionOnTargetSystem",
        "RequiresAutomatedInteractionOnTargetSystem",
        "InteractionValueJson",
        "InteractionValueContentType",
        "IOneWireCapabilityProvider",
        "IOneWireTransportAdapter",
    ]:
        assert token in wire, f"Wire protocol contract missing: {token}"

    refs = assert_project_closure()
    sources = assert_no_missing_implicit_sources()
    print(
        "LocalGPT hotfix-3 source contracts passed: "
        f"{len(generated_regex_seeds)} builtin regex seeds, {refs} project references, {sources} critical sources."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"SOURCE CONTRACT FAILURE: {exc}", file=sys.stderr)
        raise SystemExit(1)
