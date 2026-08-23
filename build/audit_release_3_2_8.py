#!/usr/bin/env python3
"""Source-only release audit for LocalGPT 3.2.8 DX Functions render repair."""
from pathlib import Path
import json
import re

ROOT = Path(__file__).resolve().parents[1]
failures = []
checks = []


def read(relative_path: str) -> str:
    """Read one repository text file with the repository's UTF-8/BOM tolerance."""
    return (ROOT / relative_path).read_text(encoding="utf-8-sig")


def require(text: str, needle: str, label: str) -> None:
    """Record a release check requiring one exact source marker."""
    if needle not in text:
        failures.append(f"missing {label}: {needle}")
    else:
        checks.append(label)


for relative_path in [
    "src/LocalGPT/LocalGPT.csproj",
    "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
    "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
]:
    require(read(relative_path), "<Version>3.2.8</Version>", f"3.2.8 version in {relative_path}")

_major, minor, patch = map(int, "3.2.8".split("."))
if minor >= 10 or patch >= 10:
    failures.append("release version violates single-digit minor/patch policy")
else:
    checks.append("single-digit minor/patch release policy")

require(read("src/LocalGPT/LocalGPT.csproj"), "<DevExpressVersion>25.2.9</DevExpressVersion>", "DevExpress 25.2.9 retention")
require(read("src/LocalGPT/Components/App.razor"), "js/localgpt-chat-ui.js?v=3.2.8", "browser cache version marker")
require(read("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs"), "LocalGPT/3.2.8", "outbound LocalGPT product version")

catalog = read("src/LocalGPT/Components/Pages/DxFunctionCatalog.razor")
require(catalog, "@rendermode InteractiveServer", "DX Functions InteractiveServer boundary")
require(catalog, "private IEnumerable<DxAiFunctionCatalogEntry> FilteredEntries => _entries", "derived filtered catalog view")
require(catalog, "Catalog.GetEntriesAsync().ConfigureAwait(true)", "renderer-affine catalog reload")
require(catalog, "Catalog.SynchronizeAsync().ConfigureAwait(true)", "renderer-affine catalog synchronization")
require(catalog, "}).ConfigureAwait(true);", "renderer-affine catalog save continuation")

post_load_render = re.search(
    r"StateHasChanged\(\);\s*await\s+ReloadAsync\(\)\.ConfigureAwait\(true\);\s*StateHasChanged\(\);",
    catalog,
    re.MULTILINE,
)
if post_load_render is None:
    failures.append("DX Functions first interactive load does not render both loading and completed states")
else:
    checks.append("DX Functions first interactive load renders completed catalog state")

policy = json.loads(read("build/async-continuation-policy.json"))
helpers = policy.get("rendererAffineHelperMethods", {}).get("Components/Pages/DxFunctionCatalog.razor", [])
for method_name in ["ReloadAsync", "SynchronizeAsync", "SaveAsync", "SaveVisibleAsync", "HandleUserFunctionsChangedAsync"]:
    if method_name not in helpers:
        failures.append(f"DX Functions renderer-affine policy is missing {method_name}")
    else:
        checks.append(f"renderer-affine policy includes {method_name}")

require(read("VALIDATION-v3.2.8-source.md"), "Blazor does not automatically schedule a render", "DX Functions render-regression explanation")
require(read("RELEASE.md"), "PublisherStudio remains at 2.9.7", "unchanged PublisherStudio version statement")
require(read("VALIDATION-v3.2.8-source.md"), "source-only and not compiled", "source-only validation disclosure")

if failures:
    print("LocalGPT 3.2.8 source release audit failed:")
    for failure in failures:
        print("  -", failure)
    raise SystemExit(1)

print(f"LocalGPT 3.2.8 source release audit passed: {len(checks)} checks.")
