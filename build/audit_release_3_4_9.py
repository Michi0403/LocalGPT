\
#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

def text(rel):
    p = ROOT / rel
    if not p.is_file():
        errors.append(f"missing file: {rel}")
        return ""
    return p.read_text(encoding="utf-8-sig", errors="replace")

def require(rel, needle, label=None):
    body = text(rel)
    if needle not in body:
        errors.append(label or f"{rel} missing required marker: {needle}")

def forbid(rel, needle, label=None):
    body = text(rel)
    if needle in body:
        errors.append(label or f"{rel} still contains forbidden marker: {needle}")

# Version identity / numbering policy.
for rel in ["src/LocalGPT/LocalGPT.csproj", "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj", "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj"]:
    require(rel, "<Version>3.4.9</Version>")
major, minor, patch = (3, 4, 9)
if minor > 9 or patch > 9:
    errors.append("release version violates the one-digit minor/patch slot policy")
require("RELEASE.md", "CHANGELOG-v3.4.9-OPERATOR-POLICY-RELEASE-PACKAGING.md")
require("RELEASE.md", "VALIDATION-v3.4.9-source.md")
text("CHANGELOG-v3.4.9-OPERATOR-POLICY-RELEASE-PACKAGING.md")
text("VALIDATION-v3.4.9-source.md")

# Operator-owned runtime limits.
forbid("src/LocalGPT/Services/OneWire/LocalVisionOcrService.cs", "MaximumImageBytes = 6 * 1024 * 1024", "old 6 MiB OCR ceiling remains")
require("src/LocalGPT/Services/OneWire/LocalVisionOcrService.cs", "LocalGptRuntimeValue.LocalVisionMaximumImageBytes")
require("src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs", 'LocalVisionMaximumImageBytes, nameof(LocalGptRuntimeValue.LocalVisionMaximumImageBytes), "2147483647"')
require("src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs", 'LocalVisionRequestTimeoutSeconds, nameof(LocalGptRuntimeValue.LocalVisionRequestTimeoutSeconds), "0"')
require("src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs", 'LocalVisionMaximumOutputTokens, nameof(LocalGptRuntimeValue.LocalVisionMaximumOutputTokens), "2147483647"')
require("src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs", 'ProviderStreamRepetitionWatchdogEnabled, nameof(LocalGptRuntimeValue.ProviderStreamRepetitionWatchdogEnabled), "0"')
forbid("src/LocalGPT/Services/Formatting/ChatContentRenderer.cs", "AutomaticStructuredTranslationLimit")
require("src/LocalGPT/Services/Formatting/ChatContentRenderer.cs", "LocalGptRuntimeValue.StructuredTextMaximumInputCharacters")
forbid("src/LocalGPT/Services/MultiModelCouncilService.WorkflowPrompting.cs", "const int perMemberLimit = 48000")
forbid("src/LocalGPT/Services/MultiModelCouncilService.WorkflowPrompting.cs", "const int totalLimit = 160000")
require("src/LocalGPT/Services/MultiModelCouncilService.WorkflowPrompting.cs", "CouncilRoleEvidenceMaximumPerMemberCharacters")
require("src/LocalGPT/Services/MultiModelCouncilService.WorkflowPrompting.cs", "CouncilRoleEvidenceMaximumTotalCharacters")
forbid("src/LocalGPT/BusinessObjects/RemoteControlIntegrationModels.cs", "class RemoteControlLimits")
require("src/LocalGPT/Services/RemoteControlConnectorService.cs", "RemoteControlMinimumPollIntervalSeconds")

# Release packaging.
build = text("Build-Release.ps1")
for rid in ["win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64"]:
    if rid not in build: errors.append(f"Build-Release.ps1 missing RID {rid}")
require("Build-Release.ps1", "Publish-UnixRuntime")
require("Build-Release.ps1", "application payload (no setup console)")
require("Build-Release.ps1", "SHA256SUMS.txt")
require("Build-Release.ps1", "Ensure-ReleasePackagingPackage.ps1")
text("src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj")
require("src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj", "<PackageId>LocalGPT.ReleasePackaging</PackageId>")
require("src/LocalGPT.ReleasePackaging/Program.cs", '"control.tar.gz"')
require("src/LocalGPT.ReleasePackaging/Program.cs", "SHA256.HashData")
native = text("build/NativeReleasePackaging.ps1")
for marker in [".dmg", ".tar.gz", ".AppImage", ".deb", ".rpm", "hdiutil", "appimagetool", "rpmbuild"]:
    if marker not in native: errors.append(f"NativeReleasePackaging.ps1 missing {marker}")
if "dpkg-deb" in build or "dpkg-deb" in native:
    errors.append("dpkg-deb remains in active release packaging")

# Reviewed InteractiveServer boundaries from supplied baseline.
for rel in [
    "Components/Layout/NavMenu.razor", "Components/Pages/Database.razor", "Components/Pages/RemoteControl.razor",
    "Components/Pages/Chat.razor", "Components/Pages/OneWireSecurity.razor", "Components/Pages/ModelCouncil.razor",
    "Components/Pages/CouncilTeams.razor", "Components/Pages/Install.razor", "Components/Pages/Help.razor",
    "Components/Pages/DxFunctionCatalog.razor", "Components/Pages/ProjectMaintenance.razor", "Components/Pages/Projects.razor",
    "Components/Pages/Index.razor", "Components/Pages/MinecraftModBuilder.razor", "Components/Pages/TestLab.razor"]:
    require("src/LocalGPT/" + rel, "@rendermode InteractiveServer", f"InteractiveServer boundary missing from {rel}")

if errors:
    print("LocalGPT 3.4.9 static release audit FAILED:")
    for e in errors: print(" -", e)
    sys.exit(1)
print("LocalGPT 3.4.9 static release audit passed.")
