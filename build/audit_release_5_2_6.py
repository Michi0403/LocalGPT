from __future__ import annotations

from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
failures: list[str] = []


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        failures.append(f"missing {rel}")
        return ""
    return path.read_text(encoding="utf-8-sig")


def require(rel: str, token: str, label: str) -> None:
    if token not in read(rel):
        failures.append(f"{label} missing from {rel}: {token}")


def reject(rel: str, token: str, label: str) -> None:
    if token in read(rel):
        failures.append(f"{label} still present in {rel}: {token}")


require("build/Assert-LocalizationIntegrity.ps1", "Join-Path $root 'src/LocalGPT'", "portable localization source root")
reject("build/Assert-LocalizationIntegrity.ps1", "Join-Path $root 'src\\LocalGPT'", "Windows-only localization source root")
require("build/Assert-PowerShellCompatibility.ps1", "$repositoryBackslashPathLiteralPattern", "indirect repository-path compatibility guard")
require("build/Assert-PowerShellCompatibility.ps1", "repository-relative path literal with Windows-only backslash separators", "portable-path remediation diagnostic")
require("AGENTS.md", "Repository-relative PowerShell path literals use forward slashes", "repository PowerShell path contract")
require("build/Assert-PublishConfiguration.ps1", "function Assert-PathProperty", "publish path separator normalization")
require("build/Assert-StaticWebAssets.ps1", "$normalized = $path.Replace('\\', '/')", "static asset path normalization")
require("build/Assert-OperationalDiagnostics.ps1", "'src/LocalGPT/Components/_Imports.razor'", "operational diagnostics portable path")
require("build/Assert-OneWireArchitecture.ps1", "'src/LocalGPT/Services/OneWire/OneWireExecutionServices.cs'", "1-Wire diagnostics portable path")

portable = subprocess.run(
    [sys.executable, str(ROOT / "build/audit_powershell_portable_paths.py")],
    text=True,
    capture_output=True,
)
if portable.returncode != 0:
    failures.append("PowerShell portable-path audit failed: " + (portable.stderr or portable.stdout).strip())

transient = subprocess.run(
    [sys.executable, str(ROOT / "build/audit_transient_ui_state_ownership.py"), "--root", str(ROOT), "--product", "localgpt"],
    text=True,
    capture_output=True,
)
if transient.returncode != 0:
    failures.append("transient UI-state ownership regression audit failed: " + (transient.stderr or transient.stdout).strip())

require("CHANGELOG-v5.2.6-MACOS-POWERSHELL-PATH-COMPATIBILITY.md", "# LocalGPT 5.2.6", "5.2.6 changelog")
require("RELEASE.md", "# LocalGPT 5.2.6", "active release notes")
require("VALIDATION-v5.2.6-source.md", "# LocalGPT 5.2.6 source validation", "versioned source validation")
require("VALIDATION.md", "# LocalGPT 5.2.6 source validation", "active source validation")

version = "5.2.6"
for rel in (
    "src/LocalGPT/LocalGPT.csproj",
    "src/LocalGPT/Components/App.razor",
    "src/LocalGPT/Components/Shared/ChatMicrophone.razor",
):
    if version not in read(rel):
        failures.append(f"{version} active identity missing from {rel}")

m = re.fullmatch(r"(\d+)\.(\d+)\.(\d+)", version)
if not m or int(m.group(2)) > 9 or int(m.group(3)) > 9:
    failures.append(f"release version violates single-digit minor/patch policy: {version}")

try:
    project = ET.fromstring((ROOT / "src/LocalGPT/LocalGPT.csproj").read_text(encoding="utf-8-sig"))
    found = project.findtext(".//Version")
    if found != version:
        failures.append(f"LocalGPT.csproj Version is {found!r}, expected {version!r}")
except Exception as exc:
    failures.append(f"could not parse LocalGPT.csproj: {exc}")

try:
    ET.parse(ROOT / "Directory.Build.targets")
except Exception as exc:
    failures.append(f"Directory.Build.targets XML is invalid: {exc}")

if failures:
    for failure in failures:
        print(f"FAIL: {failure}", file=sys.stderr)
    raise SystemExit(1)

print("LocalGPT 5.2.6 macOS/Linux PowerShell path compatibility audit passed: repository-relative script paths are portable, the compatibility guard covers indirect helper literals, transient UI-state ownership remains intact, and release identity is aligned.")
