from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
pattern = re.compile(r"(?<!`)\$([A-Za-z_][A-Za-z0-9_]*)\:")
valid_scopes = {"env", "script", "global", "local", "private", "using", "variable", "function", "alias"}
failures = []
for path in sorted(root.rglob("*.ps1")):
    try:
        text = path.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        continue
    for line_number, line in enumerate(text.splitlines(), 1):
        for match in pattern.finditer(line):
            name = match.group(1)
            if name.lower() in valid_scopes:
                continue
            failures.append(f"{path.relative_to(root)}:{line_number}: invalid or ambiguous PowerShell variable reference {match.group(0)!r}; delimit the variable as ${{{name}}}: when a literal colon follows")
if failures:
    print("PowerShell variable interpolation audit FAILED:")
    print("\n".join(f" - {item}" for item in failures))
    sys.exit(1)
print("PowerShell variable interpolation audit passed: no invalid non-scope $name: references found in maintained .ps1 files.")
