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
        /// Generates solution navigation razor as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="archetype">Archetype value supplied to the council text operation and used when producing its result.</param>
        /// <param name="promiseModules">Generated promise module dependency used by the council text workflow to provide the corresponding application capability.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionNavigationRazor(
             GeneratedSolutionArchetype archetype,
            IReadOnlyList<GeneratedPromiseModule> promiseModules, ILogger logger)
        {
            try
            {
                var isAiHostLab = archetype == GeneratedSolutionArchetype.AiHost;
                var labName = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "AI Host Control Plane",
                    _ => "LocalGPT Generation Lab"
                };
                var catalogHref = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "/models",
                    _ => "/knowledge"
                };
                var catalogText = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "Model Catalog",
                    _ => "Knowledge"
                };
                var detailHref = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "/api-console",
                    _ => "/implementation-plan"
                };
                var detailText = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "API Console",
                    _ => "Implementation Plan"
                };
                var aiHostLinks = isAiHostLab
                    ? """
                    <a href="/chat">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Chat</span>
                    </a>
                    <a href="/running-models">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>Running</span>
                    </a>
                    <a href="/model-downloads">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>Downloads</span>
                    </a>
                    <a href="/templates">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Templates</span>
                    </a>
                    <a href="/hardware">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>Hardware</span>
                    </a>
                    <a href="/runner-plugins">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>Runner Plugins</span>
                    </a>
                    <a href="/logs">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>Logs</span>
                    </a>
                    <a href="/settings">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Settings</span>
                    </a>
                """
                    : string.Empty;
                var archetypeLinks = archetype switch
                {
                    GeneratedSolutionArchetype.LocalGpt => """
                    <a href="/chat">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>DXAiChat</span>
                    </a>
                    <a href="/model-council">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>AI Council</span>
                    </a>
                    <a href="/database">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>SQLite</span>
                    </a>
                    <a href="/minecraft-mod-builder">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>Minecraft</span>
                    </a>
                    <a href="/test-lab">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Test Lab</span>
                    </a>
                """,
                    GeneratedSolutionArchetype.TacosPortal => """
                    <a href="/telegram-ingestion">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>Ingestion</span>
                    </a>
                    <a href="/persistence">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>Persistence</span>
                    </a>
                    <a href="/workers">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Workers</span>
                    </a>
                    <a href="/admin">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>Admin</span>
                    </a>
                    <a href="/client-shells">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Client Shells</span>
                    </a>
                """,
                    GeneratedSolutionArchetype.BotBackend => """
                    <a href="/webhooks">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Webhooks</span>
                    </a>
                    <a href="/conversations">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>Conversations</span>
                    </a>
                    <a href="/bot-settings">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>Bot Settings</span>
                    </a>
                    <a href="/python-interop">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>Python Interop</span>
                    </a>
                """,
                    _ => string.Empty
                };
                var promiseLinks = BuildPromiseNavigationLinks(promiseModules, logger);

                return $$"""
                <nav class="generated-nav" aria-label="{{labName}} navigation">
                    <a class="generated-brand" href="/">{{labName}}</a>
                    <a href="/dashboard">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/dashboard-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/dashboard-solid.svg" alt="" aria-hidden="true" />
                        <span>Dashboard</span>
                    </a>
                    <a href="{{catalogHref}}">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>{{catalogText}}</span>
                    </a>
                    <a href="{{detailHref}}">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>{{detailText}}</span>
                    </a>
                    <a href="/source-fidelity">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/catalog-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/catalog-solid.svg" alt="" aria-hidden="true" />
                        <span>Source Fidelity</span>
                    </a>
                    {{aiHostLinks}}
                    {{archetypeLinks}}
                    {{promiseLinks}}
                </nav>

                @code {
                    [Parameter]
                    public bool IsAiHostLab { get; set; }
                }
                """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionNavigationRazor archetype:{archetype.ToString()} promiseModules:{promiseModules.ToString()}", archetype, promiseModules);
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds promise navigation links as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="promiseModules">Generated promise module dependency used by the council text workflow to provide the corresponding application capability.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string BuildPromiseNavigationLinks(IReadOnlyList<GeneratedPromiseModule> promiseModules, ILogger logger)
        {
            try
            {
                if (promiseModules.Count == 0)
                    return string.Empty;

                var builder = new StringBuilder();
                foreach (var module in promiseModules.Take(6))
                {
                    builder.AppendLine($$"""
                    <a href="{{module.Route}}">
                        <img class="generated-nav-icon generated-nav-icon-line" src="/icons/nav/detail-line.svg" alt="" aria-hidden="true" />
                        <img class="generated-nav-icon generated-nav-icon-solid" src="/icons/nav/detail-solid.svg" alt="" aria-hidden="true" />
                        <span>{{module.Title}}</span>
                    </a>
                """);
                }

                return builder.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildPromiseNavigationLinks promiseModules:{promiseModules.ToString()}", promiseModules);
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Generates solution index razor as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council text operation and used when producing its result.</param>
        /// <param name="archetype">Archetype value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionIndexRazor(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            GeneratedSolutionArchetype archetype, ILogger logger)
        {
            try
            {
                var isAiHostLab = archetype == GeneratedSolutionArchetype.AiHost;
                var isAiHostLiteral = isAiHostLab ? "true" : "false";
                var title = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "AI Host Control Plane",
                    GeneratedSolutionArchetype.LocalGpt => "LocalGPT Workbench",
                    GeneratedSolutionArchetype.TacosPortal => "TacosPortal Operations",
                    GeneratedSolutionArchetype.BotBackend => "Bot Backend Control Plane",
                    _ => "LocalGPT Feature Generation Lab"
                };
                var subtitle = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "A DevExpress Blazor shell for provider-compatible API routes, model cataloging, endpoint checks, and external runner boundaries.",
                    GeneratedSolutionArchetype.LocalGpt => "A local-first AI workbench with DXAiChat, AI Council, SQLite memory, artifact downloads, Minecraft generation, setup, and test-lab surfaces.",
                    GeneratedSolutionArchetype.TacosPortal => "A server-interactive operations portal with menu, orders, reservations, admin CRUD, notifications, and a simple bot backend boundary.",
                    GeneratedSolutionArchetype.BotBackend => "A compact bot backend with webhooks, conversation state, moderation/retry queues, Python interop boundaries, and operator settings.",
                    _ => "A LocalGPT/TacosPortalOpen-style sandbox for AI Council feature requests, implementation planning, knowledge-backed generation, and artifact review."
                };
                var primaryHref = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "/api-console",
                    GeneratedSolutionArchetype.LocalGpt => "/chat",
                    GeneratedSolutionArchetype.TacosPortal => "/orders",
                    GeneratedSolutionArchetype.BotBackend => "/webhooks",
                    _ => "/implementation-plan"
                };
                var primaryLabel = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "Open API console",
                    GeneratedSolutionArchetype.LocalGpt => "Open DXAiChat",
                    GeneratedSolutionArchetype.TacosPortal => "Open orders",
                    GeneratedSolutionArchetype.BotBackend => "Open webhooks",
                    _ => "Open implementation plan"
                };
                var secondaryHref = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "/models",
                    GeneratedSolutionArchetype.LocalGpt => "/model-council",
                    GeneratedSolutionArchetype.TacosPortal => "/menu",
                    GeneratedSolutionArchetype.BotBackend => "/conversations",
                    _ => "/knowledge"
                };
                var secondaryLabel = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "Review model catalog",
                    GeneratedSolutionArchetype.LocalGpt => "Review AI Council",
                    GeneratedSolutionArchetype.TacosPortal => "Review menu",
                    GeneratedSolutionArchetype.BotBackend => "Review conversations",
                    _ => "Review knowledge table"
                };
                var kicker = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "AI host lab",
                    _ => "LocalGPT lab"
                };
                var requestSummary = EscapeCSharpString(TrimForCodeComment(request.Prompt, 500, logger), logger);
                var consensusSummary = EscapeCSharpString(TrimForCodeComment(result.FinalAnswer, 700, logger), logger);

                return $$"""
                @page "/"
                @rendermode InteractiveServer
                @inject GeneratedHealthSummaryService HealthService

                <PageTitle>{{title}}</PageTitle>

                <main class="generated-shell">
                    <GeneratedNavigation IsAiHostLab="{{isAiHostLiteral}}" />

                    <section class="generated-hero">
                        <div>
                            <p class="generated-kicker">{{kicker}}</p>
                            <h1>{{title}}</h1>
                            <p>{{subtitle}}</p>
                        </div>
                        <div class="generated-actions">
                            <DxButton Text="{{primaryLabel}}"
                                      NavigateUrl="{{primaryHref}}"
                                      RenderStyle="ButtonRenderStyle.Primary"
                                      RenderStyleMode="ButtonRenderStyleMode.Contained" />
                            <DxButton Text="{{secondaryLabel}}"
                                      NavigateUrl="{{secondaryHref}}"
                                      RenderStyle="ButtonRenderStyle.Secondary"
                                      RenderStyleMode="ButtonRenderStyleMode.Outline" />
                        </div>
                    </section>

                    <section class="generated-split">
                        <div>
                            <h2>What This Artifact Is</h2>
                            <DxGrid Data="@HealthService.GetCards()"
                                    KeyFieldName="@nameof(GeneratedHealthCard.Area)"
                                    CssClass="generated-grid compact"
                                    TextWrapEnabled="true">
                                <Columns>
                                    <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Area)" Caption="Area" />
                                    <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Status)" Caption="Status" />
                                    <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.NextAction)" Caption="Next Action" />
                                </Columns>
                            </DxGrid>
                        </div>
                        <div>
                            <h2>Original Council Context</h2>
                            <DxFormLayout CssClass="generated-form">
                                <DxFormLayoutItem Caption="Request" ColSpanMd="12">
                                    <DxMemo Text="@RequestSummary" Rows="5" ReadOnly="true" />
                                </DxFormLayoutItem>
                                <DxFormLayoutItem Caption="Consensus" ColSpanMd="12">
                                    <DxMemo Text="@CouncilSummary" Rows="6" ReadOnly="true" />
                                </DxFormLayoutItem>
                            </DxFormLayout>
                        </div>
                    </section>
                </main>

                @code {
                    string RequestSummary { get; } = "{{requestSummary}}";
                    string CouncilSummary { get; } = "{{consensusSummary}}";
                }
                """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateSolutionIndexRazor");
                return string.Empty;
            }
           
        }
        /// <summary>
        /// Generates solution dashboard razor as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council text operation and used when producing its result.</param>
        /// <param name="archetype">Archetype value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionDashboardRazor(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            GeneratedSolutionArchetype archetype, ILogger logger)
        {
            try
            {
                var isAiHostLab = archetype == GeneratedSolutionArchetype.AiHost;
                var isAiHostLiteral = isAiHostLab ? "true" : "false";
                var requestSummary = EscapeCSharpString(TrimForCodeComment(request.Prompt, 700, logger), logger);
                var consensusSummary = EscapeCSharpString(TrimForCodeComment(result.FinalAnswer, 900, logger), logger);
                var title = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "AI Host Dashboard",
                    GeneratedSolutionArchetype.LocalGpt => "LocalGPT Workbench Dashboard",
                    GeneratedSolutionArchetype.TacosPortal => "TacosPortal Operations Dashboard",
                    GeneratedSolutionArchetype.BotBackend => "Bot Backend Dashboard",
                    _ => "LocalGPT Generation Dashboard"
                };
                var subtitle = archetype switch
                {
                    GeneratedSolutionArchetype.AiHost => "Track API compatibility, model catalog readiness, runner adapter boundaries, and endpoint-test status.",
                    GeneratedSolutionArchetype.LocalGpt => "Track model connectivity, Council health, SQLite memory, generated artifacts, Minecraft builder readiness, and frontend test status.",
                    GeneratedSolutionArchetype.TacosPortal => "Track order throughput, kitchen state, menu publishing, reservations, admin CRUD, and bot notification boundaries.",
                    GeneratedSolutionArchetype.BotBackend => "Track webhook health, queue state, conversation memory, retry policy, Python interop, and operator approvals.",
                    _ => "Track AI Council feature-generation readiness, knowledge grounding, artifact review, and integration safety."
                };
                return $$"""
            @page "/dashboard"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>{{title}}</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="{{isAiHostLiteral}}" />

                <section class="generated-header">
                    <div>
                        <h1>{{title}}</h1>
                        <p>{{subtitle}}</p>
                    </div>
                    <DxButton Text="Refresh"
                              RenderStyle="ButtonRenderStyle.Primary"
                              RenderStyleMode="ButtonRenderStyleMode.Contained"
                              Click="RefreshAsync" />
                </section>

                <DxGrid Data="@Cards"
                        KeyFieldName="@nameof(GeneratedHealthCard.Area)"
                        ShowSearchBox="true"
                        ShowFilterRow="true"
                        TextWrapEnabled="false"
                        CssClass="generated-grid">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Area)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Status)" Caption="Status" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.NextAction)" Caption="Next Action" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Detail)" Caption="Detail" />
                    </Columns>
                </DxGrid>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Generation Context" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Request" ColSpanMd="12">
                            <DxMemo Text="@RequestSummary" Rows="4" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Council Output" ColSpanMd="12">
                            <DxMemo Text="@CouncilSummary" Rows="5" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                IReadOnlyList<GeneratedHealthCard> Cards { get; set; } = [];
                string RequestSummary { get; } = "{{requestSummary}}";
                string CouncilSummary { get; } = "{{consensusSummary}}";

                protected override Task OnInitializedAsync() => RefreshAsync();

                Task RefreshAsync()
                {
                    Cards = HealthService.GetCards();
                    return Task.CompletedTask;
                }
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateSolutionDashboardRazor");
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates solution knowledge table razor as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="isAiHostLab">Value indicating whether is AI host lab should apply to this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionKnowledgeTableRazor(bool isAiHostLab, ILogger logger)
        {
            try
            {
                if (isAiHostLab)
                {
                    return """
                @page "/models"
                @rendermode InteractiveServer
                @inject GeneratedHealthSummaryService HealthService

                <PageTitle>AI Host Model Catalog</PageTitle>

                <main class="generated-shell">
                    <GeneratedNavigation IsAiHostLab="true" />

                    <h1>AI Host Model Catalog</h1>
                    <p class="generated-muted">Model rows are compatibility records for local model files and native runner readiness.</p>

                    <DxGrid Data="@HealthService.GetModelCatalog()"
                            ShowSearchBox="true"
                            CssClass="generated-grid">
                        <Columns>
                            <DxGridDataColumn FieldName="@nameof(GeneratedModelCard.Name)" Caption="Model or adapter" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedModelCard.Status)" Caption="Status" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedModelCard.SizeMegabytes)" Caption="Size MB" />
                            <DxGridDataColumn FieldName="@nameof(GeneratedModelCard.SupportsNativeInference)" Caption="Native inference" />
                        </Columns>
                    </DxGrid>
                </main>
                """;
                }

                return """
            @page "/knowledge"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>Generation Knowledge</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="false" />

                <h1>Generation Knowledge</h1>
                <p class="generated-muted">This page demonstrates the knowledge-first path the LocalGPT AI Council should use before proposing integration.</p>

                <DxGrid Data="@HealthService.GetCards()"
                        ShowSearchBox="true"
                        CssClass="generated-grid">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Area)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Status)" Caption="Status" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedHealthCard.Detail)" Caption="Detail" />
                    </Columns>
                </DxGrid>
            </main>
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionKnowledgeTableRazor isAiHostLab:{isAiHostLab}");
                return string.Empty;
            }
            
        }
 
        

        /// <summary>
        /// Generates solution file as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="projectGuid">Identifier of the project gu to use for this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionFile(string projectName, string projectGuid, ILogger logger)
        {
            try
            {
                return $$"""
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            VisualStudioVersion = 17.0.31903.59
            MinimumVisualStudioVersion = 10.0.40219.1
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{projectName}}", "src\{{projectName}}\{{projectName}}.csproj", "{{projectGuid}}"
            EndProject
            Global
            	GlobalSection(SolutionConfigurationPlatforms) = preSolution
            		Debug|Any CPU = Debug|Any CPU
            		Release|Any CPU = Release|Any CPU
            	EndGlobalSection
            	GlobalSection(ProjectConfigurationPlatforms) = postSolution
            		{{projectGuid}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
            		{{projectGuid}}.Debug|Any CPU.Build.0 = Debug|Any CPU
            		{{projectGuid}}.Release|Any CPU.ActiveCfg = Release|Any CPU
            		{{projectGuid}}.Release|Any CPU.Build.0 = Release|Any CPU
            	EndGlobalSection
            EndGlobal
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionFile projectName:{projectName} projectGuid:{projectGuid}");
                return string.Empty;
            }

        }
            

       

        /// <summary>
        /// Generates solution app settings as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="isAiHostLab">Value indicating whether is AI host lab should apply to this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionAppSettings(bool isAiHostLab,ILogger logger)
        {
            try
            {
                if (!isAiHostLab)
                {
                    return """
                {
                  "Logging": {
                    "LogLevel": {
                      "Default": "Information"
                    }
                  }
                }
                """;
                }

                return """
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information"
                }
              },
              "AiHost": {
                "DefaultModel": "gpt-oss:20b",
                "SafeStorageRoot": "%LOCALAPPDATA%/GeneratedAiHost",
                "PluginRoot": "plugins",
                "ModelsRoot": "%LOCALAPPDATA%/GeneratedAiHost/Models",
                "NativeRunnerExecutable": "",
                "EnableRunnerAutoDetect": true,
                "NativeRunnerInstallUrl": "https://github.com/ggml-org/llama.cpp/releases",
                "RunnerSearchRoots": [
                  "%LOCALAPPDATA%/LocalGPT/Runners",
                  "%LOCALAPPDATA%/Programs/Ollama",
                  "%PROGRAMFILES%/Ollama",
                  "%USERPROFILE%/.local/bin"
                ],
                "RunnerExecutableNames": [
                  "llama-cli.exe",
                  "llama-server.exe",
                  "ollama.exe",
                  "llama-cli",
                  "llama-server",
                  "ollama"
                ],
                "ModelSearchRoots": [
                  "%USERPROFILE%/.ollama/models",
                  "%LOCALAPPDATA%/LocalGPT/ModelFiles",
                  "%LOCALAPPDATA%/GeneratedAiHost/Models"
                ],
                "ContextTokens": 262144,
                "GpuLayers": 20,
                "MaxParallelModels": 2,
                "TargetGpuLoadPercent": 85,
                "AllowNativeRunner": false,
                "AllowPythonNet": false,
                "AllowPowerShellScripts": false,
                "AllowTypeScriptAdapters": false
              }
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionAppSettings isAiHostLab:{isAiHostLab}");
                return string.Empty;
            }
            
        }

    }
}
