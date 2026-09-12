from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
source_root = root / "src/LocalGPT"
allowed_definition = source_root / "BusinessObjects/ConfigurationRoot.cs"
bare_pattern = re.compile(r"(?<![.\w])ConfigurationRoot\b")
failures: list[str] = []

for path in source_root.rglob("*.cs"):
    if path == allowed_definition:
        continue
    if any(part in {"bin", "obj"} for part in path.parts):
        continue
    text = path.read_text(encoding="utf-8-sig", errors="replace")
    for line_number, line in enumerate(text.splitlines(), 1):
        if bare_pattern.search(line):
            relative = path.relative_to(root).as_posix()
            failures.append(f"{relative}:{line_number}: bare ConfigurationRoot must be qualified or aliased as LocalGptConfigurationRoot")

required_aliases = {
    "src/LocalGPT/Services/OllamaProcessService.cs",
    "src/LocalGPT/Services/ProviderRuntimeManagementService.cs",
    "src/LocalGPT/Services/ProviderModelRuntimeService.cs",
}
for relative in sorted(required_aliases):
    path = root / relative
    text = path.read_text(encoding="utf-8-sig") if path.is_file() else ""
    token = "using LocalGptConfigurationRoot = LocalGPT.BusinessObjects.ConfigurationRoot;"
    if token not in text:
        failures.append(f"{relative}: missing explicit LocalGptConfigurationRoot alias")

if failures:
    print("LocalGPT ConfigurationRoot qualification audit FAILED:")
    for failure in failures:
        print(f" - {failure}")
    sys.exit(1)

print("LocalGPT ConfigurationRoot qualification audit passed: domain configuration references are explicitly qualified or aliased, avoiding Microsoft.Extensions.Configuration.ConfigurationRoot ambiguity.")
