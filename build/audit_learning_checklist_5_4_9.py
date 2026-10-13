"""Source-only consistency checks for the LocalGPT 5.4.9 learning-file repair.

This audit does not execute .NET, database migrations, providers or UI components.
"""

from __future__ import annotations

import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "src" / "LocalGPT"


def source(relative: str) -> str:
    return (SRC / relative).read_text(encoding="utf-8")


def require(condition: bool, description: str) -> None:
    if not condition:
        raise AssertionError(description)


project = source("LocalGPT.csproj")
version = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", project)
require(version is not None, "LocalGPT must declare a release version")
require(tuple(map(int, version.groups())) == (5, 6, 3), "Expected current LocalGPT release 5.6.3")
require(all(int(part) <= 9 for part in version.groups()[1:]), "Version slots must roll over at 10")

models = source("BusinessObjects/ProjectMaintenanceModels.cs")
context = source("BusinessObjects/EFCore/LocalGptMemoryDbContext.cs")
snapshot = source("Migrations/LocalGptMemoryDbContextModelSnapshot.cs")
migration = source("Migrations/20261010210000_AddTrackedFileLearningChecklist.cs")
fields = (
    "LearningStatus", "LearningReadCharacters", "LearningReadComplete",
    "LearningReviewedHash", "LearningEvidence", "LearningBlockReason",
    "LearningUpdatedAtUtc", "LearningCompletedAtUtc",
)
for field in fields:
    require(field in models and f'"{field}"' in snapshot and f'name: "{field}"' in migration,
            f"Tracked-file learning field {field} missing from model, snapshot or migration")
require("LearningStatus" in context and "HasIndex" in context, "Checklist EF index missing")
require("defaultValue: \"Pending\"" in migration, "Existing files must start Pending, not Completed")

reader = source("Services/LearningRoundService.Checklist.cs")
require("ReadNextFileChunkAsync" in reader and "SHA256.HashDataAsync" in reader, "Verified read cursor unavailable")
require("file.LearningReadComplete = !hasMore" in reader, "EOF must be source-backed")
require("!file.LearningReadComplete" in reader, "Premature review must be rejected")
require("Source changed after the complete read" in reader, "Review must verify the current hash")
require("HasUnsafeReparsePoint" in reader, "Link/path source ownership check is required")
for sync_file in ("Services/ProjectMaintenanceService.cs", "Services/LearningProjectWorkspaceSyncService.cs"):
    item = source(sync_file)
    require('LearningStatus = "Pending"' in item and "LearningReviewedHash = string.Empty" in item,
            f"File identity/hash changes must reopen tasks in {sync_file}")

function_source = source("Services/LearningFileChecklistDxAiFunctions.cs")
schemas = re.findall(r'ParameterSchemaJson: """\s*(\{.*?\})\s*"""', function_source, re.S)
require(len(schemas) == 3, "Three learning-file function schemas are required")
for schema in schemas:
    parsed = json.loads(schema)
    require(parsed["type"] == "object" and not parsed["additionalProperties"], "Function schemas must be strict")
for name in ("localgpt.learning.files", "localgpt.learning.file.read", "localgpt.learning.file.review"):
    require(f'"{name}"' in function_source, f"Learning-file DXFunction {name} missing")

maintain = source("Services/LearningRoundDxAiFunctions.cs")
maintain_schemas = re.findall(r'ParameterSchemaJson: """\s*(\{.*?\})\s*"""', maintain, re.S)
require(len(maintain_schemas) == 2, "Learning maintenance schemas changed unexpectedly")
pattern_fields = json.loads(maintain_schemas[1])["properties"]["regexPatterns"]["items"]["properties"]
require("description" in pattern_fields and "sampleText" in pattern_fields,
        "Learning maintenance must accept description and test samples")

ollama = source("Services/OllamaThinkingChatClient.ToolExecution.cs")
require("function.Name.Equals(toolName" in ollama and "ToOllamaToolName(function.Name).Equals(toolName" in ollama,
        "Canonical and transport-safe tool names must both be resolved")
require("var functions = GetAutomaticFunctions()" in ollama, "Resolution must remain allow-list scoped")
require("function.SupportsAutomaticInvocation" in source("Services/OllamaThinkingChatClient.Tools.cs"),
        "Advertised automatic functions must actually allow automatic invocation")

ui = source("Components/Pages/ProjectMaintenance.razor")
require("<DxGrid Data=\"@trackedFiles\"" in ui and "LearningStatus" in ui and "LearningBlockReason" in ui,
        "The durable checklist must remain visible through DevExpress")
catalog_dir = SRC / "Localization"
catalogs = {item.stem: json.loads(item.read_text(encoding="utf-8")) for item in catalog_dir.glob("*.json")}
require(len(catalogs) == 6, "All six supported UI cultures are required")
english = catalogs["en-US"]
german = catalogs["de-DE"]
for culture, catalog in catalogs.items():
    require(set(catalog) == set(english), f"Localized catalog key parity failed for {culture}")
for key in (key for key in english if key.startswith("ProjectMaintenance.Learning")):
    require(english[key] and german[key] and english[key] != german[key], f"Missing German learning UI translation: {key}")
seed = source("Services/OrganicCouncilBlueprintSeedDataService.cs")
require("After localgpt.learning.maintain synchronizes source projects" in seed and
        '"localgpt.learning.file.read"' in seed,
        "Learning Council must be instructed and allowed to resume the per-file queue")
print("LocalGPT 5.6.3 retained learning-file source contract passed: schema, migration, state transitions, tools, seeded Council, DevExpress grid and all localization catalogs.")
