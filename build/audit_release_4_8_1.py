#!/usr/bin/env python3
"""Source-only release audit for LocalGPT 4.8.1. Never invokes dotnet, GitHub, installers, or project code."""
from __future__ import annotations

import ast
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VERSION = "4.8.1"


def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8")


def require(rel: str, *needles: str) -> None:
    text = read(rel)
    for needle in needles:
        if needle not in text:
            raise AssertionError(f"{rel}: missing {needle!r}")


def forbid(rel: str, *needles: str) -> None:
    text = read(rel)
    for needle in needles:
        if needle in text:
            raise AssertionError(f"{rel}: forbidden {needle!r}")


def main() -> int:
    # Release identity and single-digit slot policy.
    for rel in (
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    ):
        root = ET.parse(ROOT / rel).getroot()
        versions = [node.text for node in root.iter() if node.tag.endswith("Version") and node.text]
        if VERSION not in versions:
            raise AssertionError(f"{rel}: expected Version {VERSION}, got {versions}")
    if any(int(part) >= 10 for part in VERSION.split(".")):
        raise AssertionError("release version violates single-digit slot policy")

    require("src/LocalGPT/Components/App.razor", "localgpt-game-console.js?v=4.8.1", "localgpt-chat-ui.js?v=4.8.1")
    require("docs/index.md", "**Version 4.8.1**", "LocalGPT-4.8.1.pdf")
    require("docs/docfx.json", '"localgptVersion": "4.8.1"')
    require("docs/pdf/toc.yml", "LocalGPT-4.8.1.pdf")
    require("docs/pdf-cover.html", "LocalGPT 4.8.1 Documentation", "Version 4.8.1")
    require("CHANGELOG-v4.8.1-MAINTENANCE-POLICY-COMPILE-REPAIR.md", "# LocalGPT 4.8.1")
    require("VALIDATION-v4.8.1-source.md", "# LocalGPT 4.8.1 source validation")

    # Maintained JSON and Python syntax only; no imports/execution of repository code.
    for rel in ("src/LocalGPT/appsettings.json", "src/LocalGPT/appsettings.Development.json", "docs/docfx.json"):
        json.loads(read(rel))
    ast.parse(read("src/LocalGPT/Runtime/Python/localgpt_runtime_bridge.py"), filename="localgpt_runtime_bridge.py")

    # Requested ASCII console regression fix.
    require("src/LocalGPT/Components/Shared/OperationalActivityFeed.razor", '<pre class="localgpt-operational-feed @CssClass"')
    forbid("src/LocalGPT/Components/Shared/OperationalActivityFeed.razor", "<DxMemo")

    # Direct upstream/GitHub-first AI path and optional model-hub boundary.
    require(
        "src/LocalGPT/Services/LocalAiAcquisitionService.cs",
        'Key = "openai-whisper"',
        'https://github.com/openai/whisper/archive/refs/heads/main.zip',
        'Adapter = "openai-whisper"',
    )
    require(
        "src/LocalGPT/Runtime/Python/localgpt_runtime_bridge.py",
        '"openai_whisper_download": _openai_whisper_download',
        'if adapter == "openai-whisper"',
    )
    core = json.loads(read("src/LocalGPT/appsettings.json"))["PythonCore"]["PackageProfiles"]
    profiles = {row["Key"]: row for row in core}
    if "hub-huggingface" not in profiles or "whisper-runtime" not in profiles:
        raise AssertionError("expected optional Hugging Face and Whisper profiles")
    if any("huggingface_hub" in package for package in profiles["core"].get("Packages", [])):
        raise AssertionError("Hugging Face client leaked back into primary Python core profile")

    # User/AI-team runtime configuration surfaces.
    require(
        "src/LocalGPT/Services/LocalAiRuntimeDxAiFunctions.cs",
        '"localai.runtime.configuration"',
        '"localai.runtime.configuration.update"',
        'SupportsDeferredApprovalRequest: true',
        '"task":{"type":"string","enum":["transcribe","translate"]}',
    )
    require("src/LocalGPT/Controller/LocalAiRuntimeController.cs", '[Route("api/local-ai/runtime")]', '[HumanApprovalRequired("localai.runtime.configuration.update"')
    require("src/LocalGPT/Components/Pages/Install.razor", "Python / Whisper execution policy", "Save Python / Whisper policy")

    # Toolchain/environment architecture surfaces.
    require(
        "src/LocalGPT/Services/ToolchainRuntimeDxAiFunctions.cs",
        '"toolchain.environment.list"',
        '"toolchain.environment.change"',
        '"toolchain.acquisition.catalog"',
        '"toolchain.acquisition.download"',
    )
    require("src/LocalGPT/Controller/ToolchainRuntimeController.cs", '[Route("api/toolchains")]')
    require("src/LocalGPT/Components/Pages/Install.razor", "Python for embedded AI", "Environment variables", "Reviewed online acquisition")
    require("src/LocalGPT/Components/Pages/Install.razor", "Reusable process profiles", "ToolchainExecutionProfiles")
    require("src/LocalGPT/Services/ToolchainExecutionProfileService.cs", "ProcessStartInfo", "ArgumentList.Add", 'DataType = "toolchain.profile"')
    require("src/LocalGPT/BusinessObjects/ToolchainRuntimeManagementModels.cs", "class ToolchainExecutionProfile", "class ToolchainEnvironmentOverrideRecord")
    require("src/LocalGPT/Services/ToolchainRuntimeManagementService.cs", 'DataType = "toolchain.environment"', "ToolchainInstallationId")

    # Promotion uses generic trusted approval instead of model-supplied userConfirmed.
    promo = read("src/LocalGPT/Services/ProjectAutomationDxAiFunctions.cs")
    promo_block = promo[promo.index("public sealed class PromoteProjectIngestionFunction"):promo.index("public sealed class StartProjectBlobFunction")]
    for needle in ("SupportsDeferredApprovalRequest: true", "ApprovalRequiredBeforeCompletion: true", "json.Bind<WorkspaceNameParameters>", "request.UserConfirmed"):
        if needle not in promo_block:
            raise AssertionError(f"promotion block missing {needle}")
    if '"userConfirmed"' in promo_block:
        raise AssertionError("promotion schema still lets callers self-assert userConfirmed")
    require("src/LocalGPT/Services/ArtifactWorkspaceDxAiFunctions.cs", '"council.artifact_workspace_zip"', "SupportsDeferredApprovalRequest: true", "ApprovalRequiredBeforeCompletion: true")

    # User-owned permission policy reaches visible Human Collaboration approval instead of preemptive denial.
    require("src/LocalGPT/Services/DxAiFunctionRegistry.cs", "IDxAiFunctionCatalogService", "deferredInvocations.QueueAsync", "RequiresFrontendConfirmation")
    forbid("src/LocalGPT/Services/DxAiFunctionRegistry.cs", '"AutomaticInvocationDenied"')
    require("src/LocalGPT/Services/OllamaThinkingChatClient.Tools.cs", "SupportsDirectInvocation")
    require("src/LocalGPT/Services/ProviderModelBenchmarkService.ProfileExecution.cs", "forceEnabled: true")

    # Rejoin continuity, bounded broader history, and popup grid sizing.
    require("src/LocalGPT/Components/Pages/Chat.LiveCouncil.razor.cs", "canonicalConversationMessages.AddRange(councilSession.Messages)")
    require("src/LocalGPT/Components/Pages/Chat.PersistenceAndMemory.razor.cs", "liveCouncilAttached")
    require("src/LocalGPT/Services/CouncilRuntimeService.PromptAndTextRuntime.cs", "maximumAssistantTurns = 4", "maximumUserTurns = 24")
    require("src/LocalGPT/Services/MultiModelCouncilService.RecoveryAndPersistence.cs", ".TakeLast(24))")
    require("src/LocalGPT/Components/Pages/Chat.razor.css", "height: auto;", "align-self: stretch;")
    require("src/LocalGPT/Components/Layout/HumanCollaborationInbox.razor", "CancellationToken lifetimeToken", "catch (ObjectDisposedException) when (isDisposed)")

    # Interactive render-mode ownership must match the supplied 4.7.9 source shape.
    razor_files = list((ROOT / "src/LocalGPT/Components").rglob("*.razor"))
    interactive = [p for p in razor_files if "@rendermode InteractiveServer" in p.read_text(encoding="utf-8")]
    if len(interactive) != 15:
        raise AssertionError(f"expected 15 InteractiveServer Razor files, found {len(interactive)}")
    missing_pages = [p.relative_to(ROOT).as_posix() for p in razor_files if "@page" in p.read_text(encoding="utf-8") and "@rendermode InteractiveServer" not in p.read_text(encoding="utf-8")]
    if missing_pages != ["src/LocalGPT/Components/Pages/Error.razor"]:
        raise AssertionError(f"unexpected page render-mode exceptions: {missing_pages}")

    # 4.8.1 maintenance-policy repair: fix code, do not weaken the guards.
    require("src/LocalGPT/Services/ToolchainExecutionProfileService.cs", "private readonly JsonSerializerOptions JsonOptions", "catch (Exception exception)")
    forbid("src/LocalGPT/Services/ToolchainExecutionProfileService.cs", "private static readonly JsonSerializerOptions", "private static ToolchainExecutionProfile NormalizeProfile", "private static string ResolveExecutable")
    require("src/LocalGPT/Services/ToolchainRuntimeManagementService.cs", "FilterEntries(IReadOnlyCollection<ToolchainEnvironmentEntry>", "private readonly JsonSerializerOptions JsonOptions")
    forbid("src/LocalGPT/Services/ToolchainRuntimeManagementService.cs", "private static readonly JsonSerializerOptions", "private static ToolchainEnvironmentOverrideRecord? DeserializeRecord")
    require("src/LocalGPT/Interfaces/IToolchainRuntimeManagementService.cs", "IReadOnlyList<ToolchainEnvironmentEntry> FilterEntries")
    forbid("src/LocalGPT/Components/Pages/Install.ToolchainRuntime.razor.cs", "item.Name.Contains(ToolchainEnvironmentSearch", "item.Source.Contains(ToolchainEnvironmentSearch")
    require("src/LocalGPT/Controller/ToolchainRuntimeController.cs", '/// <param name="profiles">')
    require("src/LocalGPT/Services/LocalAiRuntimeService.cs", "Loading the LocalGPT Python execution policy failed", "Loading the database-backed LocalGPT local-AI runtime policy failed")

    # No generated Python cache in source handoff.
    bad_cache = [p.relative_to(ROOT).as_posix() for p in ROOT.rglob("*") if p.is_file() and (p.suffix in {".pyc", ".pyo"} or "__pycache__" in p.parts)]
    if bad_cache:
        raise AssertionError(f"compiled Python cache files present: {bad_cache[:8]}")

    # No accidental new two-digit current version string such as 4.7.10.
    current_sources = [
        ROOT / "RELEASE.md",
        ROOT / "VALIDATION.md",
    ]
    version_re = re.compile(r"\b\d+\.\d{2,}\.\d+\b|\b\d+\.\d+\.\d{2,}\b")
    for path in current_sources:
        match = version_re.search(path.read_text(encoding="utf-8"))
        if match:
            raise AssertionError(f"{path.name}: two-digit version slot found: {match.group(0)}")

    print("LocalGPT 4.8.1 source audit: PASS")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"LocalGPT 4.8.1 source audit: FAIL: {exc}", file=sys.stderr)
        raise
