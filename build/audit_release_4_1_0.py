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


def method_section(text, name):
    match = re.search(r"(?m)^\s*(?:private|public|internal|protected)\s+[^\n]+\b" + re.escape(name) + r"\s*\(", text)
    if not match:
        return ""
    next_doc = text.find("\n    /// <summary>", match.start() + 1)
    return text[match.start(): next_doc if next_doc >= 0 else len(text)]

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
    version(relative, "4.1.0")

for relative, token in (
    ("docs/docfx.json", '"localgptVersion": "4.1.0"'),
    ("docs/pdf/toc.yml", "LocalGPT-4.1.0.pdf"),
    ("docs/pdf-cover.html", "LocalGPT 4.1.0 Documentation"),
    ("docs/index.md", "**Version 4.1.0**"),
    ("src/LocalGPT/Components/App.razor", "localgpt-chat-ui.js?v=4.1.0"),
    ("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/4.1.0"),
    ("src/LocalGPT/Services/InitialSetupAssistantService.cs", "LocalGPT/4.1.0"),
    ("docs/reference/ai-provider-installation.md", "LocalGPT/4.1.0"),
    ("CHANGELOG-v4.1.0-MATRIX-OPERATOR-CONTROL-PLANE.md", "4.1.0"),
    ("VALIDATION-v4.1.0-source.md", "4.1.0"),
    ("RELEASE.md", "LocalGPT 4.1.0"),
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


# 4.1.0 Matrix operator control-plane contract.
operator_interface = read("src/LocalGPT/Interfaces/IConsoleOperatorService.cs")
operator_service = read("src/LocalGPT/Services/ConsoleOperatorService.cs")
command_interface = read("src/LocalGPT/Interfaces/IConsoleCommandService.cs")
command_service = read("src/LocalGPT/Services/ConsoleCommandService.cs")
platform_service = read("src/LocalGPT/Services/LocalConsolePlatformServices.cs")
game_console = read("src/LocalGPT/Components/Shared/ChatGameConsole.razor")
registration = read("src/LocalGPT/Program.ServiceRegistration.cs")
setup_operator = read("src/LocalGPTInstallerConsole/Helper/SetupOperatorConsole.cs")
setup_program = read("src/LocalGPTInstallerConsole/Program.cs")

for relative, text, tokens in (
    ("IConsoleOperatorService.cs", operator_interface, ("GetAvailableShells()", "SubmitAsync(LocalConsoleOperatorRequest request")),
    ("ConsoleOperatorService.cs", operator_service, ("case \"shells\":", "case \"jobs\":", "case \"cancel\":", "case \"signal\":", "case \"cd\":", "case \"clear\":")),
    ("IConsoleCommandService.cs", command_interface, ("Guid Start(LocalConsoleCommandRequest request)", "GetActiveOperations()", "bool Cancel(Guid operationId)", "SendSignalAsync(Guid operationId", "PublishOperatorMessage(string text)")),
    ("ConsoleCommandService.cs", command_service, ("ISupervisedTaskRunner", "CreateNoWindow = true", "operation.Cancellation.Cancel()")),
    ("LocalConsolePlatformServices.cs", platform_service, ("LocalConsoleShellKind.Zsh", "LocalConsoleShellKind.Bash", "LocalConsoleShellKind.Sh", "LocalConsoleShellKind.PowerShell", "LocalConsoleShellKind.Cmd", "SendSignalAsync")),
    ("ChatGameConsole.razor", game_console, ("@inject IConsoleOperatorService Operator", "MATRIX OPERATOR", "ToggleOperatorModeAsync", "SubmitOperatorLineAsync", "CanSubmitHumanControl && !operatorMode")),
    ("Program.ServiceRegistration.cs", registration, ("AddSingleton<IConsoleOperatorService, ConsoleOperatorService>()",)),
    ("SetupOperatorConsole.cs", setup_operator, (":operator on|off", ":shells", ":jobs", ":cancel", ":signal", "pid:", "CreateNoWindow = true", "TrackProcess(Process process", "CancellationToken => cancellation.Token")),
    ("LocalGPTInstallerConsole/Program.cs", setup_program, ("SetupOperatorConsole.Start(logger)", "SetupOperatorConsole.ThrowIfCancellationRequested()", "WaitForExitAsync(SetupOperatorConsole.CancellationToken)", "CreateLinkedTokenSource(timeout.Token, SetupOperatorConsole.CancellationToken)")),
):
    for token in tokens:
        if token not in text:
            failures.append(f"4.1.0 operator contract regressed in {relative}: missing {token!r}")

# Cancellation must be a first-class exit path, not swallowed by generic resilience catches.
for method_name in (
    "InstallOllamaAsync", "PullModelsAsync", "InstallLocalGptAsync",
    "ImportGitHubSourceToLearningBaseAsync", "DownloadLatestReleaseAssetAsync",
    "DownloadGitHubSourceZipAsync", "GetGitHubDefaultBranchCommitShaAsync",
    "DownloadFileAsync", "MoveFileWithRetryAsync", "RunProcessAsync",
):
    body = method_section(setup_program, method_name)
    if not body:
        failures.append(f"4.1.0 cancellation guard cannot find {method_name}")
        continue
    if "OperationCanceledException" not in body:
        failures.append(f"4.1.0 cancellation guard: {method_name} does not preserve OperationCanceledException")

if "catch (OperationCanceledException)\n            {\n                throw;\n            }\n            catch (Exception ex)\n            {\n                logger.LogError(ex, $\"Error in Setup:" not in setup_program:
    failures.append("4.1.0 cancellation guard: setup-level generic catch can still swallow operator cancellation")
if "Processes.Values.Where(item => !item.IsOperatorJob)" not in setup_operator:
    failures.append("4.1.0 ownership guard: setup cancellation no longer distinguishes human shell jobs from setup-owned children")
if "command text was omitted" not in operator_service.lower() and "command text" not in operator_service.lower():
    failures.append("4.1.0 privacy guard: operator diagnostics no longer state command-text omission")

if failures:
    print("LocalGPT 4.1.0 release audit FAILED:")
    print("\n".join(f" - {failure}" for failure in failures))
    sys.exit(1)
print("LocalGPT 4.1.0 release audit passed: Matrix operator shell/job/signal control, cancellation propagation and ownership separation are guarded while the 4.0.9 macOS signing/PDF protections remain intact.")
