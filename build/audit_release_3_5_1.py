#!/usr/bin/env python3
from pathlib import Path
import re

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


for rel in (
    "src/LocalGPT/LocalGPT.csproj",
    "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
):
    require(rel, "<Version>3.5.1</Version>")
major, minor, patch = (3, 5, 1)
if minor > 9 or patch > 9:
    errors.append("release version violates the one-digit minor/patch slot policy")
require("RELEASE.md", "CHANGELOG-v3.5.1-COMPILER-SHARED-PACKAGING-FOLLOWUP.md")
require("RELEASE.md", "VALIDATION-v3.5.1-source.md")
text("CHANGELOG-v3.5.1-COMPILER-SHARED-PACKAGING-FOLLOWUP.md")
text("VALIDATION-v3.5.1-source.md")
require("docs/docfx.json", '"localgptVersion": "3.5.1"')
require("docs/pdf/toc.yml", "LocalGPT-3.5.1.pdf")
require("src/LocalGPT/Components/App.razor", "localgpt-chat-ui.js?v=3.5.1")
require("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/3.5.1")

# User-reported compile regressions.
localization = "src/LocalGPT/Services/Localization/LocalGptLocalizationService.cs"
require(localization, "using LocalGPT.Interfaces;")
require(localization, "ILocalGptRuntimePolicyDataService runtimePolicy")
chat_renderer = "src/LocalGPT/Services/Formatting/ChatContentRenderer.cs"
require(chat_renderer, "using LocalGPT.BusinessObjects;")
require(chat_renderer, "LocalGptRuntimeValue.StructuredTextMaximumInputCharacters")
theme_service = "src/LocalGPT/Services/ThemeService.cs"
require(theme_service, "public int MaxFusionRouteSteps")
dispatcher = "src/LocalGPT/Components/Layout/ThemeJsChangeDispatcher.cs"
require(dispatcher, "Themes.MaxFusionRouteSteps")
forbid(dispatcher, "ThemeService.MaxFusionRouteSteps", "Theme dispatcher still accesses instance policy as a static member")
human = "src/LocalGPT/Services/HumanCollaborationService.cs"
require(human, "private int MaxTextLength =>")
require(human, "LocalGptRuntimeValue.HumanCollaborationMaximumTextLength")
require("src/LocalGPT/Services/HumanCollaborationService.ContributionQueue.cs", "NormalizeMultiline(evaluation, MaxTextLength)")
require("src/LocalGPT/Services/HumanCollaborationService.Contributions.cs", "NormalizeMultiline(content, MaxTextLength)")

# Preserve earlier runtime-policy maintenance.
telemetry = "src/LocalGPT/Services/EmbeddedTelemetryIngressService.cs"
forbid(telemetry, "private const int Math.Max", "malformed telemetry const declaration remains")
require(telemetry, "LocalGptRuntimeValue.EmbeddedTelemetryMaximumSnapshots")
require("src/LocalGPT/Services/OneWire/LocalVisionOcrService.cs", "LocalGptRuntimeValue.LocalVisionMaximumImageBytes")
forbid("src/LocalGPT/Services/OneWire/LocalVisionOcrService.cs", "MaximumImageBytes = 6 * 1024 * 1024")
require("src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs", 'ProviderStreamRepetitionWatchdogEnabled, nameof(LocalGptRuntimeValue.ProviderStreamRepetitionWatchdogEnabled), "0"')

# LocalGPT owns and publishes the shared release-packaging package.
require("src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj", "<PackageId>LocalGPT.ReleasePackaging</PackageId>")
require("build/Publish-ReleasePackagingPackage.ps1", "dotnet pack")
ensure = text("build/Ensure-ReleasePackagingPackage.ps1")
if "--add-source" in ensure and "avoids inheriting" not in ensure:
    errors.append("LocalGPT release-packaging tool install still actively uses --add-source")
require("build/Ensure-ReleasePackagingPackage.ps1", "--configfile $nugetConfig")
build = text("Build-Release.ps1")
for marker in (
    "$releasePackagingPackageName",
    "ReleasePackagingPackagePath",
    "Copy-Item $releasePackagingPackage (Join-Path $sharedWirePackageDirectory $releasePackagingPackageName)",
    "SHA256SUMS.txt",
    "Publish-UnixRuntime",
):
    if marker not in build:
        errors.append(f"Build-Release.ps1 missing release-packaging marker: {marker}")
for rid in ("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64"):
    if rid not in build:
        errors.append(f"Build-Release.ps1 missing RID {rid}")
native = text("build/NativeReleasePackaging.ps1")
for marker in (".dmg", ".tar.gz", ".AppImage", ".deb", ".rpm", "hdiutil", "appimagetool"):
    if marker not in native:
        errors.append(f"NativeReleasePackaging.ps1 missing {marker}")

# Keep reviewed InteractiveServer boundaries explicit.
for rel in (
    "Components/Layout/NavMenu.razor", "Components/Pages/Database.razor", "Components/Pages/RemoteControl.razor",
    "Components/Pages/Chat.razor", "Components/Pages/OneWireSecurity.razor", "Components/Pages/ModelCouncil.razor",
    "Components/Pages/CouncilTeams.razor", "Components/Pages/Install.razor", "Components/Pages/Help.razor",
    "Components/Pages/DxFunctionCatalog.razor", "Components/Pages/ProjectMaintenance.razor", "Components/Pages/Projects.razor",
    "Components/Pages/Index.razor", "Components/Pages/MinecraftModBuilder.razor", "Components/Pages/TestLab.razor",
):
    require("src/LocalGPT/" + rel, "@rendermode InteractiveServer", f"InteractiveServer boundary missing from {rel}")

# Catch the source-corruption class that previously escaped static checks.
for path in (ROOT / "src").rglob("*.cs"):
    source = path.read_text(encoding="utf-8-sig", errors="replace")
    if re.search(r"\bconst\s+(?:int|long|double|float|decimal|bool|string)\s+Math\s*\.", source):
        errors.append(f"malformed const/member expression found: {path.relative_to(ROOT).as_posix()}")

if errors:
    print("LocalGPT 3.5.1 static release audit FAILED:")
    for error in errors:
        print(" -", error)
    raise SystemExit(1)
print("LocalGPT 3.5.1 static release audit passed.")
