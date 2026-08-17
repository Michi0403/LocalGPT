using DevExpress.Blazor;
using System.Collections.Frozen;
using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.CodeParser;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates local GPT catalog behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class LocalGptCatalogService
    {
    /// <summary>
        /// <summary>
        /// Gets the generate solution CSS value that forms part of the local GPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// Gets the generate solution CSS value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate solution CSS value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateSolutionCss =>
            """
            :root {
                color-scheme: light;
                font-family: "Segoe UI", Arial, sans-serif;
            }

            body {
                margin: 0;
                background: #f7f8fa;
                color: #1f2937;
            }

            .generated-shell {
                max-width: 1180px;
                margin: 0 auto;
                padding: 32px;
            }

            .generated-nav {
                display: flex;
                align-items: center;
                gap: 16px;
                margin-bottom: 24px;
                padding-bottom: 14px;
                border-bottom: 1px solid #d9dee7;
            }

            .generated-nav a {
                display: inline-flex;
                align-items: center;
                gap: 6px;
                color: #384252;
                text-decoration: none;
                font-weight: 600;
            }

            .generated-nav a:hover,
            .generated-nav a:focus-visible {
                color: #0b5cab;
            }

            .generated-nav .generated-brand {
                margin-right: auto;
                color: #172033;
                font-weight: 700;
            }

            .generated-nav-icon {
                width: 18px;
                height: 18px;
                flex: 0 0 18px;
            }

            .generated-nav-icon-solid {
                display: none;
            }

            .generated-nav a:hover .generated-nav-icon-line,
            .generated-nav a:focus-visible .generated-nav-icon-line {
                display: none;
            }

            .generated-nav a:hover .generated-nav-icon-solid,
            .generated-nav a:focus-visible .generated-nav-icon-solid {
                display: inline-block;
            }

            .generated-hero {
                display: grid;
                grid-template-columns: minmax(0, 1fr) auto;
                gap: 20px;
                align-items: end;
                padding: 28px 0 24px;
            }

            .generated-hero h1 {
                margin: 0;
                font-size: 34px;
                line-height: 1.1;
            }

            .generated-hero p {
                max-width: 760px;
                color: #536173;
            }

            .generated-kicker {
                margin: 0 0 8px;
                color: #0f766e;
                font-weight: 700;
                text-transform: uppercase;
                letter-spacing: 0;
            }

            .generated-actions {
                display: flex;
                gap: 10px;
                flex-wrap: wrap;
                justify-content: flex-end;
            }

            .generated-split {
                display: grid;
                grid-template-columns: minmax(0, 1fr) minmax(320px, 0.8fr);
                gap: 24px;
                align-items: start;
            }

            .generated-header {
                display: flex;
                align-items: center;
                justify-content: space-between;
                gap: 16px;
                margin-bottom: 20px;
            }

            .generated-header h1 {
                margin: 0;
                font-size: 28px;
            }

            .generated-header p,
            .generated-muted {
                margin: 6px 0 0;
                color: #5f6b7a;
            }

            .generated-grid,
            .generated-form {
                margin-top: 18px;
            }

            .generated-note {
                margin-top: 22px;
            }

            .generated-code {
                overflow: auto;
                padding: 16px;
                border: 1px solid #d9dee7;
                background: #ffffff;
                border-radius: 6px;
            }

            @media (max-width: 860px) {
                .generated-shell {
                    padding: 20px;
                }

                .generated-hero,
                .generated-split {
                    grid-template-columns: 1fr;
                }

                .generated-actions {
                    justify-content: flex-start;
                }
            }
            """;
        /// <summary>
        /// Gets the generate AI host settings razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate AI host settings razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateAiHostSettingsRazor =>
            """
            @page "/settings"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>AI Host Settings</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>AI Host Settings</h1>
                        <p>Configuration is shown as safe generated defaults. Real persistence should be added through backend services and EF/SQLite after user approval.</p>
                    </div>
                </section>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Generated Runtime Profile" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Model Source" ColSpanMd="6">
                            <DxTextBox Text="@LabSettings.BaseUri" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Default Model" ColSpanMd="6">
                            <DxTextBox Text="@LabSettings.DefaultModel" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Keep Alive" ColSpanMd="4">
                            <DxTextBox Text="@LabSettings.KeepAlive" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Context Tokens" ColSpanMd="4">
                            <DxTextBox Text="@LabSettings.ContextTokens.ToString()" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="GPU Layers" ColSpanMd="4">
                            <DxTextBox Text="@LabSettings.GpuLayers.ToString()" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Native Runner Attached" ColSpanMd="6">
                            <DxCheckBox @bind-Checked="NativeRunnerAttached" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Pull Planning Enabled" ColSpanMd="6">
                            <DxCheckBox @bind-Checked="AllowPullPlanning" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Settings Summary" ColSpanMd="12">
                            <DxMemo Text="@HealthService.BuildSettingsSummary()" Rows="5" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>
            </main>

            @code {
                GeneratedAiHostSettings LabSettings { get; set; } = new();
                bool NativeRunnerAttached { get; set; }
                bool AllowPullPlanning { get; set; }

                protected override void OnInitialized()
                {
                    LabSettings = HealthService.GetSettings();
                    NativeRunnerAttached = LabSettings.NativeRunnerAttached;
                    AllowPullPlanning = LabSettings.AllowPullPlanning;
                }
            }
            """;
        /// <summary>
        /// Gets the generate AI host logs razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate AI host logs razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateAiHostLogsRazor =>
            """
            @page "/logs"
            @rendermode InteractiveServer
            @inject GeneratedHealthSummaryService HealthService

            <PageTitle>AI Host Logs</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>AI Host Logs</h1>
                        <p>Surface control-plane diagnostics and runtime-boundary notes where users can inspect them.</p>
                    </div>
                </section>

                <DxGrid Data="@HealthService.GetRuntimeLogRows()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Method)" Caption="Level" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Route)" Caption="Area" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Purpose)" Caption="Message" />
                        <DxGridDataColumn FieldName="@nameof(GeneratedEndpointCard.Boundary)" Caption="Action" />
                    </Columns>
                </DxGrid>
            </main>
            """;
        /// <summary>
        /// Gets the generate AI host runner plugins razor value that forms part of the LocalGPT catalog state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The generate AI host runner plugins razor value exposed by <see cref="LocalGptCatalogService"/>.</value>
        public string GenerateAiHostRunnerPluginsRazor =>
            """
            @page "/runner-plugins"
            @rendermode InteractiveServer
            @inject IPluginCatalogService PluginCatalog
            @inject IInferenceRunner Runner
            @inject IHardwareBudgetService HardwareBudget
            @inject IChatTemplateService ChatTemplates

            <PageTitle>Runner Plugins</PageTitle>

            <main class="generated-shell">
                <GeneratedNavigation IsAiHostLab="true" />

                <section class="generated-header">
                    <div>
                        <h1>Runner Plugins</h1>
                        <p>Show native-runner boundaries, optional catalog/provider adapters, Python.NET, PowerShell, and managed inference paths as explicit architecture contracts.</p>
                    </div>
                    <DxButton Text="Refresh capability"
                              RenderStyle="ButtonRenderStyle.Primary"
                              RenderStyleMode="ButtonRenderStyleMode.Contained"
                              Click="RefreshCapabilityAsync" />
                </section>

                <div class="generated-status-strip">
                    <article>
                        <strong>Native inference</strong>
                        <span>@(Capability?.NativeInferenceImplemented == true ? "Implemented" : "Capability gap")</span>
                    </article>
                    <article>
                        <strong>GPU target</strong>
                        <span>@Budget.TargetGpuLoadPercent% sustained</span>
                    </article>
                    <article>
                        <strong>Parallel models per AI host</strong>
                        <span>@Budget.MaxParallelModels</span>
                    </article>
                </div>

                <DxGrid Data="@PluginCatalog.GetPlugins()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Id)" Caption="Plugin Id" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.DisplayName)" Caption="Name" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Contract)" Caption="Contract" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Approved)" Caption="Approved" />
                        <DxGridDataColumn FieldName="@nameof(AiHostPluginManifest.Notes)" Caption="Notes" />
                    </Columns>
                </DxGrid>

                <DxFormLayout CssClass="generated-form">
                    <DxFormLayoutGroup Caption="Runner capability" ColSpanMd="12">
                        <DxFormLayoutItem Caption="Runner kind" ColSpanMd="4">
                            <DxTextBox Text="@Runner.RunnerKind" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Supported formats" ColSpanMd="8">
                            <DxTextBox Text="@SupportedFormatsText" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Missing capability" ColSpanMd="12">
                            <DxMemo Text="@(Capability?.MissingCapability ?? "Capability not loaded yet.")" Rows="3" ReadOnly="true" />
                        </DxFormLayoutItem>
                        <DxFormLayoutItem Caption="Next milestone" ColSpanMd="12">
                            <DxMemo Text="@(Capability?.NextMilestone ?? "Click Refresh capability.")" Rows="3" ReadOnly="true" />
                        </DxFormLayoutItem>
                    </DxFormLayoutGroup>
                </DxFormLayout>

                <DxGrid Data="@ChatTemplates.GetTemplateRules()"
                        CssClass="generated-grid"
                        ShowSearchBox="true"
                        TextWrapEnabled="true">
                    <Columns>
                        <DxGridDataColumn FieldName="@nameof(ChatTemplateRule.Name)" Caption="Template" />
                        <DxGridDataColumn FieldName="@nameof(ChatTemplateRule.Rule)" Caption="Rule" />
                    </Columns>
                </DxGrid>
            </main>

            @code {
                RunnerCapabilityReport? Capability { get; set; }
                HardwareBudgetSnapshot Budget { get; set; } = new(85, 20, 2048, 1, "Sequential by default.");
                string SupportedFormatsText => Capability is null ? string.Empty : string.Join(", ", Capability.SupportedFormats);

                protected override async Task OnInitializedAsync()
                {
                    Budget = HardwareBudget.GetBudget();
                    Capability = await Runner.GetCapabilityAsync();
                }

                async Task RefreshCapabilityAsync()
                {
                    Budget = HardwareBudget.GetBudget();
                    Capability = await Runner.GetCapabilityAsync();
                }
            }
            """;

    }
}
