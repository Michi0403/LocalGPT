#!/usr/bin/env python3
"""Source-only release checks for LocalGPT 3.9.8."""
from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VERSION = "3.9.8"
ERRORS: list[str] = []


def fail(message: str) -> None:
    ERRORS.append(message)


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        fail(f"missing file: {rel}")
        return ""
    return path.read_text(encoding="utf-8-sig")


def require(rel: str, needle: str) -> None:
    if needle not in read(rel):
        fail(f"{rel}: missing {needle!r}")


# Version surfaces and the one-digit minor/patch release policy.
for rel in (
    "src/LocalGPT/LocalGPT.csproj",
    "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
    "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
):
    text = read(rel)
    try:
        version = ET.fromstring(text).findtext("PropertyGroup/Version")
    except ET.ParseError as exc:
        fail(f"{rel}: invalid XML: {exc}")
        continue
    if version != VERSION:
        fail(f"{rel}: expected Version {VERSION}, found {version!r}")
    if version and not re.fullmatch(r"\d+\.\d\.\d", version):
        fail(f"{rel}: version violates one-digit minor/patch policy: {version}")

for rel, needle in (
    ("src/LocalGPT/Components/App.razor", f"localgpt-chat-ui.js?v={VERSION}"),
    ("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", f"LocalGPT/{VERSION}"),
    ("src/LocalGPT/Services/InitialSetupAssistantService.cs", f"LocalGPT/{VERSION}"),
    ("docs/index.md", f"Version {VERSION}"),
    ("docs/docfx.json", f'"localgptVersion": "{VERSION}"'),
    ("docs/pdf-cover.html", f"Version {VERSION}"),
    ("docs/pdf/toc.yml", f"LocalGPT-{VERSION}.pdf"),
    ("RELEASE.md", f"# LocalGPT {VERSION}"),
    ("CHANGELOG-v3.9.8-SETUP-LOCALIZATION-NAMING-REPAIR.md", "setup localization and naming repair"),
):
    require(rel, needle)

# Setup assistant localization/naming contract.
panel_rel = "src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor"
panel = read(panel_rel)
for needle in (
    "@inject LocalGPT.Services.Localization.ILocalGptLocalizationService Localization",
    "private string T(string source) => Localization.GetText(source);",
    "private string TF(string source, params object?[] args)",
    "CanIRun.ai hardware match (optional catalog variant)",
    'TF("{0} hardware device(s), {1} provider profile(s), {2} installed model(s)."',
    '@T("Use")',
    '@T("Curator")',
    '@T("runtime")',
):
    if needle not in panel:
        fail(f"{panel_rel}: missing release contract {needle!r}")
if "CanIRun hardware match (optional catalog variant)" in panel:
    fail(f"{panel_rel}: obsolete CanIRun naming remains")
if re.search(r"statusText\s*=\s*\$\"Loaded ", panel):
    fail(f"{panel_rel}: English-only generated Loaded status remains")
if re.search(r"^\s*@rendermode\b", panel, flags=re.MULTILINE):
    fail(f"{panel_rel}: child setup panel must inherit the routed page circuit, not create a nested render boundary")

# Every literal T/TF source must exist in every maintained catalog.
localization_dir = ROOT / "src/LocalGPT/Localization"
catalog_paths = sorted(localization_dir.glob("*.json"))
if not catalog_paths:
    fail("no LocalGPT localization catalogs found")
else:
    catalogs: dict[str, dict[str, str]] = {}
    for path in catalog_paths:
        try:
            catalogs[path.name] = json.loads(path.read_text(encoding="utf-8-sig"))
        except (OSError, json.JSONDecodeError) as exc:
            fail(f"{path.relative_to(ROOT)}: invalid JSON: {exc}")
    if catalogs:
        reference_name, reference = next(iter(catalogs.items()))
        reference_keys = set(reference)
        for name, catalog in catalogs.items():
            if set(catalog) != reference_keys:
                missing = sorted(reference_keys - set(catalog))[:8]
                extra = sorted(set(catalog) - reference_keys)[:8]
                fail(f"localization key parity mismatch {name} vs {reference_name}; missing={missing}, extra={extra}")
        literal_sources = set(re.findall(r'\bT(?:F)?\("((?:[^"\\]|\\.)*)"', panel))
        for source in sorted(literal_sources):
            source = bytes(source, "utf-8").decode("unicode_escape") if "\\" in source else source
            key = "Text." + source.replace(" ", "␠")
            for name, catalog in catalogs.items():
                if key not in catalog:
                    fail(f"{name}: missing setup localization key {key}")
        de = catalogs.get("de-DE.json", {})
        expected_de = {
            "Text.1.␠Local␠host␠hardware": "1. Lokale Host-Hardware",
            "Text.3.␠Local␠AI␠provider": "3. Lokaler KI-Anbieter",
            "Text.Use": "Verwenden",
            "Text.running": "läuft",
            "Text.runtime": "Laufzeit",
            "Text.Install␠model": "Modell installieren",
            "Text.Curator": "Kurator",
        }
        for key, value in expected_de.items():
            if de.get(key) != value:
                fail(f"de-DE.json: expected {key}={value!r}, found {de.get(key)!r}")

# Routed page render-boundary contract. Error is intentionally static.
pages = ROOT / "src/LocalGPT/Components/Pages"
route_count = 0
interactive_count = 0
for path in sorted(pages.rglob("*.razor")):
    text = path.read_text(encoding="utf-8-sig")
    if not re.search(r'^\s*@page\s+"', text, flags=re.MULTILINE):
        continue
    route_count += 1
    is_error = path.name == "Error.razor"
    has_interactive = bool(re.search(r"^\s*@rendermode\s+InteractiveServer\s*$", text, flags=re.MULTILINE))
    if has_interactive:
        interactive_count += 1
    if not is_error and not has_interactive:
        fail(f"{path.relative_to(ROOT)}: routed page is missing @rendermode InteractiveServer")
    if is_error and has_interactive:
        fail(f"{path.relative_to(ROOT)}: Error page should remain intentionally static")
if route_count < 2:
    fail(f"unexpected routed page count: {route_count}")

# Source-package hygiene: this handoff must not contain compiled artifacts.
for path in ROOT.rglob("*"):
    if path.is_dir() and path.name in {"bin", "obj"}:
        fail(f"compiled-artifact directory present: {path.relative_to(ROOT)}")
        break

if ERRORS:
    print("LocalGPT 3.9.8 source audit FAILED:")
    for error in ERRORS:
        print(f" - {error}")
    sys.exit(1)
print(f"LocalGPT {VERSION} source audit passed: {route_count} routed pages, {interactive_count} InteractiveServer routed boundaries, {len(catalog_paths)} localization catalogs.")
