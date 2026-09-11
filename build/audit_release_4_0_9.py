from pathlib import Path
import plistlib
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
    version(relative, "4.0.9")

for relative, token in (
    ("docs/docfx.json", '"localgptVersion": "4.0.9"'),
    ("docs/pdf/toc.yml", "LocalGPT-4.0.9.pdf"),
    ("docs/pdf-cover.html", "LocalGPT 4.0.9 Documentation"),
    ("docs/index.md", "**Version 4.0.9**"),
    ("src/LocalGPT/Components/App.razor", "localgpt-chat-ui.js?v=4.0.9"),
    ("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/4.0.9"),
    ("src/LocalGPT/Services/InitialSetupAssistantService.cs", "LocalGPT/4.0.9"),
    ("docs/reference/ai-provider-installation.md", "LocalGPT/4.0.9"),
    ("CHANGELOG-v4.0.9-MACOS-SIGNING-PDF-RENDER-LATENCY-REPAIR.md", "4.0.9"),
    ("VALIDATION-v4.0.9-source.md", "4.0.9"),
    ("RELEASE.md", "LocalGPT 4.0.9"),
):
    require(relative, token)

entitlements_path = root / "build/assets/mac-apphost-entitlements.plist"
if not entitlements_path.is_file():
    failures.append("macOS apphost entitlements asset is missing")
else:
    try:
        with entitlements_path.open("rb") as stream:
            entitlements = plistlib.load(stream)
        if entitlements != {"com.apple.security.cs.allow-jit": True}:
            failures.append(f"unexpected macOS apphost entitlements: {entitlements!r}")
    except Exception as exc:
        failures.append(f"macOS apphost entitlements are not parseable: {exc}")

trust = read("build/Initialize-MacReleaseTrust.ps1")
for token in (
    "'security','codesign','pkgbuild','hdiutil','xcrun','plutil'",
    "assets/mac-apphost-entitlements.plist",
    "& $plutil -lint $entitlementsSourcePath",
    "macOS apphost entitlements preflight passed",
):
    if token not in trust:
        failures.append(f"Initialize-MacReleaseTrust.ps1: missing early entitlement guard {token!r}")

native = read("build/NativeReleasePackaging.ps1")
for token in (
    "$entitlementsSourcePath = Join-Path $PSScriptRoot 'assets/mac-apphost-entitlements.plist'",
    "& $plutil -convert xml1 -o $entitlementsPath $entitlementsSourcePath",
    "& $plutil -lint $entitlementsPath",
    "@('--entitlements',$entitlementsPath)",
):
    if token not in native:
        failures.append(f"NativeReleasePackaging.ps1: missing normalized entitlement signing token {token!r}")
if "$entitlements = @'" in native:
    failures.append("NativeReleasePackaging.ps1: inline PowerShell entitlement XML generation remains")

build_docs = read("build/Build-Documentation.ps1")
for token in (
    "$pdfCompletedBeforeProcessExit = $false",
    "$liveStableLengthChecks -ge 4",
    "Test-LocalGptCompletePdf -Path $PdfPath -MinimumBytes $MinimumBytes",
    "the lingering renderer was terminated after PDF validation",
):
    if token not in build_docs:
        failures.append(f"Build-Documentation.ps1: missing live PDF completion token {token!r}")
if "$process.WaitForExit($browserPdfTimeoutMilliseconds)" in build_docs:
    failures.append("Build-Documentation.ps1: successful browser PDF rendering can still block on the full timeout")
for token in (
    "function Remove-LocalGptStaleGeneratedDocumentationPdfs",
    "Save-LocalGptDocumentationHtmlCache -CacheEntryRoot",
    "LocalGPT Pages source must contain exactly one current versioned PDF",
):
    # Last token is in another file and handled below.
    if token.startswith("LocalGPT Pages"):
        continue
    if token not in build_docs:
        failures.append(f"4.0.8 documentation hygiene regressed: missing {token!r}")

pages = read("build/Update-GitHubPagesSnapshot.ps1")
if "LocalGPT Pages source must contain exactly one current versioned PDF" not in pages:
    failures.append("strict Pages PDF/version guard regressed")

setup_logger = read("src/LocalGPTInstallerConsole/Helper/SetupFileLoggerProvider.cs")
for token in ("using System;", "using System.IO;", "public IDisposable? BeginScope<TState>(TState state) where TState : notnull"):
    if token not in setup_logger:
        failures.append(f"4.0.7 installer compile repair regressed: missing {token!r}")

release_build = read("Build-Release.ps1")
preflight = release_build.find("Preflighting the LocalGPT installer compile before expensive documentation and native packaging...")
documentation = release_build.find("Prepare-LocalGptDocumentation", preflight)
if preflight < 0 or documentation < 0 or preflight > documentation:
    failures.append("installer compile preflight must remain before documentation generation")

if failures:
    print("LocalGPT 4.0.9 release audit FAILED:")
    print("\n".join(f" - {failure}" for failure in failures))
    sys.exit(1)
print("LocalGPT 4.0.9 release audit passed: macOS entitlements are checked-in/plutil-validated before expensive work, live PDF completion avoids the normal 480-second renderer wait, and 4.0.8/4.0.7 protections remain intact.")
