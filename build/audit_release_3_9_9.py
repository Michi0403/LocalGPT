#!/usr/bin/env python3
"""Source-only release checks for LocalGPT 3.9.9."""
from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VERSION = "3.9.9"
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


# Version surfaces and one-digit minor/patch policy.
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
    ("VALIDATION-v3.9.9-source.md", "# LocalGPT 3.9.9 source validation"),
    ("CHANGELOG-v3.9.9-PROVIDER-MODEL-LINK-INSTALL-CONSOLE-OLLAMA-GUIDE.md", "provider model links, safe install resolution, console repair, and guided Ollama setup"),
):
    require(rel, needle)

# Provider-link and conservative install-resolution contract.
service_rel = "src/LocalGPT/Services/InitialSetupAssistantService.cs"
service = read(service_rel)
for needle in (
    "ResolveProviderCatalogRecommendationAsync(",
    "SearchProviderCatalogAsync(profileKey, query, true, cancellationToken)",
    "Match = matches.Count == 1 ? matches[0] : null",
    "IsConservativeProviderCatalogMatch(",
    "BuildProviderCatalogResolutionQuery(",
    "BuildProviderCatalogSearchUrl(",
    "BuildProviderCatalogModelUrl(profile, providerId)",
    'return $"https://ollama.com/search?q={Uri.EscapeDataString(query ?? string.Empty)}";',
    'return $"https://ollama.com/library/{Uri.EscapeDataString(providerModelId).Replace("%3A", ":", StringComparison.OrdinalIgnoreCase)}";',
):
    if needle not in service:
        fail(f"{service_rel}: missing provider-resolution release contract {needle!r}")

interface_rel = "src/LocalGPT/Interfaces/IInitialSetupAssistantService.cs"
require(interface_rel, "Task<InitialSetupProviderCatalogResolution> ResolveProviderCatalogRecommendationAsync")
model_rel = "src/LocalGPT/BusinessObjects/InitialSetupAssistantModels.cs"
for needle in ("public bool IsProviderCatalogUrlExact { get; set; }", "public sealed class InitialSetupProviderCatalogResolution"):
    require(model_rel, needle)

# Interactive setup UI: source links, resolver/install action and guided Ollama sequence.
panel_rel = "src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor"
panel = read(panel_rel)
for needle in (
    '@T(choice.IsProviderCatalogUrlExact ? "Provider model page" : "Search provider catalog for this model")',
    "ResolveAndInstallModelAsync(choice)",
    'T("Resolve & install model")',
    "Setup.ResolveProviderCatalogRecommendationAsync(",
    "Providers.InstallModelAsync(selectedProviderKey, match.ProviderModelId, true)",
    "GuidedOllamaSetupAsync",
    "Providers.InstallAsync(selectedProviderKey, true)",
    "OllamaProcesses.GetStatusAsync()",
    "OllamaProcesses.StartAsync()",
    "Providers.ConfigureEndpointAsync(selectedProviderKey, true)",
    'T("Set up Ollama (confirmed: install / start / register)")',
):
    if needle not in panel:
        fail(f"{panel_rel}: missing setup release contract {needle!r}")
if re.search(r"^\s*@rendermode\b", panel, flags=re.MULTILINE):
    fail(f"{panel_rel}: child setup panel must inherit the routed page circuit, not create a nested render boundary")

# Shared console must decode provider UTF-8 and flatten terminal controls before display.
console_rel = "src/LocalGPT/Services/ConsoleCommandService.cs"
console = read(console_rel)
for needle in (
    "StandardOutputEncoding = Encoding.UTF8",
    "StandardErrorEncoding = Encoding.UTF8",
    "NormalizeTerminalDisplayText(line)",
    "NormalizeTerminalDisplayText(text)",
    "private string NormalizeTerminalDisplayText(string text)",
    "if (character == '\\r')",
    "if (final == 'G')",
    "else if (final == 'K'",
    "text[index + 1] == ']'",
):
    if needle not in console:
        fail(f"{console_rel}: missing console-normalization contract {needle!r}")

console_css_rel = "src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor.css"
console_css = read(console_css_rel)
for needle in ('white-space: pre;', 'overflow: auto;', 'font-variant-ligatures: none;'):
    if needle not in console_css:
        fail(f"{console_css_rel}: missing console-layout contract {needle!r}")
if "white-space: pre-wrap" in console_css:
    fail(f"{console_css_rel}: terminal console still uses pre-wrap")

# Documentation must describe the explicit guided sequence and conservative model resolution.
doc_rel = "docs/reference/ai-provider-installation.md"
for needle in (
    "## Guided Ollama setup in `/install`",
    "install only when Ollama is missing",
    "the assistant stops instead of pretending setup succeeded",
    "one unique conservative provider identity match",
):
    require(doc_rel, needle)

# Every literal setup T/TF source must exist in every maintained catalog, with exact key parity.
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
            "Text.Resolve␠&␠install␠model": "Anbietermodell ermitteln und installieren",
            "Text.Search␠provider␠catalog␠for␠this␠model": "Anbieterkatalog nach diesem Modell durchsuchen",
            "Text.Provider␠model␠page": "Anbietermodell-Seite",
            "Text.Set␠up␠Ollama␠(confirmed:␠install␠/␠start␠/␠register)": "Ollama einrichten (bestätigt: installieren / starten / registrieren)",
            "Text.CanIRun.ai␠hardware-fit␠discovery␠matched␠uniquely␠in␠the␠selected␠provider␠catalog.": "CanIRun.ai-Hardwareempfehlung wurde eindeutig dem ausgewählten Anbieterkatalog zugeordnet.",
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

# Source-package hygiene.
for path in ROOT.rglob("*"):
    if path.is_dir() and path.name in {"bin", "obj", "__pycache__"}:
        fail(f"generated/compiled directory present: {path.relative_to(ROOT)}")
        break
    if path.is_file() and path.suffix in {".pyc", ".pyo"}:
        fail(f"generated Python bytecode present: {path.relative_to(ROOT)}")
        break

if ERRORS:
    print("LocalGPT 3.9.9 source audit FAILED:")
    for error in ERRORS:
        print(f" - {error}")
    sys.exit(1)
print(f"LocalGPT {VERSION} source audit passed: {route_count} routed pages, {interactive_count} InteractiveServer routed boundaries, {len(catalog_paths)} localization catalogs.")
