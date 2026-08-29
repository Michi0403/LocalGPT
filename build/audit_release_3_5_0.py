#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors: list[str] = []


def text(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        errors.append(f"missing file: {rel}")
        return ""
    return path.read_text(encoding="utf-8-sig", errors="replace")


def require(rel: str, needle: str, label: str | None = None) -> None:
    if needle not in text(rel):
        errors.append(label or f"{rel} missing required marker: {needle}")


def forbid(rel: str, needle: str, label: str | None = None) -> None:
    if needle in text(rel):
        errors.append(label or f"{rel} still contains forbidden marker: {needle}")


# Version identity and one-digit minor/patch policy.
for rel in (
    "src/LocalGPT/LocalGPT.csproj",
    "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
):
    require(rel, "<Version>3.5.0</Version>")
major, minor, patch = (3, 5, 0)
if minor > 9 or patch > 9:
    errors.append("release version violates the one-digit minor/patch slot policy")
require("RELEASE.md", "CHANGELOG-v3.5.0-BUILD-MAINTENANCE-CROSS-PLATFORM-REVIEW.md")
require("RELEASE.md", "VALIDATION-v3.5.0-source.md")
text("CHANGELOG-v3.5.0-BUILD-MAINTENANCE-CROSS-PLATFORM-REVIEW.md")
text("VALIDATION-v3.5.0-source.md")
require("docs/docfx.json", '"localgptVersion": "3.5.0"')
require("docs/pdf/toc.yml", "LocalGPT-3.5.0.pdf")
require("src/LocalGPT/Components/App.razor", "localgpt-chat-ui.js?v=3.5.0")

# Reported compiler failure and operator-owned telemetry capacity.
telemetry_rel = "src/LocalGPT/Services/EmbeddedTelemetryIngressService.cs"
forbid(telemetry_rel, "private const int Math.Max", "malformed const declaration remains in EmbeddedTelemetryIngressService")
forbid(telemetry_rel, "Math.Clamp(maximum, 1, 500)", "hidden 500-snapshot GetRecent ceiling remains")
require(telemetry_rel, "ILocalGptRuntimePolicyDataService runtimePolicy")
require(telemetry_rel, "LocalGptRuntimeValue.EmbeddedTelemetryMaximumSnapshots")
require("src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs", 'EmbeddedTelemetryMaximumSnapshots, nameof(LocalGptRuntimeValue.EmbeddedTelemetryMaximumSnapshots), "2147483647"')

# Preserve 3.4.9 operator-policy fixes.
forbid("src/LocalGPT/Services/OneWire/LocalVisionOcrService.cs", "MaximumImageBytes = 6 * 1024 * 1024", "old 6 MiB OCR ceiling remains")
require("src/LocalGPT/Services/OneWire/LocalVisionOcrService.cs", "LocalGptRuntimeValue.LocalVisionMaximumImageBytes")
require("src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs", 'LocalVisionMaximumImageBytes, nameof(LocalGptRuntimeValue.LocalVisionMaximumImageBytes), "2147483647"')
require("src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs", 'ProviderStreamRepetitionWatchdogEnabled, nameof(LocalGptRuntimeValue.ProviderStreamRepetitionWatchdogEnabled), "0"')
forbid("src/LocalGPT/Services/Formatting/ChatContentRenderer.cs", "AutomaticStructuredTranslationLimit")
forbid("src/LocalGPT/BusinessObjects/RemoteControlIntegrationModels.cs", "class RemoteControlLimits")

# Debug documentation must not demand a Release PDF or crash on an empty PDF collection.
require("Directory.Build.targets", "<LocalGptPagesPdfArgument Condition=\"'$(RequireLocalGptDocumentationPdf)' != 'true'\">-AllowMissingPdf</LocalGptPagesPdfArgument>")
require("build/Update-GitHubPagesSnapshot.ps1", "[switch]$AllowMissingPdf")
require("build/Update-GitHubPagesSnapshot.ps1", "$foundPdfNames = @($versionedDocumentationPdfs | ForEach-Object { $_.Name })")
require("build/Update-GitHubPagesSnapshot.ps1", "--html-only")
require("build/Update-GitHubPagesSnapshot.ps1", "pdfAvailable=false")
require("Build-Release.ps1", "$versionedPdfDisplay = if ($versionedPdfNames.Count -eq 0) { '<none>' }")

# Cross-platform release matrix and Unix packaging contract.
build = text("Build-Release.ps1")
for rid in ("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64"):
    if rid not in build:
        errors.append(f"Build-Release.ps1 missing RID {rid}")
for marker in ("Publish-UnixRuntime", "application payload (no setup console)", "SHA256SUMS.txt", "Ensure-ReleasePackagingPackage.ps1"):
    if marker not in build:
        errors.append(f"Build-Release.ps1 missing release marker: {marker}")
require("src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj", "<PackageId>LocalGPT.ReleasePackaging</PackageId>")
require("src/LocalGPT.ReleasePackaging/Program.cs", 'case "tar"')
require("src/LocalGPT.ReleasePackaging/Program.cs", 'case "deb"')
require("src/LocalGPT.ReleasePackaging/Program.cs", 'case "sha256"')
native = text("build/NativeReleasePackaging.ps1")
for marker in (".dmg", ".tar.gz", ".AppImage", ".deb", ".rpm", "hdiutil", "appimagetool"):
    if marker not in native:
        errors.append(f"NativeReleasePackaging.ps1 missing {marker}")
if "dpkg-deb" in build or "dpkg-deb" in native:
    errors.append("dpkg-deb remains in active release packaging")

# Baseline InteractiveServer boundaries supplied by the user must remain explicit.
for rel in (
    "Components/Layout/NavMenu.razor", "Components/Pages/Database.razor", "Components/Pages/RemoteControl.razor",
    "Components/Pages/Chat.razor", "Components/Pages/OneWireSecurity.razor", "Components/Pages/ModelCouncil.razor",
    "Components/Pages/CouncilTeams.razor", "Components/Pages/Install.razor", "Components/Pages/Help.razor",
    "Components/Pages/DxFunctionCatalog.razor", "Components/Pages/ProjectMaintenance.razor", "Components/Pages/Projects.razor",
    "Components/Pages/Index.razor", "Components/Pages/MinecraftModBuilder.razor", "Components/Pages/TestLab.razor",
):
    require("src/LocalGPT/" + rel, "@rendermode InteractiveServer", f"InteractiveServer boundary missing from {rel}")

# Catch the specific class of accidental source corruption that caused the 3.4.9 build stop.
for path in (ROOT / "src").rglob("*.cs"):
    source = path.read_text(encoding="utf-8-sig", errors="replace")
    if re.search(r"\bconst\s+(?:int|long|double|float|decimal|bool|string)\s+Math\s*\.", source):
        errors.append(f"malformed const/member expression found: {path.relative_to(ROOT).as_posix()}")

if errors:
    print("LocalGPT 3.5.0 static release audit FAILED:")
    for error in errors:
        print(" -", error)
    raise SystemExit(1)
print("LocalGPT 3.5.0 static release audit passed.")
