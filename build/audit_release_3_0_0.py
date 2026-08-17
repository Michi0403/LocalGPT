\
#!/usr/bin/env python3
"""Source-only regression audit for LocalGPT 3.0.0 EF migration/startup repair."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]

def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8", errors="replace")

def require(rel: str, needle: str) -> None:
    text = read(rel)
    if needle not in text:
        raise AssertionError(f"{rel}: missing expected text: {needle}")

def braced_block(text: str, start: int) -> str:
    opening = text.find("{", start)
    if opening < 0:
        raise AssertionError("opening brace not found")
    depth = 0
    for index in range(opening, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[opening:index + 1]
    raise AssertionError("unterminated brace block")

def assert_scalar_snapshot_coverage() -> tuple[int, int]:
    context = read("src/LocalGPT/BusinessObjects/EFCore/LocalGptMemoryDbContext.cs")
    snapshot = read("src/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs")
    bo_root = ROOT / "src/LocalGPT/BusinessObjects"
    files = list(bo_root.rglob("*.cs"))

    enums: set[str] = set()
    source_texts: dict[Path, str] = {}
    for path in files:
        text = path.read_text(encoding="utf-8", errors="replace")
        source_texts[path] = text
        enums.update(re.findall(r"\benum\s+([A-Za-z_]\w*)", text))

    scalar_types = {
        "string", "Guid", "DateTime", "DateTimeOffset", "DateOnly", "TimeOnly",
        "TimeSpan", "bool", "byte", "sbyte", "short", "ushort", "int", "uint",
        "long", "ulong", "float", "double", "decimal", "char",
    }
    entity_types = sorted(set(re.findall(r"DbSet<([A-Za-z_]\w*)>", context)))
    checked_properties = 0
    missing: list[str] = []

    for entity_type in entity_types:
        class_pattern = re.compile(
            r"\b(?:public|internal)\s+(?:(?:sealed|partial|abstract)\s+)*class\s+"
            + re.escape(entity_type) + r"\b"
        )
        class_block = None
        for text in source_texts.values():
            match = class_pattern.search(text)
            if match:
                class_block = braced_block(text, match.start())
                break
        if class_block is None:
            raise AssertionError(f"{entity_type}: entity class body not found")

        snapshot_match = re.search(
            r'modelBuilder\.Entity\("[^"]*\.' + re.escape(entity_type) + r'", b =>\s*\{',
            snapshot,
        )
        if snapshot_match is None:
            raise AssertionError(f"{entity_type}: model snapshot entity block not found")
        snapshot_block = braced_block(snapshot, snapshot_match.start())
        snapshot_properties = set(
            re.findall(r'b\.Property<[^>]+>\("([A-Za-z_]\w*)"\)', snapshot_block)
        )

        property_pattern = re.compile(
            r"public\s+(?:required\s+)?(?P<type>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*\??)"
            r"\s+(?P<name>[A-Za-z_]\w*)\s*\{\s*get;\s*(?:private\s+)?set;\s*\}",
            re.S,
        )
        for prop in property_pattern.finditer(class_block):
            type_name = prop.group("type").rstrip("?")
            if type_name not in scalar_types and type_name not in enums:
                continue
            prefix = class_block[max(0, prop.start() - 240):prop.start()]
            if re.search(r"\[NotMapped(?:Attribute)?\]", prefix):
                continue
            checked_properties += 1
            name = prop.group("name")
            if name not in snapshot_properties:
                missing.append(f"{entity_type}.{name}")

    if missing:
        raise AssertionError(
            "Persisted scalar properties missing from EF model snapshot: " + ", ".join(missing)
        )
    return len(entity_types), checked_properties

def main() -> None:
    for rel in (
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    ):
        require(rel, "<Version>3.0.1</Version>")

    migration = "src/LocalGPT/Migrations/20260816233500_AddCouncilTeamUserPolicyFields.cs"
    required_columns = {
        "AllowedAutomaticFunctionsJson": 'defaultValue: "[]"',
        "IsDeleted": "defaultValue: false",
        "AllMembersReadinessPreflightMode": "defaultValue: 0",
        "IncludeAllMembersReadinessPreflightInWorkflowContext": "defaultValue: false",
        "AllMembersReadinessPreflightMaxOutputTokens": "defaultValue: 192",
        "AllMembersReadinessPreflightPromptTemplate": 'defaultValue: ""',
    }
    require(migration, '[Migration("20260816233500_AddCouncilTeamUserPolicyFields")]')
    migration_text = read(migration)
    for column, default in required_columns.items():
        if f'name: "{column}"' not in migration_text or default not in migration_text:
            raise AssertionError(f"{migration}: missing {column} with {default}")

    snapshot = read("src/LocalGPT/Migrations/LocalGptMemoryDbContextModelSnapshot.cs")
    team_match = re.search(
        r'modelBuilder\.Entity\("LocalGPT\.BusinessObjects\.CouncilTeamConfiguration", b =>\s*\{',
        snapshot,
    )
    if team_match is None:
        raise AssertionError("CouncilTeamConfiguration snapshot block missing")
    team_block = braced_block(snapshot, team_match.start())
    for column in required_columns:
        if f'("{column}")' not in team_block:
            raise AssertionError(f"CouncilTeamConfiguration snapshot missing {column}")

    require("Directory.Build.targets", "<EfModelSnapshotConsistencyScript>")
    require("Directory.Build.targets", 'Name="AssertLocalGptEfModelSnapshotConsistency"')
    require("build/Assert-EfModelSnapshotConsistency.ps1", "EF model/snapshot consistency validation passed")

    entities, properties = assert_scalar_snapshot_coverage()
    print(
        f"LocalGPT 3.0.0 EF migration/startup source audit passed: "
        f"{entities} DbSet entities and {properties} persisted scalar properties covered."
    )

if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(f"LocalGPT 3.0.0 source audit failed: {exc}", file=sys.stderr)
        sys.exit(1)
