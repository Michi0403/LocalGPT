#!/usr/bin/env python3
"""Static cross-platform boundary audit for LocalGPT.

This audit intentionally does not invoke dotnet. It protects the source architecture from
reintroducing Windows-only APIs or host-filesystem assumptions into common services.
"""
from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src" / "LocalGPT"
failures: list[str] = []
passes: list[str] = []


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def require(condition: bool, message: str) -> None:
    (passes if condition else failures).append(message)


def text(path: Path) -> str:
    return path.read_text(encoding="utf-8")

# Dependencies/APIs that should never be needed by the platform-neutral application backend.
csproj = text(APP / "LocalGPT.csproj")
for package in (
    "System.Drawing.Common",
    "System.Data.OleDb",
    "System.Diagnostics.PerformanceCounter",
    "Microsoft.Windows.AI.MachineLearning",
):
    require(package not in csproj, f"platform-neutral app does not reference {package}")

for path in APP.rglob("*.cs"):
    source = text(path)
    for forbidden in ("using System.Drawing;", "System.Drawing.Bitmap", "System.Drawing.Graphics"):
        if forbidden in source:
            failures.append(f"{rel(path)} contains forbidden GDI/System.Drawing backend usage: {forbidden}")

# Host OS selection belongs at the composition root or inside platform implementations only.
approved_os_branch_files = {
    "src/LocalGPT/Program.ServiceRegistration.cs",
    "src/LocalGPT/Services/PlatformRuntimeServices.cs",
    "src/LocalGPT/Services/HardwarePlatformProbeServices.cs",
}
os_pattern = re.compile(r"\b(?:OperatingSystem\.Is(?:Windows|Linux|MacOS)|RuntimeInformation\.IsOSPlatform|OSPlatform\.(?:Windows|Linux|OSX))")
for path in APP.rglob("*.cs"):
    source = text(path)
    if os_pattern.search(source) and rel(path) not in approved_os_branch_files:
        failures.append(f"{rel(path)} branches on the operating system outside the composition/platform boundary")

# Windows environment details and native executable choices belong behind platform services.
windows_detail_pattern = re.compile(
    r'GetEnvironmentVariable\("(?:LOCALAPPDATA|APPDATA|WINDIR|ProgramFiles|PROGRAMFILES)"\)|'
    r'"(?:cmd\.exe|powershell\.exe|where\.exe|explorer\.exe)"'
)
approved_windows_detail_files = {
    "src/LocalGPT/Services/LocalConsolePlatformServices.cs",
    "src/LocalGPT/Services/PlatformRuntimeServices.cs",
    # Runtime policy entries are data describing allowable external tools, not host invocation logic.
    "src/LocalGPT/Services/Persistence/LocalGptRuntimePolicySeedDataService.cs",
    # SqliteUtilityService classifies configured executable names; it does not select the host executable.
    "src/LocalGPT/Services/SqliteUtilityService.cs",
}
for path in APP.rglob("*.cs"):
    source = text(path)
    if windows_detail_pattern.search(source) and rel(path) not in approved_windows_detail_files:
        failures.append(f"{rel(path)} contains Windows-specific environment/executable logic outside a platform boundary")

# Security-sensitive physical path containment must not be implemented with a hard-coded
# case-insensitive string prefix. Linux is case-sensitive and macOS can be either.
physical_path_forbidden = {
    APP / "Services" / "CouncilRuntimeService.ArtifactWorkspaceRuntime.cs": [
        "path.StartsWith(root, StringComparison.OrdinalIgnoreCase)",
    ],
    APP / "Services" / "CouncilRuntimeService.PromptAndTextRuntime.cs": [
        "normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)",
    ],
    APP / "Services" / "RemoteKnowledgeImportService.ArchiveHandling.cs": [
        "destination.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)",
        "source.StartsWith(cacheFull, StringComparison.OrdinalIgnoreCase)",
        "source.StartsWith(selectedFull, StringComparison.OrdinalIgnoreCase)",
        "destination.StartsWith(selectedFull, StringComparison.OrdinalIgnoreCase)",
    ],
    APP / "Services" / "DocumentationCatalogService.cs": [
        "normalizedCandidate.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)",
    ],
    APP / "Services" / "ProjectMaintenanceService.TextAndValidation.cs": [
        "directory.StartsWith(root, StringComparison.OrdinalIgnoreCase)",
    ],
}
for path, forbidden_fragments in physical_path_forbidden.items():
    source = text(path)
    for fragment in forbidden_fragments:
        if fragment in source:
            failures.append(f"{rel(path)} still contains hard-coded physical-path containment: {fragment}")

# Unix physical path checks must be case-sensitive. macOS volumes can be either case-sensitive or
# case-insensitive, and a host-global probe can disagree with an external/workspace volume. Ordinal
# comparison is conservative and cannot turn a case-distinct sibling into a reviewed descendant.
platform_source = text(APP / "Services" / "PlatformRuntimeServices.cs")
unix_section = platform_source.split("public sealed class UnixPlatformRuntimeService", 1)[1]
require("public StringComparer PathComparer => StringComparer.Ordinal;" in unix_section,
        "Unix path comparer is ordinal/case-sensitive")
require("public StringComparison PathComparison => StringComparison.Ordinal;" in unix_section,
        "Unix path comparison is ordinal/case-sensitive")

# Required platform boundary and DI wiring.
required_snippets = [
    (APP / "Interfaces" / "IPlatformRuntimeService.cs", "interface IPlatformRuntimeService"),
    (APP / "Interfaces" / "ILocalConsolePlatformService.cs", "interface ILocalConsolePlatformService"),
    (APP / "Interfaces" / "IHardwarePlatformProbeService.cs", "interface IHardwarePlatformProbeService"),
    (APP / "Interfaces" / "IRuntimeSecretFileProtectionService.cs", "interface IRuntimeSecretFileProtectionService"),
    (APP / "Services" / "PlatformRuntimeServices.cs", "sealed class UnixPlatformRuntimeService"),
    (APP / "Services" / "PlatformRuntimeServices.cs", "sealed class WindowsPlatformRuntimeService"),
]
for path, snippet in required_snippets:
    require(snippet in text(path), f"{rel(path)} contains {snippet}")

registration = text(APP / "Program.ServiceRegistration.cs")
for snippet in (
    "IPlatformRuntimeService, WindowsPlatformRuntimeService",
    "IPlatformRuntimeService, UnixPlatformRuntimeService",
    "ILocalConsolePlatformService, WindowsLocalConsolePlatformService",
    "ILocalConsolePlatformService, UnixLocalConsolePlatformService",
    "IHardwarePlatformProbeService, WindowsHardwarePlatformProbeService",
    "IHardwarePlatformProbeService, UnixHardwarePlatformProbeService",
    "IRuntimeSecretFileProtectionService, WindowsRuntimeSecretFileProtectionService",
    "IRuntimeSecretFileProtectionService, UnixRuntimeSecretFileProtectionService",
):
    require(snippet in registration, f"DI registration contains {snippet}")

if failures:
    print("LocalGPT cross-platform boundary audit failed:")
    for failure in failures:
        print(f" - {failure}")
    raise SystemExit(1)

print(f"LocalGPT cross-platform boundary audit passed: {len(passes)} checks; no platform leaks detected.")
