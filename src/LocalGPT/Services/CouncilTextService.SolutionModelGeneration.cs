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
        /// Generates solution model as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionModel(string projectName, ILogger logger)
        {
            try
            {
                return $$"""
            using System.Text.Json.Serialization;

            namespace {{projectName}}.Models;

            /// <summary>
            /// Describes one status card rendered by the generated LocalGPT workbench.
            /// </summary>
            public sealed class GeneratedHealthCard
            {
                /// <summary>
                /// Creates a generated health card.
                /// </summary>
                public GeneratedHealthCard(string area, string status, string nextAction, string detail)
                {
                    Area = area;
                    Status = status;
                    NextAction = nextAction;
                    Detail = detail;
                }

                /// <summary>
                /// Gets the subsystem or concern represented by the card.
                /// </summary>
                public string Area { get; }

                /// <summary>
                /// Gets the current generated status.
                /// </summary>
                public string Status { get; }

                /// <summary>
                /// Gets the next suggested action.
                /// </summary>
                public string NextAction { get; }

                /// <summary>
                /// Gets the implementation detail shown in expanded views.
                /// </summary>
                public string Detail { get; }
            }

            /// <summary>
            /// Describes one model or adapter row in the generated AI host control-plane lab.
            /// </summary>
            public sealed class GeneratedModelCard
            {
                /// <summary>
                /// Creates a generated model compatibility row.
                /// </summary>
                public GeneratedModelCard(string name, string status, long sizeMegabytes, bool supportsNativeInference)
                {
                    Name = name;
                    Status = status;
                    SizeMegabytes = sizeMegabytes;
                    SupportsNativeInference = supportsNativeInference;
                }

                /// <summary>
                /// Gets the model, adapter, or backend name.
                /// </summary>
                public string Name { get; }

                /// <summary>
                /// Gets the current compatibility status.
                /// </summary>
                public string Status { get; }

                /// <summary>
                /// Gets the sample size in megabytes, or zero when the lab does not own a model binary.
                /// </summary>
                public long SizeMegabytes { get; }

                /// <summary>
                /// Gets whether this row represents a real native inference path.
                /// </summary>
                public bool SupportsNativeInference { get; }
            }

            /// <summary>
            /// Describes an API endpoint or implementation step in the generated solution.
            /// </summary>
            public sealed class GeneratedEndpointCard
            {
                /// <summary>
                /// Creates a generated endpoint or workflow row.
                /// </summary>
                public GeneratedEndpointCard(string method, string route, string purpose, string boundary)
                {
                    Method = method;
                    Route = route;
                    Purpose = purpose;
                    Boundary = boundary;
                }

                /// <summary>
                /// Gets the HTTP method or ordered workflow step.
                /// </summary>
                public string Method { get; }

                /// <summary>
                /// Gets the route, owner, or target area.
                /// </summary>
                public string Route { get; }

                /// <summary>
                /// Gets the row purpose.
                /// </summary>
                public string Purpose { get; }

                /// <summary>
                /// Gets the safety or implementation boundary.
                /// </summary>
                public string Boundary { get; }
            }

            /// <summary>
            /// Describes one AI-host-compatible model row returned by generated catalog routes.
            /// </summary>
            public sealed class GeneratedAiHostModelTag
            {
                /// <summary>
                /// Creates a generated AI-host-compatible model row.
                /// </summary>
                public GeneratedAiHostModelTag(
                    string name,
                    string family,
                    string parameterSize,
                    string quantizationLevel,
                    long size)
                {
                    Name = name;
                    Model = name;
                    ModifiedAt = DateTimeOffset.UtcNow;
                    Size = size;
                    Digest = $"generated-{Math.Abs(name.GetHashCode(StringComparison.Ordinal)):x}";
                    Details = new GeneratedAiHostModelDetails("gguf", family, parameterSize, quantizationLevel);
                }

                /// <summary>
                /// Gets the provider-compatible model name field.
                /// </summary>
                [JsonPropertyName("name")]
                public string Name { get; }

                /// <summary>
                /// Gets the model identifier.
                /// </summary>
                [JsonPropertyName("model")]
                public string Model { get; }

                /// <summary>
                /// Gets the generated modification timestamp.
                /// </summary>
                [JsonPropertyName("modified_at")]
                public DateTimeOffset ModifiedAt { get; }

                /// <summary>
                /// Gets the generated model size in bytes.
                /// </summary>
                [JsonPropertyName("size")]
                public long Size { get; }

                /// <summary>
                /// Gets a deterministic generated digest placeholder.
                /// </summary>
                [JsonPropertyName("digest")]
                public string Digest { get; }

                /// <summary>
                /// Gets model detail metadata.
                /// </summary>
                [JsonPropertyName("details")]
                public GeneratedAiHostModelDetails Details { get; }
            }

            /// <summary>
            /// Describes generated AI-host-compatible model details.
            /// </summary>
            public sealed class GeneratedAiHostModelDetails
            {
                /// <summary>
                /// Creates generated model details.
                /// </summary>
                public GeneratedAiHostModelDetails(
                    string format,
                    string family,
                    string parameterSize,
                    string quantizationLevel)
                {
                    Format = format;
                    Family = family;
                    Families = [family];
                    ParameterSize = parameterSize;
                    QuantizationLevel = quantizationLevel;
                }

                /// <summary>
                /// Gets the generated model file format.
                /// </summary>
                [JsonPropertyName("format")]
                public string Format { get; }

                /// <summary>
                /// Gets the generated primary model family.
                /// </summary>
                [JsonPropertyName("family")]
                public string Family { get; }

                /// <summary>
                /// Gets generated model families.
                /// </summary>
                [JsonPropertyName("families")]
                public IReadOnlyList<string> Families { get; }

                /// <summary>
                /// Gets the generated parameter size label.
                /// </summary>
                [JsonPropertyName("parameter_size")]
                public string ParameterSize { get; }

                /// <summary>
                /// Gets the generated quantization label.
                /// </summary>
                [JsonPropertyName("quantization_level")]
                public string QuantizationLevel { get; }
            }

            /// <summary>
            /// Represents a generated AI host action request.
            /// </summary>
            public sealed class GeneratedModelActionRequest
            {
                /// <summary>
                /// Gets or sets the model name.
                /// </summary>
                [JsonPropertyName("model")]
                public string? Model { get; set; }

                /// <summary>
                /// Gets or sets the prompt for generate/embed-style routes.
                /// </summary>
                [JsonPropertyName("prompt")]
                public string? Prompt { get; set; }

                /// <summary>
                /// Gets or sets whether the caller requested streaming.
                /// </summary>
                [JsonPropertyName("stream")]
                public bool Stream { get; set; }

                /// <summary>
                /// Gets or sets Ollama-compatible request options.
                /// </summary>
                [JsonPropertyName("options")]
                public GeneratedRequestOptions? Options { get; set; }
            }

            /// <summary>
            /// Represents Ollama-compatible request options accepted by the generated host.
            /// </summary>
            public sealed class GeneratedRequestOptions
            {
                /// <summary>
                /// Gets or sets the requested context token budget.
                /// </summary>
                [JsonPropertyName("num_ctx")]
                public int? NumCtx { get; set; }

                /// <summary>
                /// Gets or sets the requested output token budget.
                /// </summary>
                [JsonPropertyName("num_predict")]
                public int? NumPredict { get; set; }

                /// <summary>
                /// Gets or sets the requested GPU layer count.
                /// </summary>
                [JsonPropertyName("num_gpu")]
                public int? NumGpu { get; set; }

                /// <summary>
                /// Gets or sets the requested sampling temperature.
                /// </summary>
                [JsonPropertyName("temperature")]
                public float? Temperature { get; set; }
            }

            /// <summary>
            /// Represents a generated AI host copy request.
            /// </summary>
            public sealed class GeneratedModelCopyRequest
            {
                /// <summary>
                /// Gets or sets the source model.
                /// </summary>
                [JsonPropertyName("source")]
                public string? Source { get; set; }

                /// <summary>
                /// Gets or sets the destination model.
                /// </summary>
                [JsonPropertyName("destination")]
                public string? Destination { get; set; }
            }

            /// <summary>
            /// Represents a generated AI host chat request.
            /// </summary>
            public sealed class GeneratedChatRequest
            {
                /// <summary>
                /// Gets or sets the requested model.
                /// </summary>
                [JsonPropertyName("model")]
                public string? Model { get; set; }

                /// <summary>
                /// Gets or sets the chat messages.
                /// </summary>
                [JsonPropertyName("messages")]
                public List<GeneratedChatMessage> Messages { get; set; } = [];

                /// <summary>
                /// Gets or sets whether the caller requested streaming.
                /// </summary>
                [JsonPropertyName("stream")]
                public bool Stream { get; set; }

                /// <summary>
                /// Gets or sets Ollama-compatible request options.
                /// </summary>
                [JsonPropertyName("options")]
                public GeneratedRequestOptions? Options { get; set; }
            }

            /// <summary>
            /// Represents a generated AI host chat message.
            /// </summary>
            public sealed class GeneratedChatMessage
            {
                /// <summary>
                /// Creates an empty generated chat message.
                /// </summary>
                public GeneratedChatMessage()
                {
                }

                /// <summary>
                /// Creates a generated chat message.
                /// </summary>
                public GeneratedChatMessage(string role, string content)
                {
                    Role = role;
                    Content = content;
                }

                /// <summary>
                /// Gets or sets the chat role.
                /// </summary>
                [JsonPropertyName("role")]
                public string Role { get; set; } = string.Empty;

                /// <summary>
                /// Gets or sets the chat content.
                /// </summary>
                [JsonPropertyName("content")]
                public string Content { get; set; } = string.Empty;
            }

            /// <summary>
            /// Represents a generated AI host chat response with typed properties for Razor and service code.
            /// </summary>
            public sealed class GeneratedChatResponse
            {
                /// <summary>
                /// Creates a generated chat response.
                /// </summary>
                public GeneratedChatResponse(
                    string model,
                    DateTimeOffset createdAt,
                    GeneratedChatMessage message,
                    bool done)
                {
                    Model = model;
                    CreatedAt = createdAt;
                    Message = message;
                    Done = done;
                }

                /// <summary>
                /// Gets the model name used by the generated response.
                /// </summary>
                [JsonPropertyName("model")]
                public string Model { get; }

                /// <summary>
                /// Gets the generated response timestamp.
                /// </summary>
                [JsonPropertyName("created_at")]
                public DateTimeOffset CreatedAt { get; }

                /// <summary>
                /// Gets the generated assistant message.
                /// </summary>
                [JsonPropertyName("message")]
                public GeneratedChatMessage Message { get; }

                /// <summary>
                /// Gets whether the generated response is complete.
                /// </summary>
                [JsonPropertyName("done")]
                public bool Done { get; }
            }

            /// <summary>
            /// Describes a generated model download planning row.
            /// </summary>
            public sealed class GeneratedModelDownloadCandidate
            {
                /// <summary>
                /// Creates a generated download candidate.
                /// </summary>
                public GeneratedModelDownloadCandidate(
                    string name,
                    string sourceType,
                    string sourceUrl,
                    string recommendedFor,
                    string downloadRoute,
                    string safetyNote)
                {
                    Name = name;
                    SourceType = sourceType;
                    SourceUrl = sourceUrl;
                    RecommendedFor = recommendedFor;
                    DownloadRoute = downloadRoute;
                    SafetyNote = safetyNote;
                }

                /// <summary>
                /// Gets the candidate model name.
                /// </summary>
                public string Name { get; }

                /// <summary>
                /// Gets the catalog or provider type.
                /// </summary>
                public string SourceType { get; }

                /// <summary>
                /// Gets the catalog URL or provider base URI.
                /// </summary>
                public string SourceUrl { get; }

                /// <summary>
                /// Gets the recommended use case.
                /// </summary>
                public string RecommendedFor { get; }

                /// <summary>
                /// Gets the route used to plan the download.
                /// </summary>
                public string DownloadRoute { get; }

                /// <summary>
                /// Gets the safety note for the generated lab.
                /// </summary>
                public string SafetyNote { get; }
            }

            /// <summary>
            /// Holds generated AI host lab settings shown in the DevExpress form.
            /// </summary>
            public sealed class GeneratedAiHostSettings
            {
                /// <summary>
                /// Gets or sets the local model source summary.
                /// </summary>
                public string BaseUri { get; set; } = "native local model files";

                /// <summary>
                /// Gets or sets the default model for generated request examples.
                /// </summary>
                public string DefaultModel { get; set; } = "gpt-oss:20b";

                /// <summary>
                /// Gets or sets the generated keep-alive value.
                /// </summary>
                public string KeepAlive { get; set; } = "5m";

                /// <summary>
                /// Gets or sets the generated context token budget.
                /// </summary>
                public int ContextTokens { get; set; } = 2048;

                /// <summary>
                /// Gets or sets the generated GPU layer budget.
                /// </summary>
                public int GpuLayers { get; set; } = 0;

                /// <summary>
                /// Gets or sets whether a native runner is attached.
                /// </summary>
                public bool NativeRunnerAttached { get; set; }

                /// <summary>
                /// Gets or sets whether pull planning is enabled.
                /// </summary>
                public bool AllowPullPlanning { get; set; } = true;
            }

            /// <summary>
            /// Describes a generated AI-host-compatible operation result.
            /// </summary>
            public sealed class GeneratedAiHostOperation
            {
                /// <summary>
                /// Creates a generated operation result.
                /// </summary>
                public GeneratedAiHostOperation(
                    string status,
                    string model,
                    string route,
                    bool done,
                    string detail)
                {
                    Status = status;
                    Model = model;
                    Route = route;
                    Done = done;
                    Detail = detail;
                }

                /// <summary>
                /// Gets the generated operation status.
                /// </summary>
                [JsonPropertyName("status")]
                public string Status { get; }

                /// <summary>
                /// Gets the affected model or model mapping.
                /// </summary>
                [JsonPropertyName("model")]
                public string Model { get; }

                /// <summary>
                /// Gets the route that produced the result.
                /// </summary>
                [JsonPropertyName("route")]
                public string Route { get; }

                /// <summary>
                /// Gets whether the generated operation is complete.
                /// </summary>
                [JsonPropertyName("done")]
                public bool Done { get; }

                /// <summary>
                /// Gets the generated explanation.
                /// </summary>
                [JsonPropertyName("detail")]
                public string Detail { get; }
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionModel projectName:{projectName}");
                return string.Empty;
            }

        }
        /// <summary>
        /// Generates navigation icon svgs as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The collection produced by the operation.</returns>
        public IReadOnlyList<(string FileName, string Svg)>? GenerateNavigationIconSvgs( ILogger logger)
        {
            try
            {
                return [
            ("dashboard-line.svg", """
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-labelledby="dashboard-line-title">
                  <title id="dashboard-line-title">Dashboard line icon</title>
                  <g fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                    <rect x="4" y="4" width="7" height="7" rx="1.5" />
                    <rect x="13" y="4" width="7" height="4.8" rx="1.5" />
                    <rect x="13" y="11.2" width="7" height="8.8" rx="1.5" />
                    <rect x="4" y="13" width="7" height="7" rx="1.5" />
                  </g>
                </svg>
                """),
            ("dashboard-solid.svg", """
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-labelledby="dashboard-solid-title">
                  <title id="dashboard-solid-title">Dashboard solid icon</title>
                  <path fill="currentColor" opacity=".22" d="M4 5.6A1.6 1.6 0 0 1 5.6 4h4.8A1.6 1.6 0 0 1 12 5.6v4.8A1.6 1.6 0 0 1 10.4 12H5.6A1.6 1.6 0 0 1 4 10.4z" />
                  <path fill="currentColor" d="M13 5.6A1.6 1.6 0 0 1 14.6 4h4.8A1.6 1.6 0 0 1 21 5.6v3A1.6 1.6 0 0 1 19.4 10h-4.8A1.6 1.6 0 0 1 13 8.6z" />
                  <path fill="currentColor" d="M13 12.6a1.6 1.6 0 0 1 1.6-1.6h4.8a1.6 1.6 0 0 1 1.6 1.6v5.8a1.6 1.6 0 0 1-1.6 1.6h-4.8a1.6 1.6 0 0 1-1.6-1.6z" />
                  <path fill="currentColor" d="M4 14.6A1.6 1.6 0 0 1 5.6 13h4.8a1.6 1.6 0 0 1 1.6 1.6v4.8a1.6 1.6 0 0 1-1.6 1.6H5.6A1.6 1.6 0 0 1 4 19.4z" />
                </svg>
                """),
            ("catalog-line.svg", """
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-labelledby="catalog-line-title">
                  <title id="catalog-line-title">Catalog line icon</title>
                  <g fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M5 5.5A2.5 2.5 0 0 1 7.5 3H19v15.5H7.5A2.5 2.5 0 0 0 5 21z" />
                    <path d="M5 18.5A2.5 2.5 0 0 0 7.5 21H19" />
                    <path d="M9 7h6" />
                    <path d="M9 10.5h5" />
                    <path d="M9 14h4" />
                  </g>
                </svg>
                """),
            ("catalog-solid.svg", """
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-labelledby="catalog-solid-title">
                  <title id="catalog-solid-title">Catalog solid icon</title>
                  <path fill="currentColor" opacity=".2" d="M5 5.5A2.5 2.5 0 0 1 7.5 3H19v15.5H7.5A2.5 2.5 0 0 0 5 21z" />
                  <path fill="currentColor" d="M7.5 3A2.5 2.5 0 0 0 5 5.5V21a2.5 2.5 0 0 1 2.5-2.5H19V3zm1.8 4.2h6.4v1.7H9.3zm0 3.8h5.4v1.7H9.3zm0 3.8h4.2v1.7H9.3z" />
                </svg>
                """),
            ("detail-line.svg", """
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-labelledby="detail-line-title">
                  <title id="detail-line-title">Detail line icon</title>
                  <g fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                    <rect x="4" y="4" width="16" height="16" rx="2.2" />
                    <path d="M8 8h8" />
                    <path d="M8 12h8" />
                    <path d="M8 16h4.5" />
                    <path d="m14.8 16.1 1.3 1.3 2.3-2.7" />
                  </g>
                </svg>
                """),
            ("detail-solid.svg", """
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-labelledby="detail-solid-title">
                  <title id="detail-solid-title">Detail solid icon</title>
                  <path fill="currentColor" opacity=".2" d="M4 6.2A2.2 2.2 0 0 1 6.2 4h11.6A2.2 2.2 0 0 1 20 6.2v11.6a2.2 2.2 0 0 1-2.2 2.2H6.2A2.2 2.2 0 0 1 4 17.8z" />
                  <path fill="currentColor" d="M8 7.4h8v1.8H8zm0 3.7h8v1.8H8zm0 3.7h4.5v1.8H8z" />
                  <path fill="currentColor" d="m15 16.3 1.1 1.1 2.6-3 .9 1-3.5 4L14 17.3z" />
                </svg>
                """)
        ];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateNavigationIconSvgs");
                return null;
            }

        }

        /// <summary>
        /// Generates solution readme as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council text operation and used when producing its result.</param>
        /// <param name="isAiHostLab">Value indicating whether is AI host lab should apply to this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionReadme(
            string projectName,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab, ILogger logger)
        {
            try
            {
                var description = isAiHostLab
                ? "Generated by LocalGPT as a .NET 10 ASP.NET Core and DevExpress Blazor AI host with a native local-model-file runner contract."
                : "Generated by LocalGPT as a whole-solution AI Council artifact.";

                var notes = isAiHostLab
                    ? """
                  ## AI Host Control-Plane Lab Scope

                  This prototype demonstrates selected provider-compatible HTTP routes, model catalog UX, health cards, endpoint testing in .NET/Blazor, and a native local-model-file runner boundary.

                  It does not proxy `/api/chat` or `/api/generate` to upstream Ollama/LM Studio/OpenAI-compatible hosts. It can read local model-file metadata, resolve `.gguf` or Ollama-managed blob candidates, and invoke a configured approved native executable.
                  """
                    : """
                  ## Scope

                  This is a LocalGPT/TacosPortalOpen-style .NET 10 Blazor and DevExpress generation sandbox.
                  """;

                return
                $$"""
            # {{projectName}}

            {{description}}

            This zip is a sandbox prototype. Review it before copying any file into LocalGPT, TacosPortalOpen, or another real project.

            ## Contents

            - `{{projectName}}.sln`
            - `src/{{projectName}}/{{projectName}}.csproj`
            - Blazor Web App `Program.cs`, `App.razor`, `catalog.Routes.razor`
            - Routable Razor pages under `Components/Pages`
            - Service/model code under `Services` and `Models`
            - `wwwroot/app.css`
            - Navigation SVG pairs under `wwwroot/icons/nav`
            - `PROJECT_INDEX.md`
            - `ARCHITECTURE.md`
            - `SOURCE_FIDELITY.md`
            - `BUILD_AND_RUN.md`
            - `.localgpt-generation.json`
            - `LocalGPT.GenerationManifest.json`

            {{notes}}

            ## Design Contract

            The UI uses Bootstrap v5-style spacing and responsive layout with DevExpress Blazor controls for actual interaction. Navigation includes line SVG icons for the default state and solid SVG icons for hover/focus states so generated apps have a reusable icon language.

            ## Build

            ```powershell
            dotnet restore
            dotnet build
            ```

            ## Original Request

            {{TrimForCodeComment(request.Prompt, 1200, logger)}}

            ## Council Output Summary

            {{TrimForCodeComment(result.FinalAnswer, 1200, logger)}}
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateSolutionReadme");
                return string.Empty;
            }

        }
        /// <summary>
        /// Generates solution project index as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council text operation and used when producing its result.</param>
        /// <param name="isAiHostLab">Value indicating whether is AI host lab should apply to this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionProjectIndex(
            string projectName,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab, ILogger logger)
        {
            try
            {
                var projectKind = isAiHostLab ? "dotnet_service" : "localgpt_feature";
                var purpose = isAiHostLab
                    ? "Prototype an AI-host-shaped .NET/Blazor control plane without claiming native model inference."
                    : "Prototype a LocalGPT/TacosPortalOpen-style AI Council feature workspace with reviewable Blazor pages.";
                var catalogPage = isAiHostLab ? "GeneratedKnowledgeTable.razor route `/models`" : "GeneratedKnowledgeTable.razor route `/knowledge`";
                var detailPage = isAiHostLab ? "ApiConsole.razor route `/api-console`" : "ImplementationPlan.razor route `/implementation-plan`";
                var aiHostExpectedEntryPoints = isAiHostLab
                    ? $$"""
                ,
                "src/{{projectName}}/Components/Pages/Chat.razor",
                "src/{{projectName}}/Components/Pages/RunningModels.razor",
                "src/{{projectName}}/Components/Pages/ModelDownloads.razor",
                "src/{{projectName}}/Components/Pages/Templates.razor",
                "src/{{projectName}}/Components/Pages/Hardware.razor",
                "src/{{projectName}}/Components/Pages/RunnerPlugins.razor",
                "src/{{projectName}}/Components/Pages/Logs.razor",
                "src/{{projectName}}/Components/Pages/Settings.razor"
                """
                    : string.Empty;
                var aiHostEntryPoints = isAiHostLab
                    ? $$"""
            - `src/{{projectName}}/Components/Pages/Chat.razor` - safe chat route preview with typed response data.
            - `src/{{projectName}}/Components/Pages/RunningModels.razor` - running-model status grid.
            - `src/{{projectName}}/Components/Pages/ModelDownloads.razor` - DevExpress model pull planning page.
            - `src/{{projectName}}/Components/Pages/Templates.razor` - chat template and thinking-format guidance.
            - `src/{{projectName}}/Components/Pages/Hardware.razor` - GPU/VRAM/context policy page.
            - `src/{{projectName}}/Components/Pages/RunnerPlugins.razor` - native-runner, plugin, script, and adapter boundary page.
            - `src/{{projectName}}/Components/Pages/Logs.razor` - runtime-boundary diagnostics page.
            - `src/{{projectName}}/Components/Pages/Settings.razor` - AI host settings and runner-boundary page.
            """
                    : string.Empty;
                var aiHostGeneratedFiles = isAiHostLab
                    ? $$"""
            | `src/{{projectName}}/Components/Pages/Chat.razor` | DevExpress chat page with safe typed `/api/chat` preview. |
            | `src/{{projectName}}/Components/Pages/RunningModels.razor` | Running model status grid for `/api/ps`-style state. |
            | `src/{{projectName}}/Components/Pages/ModelDownloads.razor` | DevExpress UI for provider-style pull planning and download guidance. |
            | `src/{{projectName}}/Components/Pages/Templates.razor` | Chat template, Harmony, and thinking-format compatibility page. |
            | `src/{{projectName}}/Components/Pages/Hardware.razor` | GPU/VRAM/context budget and queue-policy page. |
            | `src/{{projectName}}/Components/Pages/RunnerPlugins.razor` | Runner/plugin/script adapter surface for native local model-file execution. |
            | `src/{{projectName}}/Components/Pages/Logs.razor` | Runtime-boundary diagnostic log page. |
            | `src/{{projectName}}/Components/Pages/Settings.razor` | DevExpress settings page for local model source, context, and native-runner boundaries. |
            | `src/{{projectName}}/Services/GeneratedAiHostArchitectureServices.cs` | Provider, runner, plugin, script, hardware, and template contracts wired through DI. |
            """
                    : string.Empty;

                return $$"""
            # Project Index

            ## Purpose

            {{purpose}}

            ## Archetype

            ```json
            {
              "project_kind": "{{projectKind}}",
              "complexity": "normal",
              "needs_datagen": false,
              "needs_tests": true,
              "needs_native_commands": {{(isAiHostLab ? "true" : "false")}},
              "needs_index": true,
              "needs_version_resolver": false,
              "expected_entrypoints": [
                "src/{{projectName}}/Program.cs",
                "src/{{projectName}}/Components/Pages/Index.razor",
                "src/{{projectName}}/Components/Pages/GeneratedDashboard.razor",
                "src/{{projectName}}/Components/Pages/SourceFidelity.razor"{{aiHostExpectedEntryPoints}}
              ]
            }
            ```

            ## Entry Points

            - `src/{{projectName}}/Program.cs` - ASP.NET Core service registration, DevExpress setup, and app pipeline.
            - `src/{{projectName}}/Components/App.razor` - document shell and static asset links.
            - `src/{{projectName}}/Components/catalog.Routes.razor` - Blazor route discovery.
            - `src/{{projectName}}/Components/GeneratedNavigation.razor` - generated app navigation.
            - `src/{{projectName}}/Components/Pages/Index.razor` - first viewport and page hub.
            - `src/{{projectName}}/Components/Pages/GeneratedDashboard.razor` - health/status grid.
            - `src/{{projectName}}/Components/Pages/{{catalogPage}}` - archetype catalog page.
            - `src/{{projectName}}/Components/Pages/{{detailPage}}` - archetype-specific detail page.
            - `src/{{projectName}}/Components/Pages/SourceFidelity.razor` - source-fidelity review grid.
            {{aiHostEntryPoints}}

            ## Generated Files

            | File | Why it exists |
            | --- | --- |
            | `{{projectName}}.sln` | Visual Studio and CLI solution entry point. |
            | `src/{{projectName}}/{{projectName}}.csproj` | .NET 10 Blazor Web App project with DevExpress dependency. |
            | `src/{{projectName}}/Services/GeneratedHealthSummaryService.cs` | Typed demo service instead of Razor-only fake data. |
            | `src/{{projectName}}/Services/GeneratedSourceFidelityService.cs` | Typed evidence that the sandbox preserves source workflows or marks capability gaps. |
            | `src/{{projectName}}/Models/GeneratedHealthCard.cs` | Shared model records for grids/catalog rows. |
            {{aiHostGeneratedFiles}}
            | `src/{{projectName}}/wwwroot/app.css` | Local styling for the generated shell. |
            | `src/{{projectName}}/wwwroot/icons/nav/*-line.svg` | Default navigation icon style. |
            | `src/{{projectName}}/wwwroot/icons/nav/*-solid.svg` | Hover/focus navigation icon style. |
            | `PROJECT_INDEX.md` | Required generated-project map and archetype declaration. |
            | `ARCHITECTURE.md` | Explains why this artifact differs from other project types. |
            | `SOURCE_FIDELITY.md` | Explains why a compiling artifact still needs architectural fidelity review. |
            | `PROMISE_MAP.md` | Maps the council's promised workflows into generated modules and review surfaces. |
            | `DESIGN_REVIEW.md` | Explains layout choices, DevExpress components, mocked pieces, and follow-up wiring. |
            | `BUILD_AND_RUN.md` | Exact restore/build/run commands and expected checks. |
            | `.localgpt-generation.json` | Machine-readable generation contract. |
            | `LocalGPT.GenerationManifest.json` | LocalGPT artifact metadata and safety notes. |

            ## Validation Status

            Generated only. The LocalGPT artifact service validated the required file contract before zipping. Run `dotnet build` before treating this as build-passed.

            ## Original Request

            {{TrimForCodeComment(request.Prompt, 900, logger)}}

            ## Council Summary

            {{TrimForCodeComment(result.FinalAnswer, 900, logger)}}
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateSolutionProjectIndex");
                return string.Empty;
            }

        }

    }
}
