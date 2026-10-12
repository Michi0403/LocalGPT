"""Source-only release-packaging handshake, cache and PDF-size regression checks.

Runs without .NET, NuGet, GitHub or external services. It does not build packages.
"""

from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[1]


def source(relative_path: str) -> str:
    return (ROOT / relative_path).read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f"LocalGPT 5.6.1 release packaging contract failed: {message}")


application = source("src/LocalGPT/LocalGPT.csproj")
version_match = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", application)
require(version_match is not None, "application version is absent")
version = tuple(int(part) for part in version_match.groups())
require(version == (5, 6, 1) and all(part < 10 for part in version[1:]), "application version is not 5.6.1")

release = source("Build-Release.ps1")
ensure = source("build/Ensure-ReleasePackagingPackage.ps1")
publish = source("build/Publish-ReleasePackagingPackage.ps1")
project = source("src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj")
program = source("src/LocalGPT.ReleasePackaging/Program.cs")
docs = source("build/Build-Documentation.ps1")

require("$releasePackagingVersion = '1.0.4'" in release, "release entrypoint still requests a stale helper")
require('<Version>1.0.4</Version>' in project, "helper source project version does not match release")
for name, script in (("ensure", ensure), ("publish", publish)):
    require('[string]$Version = "1.0.4"' in script, f"{name} script default version is stale")
    require("$declaredPackageVersion" in script and "[string]::Equals($Version, $declaredPackageVersion" in script,
            f"{name} script no longer checks the source-project version")

require('case "capabilities":' in program and 'Console.WriteLine("pdf-merge-input-list")' in program,
        "packaging helper does not advertise manifest support")
require('Multi(values, "input-list")' in program and 'File.ReadAllLines(Path.GetFullPath(inputLists[0]), Encoding.UTF8)' in program,
        "advertised manifest support is not actually implemented by the PDF merge")
require('case "pdf-optimize": OptimizePdf(values)' in program,
        "managed PDF compression/optimization command was removed")
require("$capabilities = @(& $command capabilities" in ensure and "$capabilities -cnotcontains 'pdf-merge-input-list'" in ensure,
        "installed NuGet tool is not checked before documentation")
require(ensure.index("$declaredPackageVersion") < ensure.index("if (-not (Test-Path -LiteralPath $package"),
        "stale cached packages can be used before checking requested/project versions")
require("$packagingCapabilities = @(& $PackagingTool capabilities" in docs
        and "$packagingCapabilities -cnotcontains 'pdf-merge-input-list'" in docs,
        "standalone/MSBuild documentation can launch with the wrong helper")
require(docs.index("$packagingCapabilities =") < docs.index('Write-Host "LocalGPT documentation input:'),
        "documentation capability check happens too late")
require("$mergeArguments = @('pdf-merge', '--output', $PdfPath, '--input-list', $mergeInputListPath)" in docs,
        "ordered durable PDF chunks no longer use the compact UTF-8 manifest")
require("[IO.File]::WriteAllLines($mergeInputListPath" in docs,
        "durable chunk paths are not written to the manifest")
require("$pdfCompressionTriggerBytes = 134217728L" in docs and "$maximumSanePdfBytes = 268435456L" in docs,
        "PDF compression trigger or 256 MiB maximum has been weakened")
require("Optimize-LocalGptPdf -Path $pdfPath" in docs and "$pdfFileSize -gt $maximumSanePdfBytes" in docs,
        "post-merge compression or oversize-PDF rejection was removed")
require("$releasePackagingToolOutput = @(& (Join-Path $root 'build/Ensure-ReleasePackagingPackage.ps1')" in release
        and release.index("$releasePackagingToolOutput") < release.index("\nPrepare-LocalGptDocumentation\n"),
        "release helper must be installed/checked before preparing documentation")

for relative_path in (
    "build/audit_ascii_console_memory_5_6_0.py",
    "build/audit_game_studio_5_5_5.py",
    "build/audit_game_revision_chat_5_5_8.py",
    "build/audit_data_management_5_5_1.py",
    "build/audit_data_management_workspace_5_5_3.py",
    "build/audit_learning_checklist_5_4_9.py",
):
    require("5.6.1" in source(relative_path), f"retained source regression audit was not updated: {relative_path}")

require((ROOT / "CHANGELOG-v5.6.1-PDF-RELEASE-HELPER-HANDSHAKE.md").is_file(), "release changelog missing")
require((ROOT / "RELEASE-CHECKLIST-v5.6.1.md").is_file(), "per-file review checklist missing")
require((ROOT / "VALIDATION-v5.6.1-source.md").is_file(), "source verification record missing")
checklist = source("RELEASE-CHECKLIST-v5.6.1.md")
for relative_path in (
    "Build-Release.ps1",
    "build/Ensure-ReleasePackagingPackage.ps1",
    "build/Publish-ReleasePackagingPackage.ps1",
    "build/Build-Documentation.ps1",
    "src/LocalGPT.ReleasePackaging/Program.cs",
    "src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj",
    "src/LocalGPT/LocalGPT.csproj",
    "src/LocalGPT/Components/App.razor",
    "src/LocalGPT/Components/Shared/ChatMicrophone.razor",
    "build/audit_ascii_console_memory_5_6_0.py",
    "build/audit_game_studio_5_5_5.py",
    "build/audit_game_revision_chat_5_5_8.py",
    "build/audit_data_management_5_5_1.py",
    "build/audit_data_management_workspace_5_5_3.py",
    "build/audit_data_management_text_service_5_5_4.py",
    "build/audit_learning_checklist_5_4_9.py",
    "build/audit_release_packaging_5_6_1.py",
    "CHANGELOG-v5.6.1-PDF-RELEASE-HELPER-HANDSHAKE.md",
    "RELEASE-CHECKLIST-v5.6.1.md",
    "VALIDATION-v5.6.1-source.md",
):
    require(f"| `{relative_path}` |" in checklist, f"missing per-file checklist entry for {relative_path}")
print("LocalGPT 5.6.1 packaging/version handshake, cached-helper fail-fast, UTF-8 chunk manifest, PDF compression/size ceiling and prior feature-contract audit passed.")
