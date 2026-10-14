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
    require(rel, "<Version>3.5.4</Version>")
major, minor, patch = (3, 5, 4)
if minor > 9 or patch > 9:
    errors.append("release version violates the one-digit minor/patch slot policy")
require("RELEASE.md", "CHANGELOG-v3.5.4-HOST-AWARE-RELEASE-PACKAGING.md")
require("RELEASE.md", "VALIDATION-v3.5.4-source.md")
text("CHANGELOG-v3.5.4-HOST-AWARE-RELEASE-PACKAGING.md")
text("VALIDATION-v3.5.4-source.md")
require("docs/docfx.json", '"localgptVersion": "3.5.4"')
require("docs/pdf/toc.yml", "LocalGPT-3.5.4.pdf")
require("src/LocalGPT/Components/App.razor", "localgpt-chat-ui.js?v=3.5.4")
require("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/3.5.4")

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
require("build/Ensure-ReleasePackagingPackage.ps1", "dotnet tool install LocalGPT.ReleasePackaging")
require("build/Ensure-ReleasePackagingPackage.ps1", "| ForEach-Object { Write-Host $_ }")
require("build/Ensure-ReleasePackagingPackage.ps1", "$packageOutput.Count -ne 1")
require("build/Publish-ReleasePackagingPackage.ps1", "dotnet pack")
require("build/Publish-ReleasePackagingPackage.ps1", "| ForEach-Object { Write-Host $_ }")
build = text("Build-Release.ps1")
for marker in (
    "$releasePackagingToolOutput = @(",
    "$releasePackagingToolOutput.Count -ne 1",
    "Prepared release-packaging tool is missing",
    '$setupProject = Join-Path $solutionRoot "LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj"',
):
    if marker not in build:
        errors.append(f"Build-Release.ps1 missing single-value/installer marker: {marker}")
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
for marker in (".dmg", ".tar.gz", ".AppImage", ".deb", ".rpm", "hdiutil", "appimagetool", "install-dependencies.sh", "New-MacLauncher", "New-Rpm", "New-AppImage"):
    if marker not in native:
        errors.append(f"NativeReleasePackaging.ps1 missing {marker}")


# Windows-hosted native package writers must release their own handles before committing.
packaging_program = "src/LocalGPT.ReleasePackaging/Program.cs"
require(packaging_program, "using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None")
require(packaging_program, "using (var gzip = new GZipStream(file, CompressionLevel.SmallestSize, leaveOpen: false))")
require(packaging_program, "using (var tar = new TarWriter(gzip, TarEntryFormat.Pax, leaveOpen: false))")
require(packaging_program, "CommitTemporaryFile(temp, outputPath);")
require(packaging_program, "using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None")
require(packaging_program, "CommitTemporaryFile(temp, output);")
require(packaging_program, "catch (IOException) when (attempt < maximumAttempts)")
forbid(packaging_program, "using var file = new FileStream(temp, FileMode.CreateNew", "TAR writer still keeps the temporary source open through the final move")
forbid(packaging_program, "using var stream = new FileStream(temp, FileMode.CreateNew", "DEB writer still keeps the temporary source open through the final move")
require("src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj", "<Version>1.0.1</Version>")
require("build/Ensure-ReleasePackagingPackage.ps1", '[string]$Version = "1.0.1"')
require("build/Publish-ReleasePackagingPackage.ps1", '[string]$Version = "1.0.1"')
require("Build-Release.ps1", "$releasePackagingVersion = '1.0.1'")

# Host-aware release orchestration and optional native finishing.
require("Build-Release.ps1", '"all-rids"')
require("Build-Release.ps1", "function Get-ReleaseHostFamily")
require("Build-Release.ps1", "function Get-HostDefaultRuntimes")
require("Build-Release.ps1", "return @('win-x64', 'win-x86', 'win-arm64')")
require("Build-Release.ps1", "return @('linux-x64', 'linux-arm64')")
require("Build-Release.ps1", "return @('osx-x64', 'osx-arm64')")
require("Build-Release.ps1", "$requiresNativePackaging")
require("Build-Release.ps1", "[switch]$UseContainerPackaging")
require("Build-Release.ps1", "without installing its Unix packaging tool because this release contains Windows runtimes only")
require("Build-Release.ps1", "Initialize-BuildConsoleEncoding")
require("Build-Release.cmd", "chcp 65001 >nul")
require("Build-LocalDevelopment.ps1", "Initialize-BuildConsoleEncoding")
require("Build-LocalDevelopment.cmd", "chcp 65001 >nul")

native = text("build/NativeReleasePackaging.ps1")
for marker in (
    "$isLinuxHost",
    "Test-TargetMatchesHostArchitecture",
    "[switch]$UseContainerFallback",
    "Skipping RPM for $Rid",
    "Skipping AppImage for $Rid",
    "Pass -UseContainerFallback",
):
    if marker not in native:
        errors.append(f"NativeReleasePackaging.ps1 missing host-aware marker: {marker}")
for forbidden in (
    "RPM packaging needs rpmbuild, Docker, or Podman.",
    "AppImage needs appimagetool, Docker, or Podman.",
):
    if forbidden in native:
        errors.append(f"NativeReleasePackaging.ps1 still hard-fails on optional native tool absence: {forbidden}")

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
    print("LocalGPT 3.5.4 static release audit FAILED:")
    for error in errors:
        print(" -", error)
    raise SystemExit(1)
print("LocalGPT 3.5.4 static release audit passed.")
