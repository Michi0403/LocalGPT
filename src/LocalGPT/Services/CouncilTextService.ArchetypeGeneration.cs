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
        /// Generates archetype page razor as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="route">Route value supplied to the council text operation and used when producing its result.</param>
        /// <param name="title">Title value supplied to the council text operation and used when producing its result.</param>
        /// <param name="summary">Summary value supplied to the council text operation and used when producing its result.</param>
        /// <param name="areas">String dependency used by the council text workflow to provide the corresponding application capability.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateArchetypePageRazor(
            string route,
            string title,
            string summary,
            IReadOnlyList<string> areas, ILogger logger)
        {
            try
            {
                var rows = string.Join(
               "," + Environment.NewLine + "            ",
               areas.Select((area, index) => $$"""new("{{EscapeCSharpString(area, logger)}}", "{{(index == 0 ? "Ready" : "Planned")}}", "{{EscapeCSharpString(BuildArchetypeNextAction(area, logger), logger)}}")"""));

                return $$"""
            @page "{{route}}"
            @rendermode InteractiveServer

            <PageTitle>{{title}}</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation />

                <section class="generated-header">
                    <div>
                        <h1>{{title}}</h1>
                        <p>{{summary}}</p>
                    </div>
                    <DxButton Text="Refresh"
                              RenderStyle="ButtonRenderStyle.Primary"
                              RenderStyleMode="ButtonRenderStyleMode.Contained"
                              Click="Refresh" />
                </section>

                <DxGrid Data="@Rows"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedArchetypeRow.Area)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedArchetypeRow.Status)" Caption="Status" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedArchetypeRow.NextAction)" Caption="Next Action" />
                    </Columns>
                </DxGrid>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Implementation boundary" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Service rule" ColSpanMd="6">
                            <DxTextBox Text="Keep business logic in backend services." ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Persistence rule" ColSpanMd="6">
                            <DxTextBox Text="Persist durable user state in EF/SQLite." ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Safety" ColSpanMd="12">
                            <DxMemo Text="Generated sandbox page. Integrate into the real project only after user approval, build verification, and route-specific tests." Rows="3" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                IReadOnlyList<GeneratedArchetypeRow> Rows { get; set; } =
                [
                    {{rows}}
                ];

                void Refresh()
                {
                    Rows = Rows.ToArray();
                }

                public sealed record GeneratedArchetypeRow(string Area, string Status, string NextAction);
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateArchetypePageRazor route:{route} title:{title} summary:{summary} areas:{areas.ToString()}");
                return string.Empty;
            }
           
        }

        /// <summary>
        /// Builds archetype next action as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="area">Area value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildArchetypeNextAction(string area, ILogger logger)
        {
            try
            {
                return area switch
                {
                    "Minimum two members" => "Require at least two council members for feedback talks.",
                    "Replacement benchmark" => "Run benchmark task set with build validation.",
                    "Council feedback" => "Capture missing features and source requests in memory.",
                    "Webhook" or "Ingress" => "Add signature validation, idempotency, and retry logs.",
                    "Python.NET" => "Gate runtime loading behind explicit user approval.",
                    _ => $"Wire {area} through a typed service, route, and test."
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateArchetypePageRazor area:{area}");
                return string.Empty;
            }

        }

        /// <summary>
        /// Generates solution detail razor as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council text operation and used when producing its result.</param>
        /// <param name="isAiHostLab">Value indicating whether is AI host lab should apply to this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionDetailRazor(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab, ILogger logger)
        {
            try
            {
                if (isAiHostLab)
                {
                    return """
                @page "/api-console"
                @rendermode InteractiveServer
                @inject GeneratedHealthSummaryService HealthService

                <PageTitle>AI Host API Console</PageTitle>

                <main class="generated-shell">
                    <GeneratedNavigation IsAiHostLab="true" />

                    <section class="generated-header">
                        <div>
                            <h1>AI Host API Console</h1>
                        <p>Selected provider-compatible endpoints are shown as .NET routes backed by the local model-file runner contract.</p>
                        </div>
                    </section>

                    <DxGrid Data="@HealthService.GetEndpointCatalog()"
                            CssClass="generated-grid"
                            ShowSearchBox="true"
                            TextWrapEnabled="true">
                        <Columns>
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Method)" Caption="Method" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Route)" Caption="Route" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Purpose)" Caption="Purpose" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Boundary)" Caption="Boundary" />
                        </Columns>
                    </DxGrid>

                    <section class="generated-note">
                        <h2>Native-Runner Generate Request</h2>
                        <pre class="generated-code">POST /api/generate
                {
                  "model": "gpt-oss:20b",
                  "prompt": "Hello"
                }

                Response:
                {
                  "response": "NativeRunnerExecutable must point to an approved local runner before model-file inference starts.",
                  "done": true
                }</pre>
                    </section>
                </main>
                """;
                }

                var requestSummary = EscapeCSharpString(TrimForCodeComment(request.Prompt, 650, logger),logger);
                var consensusSummary = EscapeCSharpString(TrimForCodeComment(result.FinalAnswer, 800, logger), logger);
                return $$"""
                @page "/implementation-plan"
                @rendermode InteractiveServer
                @inject GeneratedHealthSummaryService HealthService

                <PageTitle>Implementation Plan</PageTitle>

                <main class="generated-shell">
                    <GeneratedNavigation IsAiHostLab="false" />

                    <section class="generated-header">
                        <div>
                            <h1>Implementation Plan</h1>
                            <p>A LocalGPT-style generated plan keeps code sandboxed, separates backend/frontend ownership, and makes review steps visible before integration.</p>
                        </div>
                    </section>

                    <DxGrid Data="@HealthService.GetEndpointCatalog()"
                            CssClass="generated-grid"
                            ShowSearchBox="true"
                            TextWrapEnabled="true">
                        <Columns>
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Method)" Caption="Step" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Route)" Caption="Owner" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Purpose)" Caption="Purpose" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Boundary)" Caption="Review Gate" />
                        </Columns>
                    </DxGrid>

                    <DxFormLayout CssClass="generated-form">
                        <DxFormLayoutGroup Caption="Council Grounding" ColSpanMd="12">
                            <DxFormLayoutItem Caption="Request" ColSpanMd="12">
                                <DxMemo Text="@RequestSummary" Rows="5" ReadOnly="true" />
                            </DxFormLayoutItem>
                            <DxFormLayoutItem Caption="Consensus" ColSpanMd="12">
                                <DxMemo Text="@CouncilSummary" Rows="6" ReadOnly="true" />
                            </DxFormLayoutItem>
                        </DxFormLayoutGroup>
                    </DxFormLayout>
                </main>

                @code {
                    string RequestSummary { get; } = "{{requestSummary}}";
                    string CouncilSummary { get; } = "{{consensusSummary}}";
                }
                """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateSolutionDetailRazor");
                return string.Empty;
            }
         
        }
        

        /// <summary>
        /// Generates solution service as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="isAiHostLab">Value indicating whether is AI host lab should apply to this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionService(string projectName, bool isAiHostLab, ILogger logger)
        {
            try
            {
                var cards = isAiHostLab
               ? """
                          new("REST API Shell", "NativeFileReady", "Map version, tags, ps, show, pull, push, create, copy, delete, generate, chat, and embed routes.", "The host owns route handling and never proxies chat/generate to upstream Ollama."),
                          new("Model Catalog", "SourceBacked", "Represent model names, tags, details, local file paths, download candidates, and runner status in .NET models.", "Ollama manifests may be read as local file metadata; the Ollama service is not used for inference."),
                          new("Native Runner", "Configurable", "Run compatible local model files through an approved native executable such as llama.cpp.", "Native AI hosts rely on model loaders, runner paths, native payloads, and hardware-specific backends."),
                          new("Model Downloads", "Ready", "Expose a /model-downloads page and /api/pull planning response.", "Pull planning is safe and explicit; it does not download binaries by itself."),
                          new("Settings", "Ready", "Expose generated runtime settings for base URI, default model, context, GPU layers, and pull policy.", "Persist real settings through EF/SQLite only after user approval."),
                          new("DevExpress UI", "Ready", "Use grids/forms for model inventory, compatibility notes, settings, downloads, and endpoint tests.", "This is the realistic Blazor/DevExpress value of the experiment.")
                  """
               : """
                          new("AI Council Request", "Ready", "Capture the feature idea, council consensus, and implementation poll result.", "This mirrors the LocalGPT workflow for user-approved feature development."),
                          new("Knowledge Grounding", "SourceBacked", "Read verified council knowledge before generating code.", "Use SQLite knowledge entries instead of bloated prompt context."),
                          new("Blazor/DevExpress UI", "Ready", "Generate real routable Razor pages with navigation, grids, forms, and review notes.", "Do not return string-builder fake pages for frontend work."),
                          new("Artifact Pipeline", "Ready", "Produce downloadable .razor, .cs, .dll, and solution zip artifacts.", "Artifacts stay sandboxed until Michi approves integration."),
                          new("Integration Review", "Required", "Build, inspect, and review generated code before copying into LocalGPT or TacosPortalOpen.", "Generated solutions are prototypes, not automatic self-expansion.")
                  """;

                var endpoints = isAiHostLab
                    ? """
                          new("GET", "/api/version", "Return a compact provider-compatible version document.", "Safe pure .NET response."),
                          new("GET", "/api/tags", "Return model catalog rows shaped like local AI host tags.", "Catalog includes direct local model-file candidates."),
                          new("GET", "/api/ps", "Return currently loaded model rows for runner-status UI.", "Runner-owned sessions are reported here when configured."),
                          new("POST", "/api/show", "Return model metadata, parameters, template, and details.", "Source-shaped but generated data only."),
                          new("POST", "/api/pull", "Return a safe model-download plan.", "Does not download model binaries without a real adapter."),
                          new("POST", "/api/push", "Return a registry-upload plan.", "No registry credentials or upload path included."),
                          new("POST", "/api/create", "Return a model-create plan.", "No Modelfile build happens in this sandbox."),
                          new("POST", "/api/copy", "Return a model-copy plan.", "No local blob mutation happens."),
                          new("DELETE", "/api/delete", "Return a model-delete plan.", "No file deletion happens."),
                          new("POST", "/api/generate", "Run prompt generation through the native model-file runner contract.", "No upstream AI-host proxying is allowed."),
                          new("POST", "/api/chat", "Run chat requests through the native model-file runner contract.", "Context/cache ownership belongs to this host and its runner adapter."),
                          new("POST", "/api/embed", "Return a tiny deterministic vector.", "Not a real embedding model.")
                  """
                    : """
                          new("1", "Backend service", "Create the durable service and data model first.", "Build and test before UI integration."),
                          new("2", "Blazor page", "Add a routable Razor page with DevExpress controls and navigation.", "Review against LocalGPT/TacosPortalOpen patterns."),
                          new("3", "SQLite knowledge", "Persist decisions, logs, and generated artifacts as approved or unverified.", "User approval decides trust state."),
                          new("4", "Artifact download", "Expose generated files through safe HTTP download routes.", "No binary blobs inside chat messages."),
                          new("5", "Frontend smoke", "Exercise the generated workflow like a user in WebView2.", "Do not rely only on backend APIs.")
                  """;
                var serviceInterfaces = isAiHostLab
                    ? " : IModelCatalogService, IModelTransferService"
                    : string.Empty;

                return $$"""
            using {{projectName}}.Models;

            namespace {{projectName}}.Services;

            /// <summary>
            /// Provides deterministic health cards for the generated LocalGPT sandbox solution.
            /// </summary>
            public sealed class GeneratedHealthSummaryService{{serviceInterfaces}}
            {
                /// <summary>
                /// Returns the cards displayed by the generated DevExpress grids.
                /// </summary>
                public IReadOnlyList<GeneratedHealthCard> GetCards()
                {
                    return
                    [
                {{cards}}
                    ];
                }

                /// <summary>
                /// Returns sample model entries for compatibility UI and API responses.
                /// </summary>
                public IReadOnlyList<GeneratedModelCard> GetModelCatalog()
                {
                    return
                    [
                        new("gpt-oss:20b", "Local model-file candidate resolved from configured search roots.", 0, true),
                        new("qwen3-coder:30b", "Local model-file candidate resolved from configured search roots.", 0, true),
                        new("deepseek-r1:8b", "Local model-file candidate resolved from configured search roots.", 0, true),
                        new("native-runner:configured", "Requires NativeRunnerExecutable plus compatible local model files.", 0, true)
                    ];
                }

                /// <summary>
                /// Returns AI-host-compatible local model rows for the /api/tags route.
                /// </summary>
                public IReadOnlyList<GeneratedAiHostModelTag> GetAiHostTags()
                {
                    return
                    [
                        new("gpt-oss:20b", "gpt-oss", "20B", "Q4_K_M", 0),
                        new("qwen3-coder:30b", "qwen", "30B", "Q4_K_M", 0),
                        new("deepseek-r1:8b", "deepseek", "8B", "Q4_K_M", 0)
                    ];
                }

                /// <summary>
                /// Returns the model rows shown as currently loaded by the generated /api/ps route.
                /// </summary>
                public IReadOnlyList<GeneratedAiHostModelTag> GetRunningModels()
                {
                    return [];
                }

                /// <summary>
                /// Returns visible runtime log rows for the generated AI host control-plane logs page.
                /// </summary>
                public IReadOnlyList<GeneratedEndpointCard> GetRuntimeLogRows()
                {
                    return
                    [
                        new("Info", "Runner", "Native model-file runner is the first-class inference boundary.", "Configure NativeRunnerExecutable and a compatible local model file before long tests."),
                        new("Info", "Downloads", "Pull requests currently create safe plans and progress shapes.", "Attach IModelTransferService before downloading model binaries."),
                        new("Warning", "Hardware", "Generated lab does not own GPU scheduling or VRAM planning.", "Implement IHardwareBudgetService before heavy local runs."),
                        new("Info", "Templates", "Harmony/thinking parsing belongs in IChatTemplateService.", "Keep model formatting adaptive per model.")
                    ];
                }

                /// <summary>
                /// Returns model metadata shaped like a provider-compatible show route.
                /// </summary>
                public object GetModelDetails(GeneratedModelActionRequest request)
                {
                    var model = NormalizeModel(request.Model);
                    return new
                    {
                        license = "Generated LocalGPT lab metadata. Needs verification before production use.",
                        modelfile = $"FROM {model}\nPARAMETER num_ctx 2048",
                        parameters = "num_ctx 2048\nnum_predict 512",
                        template = "{" + "{ .Prompt }" + "}",
                        details = new GeneratedAiHostModelDetails("gguf", "generated", "0B", "none"),
                        model_info = new
                        {
                            architecture = "generated-dotnet-control-plane",
                            native_runner_attached = false
                        }
                    };
                }

                /// <summary>
                /// Returns routes, steps, or boundaries shown by the generated detail page.
                /// </summary>
                public IReadOnlyList<GeneratedEndpointCard> GetEndpointCatalog()
                {
                    return
                    [
                {{endpoints}}
                    ];
                }

                /// <summary>
                /// Returns candidate model downloads displayed on the generated download page.
                /// </summary>
                public IReadOnlyList<GeneratedModelDownloadCandidate> GetDownloadCandidates()
                {
                    return
                    [
                        new("gpt-oss:20b", "Local provider", "http://localhost:11434", "LocalGPT debugging and balanced reasoning", "/api/pull", "Pull only when GPU/VRAM policy allows it."),
                        new("gemma3:27b", "Local provider", "http://localhost:11434", "Longer general review and writing", "/api/pull", "Use one model at a time on 24 GB VRAM."),
                        new("qwen3-coder:30b", "Local provider", "http://localhost:11434", "Code review and larger code-generation tests", "/api/pull", "Prefer CPU or reduced GPU layers after driver instability."),
                        new("deepseek-r1:8b", "Local provider", "http://localhost:11434", "Small reasoning checks", "/api/pull", "May spend short budgets on thinking."),
                        new("hf://models", "HuggingFace catalog", "https://huggingface.co/models", "Browse user-selected model cards and map compatible files into an approved download plan.", "/api/pull", "Never auto-download. Ask the user to approve license, file size, quantization, and target path."),
                        new("github://model-releases", "GitHub Releases", "https://github.com", "Represent model or runner release URLs selected by the user.", "/api/pull", "Only download from explicit user-selected release assets.")
                    ];
                }

                /// <summary>
                /// Returns chat template and thinking-format rows that a real local runner adapter would use.
                /// </summary>
                public IReadOnlyList<GeneratedEndpointCard> GetTemplateRows()
                {
                    return
                    [
                        new("Harmony", "model family metadata", "Parse final/user-visible text separately from hidden thinking markers.", "Show visible thinking summaries only when policy and model output permit it."),
                        new("ChatML", "template field", "Adapt role markers and stop sequences per model.", "Never assume all models share one prompt format."),
                        new("OpenAI-compatible", "/v1/chat/completions", "Map local provider output to common client contracts.", "Keep provider-specific options behind typed adapter settings."),
                        new("Plain prompt", "/api/generate", "Support simple generation requests for scripting and smoke tests.", "Warn when a chat request is being downgraded to plain prompt completion.")
                    ];
                }

                /// <summary>
                /// Returns hardware and scheduling rows for a safe generated control-plane prototype.
                /// </summary>
                public IReadOnlyList<GeneratedEndpointCard> GetHardwareBudgetRows()
                {
                    return
                    [
                        new("GPU", "80-90% target", "Throttle heavy runs and avoid sustained full-load peaks.", "Driver stability is a user-facing reliability requirement."),
                        new("VRAM", "24 GB class", "Queue large models one at a time unless profiling proves a safe combination.", "Council runs should favor sequential turns over concurrent large models."),
                        new("Context", "model/profile-based", "Expose context and output token budgets in settings.", "Huge defaults can stall local hardware; make presets explicit."),
                        new("Downloads", "user approved", "Require explicit user selection for HuggingFace/GitHub/provider downloads.", "Catalog browsing is not permission to mutate the machine.")
                    ];
                }

                /// <summary>
                /// Returns generated runtime settings for the settings page.
                /// </summary>
                public GeneratedAiHostSettings GetSettings()
                {
                    return new GeneratedAiHostSettings
                    {
                        BaseUri = "native local model files",
                        DefaultModel = "gpt-oss:20b",
                        KeepAlive = "0s",
                        ContextTokens = 2048,
                        GpuLayers = 20,
                        NativeRunnerAttached = true,
                        AllowPullPlanning = true
                    };
                }

                /// <summary>
                /// Builds the settings summary shown in the generated settings page.
                /// </summary>
                public string BuildSettingsSummary()
                {
                    var settings = GetSettings();
                    return $"Model source: {settings.BaseUri}\nDefault model: {settings.DefaultModel}\n" +
                        $"Context tokens: {settings.ContextTokens}\nGPU layers: {settings.GpuLayers}\n" +
                        "Native runner execution uses configured local model files and never proxies to an upstream AI host.";
                }

                /// <summary>
                /// Creates a visible chat transcript for the generated chat page through the local runner contract.
                /// </summary>
                public string CreateChatTranscript(string model, string prompt)
                {
                    var request = new GeneratedChatRequest
                    {
                        Model = NormalizeModel(model),
                        Messages =
                        [
                            new GeneratedChatMessage("user", string.IsNullOrWhiteSpace(prompt) ? "Hello" : prompt)
                        ]
                    };
                    var response = CreateChatResponse(request);
                    return $"POST /api/chat\nModel: {request.Model}\nUser: {request.Messages[0].Content}\nAssistant: {response.Message.Content}\nDone: {response.Done}";
                }

                /// <summary>
                /// Creates a safe model-pull plan without downloading model files.
                /// </summary>
                public GeneratedAiHostOperation CreatePullPlan(GeneratedModelActionRequest request)
                {
                    return new GeneratedAiHostOperation(
                        "planned",
                        NormalizeModel(request.Model),
                        "/api/pull",
                        true,
                        "This response mirrors provider pull progress shape but does not download model binaries.");
                }

                /// <summary>
                /// Creates a safe non-mutating operation response for registry and model-management routes.
                /// </summary>
                public GeneratedAiHostOperation CreateOperation(string operation, string? model)
                {
                    return new GeneratedAiHostOperation(
                        "planned",
                        NormalizeModel(model),
                        $"/api/{operation}",
                        true,
                        $"The generated lab records a {operation} plan but does not mutate model storage.");
                }

                /// <summary>
                /// Creates a safe copy plan for the /api/copy route.
                /// </summary>
                public GeneratedAiHostOperation CreateCopyPlan(GeneratedModelCopyRequest request)
                {
                    var from = NormalizeModel(request.Source);
                    var to = NormalizeModel(request.Destination);
                    return new GeneratedAiHostOperation(
                        "planned",
                        $"{from} -> {to}",
                        "/api/copy",
                        true,
                        "The generated lab records copy intent but does not mutate model storage.");
                }

                /// <summary>
                /// Creates a deterministic non-inference generate response.
                /// </summary>
                public object CreateGenerateResponse(GeneratedModelActionRequest request)
                {
                    return new
                    {
                        model = NormalizeModel(request.Model),
                        created_at = DateTimeOffset.UtcNow,
                        response = "NativeRunnerExecutable must point to an approved local runner before model-file inference starts. No upstream proxy fallback is used.",
                        done = true
                    };
                }

                /// <summary>
                /// Creates a deterministic non-inference chat response.
                /// </summary>
                public GeneratedChatResponse CreateChatResponse(GeneratedChatRequest request)
                {
                    return new GeneratedChatResponse(
                        NormalizeModel(request.Model),
                        DateTimeOffset.UtcNow,
                        new GeneratedChatMessage("assistant", "Generated lab response only. No native AI runner is attached."),
                        true);
                }

                /// <summary>
                /// Creates a deterministic tiny embedding response for plumbing tests.
                /// </summary>
                public object CreateEmbeddingResponse(GeneratedModelActionRequest request)
                {
                    return new
                    {
                        model = NormalizeModel(request.Model),
                        embeddings = new[] { new[] { 0.0, 0.25, 0.5, 0.75 } },
                        done = true
                    };
                }

                public string NormalizeModel(string? model)
                {
                    return string.IsNullOrWhiteSpace(model)
                        ? "gpt-oss:20b"
                        : model.Trim();
                }
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionService projectName:{projectName} isAiHostLab:{isAiHostLab}");
                return string.Empty;
            }
           
        }

    }
}
