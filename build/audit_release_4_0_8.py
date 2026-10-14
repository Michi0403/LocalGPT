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
    version(relative, "4.0.8")

require("docs/docfx.json", '"localgptVersion": "4.0.8"')
require("docs/pdf/toc.yml", "LocalGPT-4.0.8.pdf")
require("docs/pdf-cover.html", "LocalGPT 4.0.8 Documentation")
require("docs/pdf-cover.html", "Version 4.0.8")
require("docs/index.md", "**Version 4.0.8**")
require("docs/index.md", "LocalGPT-4.0.8.pdf")
require("CHANGELOG-v4.0.8-DOCUMENTATION-PDF-VERSION-HYGIENE.md", "4.0.8")
require("VALIDATION-v4.0.8-source.md", "4.0.8")
require("RELEASE.md", "LocalGPT 4.0.8")
require("src/LocalGPT/Components/App.razor", "localgpt-chat-ui.js?v=4.0.8")
require("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/4.0.8")
require("src/LocalGPT/Services/InitialSetupAssistantService.cs", "LocalGPT/4.0.8")
require("docs/reference/ai-provider-installation.md", "LocalGPT/4.0.8")

setup_logger = read("src/LocalGPTInstallerConsole/Helper/SetupFileLoggerProvider.cs")
for token in (
    "using System;",
    "using System.IO;",
    "public IDisposable? BeginScope<TState>(TState state) where TState : notnull",
    "Exception? exception",
    "Func<TState, Exception?, string> formatter",
):
    if token not in setup_logger:
        failures.append(f"4.0.7 installer compile repair regressed: missing {token!r}")

build_documentation = read("build/Build-Documentation.ps1")
for token in (
    "function Remove-LocalGptStaleGeneratedDocumentationPdfs",
    "Get-ChildItem -LiteralPath $Root -File -Filter 'LocalGPT-*.pdf'",
    "[string]::Equals($pdf.Name, $ExpectedPdfName, [StringComparison]::OrdinalIgnoreCase)",
    "Remove-Item -LiteralPath $pdf.FullName -Force",
    "[void](Remove-LocalGptStaleGeneratedDocumentationPdfs -Root $siteRoot -ExpectedPdfName $pdfName)",
):
    if token not in build_documentation:
        failures.append(f"Build-Documentation.ps1: missing stale generated-PDF hygiene token {token!r}")

cleanup_call = build_documentation.find("[void](Remove-LocalGptStaleGeneratedDocumentationPdfs -Root $siteRoot -ExpectedPdfName $pdfName)")
cache_save = build_documentation.find("Save-LocalGptDocumentationHtmlCache -CacheEntryRoot", cleanup_call)
publish_copy = build_documentation.find('Copy-Item -Path (Join-Path $siteRoot "*") -Destination $publishRoot -Recurse -Force', cleanup_call)
if cleanup_call < 0 or cache_save < 0 or publish_copy < 0 or not (cleanup_call < cache_save < publish_copy):
    failures.append("Build-Documentation.ps1: stale generated PDFs must be removed before durable HTML cache save and publication")

# The cleanup must operate on generated output only. It must not recursively delete or target docs/ source PDFs.
helper_start = build_documentation.find("function Remove-LocalGptStaleGeneratedDocumentationPdfs")
helper_end = build_documentation.find("function Save-LocalGptDocumentationHtmlCache", helper_start)
helper = build_documentation[helper_start:helper_end]
if "-Recurse" in helper:
    failures.append("Build-Documentation.ps1: generated-PDF hygiene unexpectedly became recursive")
if "$docsRoot" in helper or "$sourceWebRoot" in helper:
    failures.append("Build-Documentation.ps1: generated-PDF hygiene must not target source/runtime roots directly")

pages = read("build/Update-GitHubPagesSnapshot.ps1")
for token in (
    "$versionedDocumentationPdfs.Count -eq 0 -and $AllowMissingPdf",
    "$pdfAvailableProperty = $status.PSObject.Properties['pdfAvailable']",
    "if ($null -eq $pdfAvailableProperty -or [bool]$pdfAvailableProperty.Value)",
    "$versionedDocumentationPdfs.Count -ne 1",
    "LocalGPT Pages source must contain exactly one current versioned PDF",
):
    if token not in pages:
        failures.append(f"Update-GitHubPagesSnapshot.ps1: strict PDF/version guard regressed: missing {token!r}")

release_build = read("Build-Release.ps1")
preflight = release_build.find("Preflighting the LocalGPT installer compile before expensive documentation and native packaging...")
documentation = release_build.find("Prepare-LocalGptDocumentation", preflight)
if preflight < 0 or documentation < 0 or preflight > documentation:
    failures.append("Build-Release.ps1: installer compile preflight must remain before documentation generation")

if failures:
    print("LocalGPT 4.0.8 release audit FAILED:")
    print("\n".join(f" - {failure}" for failure in failures))
    sys.exit(1)

print("LocalGPT 4.0.8 release audit passed: stale generated documentation PDFs are removed before cache/publication, strict Pages validation remains intact, and the 4.0.7 installer compile repair is preserved.")
