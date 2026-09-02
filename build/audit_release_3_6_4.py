#!/usr/bin/env python3
from pathlib import Path
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


def text(rel):
    return (ROOT / rel).read_text(encoding="utf-8")


def require(rel, needle):
    value = text(rel)
    if needle not in value:
        raise AssertionError(f"{rel}: missing {needle!r}")


for rel in (
    "src/LocalGPT/LocalGPT.csproj",
    "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
    "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
):
    require(rel, "<Version>3.6.4</Version>")
    ET.parse(ROOT / rel)

require("build/NativeReleasePackaging.ps1", '"$BIN" --port 0')
require("build/NativeReleasePackaging.ps1", 'probe="${candidate%/}/health"')
require("build/NativeReleasePackaging.ps1", "terminate_stale_processes")
require("build/NativeReleasePackaging.ps1", "verify_runtime_architecture")
require("build/NativeReleasePackaging.ps1", "Assert-MacBundleArchitecture")
require("build/NativeReleasePackaging.ps1", "Remove-NonTargetMacRuntimeAssets")
if 'FALLBACK_URL="http://127.0.0.1:5000"' in text("build/NativeReleasePackaging.ps1"):
    raise AssertionError("macOS launcher still contains the fixed port-5000 fallback")

for rel, cls in (
    ("src/LocalGPT/Services/Persistence/DatabaseInitializationService.cs", "DatabaseInitializationHostedService"),
    ("src/LocalGPT/Services/Council/RuntimeCapabilityDirectoryService.cs", "RuntimeCapabilityDirectoryHostedService"),
    ("src/LocalGPT/Services/DxAiFunctionCatalogService.cs", "DxAiFunctionCatalogHostedService"),
):
    value = text(rel)
    if not re.search(rf"class\s+{re.escape(cls)}[\s\S]*?\)\s*:\s*BackgroundService", value):
        raise AssertionError(f"{rel}: {cls} is not a BackgroundService")
    block = value[value.index(f"class {cls}"):]
    handoff = "await Task.Delay(1, stoppingToken).ConfigureAwait(false);"
    if handoff not in block:
        raise AssertionError(f"{rel}: {cls} lacks the continuation-policy-compliant startup hand-off")
    if "await Task.Yield();" in block:
        raise AssertionError(f"{rel}: {cls} still contains the 3.6.3 bare Task.Yield regression")

for path in (ROOT / "src/LocalGPT").rglob("*.cs"):
    rel = path.relative_to(ROOT).as_posix()
    value = path.read_text(encoding="utf-8")
    if "IHostedService" in value and rel != "src/LocalGPT/Services/CouncilRuntimeService.RepositoryIntelligence.cs":
        raise AssertionError(f"{rel}: direct IHostedService remains in maintained runtime source")

async_audit = subprocess.run(
    [sys.executable, str(ROOT / "build/audit_async_continuations.py"), "--source-root", str(ROOT / "src/LocalGPT")],
    cwd=ROOT,
    text=True,
    capture_output=True,
)
if async_audit.returncode != 0:
    raise AssertionError(
        "async-continuation audit failed:\n" + async_audit.stdout + async_audit.stderr
    )

json.loads(text("docs/docfx.json"))
require("docs/docfx.json", '"localgptVersion": "3.6.4"')
require("docs/index.md", "**Version 3.6.4**")
require("docs/pdf/toc.yml", "LocalGPT-3.6.4.pdf")
require("CHANGELOG-v3.6.4-ASYNC-CONTINUATION-BUILD-GUARD-REPAIR.md", "Version advanced from 3.6.3 to 3.6.4.")
require("RELEASE.md", "# LocalGPT 3.6.4")
print("LocalGPT 3.6.4 source audit passed.")
