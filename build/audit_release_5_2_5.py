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
    return path.read_text(encoding="utf-8")


def require(rel: str, token: str, label: str) -> None:
    if token not in read(rel):
        failures.append(f"{label} missing from {rel}: {token}")


def reject(rel: str, token: str, label: str) -> None:
    if token in read(rel):
        failures.append(f"{label} still present in {rel}: {token}")


install_rel = "src/LocalGPT/Components/Pages/Install.razor"
runtime_rel = "src/LocalGPT/Components/Pages/Install.RuntimePlugins.razor.cs"
require(install_rel, 'Markup="@RuntimePluginSourceMarkup"', "one-way runtime-plugin HtmlEditor markup")
require(install_rel, 'MarkupChanged="RuntimePluginSourceMarkupChanged"', "observed runtime-plugin HtmlEditor markup")
require(install_rel, '@key="_runtimePluginEditorRevision"', "runtime-plugin editor generation key")
reject(install_rel, '@bind-Markup="RuntimePluginSourceMarkup"', "server-controlled runtime-plugin HtmlEditor live markup")
require(runtime_rel, "_suppressRuntimePluginEditorRender = true;", "HtmlEditor notification render suppression")
require(runtime_rel, "protected override bool ShouldRender()", "Install render gate")
require(runtime_rel, "if (_suppressRuntimePluginEditorRender)", "Install HtmlEditor render-gate branch")
require(runtime_rel, "_runtimePluginEditorRevision++;", "runtime-plugin editor generation advance")

for rel, token, label in (
    ("build/audit_transient_ui_state_ownership.py", "DxHtmlEditor must not two-way bind live", "generic HtmlEditor ownership guard"),
    ("build/audit_transient_ui_state_ownership.py", "RangeSelectorValueChangeMode.OnHandleRelease", "commit-boundary remediation guidance"),
    ("build/Assert-TransientUiStateOwnership.ps1", "Transient UI-state ownership validation failed", "PowerShell transient-state wrapper"),
    ("Directory.Build.targets", "TransientUiStateOwnershipScript", "build property for transient-state guard"),
    ("Directory.Build.targets", "AssertLocalGptTransientUiStateOwnership", "build target for transient-state guard"),
    ("build/Assert-DevExpressComponentRetention.ps1", "TransientUiStateOwnershipScript", "retention guard wiring check"),
    ("AGENTS.md", "## InteractiveServer transient UI-state ownership", "repository transient-state rule"),
):
    require(rel, token, label)

# The generic audit itself is part of this release contract.
result = subprocess.run(
    [sys.executable, str(ROOT / "build/audit_transient_ui_state_ownership.py"), "--root", str(ROOT), "--product", "localgpt"],
    text=True,
    capture_output=True,
)
if result.returncode != 0:
    failures.append("generic transient UI-state ownership audit failed: " + (result.stderr or result.stdout).strip())

require("CHANGELOG-v5.2.5-TRANSIENT-UI-STATE-OWNERSHIP.md", "# LocalGPT 5.2.5", "5.2.5 changelog")
require("RELEASE.md", "# LocalGPT 5.2.5", "active release notes")
require("VALIDATION-v5.2.5-source.md", "# LocalGPT 5.2.5 source validation", "versioned source validation")
require("VALIDATION.md", "# LocalGPT 5.2.5 source validation", "active source validation")

version = "5.2.5"
version_files = (
    "src/LocalGPT/LocalGPT.csproj",
    "src/LocalGPT/Components/App.razor",
    "src/LocalGPT/Components/Shared/ChatMicrophone.razor",
)
for rel in version_files:
    if version not in read(rel):
        failures.append(f"{version} active identity missing from {rel}")

m = re.fullmatch(r"(\d+)\.(\d+)\.(\d+)", version)
if not m or int(m.group(2)) > 9 or int(m.group(3)) > 9:
    failures.append(f"release version violates single-digit minor/patch policy: {version}")

try:
    root = ET.fromstring((ROOT / "src/LocalGPT/LocalGPT.csproj").read_text(encoding="utf-8"))
    found = root.findtext(".//Version")
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

print(
    "LocalGPT 5.2.5 transient UI-state ownership audit passed: runtime-plugin HtmlEditor live markup no longer two-way rerenders, "
    "editor generations reset intentionally, the generic build-breaking ownership guard is wired/protected, and active release identity is aligned."
)
