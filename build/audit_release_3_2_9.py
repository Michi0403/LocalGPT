#!/usr/bin/env python3
"""Source-only release audit for LocalGPT 3.2.9 database/relationship/lifecycle hardening."""
from pathlib import Path
import json
import re

ROOT = Path(__file__).resolve().parents[1]
failures: list[str] = []
checks: list[str] = []


def read(relative_path: str) -> str:
    return (ROOT / relative_path).read_text(encoding="utf-8-sig", errors="strict")


def require(relative_path: str, needle: str, label: str) -> None:
    text = read(relative_path)
    if needle not in text:
        failures.append(f"{relative_path} missing {label}: {needle}")
    else:
        checks.append(label)


def require_regex(relative_path: str, pattern: str, label: str) -> None:
    text = read(relative_path)
    if re.search(pattern, text, re.MULTILINE | re.DOTALL) is None:
        failures.append(f"{relative_path} missing {label}: /{pattern}/")
    else:
        checks.append(label)


for relative_path in [
    "src/LocalGPT/LocalGPT.csproj",
    "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
    "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
]:
    require(relative_path, "<Version>3.2.9</Version>", f"3.2.9 version in {relative_path}")

_major, minor, patch = map(int, "3.2.9".split("."))
if minor >= 10 or patch >= 10:
    failures.append("release version violates single-digit minor/patch policy")
else:
    checks.append("single-digit minor/patch release policy")

require("src/LocalGPT/LocalGPT.csproj", "<DevExpressVersion>25.2.9</DevExpressVersion>", "DevExpress 25.2.9 retention")
require("src/LocalGPT/Components/App.razor", "js/localgpt-chat-ui.js?v=3.2.9", "browser cache version marker")
require("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/3.2.9", "outbound product version")

# Database workbench and semantic row identity.
require("src/LocalGPT/Components/Pages/Database.razor", "@rendermode InteractiveServer", "Database InteractiveServer boundary")
require("src/LocalGPT/Components/Pages/Database.razor.cs", 'new("knowledge", "Knowledge & relationships"', "knowledge workbench section")
require("src/LocalGPT/Components/Pages/Database.razor.cs", 'new("tables", "SQLite tables"', "SQLite workbench section")
require("src/LocalGPT/Components/Pages/Database.razor", '<ConfigurationWorkbenchPanel SectionKey="tables"', "separate SQLite workbench panel")
require("src/LocalGPT/Components/Pages/Database.razor", 'FieldName="__record" Caption="Record"', "semantic Record grid column")
require("src/LocalGPT/BusinessObjects/SqliteTableEditorModels.cs", 'preferredColumn', "semantic row label support")
require("src/LocalGPT/Services/SqliteGridPresentationService.cs", '"__record"', "semantic grid-record presentation")

# Explicit relationship model/service/migration.
require("src/LocalGPT/BusinessObjects/KnowledgeRegexModels.cs", "public sealed class CouncilKnowledgeRegexPatternLink", "knowledge-regex relationship model")
require("src/LocalGPT/Interfaces/IKnowledgeRegexLinkService.cs", "TestRecognitionAsync", "knowledge-regex recognition contract")
require("src/LocalGPT/Services/Persistence/KnowledgeRegexLinkService.cs", ".Take(64)", "bounded recognition relationship count")
require("src/LocalGPT/Services/Persistence/KnowledgeRegexLinkService.cs", "TimeSpan.FromMilliseconds(350)", "bounded per-regex recognition timeout")
require("src/LocalGPT/Services/Persistence/KnowledgeRegexLinkService.cs", "if (!request.UserConfirmed)", "human confirmation before relationship write")
require("src/LocalGPT/Migrations/20260823145500_AddKnowledgeRegexRelationships.cs", 'name: "CouncilKnowledgeRegexPatternLinks"', "knowledge-regex migration table")
require("src/LocalGPT/Migrations/20260823145500_AddKnowledgeRegexRelationships.cs", "onDelete: ReferentialAction.Restrict", "restrictive relationship delete behavior")
require("src/LocalGPT/BusinessObjects/EFCore/LocalGptMemoryDbContext.cs", ".WithMany(entry => entry.RegexPatternLinks)", "knowledge reverse regex navigation")
require("src/LocalGPT/BusinessObjects/EFCore/LocalGptMemoryDbContext.cs", ".WithMany(pattern => pattern.KnowledgeLinks)", "regex reverse knowledge navigation")
require("src/LocalGPT/BusinessObjects/CouncilKnowledgeEntry.cs", "ProjectTopicLinks { get; set; } = [];", "knowledge reverse project-topic navigation")
require("src/LocalGPT/BusinessObjects/CouncilKnowledgeEntry.cs", "RegexPatternLinks { get; set; } = [];", "knowledge reverse regex link collection")
require("src/LocalGPT/BusinessObjects/RegexPattern.cs", "KnowledgeLinks { get; set; } = [];", "regex reverse knowledge link collection")
require("src/LocalGPT/Program.ServiceRegistration.cs", "IKnowledgeRegexLinkService", "knowledge-regex DI registration")

# Existing project-topic knowledge capability is reachable from knowledge UI.
require("src/LocalGPT/Interfaces/ILocalGptProjectService.cs", "GetKnowledgeLinksAsync", "project knowledge-link query contract")
require("src/LocalGPT/Interfaces/ILocalGptProjectService.cs", "UnlinkKnowledgeAsync", "project knowledge-unlink contract")
require("src/LocalGPT/Services/LocalGptProjectService.cs", "GetKnowledgeLinksAsync", "project knowledge-link query implementation")
require("src/LocalGPT/Services/LocalGptProjectService.cs", "UnlinkKnowledgeAsync", "project knowledge-unlink implementation")

# Restored reverse navigations must remain in the authoritative DbContext relationship configuration.
for needle, label in [
    (".WithMany(project => project.DocumentImports)", "project document-import reverse navigation"),
    (".WithMany(revision => revision.DocumentImports)", "revision document-import reverse navigation"),
    (".WithMany(revision => revision.Requirements)", "revision requirement reverse navigation"),
    (".WithMany(revision => revision.Artifacts)", "revision artifact reverse navigation"),
    (".WithMany(requirement => requirement.Artifacts)", "requirement artifact reverse navigation"),
    (".WithMany(installation => installation.BuildVerifications)", "compiler build-verification reverse navigation"),
    (".WithMany(conversation => conversation.CouncilGameSessions)", "conversation Council-game reverse navigation"),
]:
    require("src/LocalGPT/BusinessObjects/EFCore/LocalGptMemoryDbContext.cs", needle, label)

# Render lifecycle hardening: visible async state updates explicitly re-enter renderer.
require("src/LocalGPT/Components/Layout/Drawer.razor", "await InvokeAsync(StateHasChanged).ConfigureAwait(true) /* renderer-affine responsive-width refresh */;", "drawer post-browser-width rerender")
require("src/LocalGPT/Components/Pages/Database.razor", "renderer-affine busy-state refresh", "database busy-state rerender")
require("src/LocalGPT/Components/Pages/Database.razor", "renderer-affine completion refresh", "database completion rerender")

policy = json.loads(read("build/async-continuation-policy.json"))
renderer_helpers = policy.get("rendererAffineHelperMethods", {})
database_helpers = renderer_helpers.get("Components/Pages/Database.razor", [])
for method_name in ["RunUiActionAsync"]:
    if method_name not in database_helpers:
        failures.append(f"async continuation policy missing Database.{method_name}")
    else:
        checks.append(f"renderer-affine policy includes Database.{method_name}")

# Teardown races are handled narrowly, not globally suppressed.
require("src/LocalGPT/Components/Layout/ThemeJsChangeDispatcher.cs", "if (!_module.IsDisposed())", "theme JS module disposed guard")
require("src/LocalGPT/Components/Layout/ThemeJsChangeDispatcher.cs", "catch (OperationCanceledException)", "theme teardown cancellation handling")
require("src/LocalGPT/Components/Layout/ThemeJsChangeDispatcher.cs", "catch (ObjectDisposedException)", "theme teardown already-disposed handling")
require("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "if (disposed)", "game-console idempotent async disposal")
require("src/LocalGPT/Components/Shared/ChatGameConsole.razor", "catch (ObjectDisposedException)", "game-console already-disposed teardown handling")
require_regex("src/LocalGPT/Services/CompositeChatClient.cs", r"await updates\.DisposeAsync\(\)\.ConfigureAwait\(false\);.*?catch \(OperationCanceledException\).*?catch \(ObjectDisposedException\)", "primary streaming enumerator teardown hardening")
require_regex("src/LocalGPT/Services/CompositeChatClient.cs", r"await retryUpdates\.DisposeAsync\(\)\.ConfigureAwait\(false\);.*?catch \(OperationCanceledException\).*?catch \(ObjectDisposedException\)", "retry streaming enumerator teardown hardening")

# EF guard now validates entity-specific relationships instead of global duplicate navigation names.
require("build/Assert-EfSnapshotArchitecture.ps1", "Relationship checks are entity-specific", "entity-specific EF snapshot relationship policy")
require("build/Assert-EfSnapshotArchitecture.ps1", '.WithMany("RegexPatternLinks")', "EF snapshot guard knowledge-regex contract")

# Release documentation/disclosure.
require("CHANGELOG-v3.2.9-DATABASE-KNOWLEDGE-RELATIONSHIPS-LIFECYCLE-HARDENING.md", "SQLite `integrity_check`: **ok**", "database analysis result in changelog")
require("DATABASE-RELATIONSHIP-ANALYSIS-v3.2.9.md", "PRAGMA foreign_key_check` returned zero violations", "database relationship analysis")
require("HISTORICAL-CAPABILITY-REVIEW-v3.2.9.md", "v0.8", "historical capability review")
require("VALIDATION-v3.2.9-source.md", "source-only and not compiled", "source-only validation disclosure")
require("RELEASE.md", "PublisherStudio remains at **2.9.7**", "unchanged PublisherStudio statement")

if failures:
    print("LocalGPT 3.2.9 source release audit failed:")
    for failure in failures:
        print("  -", failure)
    raise SystemExit(1)

print(f"LocalGPT 3.2.9 source release audit passed: {len(checks)} checks.")
