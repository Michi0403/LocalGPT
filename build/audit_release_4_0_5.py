#!/usr/bin/env python3
"""Source-only release checks for LocalGPT 4.0.5."""
from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VERSION = "4.0.5"
ERRORS: list[str] = []


def fail(message: str) -> None:
    ERRORS.append(message)


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        fail(f"missing file: {rel}")
        return ""
    return path.read_text(encoding="utf-8-sig", errors="replace")


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
    ("VALIDATION-v4.0.5-source.md", "# LocalGPT 4.0.5 source validation"),
    ("CHANGELOG-v4.0.5-DURABLE-LOGGING-RECOVERY.md", "macOS PKG readability repair"),
):
    require(rel, needle)

# Preserve the 4.0.1 text-service/update/progress repair.
panel_rel = "src/LocalGPT/Components/Shared/InitialSetupAssistantPanel.razor"
panel = read(panel_rel)
for needle in (
    "@inject CouncilTextService CouncilText",
    'CouncilText.StartsWithText(profile.Key, "ollama-", StringComparison.OrdinalIgnoreCase)',
    'CouncilText.ContainsText(diagnostic, "requires a newer version of Ollama", StringComparison.OrdinalIgnoreCase)',
    'CouncilText.ContainsText(diagnostic, "Please download the latest version", StringComparison.OrdinalIgnoreCase)',
    '@onclick="UpdateOllamaAsync">@T("Update Ollama (confirmed)")',
    "private async Task UpdateOllamaAsync()",
    "Providers.UpdateAsync(selectedProviderKey, true)",
    "HandleOllamaUpdateRequirementAsync(result)",
    "private int consoleRenderPending;",
    "private int consoleRenderWorkerRunning;",
    "await Task.Delay(250, consoleRenderCancellation.Token).ConfigureAwait(false);",
):
    if needle not in panel:
        fail(f"{panel_rel}: missing preserved setup contract {needle!r}")
for forbidden in (
    'profile.Key.StartsWith("ollama-", StringComparison.OrdinalIgnoreCase)',
    'diagnostic.Contains("requires a newer version of Ollama", StringComparison.OrdinalIgnoreCase)',
    'diagnostic.Contains("Please download the latest version", StringComparison.OrdinalIgnoreCase)',
    "Providers.IsOllamaProfile",
):
    if forbidden in panel:
        fail(f"{panel_rel}: forbidden hot-path/direct text operation remains: {forbidden!r}")
if re.search(r"^\s*@rendermode\b", panel, flags=re.MULTILINE):
    fail(f"{panel_rel}: child setup panel must inherit the routed /install circuit")
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

console_rel = "src/LocalGPT/Services/ConsoleCommandService.cs"
console = read(console_rel)
for needle in (
    "StandardOutputEncoding = Encoding.UTF8",
    "StandardErrorEncoding = Encoding.UTF8",
    "private async Task PumpOutputAsync(StreamReader reader",
    "PublishProgressSnapshot(",
    "TryGetProgressPercentage(normalized, out var percentage, out var progressKey)",
    "private string NormalizeTerminalDisplayText(string text)",
):
    if needle not in console:
        fail(f"{console_rel}: missing preserved progress contract {needle!r}")
if "BeginOutputReadLine" in console or "BeginErrorReadLine" in console:
    fail(f"{console_rel}: line-only output capture returned")

registration_rel = "src/LocalGPT/Program.ServiceRegistration.cs"
registration = read(registration_rel)
for needle in (
    "options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(2)",
    "options.ClientTimeoutInterval = TimeSpan.FromMinutes(2)",
    "options.KeepAliveInterval = TimeSpan.FromSeconds(15)",
    "public static string ResolveProcessWorkingDirectory()",
    "public static string ResolveSafeCurrentDirectory()",
    "public static bool RepairInvalidCurrentDirectory()",
):
    if needle not in registration:
        fail(f"{registration_rel}: missing runtime durability contract {needle!r}")

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

# Windows Ollama profile retains the direct fast HTTP path while release identity moves to 4.0.5.
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
        fail(f"{doc_rel}: guided Windows install/update should share the reviewed fast path")
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
        f"LocalGPT/{VERSION}",
    ):
        if needle not in update:
            fail(f"{doc_rel}: ollama-windows fast path missing {needle!r}")
for key in ("ollama-linux", "ollama-macos"):
    item = by_key.get(key)
    if not item:
        fail(f"{doc_rel}: {key} profile missing")
        continue
    if "curl -fsSL https://ollama.com/install.sh | sh" not in str(item.get("updateCommand", "")):
        fail(f"{doc_rel}: {key} vendor update path changed unexpectedly")

# 4.0.5 release parser, runtime identity and macOS update lifecycle.
build_release_rel = "Build-Release.ps1"
build_release = read(build_release_rel)
for needle in (
    "Published LocalGPT assembly identity mismatch",
    "RELEASE-VERSION.txt",
    "SOURCE-SHA256.txt",
    "$script:releaseSourceFingerprint",
):
    if needle not in build_release:
        fail(f"{build_release_rel}: missing stale-payload prevention {needle!r}")

# PowerShell interpolation followed by ':' must delimit the variable name explicitly.
if 'Published LocalGPT assembly identity mismatch for $Rid ${mode}: expected' not in build_release:
    fail(f"{build_release_rel}: release identity error text must use ${{mode}}: so pwsh can parse it")
for match in re.finditer(r"\$([A-Za-z_][A-Za-z0-9_]*):", build_release):
    if match.group(1).lower() not in {"script", "env", "global", "local", "private", "using"}:
        fail(f"{build_release_rel}: ambiguous PowerShell variable interpolation remains: {match.group(0)!r}")

native_rel = "build/NativeReleasePackaging.ps1"
native = read(native_rel)
for needle in (
    'EXPECTED_VERSION="__VERSION__"',
    'RUNTIME_VERSION_FILE="$APP/RELEASE-VERSION.txt"',
    'RUNTIME_SOURCE_FILE="$APP/SOURCE-SHA256.txt"',
    "verify_bundle_identity()",
    '[ "${#runtime_source}" -eq 64 ]',
    'endpoint_version=$(sed -nE',
    'endpoint_executable=$(sed -nE',
    'Rejecting stale installed runtime endpoint',
    'Preserving alternate runtime rendezvous endpoint',
    'packaged_owner=0',
    'installed_owner=0',
    "terminate_stale_processes()",
    'APP_LOG_FILE="$USER_DATA_DIR/LocalGPT.log"',
    'cd "$USER_DATA_DIR/runtime"',
    'export PWD="$USER_DATA_DIR/runtime"',
    'export LoggingCore__FileCore__FilePath="$APP_LOG_FILE"',
    "$preinstall = $lifecycleCommon",
    "stop_running_product",
    'owner_command=$(/bin/ps -p "$owner_pid" -o command= 2>/dev/null || true)',
    "remove_runtime_endpoint",
    "--scripts', $pkgScripts",
    "Get-ExternalCommandPath 'productbuild'",
    "$productBuildArguments = @('--package', $componentPackage)",
    "--expand-full $Destination $expandedPackage",
    "-showChoicesXML",
    "Installer could not read the final distribution PKG",
    "/bin/launchctl asuser",
):
    if needle not in native:
        fail(f"{native_rel}: missing macOS lifecycle contract {needle!r}")

middleware_rel = "src/LocalGPT/Program.Middleware.cs"
for needle in (
    "Version = semanticVersion",
    "ExecutablePath = Environment.ProcessPath ?? string.Empty",
):
    require(middleware_rel, needle)
program_rel = "src/LocalGPT/Program.cs"
for needle in (
    "LocalGptApplicationDataPaths.RepairInvalidCurrentDirectory()",
    "LocalGPT runtime identity: assembly={AssemblyVersion}; executable={ExecutablePath}; base={BaseDirectory}; workingDirectory={WorkingDirectory}.",
    "Environment.ProcessPath ?? \"unknown\"",
):
    require(program_rel, needle)

hardware_rel = "src/LocalGPT/Services/HardwareInventoryService.cs"
hardware = read(hardware_rel)
for needle in (
    "nvidiaGpus = await platformProbe.ProbeNvidiaGpusAsync(cancellationToken).ConfigureAwait(false);",
    "Optional NVIDIA discovery was unavailable; the remaining hardware inventory stays usable.",
    "nvidiaGpus = [];",
):
    if needle not in hardware:
        fail(f"{hardware_rel}: missing fail-soft optional NVIDIA contract {needle!r}")

platform_rel = "src/LocalGPT/Services/HardwarePlatformProbeServices.cs"
platform = read(platform_rel)
for needle in (
    "if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))\n                return [];",
    "WorkingDirectory = LocalGptApplicationDataPaths.ResolveProcessWorkingDirectory()",
):
    if needle not in platform:
        fail(f"{platform_rel}: missing Mac-safe hardware process contract {needle!r}")

# Exact localization parity and route render boundary contract.
localization_dir = ROOT / "src/LocalGPT/Localization"
catalog_paths = sorted(localization_dir.glob("*.json"))
if not catalog_paths:
    fail("no LocalGPT localization catalogs found")
else:
    catalogs: dict[str, dict[str, object]] = {}
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
    text = path.read_text(encoding="utf-8-sig", errors="replace")
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

# 4.0.5 durable application/setup diagnostics.
program = read("src/LocalGPT/Program.cs")
for marker in ('TryAppendBootstrapDiagnostic', 'LocalGPT.log', 'Console.Error.WriteLine(exception)'):
    if marker not in program:
        fail(f"LocalGPT Program.cs: durable bootstrap/fatal logging missing {marker!r}")
file_logger = read("src/LocalGPT/Logging/FileLogger.cs")
if 'LocalGptApplicationDataPaths.ResolveUserPath("LocalGPT.log")' not in file_logger:
    fail("LocalGPT FileLogger does not default to the durable per-user LocalGPT.log")
installer = read("src/LocalGPTInstallerConsole/Program.cs")
for marker in ('LocalGPT.Setup.log', 'SetupFileLoggerProvider'):
    if marker not in installer:
        fail(f"LocalGPT installer: durable setup logging missing {marker!r}")
setup_logger = read("src/LocalGPTInstallerConsole/Helper/SetupFileLoggerProvider.cs")
for marker in ('File.AppendAllText', 'if (exception is not null)', 'builder.AppendLine().Append(exception)'):
    if marker not in setup_logger:
        fail(f"LocalGPT SetupFileLoggerProvider.cs: full exception logging missing {marker!r}")

if ERRORS:
    print("LocalGPT 4.0.5 source audit FAILED:")
    for error in ERRORS:
        print(f" - {error}")
    sys.exit(1)
print(
    f"LocalGPT {VERSION} source audit passed: {route_count} routed pages, "
    f"{interactive_count} InteractiveServer routed boundaries, {len(catalog_paths)} localization catalogs, "
    f"{len(profiles)} provider profiles, macOS runtime/update identity and alternate rendezvous ownership guarded."
)
