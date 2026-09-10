from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
failures = []


def read(relative):
    path = root / relative
    if not path.is_file():
        failures.append(f"missing file: {relative}")
        return ""
    return path.read_text(encoding="utf-8-sig")


def require(relative, token, label=None):
    text = read(relative)
    if token not in text:
        failures.append(label or f"{relative}: missing {token!r}")
    return text


def version(relative, expected):
    text = require(relative, f"<Version>{expected}</Version>")
    match = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", text)
    if not match:
        failures.append(f"{relative}: semantic version not found")
        return
    major, minor, patch = map(int, match.groups())
    if minor > 9 or patch > 9:
        failures.append(f"{relative}: version {major}.{minor}.{patch} violates the one-digit minor/patch release rule")


for relative in (
    "src/LocalGPT/LocalGPT.csproj",
    "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
    "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
):
    version(relative, "4.0.7")
require("docs/docfx.json", '"localgptVersion": "4.0.7"')
require("CHANGELOG-v4.0.7-INSTALLER-COMPILE-SURFACE-REPAIR.md", "4.0.7")
require("VALIDATION-v4.0.7-source.md", "4.0.7")
require("RELEASE.md", "LocalGPT 4.0.7")
require("src/LocalGPT/Components/App.razor", "localgpt-chat-ui.js?v=4.0.7")
require("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/4.0.7")
require("src/LocalGPT/Services/InitialSetupAssistantService.cs", "LocalGPT/4.0.7")

setup_project = read("src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj")
if "<ImplicitUsings>enable</ImplicitUsings>" in setup_project:
    failures.append("InstallerConsole unexpectedly enables implicit usings; the explicit compile-surface check needs review")
setup_logger = read("src/LocalGPTInstallerConsole/Helper/SetupFileLoggerProvider.cs")
for token in (
    "using System;",
    "using System.IO;",
    "using System.Text;",
    "public IDisposable? BeginScope<TState>(TState state) where TState : notnull",
    "Exception? exception",
    "Func<TState, Exception?, string> formatter",
    "Path.GetFullPath(logPath)",
    "Directory.CreateDirectory(directory)",
    "File.AppendAllText(",
):
    if token not in setup_logger:
        failures.append(f"SetupFileLoggerProvider.cs: missing compile-surface token {token!r}")

release_build = read("Build-Release.ps1")
preflight = release_build.find("Preflighting the LocalGPT installer compile before expensive documentation and native packaging...")
documentation = release_build.find("Prepare-LocalGptDocumentation", preflight)
if preflight < 0 or documentation < 0 or preflight > documentation:
    failures.append("Build-Release.ps1: installer compile preflight must remain before documentation generation")

if failures:
    print("LocalGPT 4.0.7 release audit FAILED:")
    print("\n".join(f" - {failure}" for failure in failures))
    sys.exit(1)

print("LocalGPT 4.0.7 release audit passed: installer compile surface, one-digit version rule, runtime identity/cache-buster updates, and early installer compile preflight are intact.")
