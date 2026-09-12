#!/usr/bin/env python3
"""Source-only release checks for LocalGPT 4.0.1."""
from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VERSION = "4.0.1"
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
    ("VALIDATION-v4.0.1-source.md", "# LocalGPT 4.0.1 source validation"),
    ("CHANGELOG-v4.0.1-TEXT-OWNERSHIP-FAST-OLLAMA-DOWNLOAD.md", "fast streaming path"),
):
    require(rel, needle)

# Build-guard repair: no direct component StringComparison Contains/StartsWith introduced by 4.0.0.
panel_rel = "src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor"
panel = read(panel_rel)
for needle in (
    "@inject CouncilTextService CouncilText",
    'CouncilText.StartsWithText(profile.Key, "ollama-", StringComparison.OrdinalIgnoreCase)',
    'CouncilText.ContainsText(diagnostic, "requires a newer version of Ollama", StringComparison.OrdinalIgnoreCase)',
    'CouncilText.ContainsText(diagnostic, "Please download the latest version", StringComparison.OrdinalIgnoreCase)',
):
    if needle not in panel:
        fail(f"{panel_rel}: missing text-service ownership repair {needle!r}")
for forbidden in (
    'profile.Key.StartsWith("ollama-", StringComparison.OrdinalIgnoreCase)',
    'diagnostic.Contains("requires a newer version of Ollama", StringComparison.OrdinalIgnoreCase)',
    'diagnostic.Contains("Please download the latest version", StringComparison.OrdinalIgnoreCase)',
    "Providers.IsOllamaProfile",
):
    if forbidden in panel:
        fail(f"{panel_rel}: forbidden hot-path/direct text operation remains: {forbidden!r}")
if re.search(r"^\s*@rendermode\b", panel, flags=re.MULTILINE):
    fail(f"{panel_rel}: child setup panel must inherit the routed page circuit")

# Preserve 4.0.0 update/progress/circuit behavior.
for needle in (
    '@onclick="UpdateOllamaAsync">@T("Update Ollama (confirmed)")',
    "private async Task UpdateOllamaAsync()",
    "Providers.UpdateAsync(selectedProviderKey, true)",
    "HandleOllamaUpdateRequirementAsync(result)",
    "private int consoleRenderPending;",
    "private int consoleRenderWorkerRunning;",
    "await Task.Delay(250, consoleRenderCancellation.Token).ConfigureAwait(false);",
):
    if needle not in panel:
        fail(f"{panel_rel}: missing preserved 4.0.0 setup contract {needle!r}")
if panel.count("HandleOllamaUpdateRequirementAsync(result)") != 3:
    fail(f"{panel_rel}: expected all 3 model-install paths to recognize newer Ollama requirement")

bootstrap_rel = "src/LocalGPT/Services/AiProviderBootstrapService.cs"
bootstrap = read(bootstrap_rel)
for needle in (
    "public async Task<LocalConsoleCommandResult> UpdateAsync(",
    "string.IsNullOrWhiteSpace(profile.UpdateCommand) ? profile.InstallCommand : profile.UpdateCommand",
    "TimeoutSeconds = isReadOnly ? 30 : 3600",
):
    if needle not in bootstrap:
        fail(f"{bootstrap_rel}: missing provider update/timeout contract {needle!r}")
if "TimeoutSeconds = isReadOnly ? 30 : 600" in bootstrap:
    fail(f"{bootstrap_rel}: old 10-minute mutation timeout remains")

console_rel = "src/LocalGPT/Services/ConsoleCommandService.cs"
console = read(console_rel)
for needle in (
    "StandardOutputEncoding = Encoding.UTF8",
    "StandardErrorEncoding = Encoding.UTF8",
    "HttpCompletionOption",  # harmless broad guard: must not be accidentally inserted in console process code
    "private async Task PumpOutputAsync(StreamReader reader",
    "PublishProgressSnapshot(",
    "TryGetProgressPercentage(normalized, out var percentage, out var progressKey)",
    "private string NormalizeTerminalDisplayText(string text)",
):
    if needle == "HttpCompletionOption":
        continue
    if needle not in console:
        fail(f"{console_rel}: missing preserved progress contract {needle!r}")
if "BeginOutputReadLine" in console or "BeginErrorReadLine" in console:
    fail(f"{console_rel}: line-only output capture returned")

registration_rel = "src/LocalGPT/Program.ServiceRegistration.cs"
for needle in (
    "options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(2)",
    "options.ClientTimeoutInterval = TimeSpan.FromMinutes(2)",
    "options.KeepAliveInterval = TimeSpan.FromSeconds(15)",
):
    require(registration_rel, needle)

# Provider-model discovery/resolution must remain intact.
setup_rel = "src/LocalGPT/Services/InitialSetupAssistantService.cs"
setup = read(setup_rel)
for needle in (
    "ResolveProviderCatalogRecommendationAsync(",
    "IsConservativeProviderCatalogMatch(",
    "BuildProviderCatalogModelUrl(profile, providerId)",
    "RememberProviderCandidates(candidates);",
    "private async Task<IReadOnlyList<MultiModelCouncilModelCandidate>> GetProviderCandidatesAsync",
):
    if needle not in setup:
        fail(f"{setup_rel}: missing preserved provider model contract {needle!r}")

# Windows Ollama profile must use the direct fast HTTP path; Linux/macOS remain vendor shell paths.
doc_rel = "docs/reference/ai-provider-installation.md"
doc = read(doc_rel)
profile_blocks = re.findall(r"```localgpt-provider-profile\s*(\{.*?\})\s*```", doc, flags=re.DOTALL)
profiles: list[dict[str, object]] = []
for index, block in enumerate(profile_blocks):
    try:
        profiles.append(json.loads(block))
    except json.JSONDecodeError as exc:
        fail(f"{doc_rel}: provider profile block {index} invalid JSON: {exc}")
by_key = {str(item.get("key", "")): item for item in profiles}
windows = by_key.get("ollama-windows")
if not windows:
    fail(f"{doc_rel}: ollama-windows profile missing")
else:
    install = str(windows.get("installCommand", ""))
    update = str(windows.get("updateCommand", ""))
    if install != update:
        fail(f"{doc_rel}: guided Windows install/update should share the same reviewed fast path")
    for needle in (
        "https://ollama.com/download/OllamaSetup.exe",
        "HttpClientHandler",
        "AllowAutoRedirect=$true",
        "HttpCompletionOption]::ResponseHeadersRead",
        "4194304",
        ">>> Ollama download {0}%",
        "Start-Process -FilePath $target -ArgumentList '/SILENT' -Wait -PassThru",
        "finally",
        "Remove-Item -LiteralPath $target",
        "LocalGPT/4.0.1",
    ):
        if needle not in update:
            fail(f"{doc_rel}: ollama-windows fast path missing {needle!r}")
    if "irm https://ollama.com/install.ps1 | iex" in update:
        fail(f"{doc_rel}: guided Windows updater still routes the large download through install.ps1")
for key in ("ollama-linux", "ollama-macos"):
    item = by_key.get(key)
    if not item:
        fail(f"{doc_rel}: {key} profile missing")
        continue
    if "curl -fsSL https://ollama.com/install.sh | sh" not in str(item.get("updateCommand", "")):
        fail(f"{doc_rel}: {key} vendor update path changed unexpectedly")
for needle in (
    "streaming `HttpClient` transfer",
    "4 MiB buffer",
    "bounded integer percentage output",
    "Manual users can still use Ollama's documented",
):
    if needle not in doc:
        fail(f"{doc_rel}: missing fast-download explanation {needle!r}")

# Exact localization parity and route render boundary contract.
localization_dir = ROOT / "src/LocalGPT/Localization"
catalog_paths = sorted(localization_dir.glob("*.json"))
if not catalog_paths:
    fail("no LocalGPT localization catalogs found")
else:
    catalogs = {}
    for path in catalog_paths:
        try:
            value = json.loads(path.read_text(encoding="utf-8-sig"))
            if not isinstance(value, dict):
                raise ValueError("catalog root is not an object")
            catalogs[path.name] = value
        except (OSError, json.JSONDecodeError, ValueError) as exc:
            fail(f"{path.relative_to(ROOT)}: invalid localization JSON: {exc}")
    if catalogs:
        ref_name, ref = next(iter(catalogs.items()))
        ref_keys = set(ref)
        for name, catalog in catalogs.items():
            if set(catalog) != ref_keys:
                fail(f"localization key parity mismatch: {name} vs {ref_name}")

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
        fail(f"{path.relative_to(ROOT)}: routed page missing @rendermode InteractiveServer")
    if is_error and has_interactive:
        fail(f"{path.relative_to(ROOT)}: Error page should remain static")

# Source package hygiene.
for path in ROOT.rglob("*"):
    if path.is_dir() and path.name in {"bin", "obj", "__pycache__"}:
        fail(f"generated/compiled directory present: {path.relative_to(ROOT)}")
        break
    if path.is_file() and path.suffix in {".pyc", ".pyo"}:
        fail(f"generated Python bytecode present: {path.relative_to(ROOT)}")
        break
for rel in (panel_rel, bootstrap_rel, console_rel, setup_rel):
    if b"\x1b" in (ROOT / rel).read_bytes():
        fail(f"{rel}: raw ESC byte present in source")

if ERRORS:
    print("LocalGPT 4.0.1 source audit FAILED:")
    for error in ERRORS:
        print(f" - {error}")
    sys.exit(1)
print(f"LocalGPT {VERSION} source audit passed: {route_count} routed pages, {interactive_count} InteractiveServer routed boundaries, {len(catalog_paths)} localization catalogs, {len(profiles)} provider profiles.")
