from __future__ import annotations

import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "LocalGPTWebviewWrapper" / "LocalGPT"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def normalized_hash(path: Path) -> str:
    normalized = read(path).replace("\r\n", "\n").replace("\r", "\n")
    return hashlib.sha256(normalized.encode("utf-8")).hexdigest()


def assert_security_manifest() -> None:
    manifest = ROOT / "build" / "security-rules-final19.sha256"
    for raw in read(manifest).splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        expected, relative = line.split("  ", 1)
        actual = normalized_hash(ROOT / relative)
        assert actual == expected, f"security rule changed: {relative}"


def main() -> None:
    council = read(APP / "Services" / "CouncilTextService.cs")
    for forbidden in (
        "private readonly Regex",
        "new Regex(",
        "Regex.",
        "RegexOptions.",
        "NameCleaner()",
        "ModIdCleaner()",
        "PackagePartCleaner()",
        "TimeSpan.FromSeconds(2)",
        "LocalGptCatalogService._whitespacePattern",
        "LocalGptCatalogService.MissingFeaturePattern()",
        "LocalGptCatalogService.ThinkingBlockPattern()",
    ):
        assert forbidden not in council, forbidden
    for required in (
        "ICouncilTextPatternDataService _patterns",
        "_patterns.FormerThoughtBreakPattern",
        "_patterns.FormerThoughtCodeWrapperPattern",
        "_patterns.FormerThoughtOpeningFencePattern",
        "_patterns.FormerThoughtClosingFencePattern",
        "_patterns.FormerThoughtPresentationWrapperPattern",
        "_patterns.FormerThoughtExcessLineBreakPattern",
        "_patterns.WhitespacePattern",
        "_patterns.NameCleanerPattern",
        "_patterns.ModIdCleanerPattern",
        "_patterns.PackagePartCleanerPattern",
        "_patterns.KnowledgeBlockPattern",
        "_patterns.TargetFrameworkPattern",
        "_patterns.PackageReferencePattern",
        "_patterns.ThinkingBlockPattern",
        "_patterns.MinecraftQuotedProjectNamePattern",
        "_patterns.IdentifierSeparatorPattern",
        "_patterns.AlphaNumericWordPattern",
        "_patterns.IntegerPattern",
        "_patterns.ExtractStructuredField",
    ):
        assert required in council, required

    guard = read(ROOT / "build" / "Assert-RuntimeValueOwnership.ps1")
    assert "GetRelativePath" not in guard
    assert ".Contains(" not in guard
    assert ".IndexOf(" in guard

    runtime = read(APP / "Services" / "CouncilRuntimeService.cs")
    assert "LocalGptCatalogService.TargetFrameworkPattern()" not in runtime
    assert "LocalGptCatalogService.PackageReferencePattern()" not in runtime
    assert "_text.ExtractTargetFrameworks(combined, logger)" in runtime
    assert "_text.ExtractPackageReferences(combined, logger)" in runtime

    data_service = read(APP / "Services" / "Persistence" / "CouncilTextPatternDataService.cs")
    for required in (
        "IDbContextFactory<LocalGptMemoryDbContext>",
        "db.RegexPatterns.AsNoTracking()",
        "db.SystemVariables.AsNoTracking()",
        "systemVariables.RegexMatchTimeoutMilliseconds",
        "TimeSpan.FromMilliseconds(timeoutMilliseconds)",
        "cached.Pattern.Equals(row.Pattern",
        "ExtractStructuredField(string body, string name)",
        "StructuredFieldPattern.Matches(body)",
        'GetRequired("builtin.target-framework-pattern")',
        'GetRequired("builtin.package-reference-pattern")',
    ):
        assert required in data_service, required

    seed = read(APP / "Services" / "Persistence" / "InitialDataCatalog.cs")
    for name in (
        "FormerThoughtBreakPattern",
        "FormerThoughtCodeWrapperPattern",
        "FormerThoughtOpeningFencePattern",
        "FormerThoughtClosingFencePattern",
        "FormerThoughtPresentationWrapperPattern",
        "FormerThoughtExcessLineBreakPattern",
        "StructuredFieldPattern",
        "MinecraftQuotedProjectNamePattern",
        "MinecraftExplicitProjectNamePattern",
        "MinecraftNamedProjectPattern",
        "MarkdownHeadingProjectNamePattern",
        "IdentifierSeparatorPattern",
        "AlphaNumericWordPattern",
        "IntegerPattern",
    ):
        assert f"nameof(ICouncilTextPatternDataService.{name})" in seed, name

    definitions = read(APP / "Services" / "Persistence" / "SystemVariableDefinitionService.cs")
    assert '"RegexMatchTimeoutMilliseconds", 2000' in definitions
    assert "ToInitialVariable(RegexMatchTimeoutMilliseconds)" in definitions

    program = read(APP / "Program.cs")
    assert "AddSingleton<ICouncilTextPatternDataService, CouncilTextPatternDataService>()" in program

    baseline = json.loads(read(ROOT / "build" / "runtime-value-ownership-baseline.json"))
    assert not any("CouncilTextService.cs|" in item and "Regex" in item for item in baseline)

    ET.parse(ROOT / "Directory.Build.targets")
    targets = read(ROOT / "Directory.Build.targets")
    assert "AssertLocalGptProtectedRepositoryFiles" in targets
    assert "AssertLocalGptSecurityRulePreservation" in targets
    assert "AssertLocalGptRuntimeValueOwnership" in targets

    assert_security_manifest()
    print("LocalGPT final20 runtime-value ownership checks passed.")


if __name__ == "__main__":
    main()
