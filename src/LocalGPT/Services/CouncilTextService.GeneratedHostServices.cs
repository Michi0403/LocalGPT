using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.Blazor.Viewer.Internal;
using DevExpress.DataAccess.DataFederation;
using DevExpress.Utils.About;
using DevExpress.XtraCharts;
using DevExpress.XtraReports.Serialization;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.AI;
using SQLitePCL;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Net;
using System.Reactive;
using System.Security.AccessControl;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.Extensions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council text behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilTextService
    {
        /// <summary>
        /// Generates source fidelity service as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="archetype">Archetype value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSourceFidelityService(string projectName, GeneratedSolutionArchetype archetype, ILogger logger)
        {
            try
            {
                var rows = archetype switch
                {
                    GeneratedSolutionArchetype.LocalGpt => """
                        new(
                            "DXAiChat workbench",
                            "Original LocalGPT centers user work in DXAiChat with model selection, council mode, uploads, memory, visible progress, and artifact links.",
                            "Generated Chat page plus backend service boundaries for model routing, file context, Harmony/thinking display, and downloadable artifacts.",
                            "Represented",
                            "Components/Pages/Chat.razor, Source Fidelity page, and artifact contract docs."),
                        new(
                            "AI Council",
                            "Original LocalGPT supports multi-model council talks, polls, missing-feature logs, and user-approved implementation artifacts.",
                            "Generated Model Council page with minimum-member, poll-gate, feedback-log, and artifact-delivery requirements.",
                            "Represented",
                            "Components/Pages/ModelCouncil.razor and SOURCE_FIDELITY.md."),
                        new(
                            "SQLite memory and knowledge",
                            "Original LocalGPT persists chats, thoughts, logs, knowledge, approvals, and benchmark feedback in SQLite.",
                            "Generated Database page and source-fidelity rows state EF/SQLite as durable state boundary.",
                            "Boundary",
                            "Components/Pages/Database.razor; real DbContext integration must be added when moving beyond sandbox."),
                        new(
                            "Minecraft builder",
                            "Original LocalGPT can generate datapacks and loader skeletons through backend artifact routes.",
                            "Generated Minecraft Mod Builder page represents datapack, loader matrix, version resolver, validation, and downloads.",
                            "Represented",
                            "Components/Pages/MinecraftModBuilder.razor."),
                        new(
                            "Install and diagnostics",
                            "Original LocalGPT detects Ollama/LM Studio/runtime setup and exposes frontend-facing test routes.",
                            "Generated Install and Test Lab pages require local host status, runtime checks, and route smoke tests.",
                            "Represented",
                            "Components/Pages/Install.razor and Components/Pages/TestLab.razor.")
                """,
                    GeneratedSolutionArchetype.TacosPortal => """
                        new(
                            "Multi-host topology",
                            "Original TacosPortalOpen is a multi-project .NET/Blazor system with core library, server host, WASM/client option, and WinUI/WebView2 wrapper boundaries.",
                            "Generated Client Shells page and source docs require server, WASM, WebView2, packaging, and debug/deploy boundaries.",
                            "Represented",
                            "Components/Pages/ClientShells.razor and SOURCE_FIDELITY.md."),
                        new(
                            "Telegram/event ingestion",
                            "Original TacosPortalOpen uses Telegram-style event ingestion flowing through handlers, service/API layers, persistence, workers, and UI.",
                            "Generated Telegram Ingestion page models update handling, command routing, idempotency, and retry queues.",
                            "Represented",
                            "Components/Pages/TelegramIngestion.razor."),
                        new(
                            "Normalized persistence",
                            "Original TacosPortalOpen separates domain/business objects, persistence, DTO/service boundaries, and migration safety.",
                            "Generated Persistence page requires business objects, DbContext boundaries, DTOs, and safe migrations.",
                            "Boundary",
                            "Components/Pages/Persistence.razor; real entities/migrations are a follow-up for the selected target database."),
                        new(
                            "Workers and notifications",
                            "Original TacosPortalOpen includes polling/background services, notifications, logs, and integration diagnostics.",
                            "Generated Workers page models hosted services, polling, notification dispatch, and diagnostics.",
                            "Represented",
                            "Components/Pages/Workers.razor."),
                        new(
                            "DevExpress admin/security",
                            "Original TacosPortalOpen uses DevExpress/XAF-adjacent admin, role/security, audit, validation, and CRUD forms.",
                            "Generated Admin page requires users, roles, audit, validation, and settings through DevExpress controls.",
                            "Represented",
                            "Components/Pages/Admin.razor.")
                """,
                    GeneratedSolutionArchetype.AiHost => """
                        new(
                            "Provider-compatible routes",
                            "AI-host-shaped requests need /api/version, /api/tags, /api/ps, /api/chat, /api/generate, embeddings, and OpenAI-compatible routes.",
                            "Generated Program.cs maps route endpoints through provider/catalog/runner service contracts.",
                            "Represented",
                            "Program.cs and Services/GeneratedAiHostArchitectureServices.cs."),
                        new(
                            "Native runner boundary",
                            "A real AI host needs native model loading, tokenizer/template handling, GPU scheduling, blobs, and runner lifecycle.",
                            "Generated runner/plugin pages expose the native model-file runner and configuration readiness.",
                            "Represented",
                            "Components/Pages/RunnerPlugins.razor and IInferenceRunner."),
                        new(
                            "Model catalog and downloads",
                            "AI host UX needs model inventory, running models, download/pull planning, settings, hardware, and logs.",
                            "Generated pages cover catalog, downloads, running models, templates, hardware, logs, and settings.",
                            "Represented",
                            "Components/Pages/ModelDownloads.razor, RunningModels.razor, Hardware.razor, Logs.razor, Settings.razor."),
                        new(
                            "Adapter architecture",
                            "External hosts, HuggingFace downloads, Python.NET, PowerShell, optional TypeScript client/script assets, and plugins should sit behind explicit interfaces.",
                            "Generated service file declares provider, runner, plugin, script, hardware, and template interfaces.",
                            "Represented",
                            "Services/GeneratedAiHostArchitectureServices.cs.")
                """,
                    GeneratedSolutionArchetype.BotBackend => """
                        new(
                            "Webhook ingress",
                            "Bot backend requests need signed/idempotent event intake and retry/dead-letter diagnostics.",
                            "Generated Webhooks page models ingress, signature check, idempotency, and dead letters.",
                            "Represented",
                            "Components/Pages/Webhooks.razor."),
                        new(
                            "Conversation state",
                            "Bot systems need persisted conversation memory, moderation, transcript review, and handoff.",
                            "Generated Conversations page models memory, moderation, handoff, and transcript work.",
                            "Boundary",
                            "Components/Pages/Conversations.razor; real EF/SQLite implementation must be added for production."),
                        new(
                            "Optional Python interop",
                            "Legacy examples show Python.NET/process adapters for speech, translation, or automation helpers.",
                            "Generated Python Interop page keeps this permission-gated and backend-owned.",
                            "Represented",
                            "Components/Pages/PythonInterop.razor.")
                """,
                    _ => """
                        new(
                            "Generated sandbox",
                            "User requested a downloadable .NET/Blazor/DevExpress artifact.",
                            "Generated files include navigation, pages, service/model code, docs, and contract JSON.",
                            "Represented",
                            "PROJECT_INDEX.md and .localgpt-generation.json.")
                """
                };

                return $$"""
            namespace {{projectName}}.Services;

            /// <summary>
            /// Describes whether the generated sandbox preserves the requested source architecture.
            /// </summary>
            public interface ISourceFidelityService
            {
                /// <summary>
                /// Returns source-fidelity requirements for review and benchmark scoring.
                /// </summary>
                IReadOnlyList<GeneratedSourceFidelityRequirement> GetRequirements();
            }

            /// <summary>
            /// Deterministic source-fidelity service generated by LocalGPT.
            /// </summary>
            public sealed class GeneratedSourceFidelityService : ISourceFidelityService
            {
                /// <inheritdoc />
                public IReadOnlyList<GeneratedSourceFidelityRequirement> GetRequirements()
                {
                    return
                    [
                {{rows}}
                    ];
                }
            }

            /// <summary>
            /// One source-fidelity requirement represented by this generated sandbox.
            /// </summary>
            public sealed record GeneratedSourceFidelityRequirement(
                string Area,
                string SourceSignal,
                string GeneratedBoundary,
                string Status,
                string Evidence);
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSourceFidelityService projectName:{projectName} LocalGptCatalogService:{archetype.ToString()}", archetype);
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates AI host architecture services as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateAiHostArchitectureServices(string projectName,ILogger logger) {
            try
            {
                return $$"""
            using System.Diagnostics;
            using System.Globalization;
            using System.Text;
            using System.Text.Json;
            using Microsoft.Extensions.Options;
            using {{projectName}}.Models;

            #pragma warning disable CS1591 // Generated sandbox contracts are documented in ARCHITECTURE.md and BUILD_AND_RUN.md.

            namespace {{projectName}}.Services;

            /// <summary>
            /// Typed bootstrap settings for a generated provider-compatible AI host control plane.
            /// Persist user-edited runtime values in SQLite when this lab becomes a real app.
            /// </summary>
            public sealed class AiHostRuntimeOptions
            {
                public string DefaultModel { get; set; } = "gpt-oss:20b";
                public string SafeStorageRoot { get; set; } = "%LOCALAPPDATA%/GeneratedAiHost";
                public string PluginRoot { get; set; } = "plugins";
                public string? PythonDll { get; set; }
                public string NativeRunnerExecutable { get; set; } = string.Empty;
                public List<string> ModelSearchRoots { get; set; } = new();
                public Dictionary<string, string> ModelFileOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
                public int ContextTokens { get; set; } = 2048;
                public int GpuLayers { get; set; } = 20;
                public bool AllowNativeRunner { get; set; } = false;
                public bool AllowPythonNet { get; set; }
                public bool AllowPowerShellScripts { get; set; }
                public bool AllowTypeScriptAdapters { get; set; }
            }

            public interface IModelCatalogService
            {
                IReadOnlyList<GeneratedAiHostModelTag> GetAiHostTags();
                IReadOnlyList<GeneratedAiHostModelTag> GetRunningModels();
                object GetModelDetails(GeneratedModelActionRequest request);
            }

            public interface IModelTransferService
            {
                GeneratedAiHostOperation CreatePullPlan(GeneratedModelActionRequest request);
            }

            public interface IInferenceProvider
            {
                string ProviderKind { get; }
                Task<GeneratedChatResponse> ChatAsync(GeneratedChatRequest request, CancellationToken cancellationToken = default);
                Task<object> GenerateAsync(GeneratedModelActionRequest request, CancellationToken cancellationToken = default);
            }

            public interface IInferenceRunner
            {
                string RunnerKind { get; }
                Task<RunnerCapabilityReport> GetCapabilityAsync(CancellationToken cancellationToken = default);
                Task<GeneratedChatResponse> InferAsync(GeneratedChatRequest request, CancellationToken cancellationToken = default);
            }

            public interface IPluginCatalogService
            {
                IReadOnlyList<AiHostPluginManifest> GetPlugins();
            }

            public interface IScriptExecutionService
            {
                ScriptExecutionPlan CreatePlan(string scriptKind, string target, bool userApproved);
            }

            public interface IHardwareBudgetService
            {
                HardwareBudgetSnapshot GetBudget();
            }

            public interface IChatTemplateService
            {
                IReadOnlyList<ChatTemplateRule> GetTemplateRules();
            }

            /// <summary>
            /// Routes provider-compatible requests to the generated host's own local-file runner.
            /// This class intentionally does not call an upstream Ollama/LM Studio/OpenAI endpoint.
            /// </summary>
            public sealed class NativeModelFileInferenceProvider(
                IInferenceRunner runner,
                IOptions<AiHostRuntimeOptions> options) : IInferenceProvider
            {
                public string ProviderKind => "Native local-model-file provider";

                public async Task<GeneratedChatResponse> ChatAsync(GeneratedChatRequest request, CancellationToken cancellationToken = default)
                {
                    request.Model = NormalizeModel(request.Model, options.Value.DefaultModel);
                    return await runner.InferAsync(request, cancellationToken).ConfigureAwait(false);
                }

                public async Task<object> GenerateAsync(GeneratedModelActionRequest request, CancellationToken cancellationToken = default)
                {
                    var prompt = string.IsNullOrWhiteSpace(request.Prompt)
                        ? "LocalGPT generated AI host native-runner smoke test."
                        : request.Prompt;
                    var chat = new GeneratedChatRequest
                    {
                        Model = NormalizeModel(request.Model, options.Value.DefaultModel),
                        Messages = new List<GeneratedChatMessage>
                        {
                            new("user", prompt)
                        },
                        Stream = request.Stream,
                        Options = request.Options
                    };
                    var response = await runner.InferAsync(chat, cancellationToken).ConfigureAwait(false);
                    return new
                    {
                        model = response.Model,
                        created_at = response.CreatedAt,
                        response = response.Message.Content,
                        done = response.Done,
                        upstream_proxy = false
                    };
                }

                public string NormalizeModel(string? model, string fallbackModel)
                {
                    return string.IsNullOrWhiteSpace(model)
                        ? fallbackModel
                        : model.Trim();
                }
            }

            /// <summary>
            /// Loads compatible local model files through an approved native executable.
            /// Ollama manifests may be read as local file metadata, but the Ollama service is never called.
            /// </summary>
            public sealed class NativeModelFileProcessRunner(IOptions<AiHostRuntimeOptions> options) : IInferenceRunner
            {
                public string RunnerKind => "Native model-file process runner";

                public Task<RunnerCapabilityReport> GetCapabilityAsync(CancellationToken cancellationToken = default)
                {
                    var executable = ExpandPath(options.Value.NativeRunnerExecutable);
                    var executableReady = options.Value.AllowNativeRunner &&
                        !string.IsNullOrWhiteSpace(executable) &&
                        File.Exists(executable);

                    return Task.FromResult(new RunnerCapabilityReport(
                        NativeInferenceImplemented: executableReady,
                        SupportedFormats: ["gguf", "ollama-managed-gguf-blob", "onnx-planned", "safetensors-planned"],
                        MissingCapability: executableReady
                            ? string.Empty
                            : "Set AiHost:NativeRunnerExecutable to an approved native runner such as llama-cli/llama-server before chat/generate can execute model files.",
                        NextMilestone: "Configure NativeRunnerExecutable and ModelSearchRoots, verify /api/localgpt/runner/capability, then point LocalGPT DXAiChat at this host URL."));
                }

                public async Task<GeneratedChatResponse> InferAsync(GeneratedChatRequest request, CancellationToken cancellationToken = default)
                {
                    var model = NormalizeModel(request.Model, options.Value.DefaultModel);
                    if (!options.Value.AllowNativeRunner)
                        return BuildStatusResponse(model, "Native runner execution is disabled by AiHost:AllowNativeRunner. No upstream proxy fallback is used.");

                    var executable = ExpandPath(options.Value.NativeRunnerExecutable);
                    if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
                        return BuildStatusResponse(model, "Native runner executable is not configured. Set AiHost:NativeRunnerExecutable to a trusted llama.cpp-compatible runner. No upstream proxy fallback is used.");

                    var modelPath = ResolveModelPath(model);
                    if (string.IsNullOrWhiteSpace(modelPath))
                        return BuildStatusResponse(model, $"Could not resolve a compatible local model file for '{model}'. Add a ModelFileOverrides entry or a .gguf file under ModelSearchRoots. No upstream proxy fallback is used.");

                    var prompt = BuildPrompt(request);
                    var arguments = BuildRunnerArguments(modelPath, prompt, request.Options);
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeout.CancelAfter(TimeSpan.FromMinutes(20));

                    var startInfo = new ProcessStartInfo(executable)
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(executable) ?? AppContext.BaseDirectory
                    };

                    foreach (var argument in arguments)
                        startInfo.ArgumentList.Add(argument);

                    using var process = Process.Start(startInfo);
                    if (process is null)
                        return BuildStatusResponse(model, "The native runner process could not be started.");

                    try
                    {
                        var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
                        var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
                        await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                        var output = await outputTask.ConfigureAwait(false);
                        var error = await errorTask.ConfigureAwait(false);
                        var visible = string.IsNullOrWhiteSpace(output)
                            ? $"Native runner exited with code {process.ExitCode}. {error}".Trim()
                            : output.Trim();

                        return new GeneratedChatResponse(
                            model,
                            DateTimeOffset.UtcNow,
                            new GeneratedChatMessage("assistant", visible),
                            true);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        TryKill(process);
                        return BuildStatusResponse(model, "Native runner timed out after 20 minutes. Reduce context/output tokens or use a smaller model.");
                    }
                }

                private string? ResolveModelPath(string model)
                {
                    if (options.Value.ModelFileOverrides.TryGetValue(model, out var configuredPath))
                    {
                        var expanded = ExpandPath(configuredPath);
                        if (File.Exists(expanded))
                            return expanded;
                    }

                    foreach (var root in options.Value.ModelSearchRoots.Select(ExpandPath).Where(Directory.Exists))
                    {
                        var direct = ResolveDirectGguf(root, model);
                        if (!string.IsNullOrWhiteSpace(direct))
                            return direct;

                        var managed = ResolveOllamaManagedBlob(root, model);
                        if (!string.IsNullOrWhiteSpace(managed))
                            return managed;
                    }

                    return null;
                }

                public string? ResolveDirectGguf(string root, string model)
                {
                    var sanitized = model.Replace(':', '-').Replace('/', '-').Replace('\\', '-');
                    foreach (var candidate in new[]
                    {
                        Path.Combine(root, $"{model}.gguf"),
                        Path.Combine(root, $"{sanitized}.gguf"),
                        Path.Combine(root, model, $"{sanitized}.gguf")
                    })
                    {
                        if (File.Exists(candidate))
                            return candidate;
                    }

                    try
                    {
                        return Directory.EnumerateFiles(root, "*.gguf", SearchOption.AllDirectories)
                            .Take(2000)
                            .FirstOrDefault(path => Path.GetFileNameWithoutExtension(path).Contains(sanitized, StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        return null;
                    }
                }

                public string? ResolveOllamaManagedBlob(string root, string model)
                {
                    var (name, tag) = SplitModelName(model);
                    var manifest = Path.Combine(root, "manifests", "registry.ollama.ai", "library", name, tag);
                    if (!File.Exists(manifest) && Directory.Exists(Path.Combine(root, "manifests")))
                    {
                        manifest = Directory.EnumerateFiles(Path.Combine(root, "manifests"), tag, SearchOption.AllDirectories)
                            .FirstOrDefault(path => Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty).Equals(name, StringComparison.OrdinalIgnoreCase))
                            ?? string.Empty;
                    }

                    if (string.IsNullOrWhiteSpace(manifest) || !File.Exists(manifest))
                        return null;

                    try
                    {
                        using var document = JsonDocument.Parse(File.ReadAllText(manifest));
                        if (!document.RootElement.TryGetProperty("layers", out var layers) || layers.ValueKind != JsonValueKind.Array)
                            return null;

                        return layers
                            .EnumerateArray()
                            .Select(layer => layer.TryGetProperty("digest", out var digest) ? digest.GetString() : null)
                            .Where(digest => !string.IsNullOrWhiteSpace(digest))
                            .Select(digest => Path.Combine(root, "blobs", digest!.Replace(':', '-')))
                            .FirstOrDefault(File.Exists);
                    }
                    catch
                    {
                        return null;
                    }
                }

                public IReadOnlyList<string> BuildRunnerArguments(string modelPath, string prompt, GeneratedRequestOptions? requestOptions)
                {
                    var ctx = Math.Clamp(requestOptions?.NumCtx ?? 2048, 256, 262144);
                    var predict = Math.Clamp(requestOptions?.NumPredict ?? 1024, 1, 262144);
                    var gpuLayers = Math.Clamp(requestOptions?.NumGpu ?? 0, 0, 999);
                    var args = new List<string>
                    {
                        "--model", modelPath,
                        "--prompt", prompt,
                        "--ctx-size", ctx.ToString(CultureInfo.InvariantCulture),
                        "--n-predict", predict.ToString(CultureInfo.InvariantCulture),
                        "--gpu-layers", gpuLayers.ToString(CultureInfo.InvariantCulture)
                    };

                    if (requestOptions?.Temperature is { } temperature)
                    {
                        args.Add("--temp");
                        args.Add(temperature.ToString(CultureInfo.InvariantCulture));
                    }

                    return args;
                }

                public string BuildPrompt(GeneratedChatRequest request)
                {
                    var builder = new StringBuilder();
                    foreach (var message in request.Messages.Where(message => !string.IsNullOrWhiteSpace(message.Content)))
                        builder.Append(message.Role ?? "user").Append(": ").AppendLine(message.Content);
                    if (builder.Length == 0)
                        builder.AppendLine("user: Hello");
                    builder.Append("assistant: ");
                    return builder.ToString();
                }

                public (string Name, string Tag) SplitModelName(string model)
                {
                    var parts = model.Split(':', 2, StringSplitOptions.TrimEntries);
                    return (parts[0], parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : "latest");
                }

                public string NormalizeModel(string? model, string fallbackModel)
                {
                    return string.IsNullOrWhiteSpace(model)
                        ? fallbackModel
                        : model.Trim();
                }

                public string ExpandPath(string? path)
                {
                    if (string.IsNullOrWhiteSpace(path))
                        return string.Empty;

                    var expanded = path
                        .Replace("%LOCALAPPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StringComparison.OrdinalIgnoreCase)
                        .Replace("%USERPROFILE%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), StringComparison.OrdinalIgnoreCase);

                    return expanded.StartsWith("~/", StringComparison.Ordinal) || expanded.StartsWith("~\\", StringComparison.Ordinal)
                        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), expanded[2..])
                        : expanded;
                }

                public GeneratedChatResponse BuildStatusResponse(string model, string message)
                {
                    return new GeneratedChatResponse(
                        model,
                        DateTimeOffset.UtcNow,
                        new GeneratedChatMessage("assistant", message),
                        true);
                }

                public void TryKill(Process process)
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                        // Best effort only; the host should keep serving requests.
                    }
                }
            }

            public sealed class GeneratedPluginCatalogService : IPluginCatalogService
            {
                public IReadOnlyList<AiHostPluginManifest> GetPlugins()
                {
                    return
                    [
                        new("native-process-runner", "Native Process Runner", "1.0.0", "IInferenceRunner", true, "Loads compatible local model files through an approved executable; no upstream AI-host proxying."),
                        new("pythonnet-runner", "Python.NET Runner Boundary", "planned", "IInferenceRunner", false, "Requires approved Python runtime, PYTHONNET_PYDLL, package list, and GIL-safe service code."),
                        new("powershell-runner", "PowerShell Script Boundary", "planned", "IScriptExecutionService", false, "Requires explicit script files, safe directories, constrained runspace policy, and user approval."),
                        new("typescript-client-adapter", "TypeScript Client/Adapter Boundary", "planned", "ASP.NET Core static asset or script adapter", false, "Allowed only when embedded deliberately inside the .NET app as client assets or an approved script layer, not as the control-plane owner."),
                        new("onnx-runtime-runner", "ONNX Runtime Runner Boundary", "planned", "IInferenceRunner", false, "Only for compatible ONNX models; not a universal LLM replacement.")
                    ];
                }
            }

            public sealed class PermissionGatedScriptExecutionService(IOptions<AiHostRuntimeOptions> options) : IScriptExecutionService
            {
                public ScriptExecutionPlan CreatePlan(string scriptKind, string target, bool userApproved)
                {
                    var allowed = userApproved && (scriptKind.Equals("powershell", StringComparison.OrdinalIgnoreCase)
                        ? options.Value.AllowPowerShellScripts
                        : options.Value.AllowPythonNet);

                    return new ScriptExecutionPlan(
                        scriptKind,
                        target,
                        allowed,
                        allowed
                            ? "Approved script boundary. A real implementation must execute in a safe working directory with logs and cancellation."
                            : "Not approved. The generated host must not execute scripts until the user enables this path.");
                }
            }

            public sealed class GeneratedHardwareBudgetService(IOptions<AiHostRuntimeOptions> options) : IHardwareBudgetService
            {
                public HardwareBudgetSnapshot GetBudget()
                {
                    return new HardwareBudgetSnapshot(
                        TargetGpuLoadPercent: 85,
                        GpuLayers: options.Value.GpuLayers,
                        ContextTokens: options.Value.ContextTokens,
                        MaxParallelModels: 1,
                        Notes: "Sequential local-model runs are the default until profiling proves heavier concurrency is stable.");
                }
            }

            public sealed class GeneratedChatTemplateService : IChatTemplateService
            {
                public IReadOnlyList<ChatTemplateRule> GetTemplateRules()
                {
                    return
                    [
                        new("Harmony", "Separate analysis/commentary/final markers and always surface final visible text."),
                        new("ChatML", "Map role markers, stop sequences, and system/user/assistant boundaries per model."),
                        new("Plain prompt", "Use only for /api/generate style requests, not multi-turn chat without conversion."),
                        new("Tools", "Keep tool schemas typed and require user approval before native commands or downloads.")
                    ];
                }
            }

            public sealed record RunnerCapabilityReport(
                bool NativeInferenceImplemented,
                IReadOnlyList<string> SupportedFormats,
                string MissingCapability,
                string NextMilestone);

            public sealed record AiHostPluginManifest(
                string Id,
                string DisplayName,
                string Version,
                string Contract,
                bool Approved,
                string Notes);

            public sealed record ScriptExecutionPlan(
                string ScriptKind,
                string Target,
                bool AllowedToRun,
                string SafetyNote);

            public sealed record GeneratedScriptPlanRequest(
                string ScriptKind,
                string Target,
                bool UserApproved);

            public sealed record HardwareBudgetSnapshot(
                int TargetGpuLoadPercent,
                int GpuLayers,
                int ContextTokens,
                int MaxParallelModels,
                string Notes);

            public sealed record ChatTemplateRule(string Name, string Rule);
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateAiHostArchitectureServices projectName:{projectName}");
                return string.Empty;
            }
            
        }

    }
}
