"""Source-only parity check for the mandatory explicit InteractiveServer route registry.

The original Windows PowerShell guard remains enabled and authoritative for .NET builds.
"""

from pathlib import Path
import re


ROOT = Path(__file__).resolve().parent.parent
PAGES = ROOT / "src/LocalGPT/Components/Pages"
GUARD = ROOT / "build/Assert-InteractiveServerRenderModes.ps1"
CONTENT = GUARD.read_text(encoding="utf-8")
ENTRIES = dict(re.findall(
    r"(?m)^\s*'(?P<name>Components/Pages/[^']+\.razor)'\s*=\s*'(?P<mode>[^']+)'",
    CONTENT,
))

errors = []
NON_PRERENDERED_PAGE = "Components/Pages/DataManagement.razor"
NON_PRERENDERED_MODE = "@rendermode @(new InteractiveServerRenderMode(prerender: false))"
if "$reviewedNonPrerenderedPages = @('Components/Pages/DataManagement.razor')" not in CONTENT:
    errors.append("The guard must explicitly review the sole non-prerendered routed page")
if "$reviewedNonPrerenderedPages -contains $relative" not in CONTENT:
    errors.append("The routed-page validation must verify the reviewed non-prerendered mode")
for page in PAGES.rglob("*.razor"):
    source = page.read_text(encoding="utf-8")
    if not re.search(r'(?m)^@page\s+"/', source):
        continue
    name = page.relative_to(ROOT / "src/LocalGPT").as_posix()
    if name == "Components/Pages/Error.razor":
        if "@rendermode" in source:
            errors.append(f"{name}: static error fallback must not create a circuit")
        continue
    expected_mode = NON_PRERENDERED_MODE if name == NON_PRERENDERED_PAGE else "@rendermode InteractiveServer"
    if ENTRIES.get(name) != expected_mode:
        errors.append(f"{name}: missing or mismatched explicit InteractiveServer registry entry")
    first = next((line.strip() for line in source.splitlines() if line.strip()), "")
    if first != expected_mode:
        errors.append(f"{name}: expected first directive {expected_mode}, found {first!r}")

if errors:
    raise AssertionError("InteractiveServer registry regression:\n  - " + "\n  - ".join(errors))

print(f"InteractiveServer registry source contract passed for {len(ENTRIES)} explicit routed pages; Data Management owns a reviewed non-prerendered service circuit.")
