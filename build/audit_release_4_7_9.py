#!/usr/bin/env python3
from pathlib import Path
import json
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
VERSION = "4.7.9"


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        raise SystemExit(f"LocalGPT {VERSION} audit failed: missing {rel}")
    return path.read_text(encoding="utf-8", errors="strict")


def require(rel: str, *markers: str) -> None:
    text = read(rel)
    for marker in markers:
        if marker not in text:
            raise SystemExit(f"LocalGPT {VERSION} audit failed: {rel} missing {marker!r}")


def forbid(rel: str, *markers: str) -> None:
    text = read(rel)
    for marker in markers:
        if marker in text:
            raise SystemExit(f"LocalGPT {VERSION} audit failed: {rel} contains forbidden {marker!r}")


def block(text: str, start_marker: str, end_marker: str) -> str:
    start = text.find(start_marker)
    if start < 0:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: missing block start {start_marker!r}")
    end = text.find(end_marker, start + len(start_marker))
    if end < 0:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: missing block end {end_marker!r}")
    return text[start:end]


def main() -> int:
    parts = [int(part) for part in VERSION.split(".")]
    if len(parts) != 3 or parts[1] >= 10 or parts[2] >= 10:
        raise SystemExit("LocalGPT version-slot policy failed")

    for rel in (
        "src/LocalGPT/LocalGPT.csproj",
        "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj",
        "src/LocalGPTWebviewWrapper/LocalGPTWebviewWrapper.csproj",
    ):
        ET.parse(ROOT / rel)
        require(rel, f"<Version>{VERSION}</Version>")

    require("RELEASE.md", f"# LocalGPT {VERSION}", "PublisherStudio is unchanged")
    require("VALIDATION.md", f"# LocalGPT {VERSION} source validation")
    require("CHANGELOG-v4.7.9-GLOBAL-IMPORT-NAMING.md", f"# LocalGPT {VERSION}")
    require("VALIDATION-v4.7.9-source.md", f"# LocalGPT {VERSION} source validation")
    require("src/LocalGPT/Components/App.razor", "localgpt-game-console.js?v=4.7.9", "localgpt-chat-ui.js?v=4.7.9")
    require("src/LocalGPT/Services/InitialSetupAssistantService.cs", "LocalGPT/4.7.9")
    require("src/LocalGPT/Services/CanIRunHardwareRecommendationService.cs", "LocalGPT/4.7.9")
    require("docs/index.md", "**Version 4.7.9**", "LocalGPT-4.7.9.pdf")
    require("docs/docfx.json", '"localgptVersion": "4.7.9"')
    require("docs/pdf/toc.yml", "LocalGPT-4.7.9.pdf")
    require("docs/pdf-cover.html", "LocalGPT 4.7.9 Documentation", "Version 4.7.9")
    json.loads(read("docs/docfx.json"))

    # C# project-wide imports use LocalGPT naming that is distinct from Razor _Imports.razor.
    require("src/LocalGPT/GlobalImports.cs", "global using LocalGPT.WireProtocol;")
    if (ROOT / "src/LocalGPT/GlobalUsings.OneWire.cs").exists():
        raise SystemExit(f"LocalGPT {VERSION} audit failed: legacy GlobalUsings.OneWire.cs still exists")
    require("build/Assert-OneWireArchitecture.ps1", "src/LocalGPT\\GlobalImports.cs")

    # Council false-missing-model repair.
    identity = "src/LocalGPT/BusinessObjects/ProviderModelModels.cs"
    require(
        identity,
        "AreEquivalentSelectionKeys",
        "Legacy bare model names are accepted only as candidate matches",
        "IsOllamaOpenAiCompatibilityFacade",
        "ModelNamesEquivalent(leftModel, rightModel)",
    )
    selection = "src/LocalGPT/Services/MultiModelCouncilService.WorkflowSelection.cs"
    require(
        selection,
        "ResolveConfiguredRoleParticipantKeys",
        "identity.AreEquivalentSelectionKeys(configuredModelKey, participant)",
        "no equivalent provider/model identity is active in this run",
    )
    health = "src/LocalGPT/Services/MultiModelCouncilService.LiveInputAndHealth.cs"
    require(
        health,
        "ApplyApprovedOneRunModelExclusionsAsync(",
        "OrganicCouncilTeamDefinition team",
        "GetConfiguredTeamModelBindings(team)",
        "approved one-run health exclusion consumed",
        "instead of invalidating its role assignment",
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.RunOrchestration.cs",
        "ApplyApprovedOneRunModelExclusionsAsync(selectedParticipants, organicTeam, cancellationToken)",
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.WorkflowPrompting.cs",
        "identity.AreEquivalentSelectionKeys(definition.AssignedModelName, model)",
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.RoleSynthesis.cs",
        "identity.AreEquivalentSelectionKeys(definition.RoleResultSynthesisModelName, model)",
    )

    # Tournament speed and bounded context.
    require("src/LocalGPT/BusinessObjects/CouncilGameModels.cs", "TournamentCurrentFighterIds", "TrainerGreeting")
    require("src/LocalGPT/Services/CouncilGameSessionService.Snapshots.cs", "TournamentCurrentFighterIds")
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.KernelTournament.cs",
        "LimitKernelTournamentRoundParticipantsAsync",
        "game.TournamentCurrentFighterIds.Count != 2",
        "current legal-match member(s)",
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.WorkflowDefinitionExecution.cs",
        "LimitKernelTournamentRoundParticipantsAsync(",
    )
    prompting = read("src/LocalGPT/Services/MultiModelCouncilService.WorkflowPrompting.cs")
    if "latest bounded battle context plus the opening identities" not in prompting or ".TakeLast(8)" not in prompting:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: tournament battle context is not bounded")

    seed = read("src/LocalGPT/Services/OrganicCouncilBlueprintSeedDataService.cs")
    trainer = block(seed, 'Key = "trainer-round-command"', 'Key = "fight-round"')
    creature = block(seed, 'Key = "fight-round"', 'Key = "arena-round-engine"')
    for name, content in (("trainer", trainer), ("creature", creature)):
        if 'ExecutionMode = "AllMembersParallel"' not in content:
            raise SystemExit(f"LocalGPT {VERSION} audit failed: {name} battle round is not benchmark-bounded parallel")
    if "HUD: <one new compact printable-ASCII arena HUD motif" not in trainer:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: trainer HUD motif output missing")
    if "EFFECT: <one new compact printable-ASCII motion/effect motif" not in creature:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: creature effect motif output missing")

    # Arena presentation and reusable ASCII/Pixel state stream.
    require(
        "src/LocalGPT/Services/CouncilGameWorkflowBootstrapService.cs",
        "FrameWidth = highResolutionTournament ? 168 : 80",
        "FrameHeight = highResolutionTournament ? 44 : 25",
    )
    arena = "src/LocalGPT/Services/CouncilGameSessionService.KernelTournament.cs"
    require(
        arena,
        "Math.Min(180, Math.Max(112, session.FrameWidth))",
        "Math.Min(52, Math.Max(34, session.FrameHeight))",
        "ROUND {session.TournamentRound} // TRAINER GREETING",
        "ROUND {session.TournamentRound} // MATCH GREETING",
        "CREATURE GREETING // READY STANCE",
        "CREATURE SALUTE",
        "ATTACK ANIMATION // LEFT BURST",
        "ATTACK ANIMATION // RIGHT BURST",
        "FINISHER // CHARGE",
        "FINISHER // VICTORY SIGNAL",
        'ReadTaggedValue(leftTrainer, "HUD")',
        'ReadTaggedValue(leftCreature, "EFFECT")',
        "Compact(creature.Name, 18)",
    )
    require(
        "src/LocalGPT/Components/Shared/ChatGameConsole.razor",
        'new("Pixel", "Pixel screen")',
        "data-game-pixel-screen",
    )
    require(
        "src/LocalGPT/wwwroot/js/localgpt-game-console.js",
        "renderPixelFrame",
        "state.currentFrameText",
    )

    # Responsive game modal and dropdown portal.
    require(
        "src/LocalGPT/Components/Pages/Chat.razor",
        "@rendermode InteractiveServer",
        'Width="min(1920px, 98vw)"',
        'Height="96dvh"',
        'MaxHeight="98dvh"',
    )
    chat_css = read("src/LocalGPT/Components/Pages/Chat.razor.css")
    if ".chat-game-popup-body" not in chat_css or "height: 100%;" not in chat_css or "max-height: none;" not in chat_css:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: responsive game popup sizing missing")
    require(
        "src/LocalGPT/Components/Shared/ChatGameConsole.razor.css",
        ".chat-game-console-actions ::deep .ascii-console-button",
        "min-width: 7.5rem",
    )
    require(
        "src/LocalGPT/wwwroot/css/site.css",
        "dxbl-popup-root > dxbl-popup-cell:has(> dxbl-dropdown)",
        "pointer-events: none !important",
        "dxbl-popup-root > dxbl-popup-cell:has(> dxbl-dropdown:not(.dxbl-popup-hidden))",
        "z-index: 22040 !important",
    )
    require(
        "src/LocalGPT/wwwroot/js/localgpt-game-console.js",
        "resolveScaleSurface",
        "setFittedFontSize",
        "scrollback ? 168 : Number.POSITIVE_INFINITY",
        "state.resizeObserver.observe(element);",
    )
    forbid(
        "src/LocalGPT/wwwroot/js/localgpt-game-console.js",
        "state.resizeObserver.observe(element.parentElement)",
        "state.followTailRootObserver = new MutationObserver",
    )
    require(
        "src/LocalGPT/Services/AsciiChatTextService.cs",
        "participantProjectionCache",
        "LiveParticipantTraceCharacters = 8192",
        "BoundParticipantSource",
        "canonical Chat/Council history",
    )

    # Overnight debugger-stop repair: expected framework read cancellation stays inside the provider adapter,
    # then caller-owned Council cancellation semantics are re-applied in user code.
    require(
        "src/LocalGPT/Services/OllamaThinkingChatClient.cs",
        "var streamReadCanceled = false;",
        "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
        "returned control to the Council caller without leaking a framework TaskCanceledException",
        "yield break;",
    )
    require(
        "src/LocalGPT/Services/MultiModelCouncilService.ParticipantExecution.cs",
        "if (streamCts.IsCancellationRequested && !liveInputSignal.Task.IsCompletedSuccessfully)",
        "participantCts.Token.ThrowIfCancellationRequested();",
        "exceeded the {modelTimeoutSeconds}s council timeout during {phase}",
    )


    # Benchmark first-chance JSON flood repair: scan structurally before invoking the throwing parser.
    require(
        "src/LocalGPT/Services/ProviderModelBenchmarkService.ParsingAndScoring.cs",
        "LooksLikeJsonObjectStart",
        "TryExtractBalancedJsonObject",
        "Only attempt parsing after a complete",
        "searchOffset = Math.Max(nextOffset, start + 1);",
    )
    require(
        "src/LocalGPT/Services/DxAiFunctionCallRecoveryService.cs",
        "LooksLikeCompleteJsonCarrier",
        "if (!LooksLikeCompleteJsonCarrier(json))",
    )

    # File logging must serialize all categories through one provider-owned sink and recover from I/O errors.
    require(
        "src/LocalGPT/Logging/FileLoggerProvider.cs",
        "private readonly Lazy<FileLoggerSink> sink;",
        "new FileLogger(categoryName, options, sink.Value)",
        "sink.Value.Dispose();",
    )
    require(
        "src/LocalGPT/Logging/FileLogger.cs",
        "internal sealed class FileLoggerSink",
        "FileShare.ReadWrite | FileShare.Delete",
        "for (var attempt = 0; attempt < 3 && !written; attempt++)",
        "LocalGPT-fallback.log",
        "will retry/fall back without stopping application logging",
    )
    require(
        "src/LocalGPT/appsettings.Development.json",
        '"FileCore"',
        '"CoreLogLevel": 2',
    )

    # Local AI runtime: one serialized embedded lane with a persistent bridge module/model cache.
    require(
        "src/LocalGPT/BusinessObjects/PythonCoreOptions.cs",
        "PythonExecutable",
        "PythonRuntime",
        "VirtualEnvironmentPath",
        "ModelRoot",
        "ArtifactRoot",
        "MaximumImagePixels",
        "HuggingFaceTokenEnvironmentVariable",
        "QueueCapacity",
        "CacheModels",
    )
    require(
        "src/LocalGPT/BusinessObjects/LocalAiRuntimeModels.cs",
        "ImageGeneration",
        "ImageEditing",
        "TextToVideo",
        "ImageToVideo",
        "SpeechRecognition",
        "LocalAiModelInstallation",
        "LocalAiArtifactDescriptor",
    )
    require(
        "src/LocalGPT/Program.ServiceRegistration.cs",
        "AddSingleton<IPythonNetRuntimeCoordinator, PythonNetRuntimeCoordinator>()",
        "AddSingleton<ILocalAiArtifactService, LocalAiArtifactService>()",
        "AddScoped<ILocalAiRuntimeService, LocalAiRuntimeService>()",
        "AddScoped<IHuggingFaceModelCatalogService, HuggingFaceModelCatalogService>()",
        'AddHttpClient("LocalGPTHuggingFace"',
    )
    coordinator = "src/LocalGPT/Services/PythonNetRuntimeCoordinator.cs"
    require(
        coordinator,
        "Channel.CreateBounded<QueuedJob>",
        "SingleReader = true",
        "CreateLinkedTokenSource(item.CancellationToken, cancellationToken)",
        "Runtime.PythonDLL",
        "PythonEngine",
        "BeginAllowThreads",
        "EnsureBridgeModuleLoaded",
        "sys.modules['_localgpt_runtime_bridge'].run_job",
        "bridgeModuleLoaded = true",
        'ResolveUserPath("LocalAiRuntime", "InputScratch")',
        'ResolveUserPath("LocalAiRuntime", "InstallScratch")',
        'Path.Combine(modelRoot, ".localgpt", "incoming")',
        "restartRequired = true",
    )
    forbid(coordinator, "runpy.run_path")

    bridge = "src/LocalGPT/Runtime/Python/localgpt_runtime_bridge.py"
    require(
        bridge,
        "_MODEL_CACHE: dict[str, object] = {}",
        "def run_job(request_path: str, result_path: str, cancel_path: str) -> int:",
        "_cache_key(kind, model_path, trust_remote_code, device)",
        "local_files_only=True",
        "use_safetensors=True",
        "snapshot_download(",
        "maximum_image_pixels",
        "Audio payload density is implausibly low",
        'result["backend"] = "rocm" if hip_version else "cuda"',
        '"cache_clear": _cache_clear',
        'required=("prompt", "image")',
        'required=("image",)',
        'pipeline_device = 0 if device == "cuda" else "mps" if device == "mps" else -1',
    )
    forbid(bridge, "import subprocess", "subprocess.", "os.system(", "eval(", "exec(")

    runtime_service = "src/LocalGPT/Services/LocalAiRuntimeService.cs"
    require(
        runtime_service,
        "ExecutableCapabilities",
        "RunBridgeProcessAsync(\"hf_snapshot_download\"",
        "Only reviewed non-inference bridge operations may run outside the embedded Python.NET lane.",
        'ResolveUserPath("LocalAiRuntime", "InstallScratch"',
        "Directory.Move(incomingPath, localPath)",
        "ClearModelCacheAsync",
        "maximum_image_pixels",
        "ParsePythonVersion",
        "Could not evict a model from the embedded Python cache before removal",
        "Explicit human confirmation is required",
    )
    forbid(runtime_service, "git clone", "ProcessStartInfo { FileName = \"git\"")

    require(
        "src/LocalGPT/Services/HuggingFaceModelCatalogService.cs",
        "https://huggingface.co/api/models?search=",
        "query.Length > 300",
        "HuggingFaceTokenEnvironmentVariable",
        "LocalGPT/4.7.9",
    )
    require(
        "src/LocalGPT/Services/LocalAiRuntimeDxAiFunctions.cs",
        '"localai.runtime.status"',
        '"localai.runtime.probe"',
        '"localai.runtime.cache.clear"',
        '"localai.huggingface.search"',
        '"localai.model.install"',
        '"localai.model.remove"',
        '"localai.image.generate"',
        '"localai.image.edit.workspace"',
        '"localai.video.generate"',
        '"localai.video.from-image.workspace"',
        '"localai.audio.transcribe.workspace"',
        "RequiresHumanConfirmation: true",
        "SupportsDeferredApprovalRequest: true",
    )
    require(
        "src/LocalGPT/Controller/LocalAiArtifactController.cs",
        'Response.Headers["Cache-Control"] = "private, no-store, max-age=0"',
        "enableRangeProcessing: true",
    )
    require(
        "src/LocalGPT/Services/LocalAiArtifactService.cs",
        "ArtifactRoot",
        "ResolveUserPath",
        "IsSameOrDescendantPath",
    )
    require(
        "src/LocalGPT/Components/Pages/Install.razor",
        "Local AI runtimes &amp; Hugging Face models",
        "Discover Python runtimes",
        "Create managed environment + core",
        "Search Hugging Face",
        "Unload cached models",
        "Install snapshot",
        "Remove model",
    )
    require(
        "src/LocalGPT/Components/Pages/Install.LocalAiRuntime.razor.cs",
        "LocalAiCapability.ImageGeneration",
        "LocalAiCapability.ImageEditing",
        "LocalAiCapability.TextToVideo",
        "LocalAiCapability.ImageToVideo",
        "LocalAiCapability.SpeechRecognition",
        "ClearLocalAiModelCacheAsync",
        "CanInstallLocalAiModel",
    )
    require(
        "src/LocalGPT/LocalGPT.csproj",
        'Content Include="Runtime\\Python\\localgpt_runtime_bridge.py"',
        'EmbeddedResource Include="..\\..\\docs\\reference\\local-ai-runtime.md"',
    )
    require("docs/reference/local-ai-runtime.md", "serialized", "Hugging Face", "DXFunction", "retention minimization")
    for rel in ("src/LocalGPT/appsettings.json", "src/LocalGPT/appsettings.Development.json"):
        parsed = json.loads(read(rel))
        python_core = parsed.get("PythonCore") or {}
        if python_core.get("MaximumImagePixels") != 100000000 or python_core.get("QueueCapacity") != 64:
            raise SystemExit(f"LocalGPT {VERSION} audit failed: {rel} Local-AI defaults drifted")
        keys = {str(item.get("Key")) for item in python_core.get("PackageProfiles", []) if isinstance(item, dict)}
        if not {"core", "pytorch-default", "image", "video", "speech"}.issubset(keys):
            raise SystemExit(f"LocalGPT {VERSION} audit failed: {rel} package profiles incomplete")

    # Interactive Server directives are a protected architectural surface.
    render_files = []
    for path in (ROOT / "src/LocalGPT/Components").rglob("*.razor"):
        if re.search(r"(?m)^@rendermode\s+InteractiveServer\s*$", path.read_text(encoding="utf-8")):
            render_files.append(path)
    expected_render_files = {
        "src/LocalGPT/Components/Layout/NavMenu.razor",
        "src/LocalGPT/Components/Pages/ProjectMaintenance.razor",
        "src/LocalGPT/Components/Pages/Projects.razor",
        "src/LocalGPT/Components/Pages/Chat.razor",
        "src/LocalGPT/Components/Pages/CouncilTeams.razor",
        "src/LocalGPT/Components/Pages/Index.razor",
        "src/LocalGPT/Components/Pages/TestLab.razor",
        "src/LocalGPT/Components/Pages/MinecraftModBuilder.razor",
        "src/LocalGPT/Components/Pages/RemoteControl.razor",
        "src/LocalGPT/Components/Pages/DxFunctionCatalog.razor",
        "src/LocalGPT/Components/Pages/Install.razor",
        "src/LocalGPT/Components/Pages/Help.razor",
        "src/LocalGPT/Components/Pages/OneWireSecurity.razor",
        "src/LocalGPT/Components/Pages/ModelCouncil.razor",
        "src/LocalGPT/Components/Pages/Database.razor",
    }
    actual_render_files = {str(path.relative_to(ROOT)).replace("\\", "/") for path in render_files}
    if actual_render_files != expected_render_files:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: InteractiveServer component set changed: {sorted(actual_render_files ^ expected_render_files)}")

    # Keep the previous release's major safety/feature anchors present.
    require("src/LocalGPT/Services/WorkspaceVisionOcrService.cs", "ResolveWorkspacePath", "deepseek-ocr", "var baseAddress = new Uri(normalizedHost, UriKind.Absolute)")
    require("src/LocalGPT/wwwroot/js/localgpt-context-menu.js", "localgpt.controllerMode", "controllerContextAction")
    require("src/LocalGPT/Services/ProjectRepositoryLearningService.cs", "NeedsUserReview", "ParentRevisionId")
    require("src/LocalGPT/Services/UploadFileProcessingCapabilityService.cs", "x-publisher-format-families", "publisher.media.capabilities")

    # Owner-build regression list from 4.7.6.
    for rel in (
        "src/LocalGPT/Services/LocalAiArtifactService.cs",
        "src/LocalGPT/Services/HuggingFaceModelCatalogService.cs",
        "src/LocalGPT/Services/LocalAiRuntimeService.cs",
        "src/LocalGPT/Services/PythonNetRuntimeCoordinator.cs",
    ):
        require(rel, "using Microsoft.Extensions.Options;", "LocalGptConfigurationRoot = LocalGPT.BusinessObjects.ConfigurationRoot")
        forbid(rel, "IOptionsMonitor<ConfigurationRoot>")

    require(
        "src/LocalGPT/Services/LocalAiArtifactService.cs",
        "await using (input.ConfigureAwait(false))",
        "await using (output.ConfigureAwait(false))",
    )
    require(
        "src/LocalGPT/Services/LocalAiRuntimeService.cs",
        "await using (source.ConfigureAwait(false))",
        "await using (destination.ConfigureAwait(false))",
        "string FormatPackageList",
        "string FormatCapabilities",
    )
    forbid(
        "src/LocalGPT/Components/Pages/Install.LocalAiRuntime.razor.cs",
        "string.Join(",
    )
    forbid(
        "src/LocalGPT/Components/Pages/Install.razor",
        'string.Join(" · ", profile.Packages)',
    )
    require(
        "src/LocalGPT/Components/Pages/Install.razor",
        "LocalAiRuntime.FormatPackageList(profile.Packages)",
        "LocalAiRuntime.FormatCapabilities(model.Capabilities)",
    )
    forbid("src/LocalGPT/Logging/FileLogger.cs", "private static StreamWriter OpenWriter", "private static string ResolveLogPath")
    forbid("src/LocalGPT/Services/DxAiFunctionCallRecoveryService.cs", "private static bool LooksLikeCompleteJsonCarrier")
    forbid(
        "src/LocalGPT/Services/ProviderModelBenchmarkService.ParsingAndScoring.cs",
        "private static bool LooksLikeJsonObjectStart",
        "private static bool TryExtractBalancedJsonObject",
    )

    # Owner-build regression from 4.7.7: specific ObjectDisposedException catch must precede its InvalidOperationException base catch.
    file_logger = read("src/LocalGPT/Logging/FileLogger.cs")
    enqueue_start = file_logger.index("public void Enqueue(string message)")
    enqueue_end = file_logger.index("private void ProcessLogQueue()", enqueue_start)
    enqueue_block = file_logger[enqueue_start:enqueue_end]
    disposed_index = enqueue_block.index("catch (ObjectDisposedException)")
    invalid_index = enqueue_block.index("catch (InvalidOperationException)")
    if disposed_index > invalid_index:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: FileLoggerSink.Enqueue catches InvalidOperationException before ObjectDisposedException")

    # Shared console identity is immutable business state plus stateless extensions, not one root-level global static helper.
    if (ROOT / "src/Shared/ConsoleProductIdentity.cs").exists():
        raise SystemExit(f"LocalGPT {VERSION} audit failed: legacy root-level ConsoleProductIdentity helper still exists")
    require(
        "src/Shared/BusinessObjects/ConsoleProductIdentity.cs",
        "internal sealed record ConsoleProductIdentity",
        "string Product",
        "string Version",
        "string RepositoryUrl",
        "string RepositorySlug",
        "string Owner",
        "string License",
    )
    require(
        "src/Shared/Extensions/StringExtensions.cs",
        "WithoutBuildMetadata",
        "ToRepositorySlug",
    )
    require(
        "src/Shared/Extensions/ConsoleProductIdentityExtensions.cs",
        "ToConsoleProductIdentity(this Assembly assembly)",
        "WriteStartupHeader(this ConsoleProductIdentity identity)",
        "ResolveMetadata(assembly, \"RepositoryUrl\")",
    )
    for rel in ("src/LocalGPT/LocalGPT.csproj", "src/LocalGPTInstallerConsole/LocalGPTInstallerConsole.csproj"):
        require(
            rel,
            "..\\Shared\\BusinessObjects\\ConsoleProductIdentity.cs",
            "..\\Shared\\Extensions\\StringExtensions.cs",
            "..\\Shared\\Extensions\\ConsoleProductIdentityExtensions.cs",
        )
    require(
        "src/LocalGPT/Program.cs",
        "typeof(Program).Assembly.ToConsoleProductIdentity().WriteStartupHeader();",
    )
    require(
        "src/LocalGPTInstallerConsole/Program.cs",
        "typeof(Program).Assembly.ToConsoleProductIdentity().WriteStartupHeader();",
        "typeof(Program).Assembly.ToConsoleProductIdentity().RepositorySlug",
    )

    bridge = read("src/LocalGPT/Runtime/Python/localgpt_runtime_bridge.py")
    prepare_start = bridge.index("def _prepare_pipeline_kwargs")
    prepare_end = bridge.index("\ndef _diffusion_callback", prepare_start)
    prepare_block = bridge[prepare_start:prepare_end]
    if "except (TypeError, ValueError):" not in prepare_block or "missing = [name for name in required" not in prepare_block:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: pipeline signature guard is incomplete")
    if re.search(r"except\s+Exception\s*:\s*\n\s*return kwargs", prepare_block):
        raise SystemExit(f"LocalGPT {VERSION} audit failed: pipeline signature guard can swallow required-modality failures")

    import hashlib
    manifest = {}
    for line in read("build/javascript-diagnostics-files.sha256").splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        digest, relative = stripped.split("  ", 1)
        manifest[relative.replace("\\", "/")] = digest.lower()
    js_relative = "src/LocalGPT/wwwroot/js/localgpt-game-console.js"
    js_normalized = read(js_relative).replace("\r\n", "\n").replace("\r", "\n")
    js_digest = hashlib.sha256(js_normalized.encode("utf-8")).hexdigest()
    if manifest.get(js_relative) != js_digest:
        raise SystemExit(f"LocalGPT {VERSION} audit failed: JavaScript diagnostics manifest is stale for {js_relative}")

    print(f"LocalGPT {VERSION} release audit passed: GlobalImports naming is enforced, the legacy global-using filename is absent, and prior Local AI, compiler-repair, benchmark, Council, ASCII and InteractiveServer contracts remain present.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
