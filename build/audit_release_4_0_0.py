#!/usr/bin/env python3
"""Source-only release checks for LocalGPT 4.0.0."""
from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VERSION = "4.0.0"
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
    ("VALIDATION-v4.0.0-source.md", "# LocalGPT 4.0.0 source validation"),
    ("CHANGELOG-v4.0.0-OLLAMA-UPDATE-PROGRESS-CIRCUIT-RESPONSIVENESS.md", "Ollama runtime update is now an explicit provider capability"),
):
    require(rel, needle)

# 3.9.9 provider-link and conservative install-resolution contract must remain intact.
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
):
    if needle not in service:
        fail(f"{service_rel}: missing preserved provider-resolution contract {needle!r}")

# Scoped provider-candidate reuse avoids back-to-back ~10s full provider scans.
for needle in (
    "private readonly object providerCandidateCacheSync = new();",
    "cachedProviderCandidates",
    "RememberProviderCandidates(candidates);",
    "private async Task<IReadOnlyList<MultiModelCouncilModelCandidate>> GetProviderCandidatesAsync",
    "providerCandidates = await GetProviderCandidatesAsync(cancellationToken).ConfigureAwait(false);",
    "candidates = await GetProviderCandidatesAsync(cancellationToken).ConfigureAwait(false);",
):
    if needle not in service:
        fail(f"{service_rel}: missing provider-candidate reuse contract {needle!r}")
# GetSnapshot must still force a fresh scan; helper may also scan on cache miss. No other direct scans should remain.
direct_candidate_calls = service.count("providerModels.GetCandidatesAsync(cancellationToken)")
if direct_candidate_calls != 2:
    fail(f"{service_rel}: expected exactly 2 direct provider candidate scan sites (fresh snapshot + cache miss), found {direct_candidate_calls}")

# Provider update capability: model, interface, service, docs, controller and DXFunction.
model_rel = "src/LocalGPT/BusinessObjects/InitialSetupAssistantModels.cs"
require(model_rel, "public string UpdateCommand { get; set; } = string.Empty;")
interface_rel = "src/LocalGPT/Interfaces/IInitialSetupAssistantService.cs"
require(interface_rel, "Task<LocalConsoleCommandResult> UpdateAsync(string profileKey, bool userConfirmed, CancellationToken cancellationToken = default);")
bootstrap_rel = "src/LocalGPT/Services/AiProviderBootstrapService.cs"
for needle in (
    "public async Task<LocalConsoleCommandResult> UpdateAsync(",
    "string.IsNullOrWhiteSpace(profile.UpdateCommand) ? profile.InstallCommand : profile.UpdateCommand",
    'ExecuteProfileCommandAsync(profile, "Update provider", command, isReadOnly: false, userConfirmed, cancellationToken)',
    "UpdateCommand = profile.UpdateCommand?.Trim() ?? string.Empty",
):
    require(bootstrap_rel, needle)
controller_rel = "src/LocalGPT/Controller/InitialSetupAssistantController.cs"
for needle in (
    '[HttpPost("providers/{profileKey}/update")]',
    '[HumanApprovalRequired("initial-setup.provider.update"',
    "providers.UpdateAsync(profileKey, userConfirmed, cancellationToken)",
):
    require(controller_rel, needle)
dx_rel = "src/LocalGPT/Services/InitialSetupDxAiFunctions.cs"
for needle in (
    "public sealed class UpdateProviderBootstrapFunction",
    '"initial.setup.provider.update"',
    "RequiresHumanConfirmation: true",
    "SupportsAutomaticInvocation: false",
    "providers.UpdateAsync(binding.Value.ProfileKey, true, cancellationToken)",
):
    require(dx_rel, needle)

# Documentation provider profiles must carry updateCommand for all maintained Ollama platforms.
doc_rel = "docs/reference/ai-provider-installation.md"
doc = read(doc_rel)
for needle in (
    "Profiles may define a separate `updateCommand`",
    "When an Ollama model pull explicitly reports that the installed runtime is too old",
    "The shared ASCII command console keeps provider progress useful",
    "numeric percentage snapshots",
):
    if needle not in doc:
        fail(f"{doc_rel}: missing update/progress documentation {needle!r}")
profile_blocks = re.findall(r"```localgpt-provider-profile\s*(\{.*?\})\s*```", doc, flags=re.DOTALL)
profiles: list[dict[str, object]] = []
for index, block in enumerate(profile_blocks):
    try:
        profiles.append(json.loads(block))
    except json.JSONDecodeError as exc:
        fail(f"{doc_rel}: provider profile block {index} is invalid JSON: {exc}")
ollama_profiles = [item for item in profiles if str(item.get("key", "")).startswith("ollama-")]
if {str(item.get("key")) for item in ollama_profiles} != {"ollama-windows", "ollama-linux", "ollama-macos"}:
    fail(f"{doc_rel}: expected maintained Ollama profiles for windows/linux/macos")
for item in ollama_profiles:
    if not str(item.get("updateCommand", "")).strip():
        fail(f"{doc_rel}: {item.get('key')} is missing updateCommand")

# Setup UI: old-runtime recognition, separate updater, progress-render coalescing, and no nested render boundary.
panel_rel = "src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor"
panel = read(panel_rel)
for needle in (
    '@onclick="UpdateOllamaAsync">@T("Update Ollama (confirmed)")',
    "private async Task UpdateOllamaAsync()",
    "Providers.UpdateAsync(selectedProviderKey, true)",
    "wasRunning = before.IsRunning;",
    "OllamaProcesses.StopAsync()",
    "OllamaProcesses.StartAsync()",
    "private async Task<bool> HandleOllamaUpdateRequirementAsync",
    'diagnostic.Contains("requires a newer version of Ollama", StringComparison.OrdinalIgnoreCase)',
    'diagnostic.Contains("Please download the latest version", StringComparison.OrdinalIgnoreCase)',
    "ollamaUpdateRequired = true",
    "if (result.Succeeded)",
    "private int consoleRenderPending;",
    "private int consoleRenderWorkerRunning;",
    "private bool IsOllamaProfileForRender(AiProviderBootstrapProfile profile)",
    "value.ProviderProfiles.FirstOrDefault(IsOllamaProfileForRender)?.Key",
    "Interlocked.Exchange(ref consoleRenderPending, 1)",
    "Interlocked.CompareExchange(ref consoleRenderWorkerRunning, 1, 0)",
    "await Task.Delay(250, consoleRenderCancellation.Token).ConfigureAwait(false);",
    "Volatile.Read(ref consoleRenderPending)",
    "consoleRenderCancellation.Cancel();",
):
    if needle not in panel:
        fail(f"{panel_rel}: missing setup/update/responsiveness contract {needle!r}")
if re.search(r"^\s*@rendermode\b", panel, flags=re.MULTILINE):
    fail(f"{panel_rel}: child setup panel must inherit the routed page circuit, not create a nested render boundary")
# Three model-install paths should all recognize the newer-runtime response.
if panel.count("HandleOllamaUpdateRequirementAsync(result)") != 3:
    fail(f"{panel_rel}: expected all 3 model-install paths to recognize newer Ollama requirement")
if "Providers.IsOllamaProfile" in panel:
    fail(f"{panel_rel}: render/event hot path still calls the decorated provider classifier instead of the local pure classifier")

# Shared console must preserve useful percentage progress without raw terminal-control noise.
console_rel = "src/LocalGPT/Services/ConsoleCommandService.cs"
console = read(console_rel)
for needle in (
    "StandardOutputEncoding = Encoding.UTF8",
    "StandardErrorEncoding = Encoding.UTF8",
    "PumpOutputAsync(process.StandardOutput",
    "PumpOutputAsync(process.StandardError",
    "await Task.WhenAll(stdoutPump, stderrPump).ConfigureAwait(false);",
    "private async Task PumpOutputAsync(StreamReader reader",
    "if (character is '\\r' or '\\n')",
    "PublishProgressSnapshot(",
    "var lastProgressPercentages = new Dictionary<string, int>(StringComparer.Ordinal);",
    "TryGetProgressPercentage(normalized, out var percentage, out var progressKey)",
    "ShouldPublishProgress(progressKey, percentage, lastProgressPercentages)",
    "private bool ShouldPublishProgress(string progressKey, int percentage, Dictionary<string, int> lastProgressPercentages)",
    "value is >= 0 and <= 100",
    "private string NormalizeTerminalDisplayText(string text)",
    "if (final == 'G')",
    "else if (final == 'K'",
    "text[index + 1] == ']'",
):
    if needle not in console:
        fail(f"{console_rel}: missing console progress/normalization contract {needle!r}")
if "BeginOutputReadLine" in console or "BeginErrorReadLine" in console:
    fail(f"{console_rel}: line-only Process output capture remains and can hide carriage-return percentages")
console_css_rel = "src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor.css"
console_css = read(console_css_rel)
for needle in ("white-space: pre;", "overflow: auto;", "font-variant-ligatures: none;"):
    if needle not in console_css:
        fail(f"{console_css_rel}: missing console-layout contract {needle!r}")
if "white-space: pre-wrap" in console_css:
    fail(f"{console_css_rel}: terminal console still uses pre-wrap")

# Temporary frontend interruptions get a longer server-side circuit retention window.
registration_rel = "src/LocalGPT/Program.ServiceRegistration.cs"
registration = read(registration_rel)
for needle in (
    "options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(2)",
    "options.ClientTimeoutInterval = TimeSpan.FromMinutes(2)",
    "options.KeepAliveInterval = TimeSpan.FromSeconds(15)",
):
    if needle not in registration:
        fail(f"{registration_rel}: missing circuit resilience contract {needle!r}")
reconnect_rel = "src/LocalGPT/wwwroot/js/localgpt-reconnect.js"
for needle in ("data-localgpt-reconnect", "Blazor?.reconnect", "reloadAndRejoin"):
    require(reconnect_rel, needle)

# Every literal setup T/TF source must exist in every maintained catalog, with exact key parity.
localization_dir = ROOT / "src/LocalGPT/Localization"
catalog_paths = sorted(localization_dir.glob("*.json"))
if not catalog_paths:
    fail("no LocalGPT localization catalogs found")
else:
    catalogs: dict[str, dict[str, str]] = {}
    for path in catalog_paths:
        try:
            value = json.loads(path.read_text(encoding="utf-8-sig"))
            if not isinstance(value, dict):
                raise ValueError("catalog root is not an object")
            catalogs[path.name] = value
        except (OSError, json.JSONDecodeError, ValueError) as exc:
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
            "Text.Update␠Ollama␠(confirmed)": "Ollama aktualisieren (bestätigt)",
            "Text.A␠model␠requires␠a␠newer␠Ollama␠runtime.␠Update␠Ollama␠with␠explicit␠confirmation,␠then␠retry␠the␠model␠installation.": "Ein Modell benötigt eine neuere Ollama-Laufzeit. Aktualisiere Ollama mit ausdrücklicher Bestätigung und starte danach die Modellinstallation erneut.",
            "Text.This␠model␠requires␠a␠newer␠Ollama␠runtime.␠Use␠Update␠Ollama␠(confirmed),␠then␠retry␠the␠model␠installation;␠LocalGPT␠will␠not␠update␠the␠runtime␠without␠that␠separate␠confirmation.": "Dieses Modell benötigt eine neuere Ollama-Laufzeit. Nutze „Ollama aktualisieren (bestätigt)“ und starte danach die Modellinstallation erneut; LocalGPT aktualisiert die Laufzeit nicht ohne diese separate Bestätigung.",
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

# Source-package hygiene and obvious accidental terminal-control corruption in changed source.
for path in ROOT.rglob("*"):
    if path.is_dir() and path.name in {"bin", "obj", "__pycache__"}:
        fail(f"generated/compiled directory present: {path.relative_to(ROOT)}")
        break
    if path.is_file() and path.suffix in {".pyc", ".pyo"}:
        fail(f"generated Python bytecode present: {path.relative_to(ROOT)}")
        break
for rel in (console_rel, panel_rel, bootstrap_rel, service_rel):
    data = (ROOT / rel).read_bytes()
    if b"\x1b" in data:
        fail(f"{rel}: raw ESC byte present in source")

if ERRORS:
    print("LocalGPT 4.0.0 source audit FAILED:")
    for error in ERRORS:
        print(f" - {error}")
    sys.exit(1)
print(f"LocalGPT {VERSION} source audit passed: {route_count} routed pages, {interactive_count} InteractiveServer routed boundaries, {len(catalog_paths)} localization catalogs, {len(ollama_profiles)} maintained Ollama update profiles.")
