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
        /// Generates solution program as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="isAiHostLab">Value indicating whether is AI host lab should apply to this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionProgram(string projectName, bool isAiHostLab, ILogger logger)
        {
            try
            {
                var aiHostServiceRegistrations = isAiHostLab
                ? """
                  builder.Services.Configure<AiHostRuntimeOptions>(builder.Configuration.GetSection("AiHost"));
                  builder.Services.AddSingleton<IModelCatalogService>(sp => sp.GetRequiredService<GeneratedHealthSummaryService>());
                  builder.Services.AddSingleton<IModelTransferService>(sp => sp.GetRequiredService<GeneratedHealthSummaryService>());
                  builder.Services.AddSingleton<IInferenceProvider, NativeModelFileInferenceProvider>();
                  builder.Services.AddSingleton<IInferenceRunner, NativeModelFileProcessRunner>();
                  builder.Services.AddSingleton<IPluginCatalogService, GeneratedPluginCatalogService>();
                  builder.Services.AddSingleton<IScriptExecutionService, PermissionGatedScriptExecutionService>();
                  builder.Services.AddSingleton<IHardwareBudgetService, GeneratedHardwareBudgetService>();
                  builder.Services.AddSingleton<IChatTemplateService, GeneratedChatTemplateService>();
                  """
                : string.Empty;

                var aiHostRoutes = isAiHostLab
                    ? """
                  app.MapGet("/api/version", () => new
                  {
                      version = "dotnet-lab-0.2",
                      source = "LocalGPT generated sandbox",
                      native_runner_contract = true,
                      upstream_proxy = false
                  });
                  app.MapGet("/api/tags", ([FromServices] IModelCatalogService catalog) => new { models = catalog.GetAiHostTags() });
                  app.MapGet("/api/ps", ([FromServices] IModelCatalogService catalog) => new { models = catalog.GetRunningModels() });
                  app.MapPost("/api/show", ([FromServices] IModelCatalogService catalog, [FromBody] GeneratedModelActionRequest request) => catalog.GetModelDetails(request));
                  app.MapPost("/api/pull", ([FromServices] IModelTransferService transfer, [FromBody] GeneratedModelActionRequest request) => transfer.CreatePullPlan(request));
                  app.MapPost("/api/push", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateOperation("push", request.Model));
                  app.MapPost("/api/create", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateOperation("create", request.Model));
                  app.MapPost("/api/copy", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelCopyRequest request) => service.CreateCopyPlan(request));
                  app.MapDelete("/api/delete", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateOperation("delete", request.Model));
                  app.MapPost("/api/generate", async ([FromServices] IInferenceProvider provider, [FromBody] GeneratedModelActionRequest request, CancellationToken cancellationToken) => await provider.GenerateAsync(request, cancellationToken).ConfigureAwait(false));
                  app.MapPost("/api/chat", async ([FromServices] IInferenceProvider provider, [FromBody] GeneratedChatRequest request, CancellationToken cancellationToken) => await provider.ChatAsync(request, cancellationToken).ConfigureAwait(false));
                  app.MapPost("/api/embed", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateEmbeddingResponse(request));
                  app.MapPost("/api/embeddings", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateEmbeddingResponse(request));
                  app.MapGet("/api/blobs/{digest}", (string digest) => Results.Json(new { digest, status = "planned", boundary = "Blob storage is represented as metadata only in this generated lab." }));
                  app.MapGet("/api/localgpt/runner/capability", async ([FromServices] IInferenceRunner runner, CancellationToken cancellationToken) => await runner.GetCapabilityAsync(cancellationToken).ConfigureAwait(false));
                  app.MapGet("/api/localgpt/plugins", ([FromServices] IPluginCatalogService plugins) => plugins.GetPlugins());
                  app.MapGet("/api/localgpt/hardware-budget", ([FromServices] IHardwareBudgetService hardware) => hardware.GetBudget());
                  app.MapGet("/api/localgpt/chat-templates", ([FromServices] IChatTemplateService templates) => templates.GetTemplateRules());
                  app.MapGet("/api/host/status", async ([FromServices] IInferenceRunner runner, [FromServices] IModelCatalogService catalog, [FromServices] IHardwareBudgetService hardware, CancellationToken cancellationToken) => new
                  {
                      runner = await runner.GetCapabilityAsync(cancellationToken).ConfigureAwait(false),
                      models = catalog.GetAiHostTags(),
                      running = catalog.GetRunningModels(),
                      hardware = hardware.GetBudget(),
                      upstream_proxy = false
                  });
                  app.MapPost("/api/localgpt/scripts/plan", ([FromServices] IScriptExecutionService scripts, [FromBody] GeneratedScriptPlanRequest request) => scripts.CreatePlan(request.ScriptKind, request.Target, request.UserApproved));
                  app.MapGet("/v1/models", ([FromServices] IModelCatalogService catalog) => new { data = catalog.GetAiHostTags() });
                  app.MapPost("/v1/chat/completions", async ([FromServices] IInferenceProvider provider, [FromBody] GeneratedChatRequest request, CancellationToken cancellationToken) => await provider.ChatAsync(request, cancellationToken).ConfigureAwait(false));
                  app.MapPost("/v1/embeddings", ([FromServices] GeneratedHealthSummaryService service, [FromBody] GeneratedModelActionRequest request) => service.CreateEmbeddingResponse(request));
                  """
                    : string.Empty;

                return $$"""
            using DevExpress.Blazor;
            using Microsoft.AspNetCore.Mvc;
            using {{projectName}}.Components;
            using {{projectName}}.Models;
            using {{projectName}}.Services;

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddDevExpressBlazor(options => options.SizeMode = SizeMode.Small);
            builder.Services.AddSingleton<GeneratedHealthSummaryService>();
            builder.Services.AddSingleton<ISourceFidelityService, GeneratedSourceFidelityService>();
            {{aiHostServiceRegistrations}}

            var app = builder.Build();

            app.UseStaticFiles();
            app.UseAntiforgery();
            app.MapGet("/__generated/health", (GeneratedHealthSummaryService service) => service.GetCards());
            {{aiHostRoutes}}
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionProgram projectName:{projectName} isAiHostLab:{isAiHostLab}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates solution imports as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionImports(string projectName, ILogger logger)
        {
            try
            {
                return $$"""
            @using System.Net.Http
            @using Microsoft.AspNetCore.Components
            @using Microsoft.AspNetCore.Components.Forms
            @using Microsoft.AspNetCore.Components.Routing
            @using Microsoft.AspNetCore.Components.Web
            @using Microsoft.AspNetCore.Components.Web.Virtualization
            @using static Microsoft.AspNetCore.Components.Web.RenderMode
            @using Microsoft.JSInterop
            @using DevExpress.Blazor
            @using {{projectName}}
            @using {{projectName}}.Components
            @using {{projectName}}.Components.Pages
            @using {{projectName}}.Models
            @using {{projectName}}.Services
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionImports projectName:{projectName}");
                return string.Empty;
            }

        }
       
        /// <summary>
        /// Builds data content file name as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="index">Index value supplied to the council text operation and used when producing its result.</param>
        /// <param name="mediaType">Media type value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildDataContentFileName(int index, string? mediaType, ILogger logger)
        {
            try
            {
                var extension = (mediaType ?? string.Empty).ToLowerInvariant() switch
                {
                    "application/zip" or "application/x-zip-compressed" => ".zip",
                    "application/json" => ".json",
                    "application/xml" or "text/xml" => ".xml",
                    "text/markdown" => ".md",
                    "text/css" => ".css",
                    "text/html" => ".html",
                    "text/javascript" or "application/javascript" => ".js",
                    "application/octet-stream" => ".bin",
                    _ => ".txt"
                };
                return $"dxaichat-upload-{index}{extension}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionImports index:{index} mediaType:{mediaType}");
                return string.Empty;
            }

        }
        /// <summary>
        /// Attempts to retrieve data content file name as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="content">Content value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string? TryGetDataContentFileName(DataContent content, ILogger logger)
        {
            try
            {
                foreach (var key in new[] { "name", "fileName", "filename", "FileName", "Name" })
                {
                    if (content.AdditionalProperties?.TryGetValue(key, out var value) == true &&
                        value is not null &&
                        !string.IsNullOrWhiteSpace(value.ToString()))
                    {
                        return value.ToString();
                    }
                }

                var rawName = content.RawRepresentation?
                    .GetType()
                    .GetProperty("Name")?
                    .GetValue(content.RawRepresentation)?
                    .ToString();
                return string.IsNullOrWhiteSpace(rawName) ? null : rawName;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "TryGetDataContentFileName");
                return string.Empty;
            }
            
        }

    }
}
