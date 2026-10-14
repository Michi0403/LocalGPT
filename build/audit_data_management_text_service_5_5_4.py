"""Source-only regression for Data Management field protection and text-service ownership."""

from pathlib import Path
import json
import re


ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "src" / "LocalGPT"


def require(condition: bool, detail: str) -> None:
    if not condition:
        raise SystemExit(f"LocalGPT 5.6.2 Data Management regression: {detail}")


page = (SOURCE / "Components/Pages/DataManagement.razor").read_text(encoding="utf-8-sig")
service = (SOURCE / "Services/SqliteGridPresentationService.cs").read_text(encoding="utf-8-sig")
require(
    "GridPresentation.IsReadOnlyEditorColumn(column, SelectedRow is not null, CanEditCurrentTable, Logger)" in page,
    "record editor must use the existing injected SQLite presentation service for field protection",
)
require(
    'column.IsPrimaryKey &&' in service and
    '(isExistingRow || column.Type.Contains("INT", StringComparison.OrdinalIgnoreCase))' in service,
    "primary-key protection, including integer primary keys during insertion, must be retained",
)
require('return true;' in service[service.index('public bool IsReadOnlyEditorColumn'):],
        "classification failure must leave the field read-only")

# Mirrors the source selection and matching rules in the Windows PowerShell 5.1 build gate.
baseline = set(json.loads((ROOT / "build/text-service-ownership-baseline.json").read_text(encoding="utf-8-sig")))
pattern = re.compile(
    r"(?m)^(.*(?:\bRegex\s*\.|\bnew\s+Regex\s*\(|\.Replace\s*\(|\.Split\s*\(|"
    r"\bstring\.Join\s*\(|\bWebUtility\.HtmlDecode\s*\(|\.StartsWith\s*\(|"
    r"\.EndsWith\s*\(|\.IndexOf\s*\(|\.Substring\s*\(|"
    r"\.Contains\s*\([^\r\n;]*StringComparison\.).*)$"
)
service_owned = re.compile(r"(?:CouncilText|PanelText|TextService|RegexService|StringService|ReviewerPolicy)\.")
violations = []
for folder in ("Components", "Controllers", "Controller"):
    directory = SOURCE / folder
    if not directory.is_dir():
        continue
    for path in sorted((*directory.rglob("*.cs"), *directory.rglob("*.razor"))):
        content = path.read_text(encoding="utf-8-sig")
        for match in pattern.finditer(content):
            line = " ".join(match.group(1).strip().split())
            if service_owned.search(line):
                continue
            identity = f"{path.relative_to(ROOT).as_posix()}|{line}"
            if identity not in baseline:
                violations.append(identity)

require(not violations, "Windows text-service ownership gate would reject:\n" + "\n".join(violations))
print("LocalGPT 5.6.2 text-service ownership and protected SQLite primary-key editor audit passed.")
