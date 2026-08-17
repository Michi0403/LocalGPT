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
        /// Generates solution architecture doc as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="isAiHostLab">Value indicating whether is AI host lab should apply to this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionArchitectureDoc(string projectName, bool isAiHostLab, ILogger logger)
        {
            try
            {
                var title = isAiHostLab ? "AI Host Control-Plane Architecture" : "LocalGPT Feature Lab Architecture";
                var contrast = isAiHostLab
                    ? "This is not a LocalGPT feature page clone. It is an API-control-plane lab with endpoint cataloging, model rows, and explicit runner boundaries."
                    : "This is not an AI host control-plane lab. It is a LocalGPT/TacosPortalOpen-style feature sandbox with council grounding, implementation steps, and approval gates.";
                var backendBoundary = isAiHostLab
                    ? "Native inference, GGML/GPU scheduling, model loading, and tokenizer/runtime ownership stay outside this prototype until a real runner adapter is approved."
                    : "EF/SQLite writes, native commands, report generation, and artifact creation belong in backend services and routes, not in Razor-only snippets.";

                return $$"""
            # {{title}}

            ## Why This Shape

            {{contrast}}

            The solution uses a .NET 10 Blazor Web App with Interactive Server rendering because LocalGPT's desktop wrapper and debugging flow already favor server-side ownership for diagnostics, downloads, SQLite, and native-command boundaries.

            ## Boundaries

            - Blazor pages own user interaction and DevExpress presentation.
            - Services own generated data and route-test state.
            - Models are plain, typed C# objects consumed by DevExpress grids/forms.
            - {{backendBoundary}}
            - Generated code remains sandboxed until the user explicitly approves integration.

            ## Microsoft Architecture Rules Applied

            - Prefer a single cohesive web app when the feature does not need independent deployment.
            - Introduce service-oriented separation only around real boundaries, such as independent scaling, external runner adapters, background work, or downloadable artifacts.
            - Keep configuration/bootstrap data in appsettings and durable user/application state in EF/SQLite.
            - Keep APIs and services testable through DI rather than embedding logic in markup strings.
            - Provide health/status views and build instructions so the artifact can be reviewed line by line.
            - Use Bootstrap v5 for responsive page structure and DevExpress controls for real application interactions.
            - Include paired line/solid SVG navigation icons so default and active states are visually distinct.
            - For AI-host labs, generate interface-driven provider, model catalog, download, native-runner, plugin, script, template, and hardware-budget services. A dashboard without these contracts is incomplete.

            ## Files To Review First

            1. `PROJECT_INDEX.md`
            2. `.localgpt-generation.json`
            3. `src/{{projectName}}/Program.cs`
            4. `src/{{projectName}}/Components/Pages/Index.razor`
            5. `src/{{projectName}}/Services/GeneratedHealthSummaryService.cs`
            6. `SOURCE_FIDELITY.md`
            7. `src/{{projectName}}/Services/GeneratedSourceFidelityService.cs`
            {{(isAiHostLab ? $"8. `src/{projectName}/Services/GeneratedAiHostArchitectureServices.cs`" : string.Empty)}}
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionArchitectureDoc projectName:{projectName} isAiHostLab:{isAiHostLab}");
                return string.Empty;
            }

        }
        /// <summary>
        /// Generates source fidelity doc as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="archetype">Archetype value supplied to the council text operation and used when producing its result.</param>
        /// <param name="promiseModules">Generated promise module dependency used by the council text workflow to provide the corresponding application capability.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSourceFidelityDoc(
            string projectName,
            GeneratedSolutionArchetype archetype,
            IReadOnlyList<GeneratedPromiseModule> promiseModules, ILogger logger)
        {
            try
            {
                var expectedShape = archetype switch
                {
                    GeneratedSolutionArchetype.LocalGpt =>
                        "A LocalGPT replacement must look and behave like a local-first AI workbench: DXAiChat, AI Council, SQLite memory/knowledge, artifact downloads, Minecraft builder, Install, Test Lab, diagnostics, and visible model/runtime status.",
                    GeneratedSolutionArchetype.TacosPortal =>
                        "A TacosPortalOpen replacement must preserve the multi-host/event-ingestion architecture: core/shared services, Telegram or message ingestion, normalized persistence, workers, notifications, DevExpress admin/security, optional WASM client, and WinUI/WebView2 wrapper boundaries. It is not accepted as a generic restaurant ordering portal.",
                    GeneratedSolutionArchetype.AiHost =>
                        "An AI-host replacement must expose provider-compatible API routes, catalog/download/running-model UX, chat/API console, logs, settings, templates, hardware policy, runner/plugin boundaries, and direct local model-file inference without upstream proxying.",
                    GeneratedSolutionArchetype.BotBackend =>
                        "A bot backend replacement must expose webhook ingress, conversation state, command routing, moderation/retry queues, settings/logs, optional Python interop, and permission gates.",
                    _ =>
                        "A generic generated solution must still show which source behaviors are represented, stubbed, or out of scope."
                };
                var promiseReview = promiseModules.Count == 0
                    ? "No dynamic promise modules were detected from the council answer. Review the base archetype files and the original prompt manually."
                    : string.Join(
                        Environment.NewLine,
                        promiseModules.Select(module => $"- `{module.Route}` / `{module.FileName}`: {module.Summary}"));

                return $$"""
            # Source Fidelity

            Generated project: `{{projectName}}`

            ## Acceptance Rule

            A generated solution is not accepted merely because it compiles. It must preserve the requested source application's recognizable workflows, navigation, service boundaries, persistence shape, diagnostics, and download/artifact behavior.

            ## Expected Shape

            {{expectedShape}}

            ## Dynamic Promise Modules

            {{promiseReview}}

            ## Review Files

            - `src/{{projectName}}/Components/Pages/SourceFidelity.razor`
            - `src/{{projectName}}/Services/GeneratedSourceFidelityService.cs`
            - `PROJECT_INDEX.md`
            - `.localgpt-generation.json`
            - `ARCHITECTURE.md`

            ## Benchmark Guidance

            LocalGPT replacement benchmarks should inspect this file and the generated source-fidelity service before awarding architecture points. A page-only solution should score low when it misses source workflows, service contracts, or persistence boundaries.

            ## Integration Rule

            This remains a sandbox artifact. Copying generated files into a real repo requires explicit user approval, a build, and a focused smoke test.
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSourceFidelityDoc projectName:{projectName} archetype:{archetype.ToString()} promiseModules:{promiseModules.ToString()}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates promise map doc as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council text operation and used when producing its result.</param>
        /// <param name="promiseModules">Generated promise module dependency used by the council text workflow to provide the corresponding application capability.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GeneratePromiseMapDoc(
            string projectName,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            IReadOnlyList<GeneratedPromiseModule> promiseModules, ILogger logger)
        {
            try
            {
                var moduleRows = promiseModules.Count == 0
                ? "| No dynamic modules detected | The generated solution must be reviewed against the request manually. |"
                : string.Join(
                    Environment.NewLine,
                    promiseModules.Select(module =>
                        $"| `{module.Route}` | `{module.FileName}` | {module.Summary} | {string.Join(", ", module.Areas)} |"));

                return $$"""
            # Promise Map

            Generated project: `{{projectName}}`

            This file maps the user's request and the council's promised architecture into generated review surfaces. It exists so LocalGPT does not ship a generic shell after the council described a richer application.

            ## Dynamic Modules

            | Route | File | Promise Preserved | Review Areas |
            | --- | --- | --- | --- |
            {{moduleRows}}

            ## Artifact Rule

            A concrete downloadable target is not enough by itself. If the council creates a blocking user decision poll, artifact generation must pause until the user answers or grants safe sandbox auto-choice. If no blocking poll remains, the generated artifact should preserve the council's promised workflows in pages, services, docs, and validation notes.

            ## Request Excerpt

            ```text
            {{TrimForCodeComment(request.Prompt, 1200, logger)}}
            ```

            ## Council Excerpt

            ```text
            {{TrimForCodeComment(result.FinalAnswer, 1600, logger)}}
            ```
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateSourceFidelityDoc");
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Generates design review doc as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="archetype">Archetype value supplied to the council text operation and used when producing its result.</param>
        /// <param name="promiseModules">Generated promise module dependency used by the council text workflow to provide the corresponding application capability.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateDesignReviewDoc(
            string projectName,
             GeneratedSolutionArchetype archetype,
            IReadOnlyList<GeneratedPromiseModule> promiseModules, ILogger logger)
        {
            try
            {
                var moduleList = promiseModules.Count == 0
                ? "- No dynamic promise modules were detected. The design stays with the base generated shell."
                : string.Join(Environment.NewLine, promiseModules.Select(module => $"- `{module.Title}` uses DevExpress grid/form patterns to expose {string.Join(", ", module.Areas)}."));
                var archetypeName = archetype.ToString();

                return $$"""
            # Design Review

            Generated project: `{{projectName}}`

            ## Layout Choice

            The artifact uses a compact operational layout: top navigation, dashboard/status grid, source-fidelity review, and one page per detected promise module. It avoids a marketing-style landing page because generated LocalGPT artifacts are tools first.

            ## Base Archetype

            `{{archetypeName}}`

            ## Dynamic UI Modules

            {{moduleList}}

            ## Components Used

            - DevExpress Blazor navigation-friendly pages.
            - `DxGrid` for scan-friendly operational state.
            - `DxFormLayout`, `DxTextBox`, and `DxMemo` for bounded review and settings surfaces.
            - Bootstrap-compatible CSS for responsive grid and toolbar layout.
            - Paired SVG navigation icons with line/solid states.

            ## Mocked Versus Real

            Mocked: generated rows, status values, and sample implementation boundaries.

            Real: routable Razor files, compileable project structure, static CSS/icons, docs, generation manifest, and source-fidelity contract.

            ## Needs Wiring

            - Replace generated sample services with real backend services.
            - Add EF/SQLite persistence if user-visible state must survive restarts.
            - Add route smoke tests for every generated endpoint.
            - Add real DevExpress report/export implementation only after package/API verification and user approval.
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateDesignReviewDoc projectName:{projectName} archetype:{archetype.ToString()} promiseModules:{promiseModules.ToString()}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates solution build and run doc as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="isAiHostLab">Value indicating whether is AI host lab should apply to this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionBuildAndRunDoc(string projectName, bool isAiHostLab, ILogger logger)
        {
            try
            {
                var smokeRoute = isAiHostLab
                ? "Open `/api-console`, `/model-downloads`, `/runner-plugins`, and `/settings`; then call `/api/version`, `/api/tags`, `/api/localgpt/runner/capability`, and `/api/chat` to verify route shape, local model-file runner readiness, and no upstream proxy fallback."
                : "Open `/implementation-plan` and verify implementation steps are visible.";
                return $$"""
            # Build And Run

            ## Requirements

            - .NET 10 SDK
            - DevExpress Blazor 25.1 package feed/license access
            - Visual Studio 2026 or `dotnet` CLI

            ## Commands

            ```powershell
            dotnet restore
            dotnet build
            dotnet run --project .\src\{{projectName}}\{{projectName}}.csproj
            ```

            ## Expected Checks

            - `/` shows the generated index page with navigation.
            - `/dashboard` shows the generated health/status grid.
            - `/source-fidelity` shows source-signal, boundary, status, and evidence rows.
            - {{smokeRoute}}
            - `PROJECT_INDEX.md` and `.localgpt-generation.json` describe the selected archetype.

            ## Validation Honesty

            Do not claim build success unless `dotnet build` completed and the command output is available. Do not claim production readiness; this zip is a sandbox artifact.
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateSolutionBuildAndRunDoc projectName:{projectName} isAiHostLab:{isAiHostLab}");
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Generates LocalGPT generation JSON as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council text operation and used when producing its result.</param>
        /// <param name="isAiHostLab">Value indicating whether is AI host lab should apply to this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateLocalGptGenerationJson(
            string projectName,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab, ILogger logger)
        {
            try
            {
                var projectKind = isAiHostLab ? "dotnet_service" : "localgpt_feature";
                var targetPlatform = isAiHostLab
                    ? "dotnet10_aspnetcore_devexpress_blazor_ai_host_control_plane"
                    : "dotnet10_aspnetcore_devexpress_blazor_localgpt_feature";
                var detailPage = isAiHostLab ? "ApiConsole.razor" : "ImplementationPlan.razor";
                var validationNotes = isAiHostLab
                    ? "Required docs, source-fidelity files, manifest, navigation, paired nav icons, index, dashboard, model catalog, API console, chat, running-models, model-download, templates, hardware, runner-plugin, logs, settings, and AI-host architecture service files were present before zipping."
                    : "Required docs, source-fidelity files, manifest, navigation, paired nav icons, index, dashboard, knowledge table, and implementation-plan files were present before zipping.";
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
                var aiHostGeneratedFiles = isAiHostLab
                    ? $$"""
                ,
                "src/{{projectName}}/Components/Pages/Chat.razor",
                "src/{{projectName}}/Components/Pages/RunningModels.razor",
                "src/{{projectName}}/Components/Pages/ModelDownloads.razor",
                "src/{{projectName}}/Components/Pages/Templates.razor",
                "src/{{projectName}}/Components/Pages/Hardware.razor",
                "src/{{projectName}}/Components/Pages/RunnerPlugins.razor",
                "src/{{projectName}}/Components/Pages/Logs.razor",
                "src/{{projectName}}/Components/Pages/Settings.razor",
                "src/{{projectName}}/Services/GeneratedAiHostArchitectureServices.cs"
                """
                    : string.Empty;

                return $$"""
            {
              "schema": "localgpt-generation-contract/v1",
              "project_kind": "{{projectKind}}",
              "target_platform": "{{targetPlatform}}",
              "project_name": "{{EscapeJsonString(projectName, logger)}}",
              "generated_at_utc": "{{DateTime.UtcNow:O}}",
              "complexity": "normal",
              "needs_datagen": false,
              "needs_tests": true,
              "needs_native_commands": {{(isAiHostLab ? "true" : "false")}},
              "needs_index": true,
              "needs_version_resolver": false,
              "model_names": "{{EscapeJsonString(string.Join(", ", result.ModelNames), logger)}}",
              "requested_features": "{{EscapeJsonString(TrimForCodeComment(request.Prompt, 900, logger), logger)}}",
              "validation_status": "GeneratedFilesValidatedOnly",
              "validation_notes": "{{EscapeJsonString(validationNotes, logger)}}",
              "build_test_result_provenance": "LocalGPT validated required files and contract JSON before zipping. dotnet build was not run for this sandbox artifact, so no build success is claimed.",
              "expected_entrypoints": [
                "src/{{projectName}}/Program.cs",
                "src/{{projectName}}/Components/GeneratedNavigation.razor",
                "src/{{projectName}}/Components/Pages/Index.razor",
                "src/{{projectName}}/Components/Pages/GeneratedDashboard.razor",
                "src/{{projectName}}/Components/Pages/SourceFidelity.razor",
                "src/{{projectName}}/Components/Pages/{{detailPage}}"{{aiHostExpectedEntryPoints}}
              ],
              "generated_files": [
                "{{projectName}}.sln",
                "README.md",
                "PROJECT_INDEX.md",
                "ARCHITECTURE.md",
                "SOURCE_FIDELITY.md",
                "PROMISE_MAP.md",
                "DESIGN_REVIEW.md",
                "BUILD_AND_RUN.md",
                ".localgpt-generation.json",
                "LocalGPT.GenerationManifest.json",
                "src/{{projectName}}/{{projectName}}.csproj",
                "src/{{projectName}}/Program.cs",
                "src/{{projectName}}/Components/App.razor",
                "src/{{projectName}}/Components/Routes.razor",
                "src/{{projectName}}/Components/GeneratedNavigation.razor",
                "src/{{projectName}}/Components/Pages/Index.razor",
                "src/{{projectName}}/Components/Pages/GeneratedDashboard.razor",
                "src/{{projectName}}/Components/Pages/SourceFidelity.razor",
                "src/{{projectName}}/Components/Pages/{{detailPage}}"{{aiHostGeneratedFiles}},
                "src/{{projectName}}/Services/GeneratedHealthSummaryService.cs",
                "src/{{projectName}}/Services/GeneratedSourceFidelityService.cs",
                "src/{{projectName}}/Models/GeneratedHealthCard.cs",
                "src/{{projectName}}/wwwroot/app.css",
                "src/{{projectName}}/wwwroot/icons/nav/dashboard-line.svg",
                "src/{{projectName}}/wwwroot/icons/nav/dashboard-solid.svg",
                "src/{{projectName}}/wwwroot/icons/nav/catalog-line.svg",
                "src/{{projectName}}/wwwroot/icons/nav/catalog-solid.svg",
                "src/{{projectName}}/wwwroot/icons/nav/detail-line.svg",
                "src/{{projectName}}/wwwroot/icons/nav/detail-solid.svg"
              ],
              "safety": "Sandbox artifact only. Integration requires explicit user approval.",
              "archetype_difference": "{{(isAiHostLab ? "AI host lab includes API routes, model catalog, downloads, settings, and a native local-model-file runner contract; upstream proxying is explicitly out of scope." : "LocalGPT feature sandbox includes implementation-plan and knowledge-table pages rather than AI host compatibility workflows.")}}"
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateLocalGptGenerationJson");
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Generates solution manifest as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="projectName">Project name value supplied to the council text operation and used when producing its result.</param>
        /// <param name="solutionGuid">Identifier of the solution gu to use for this operation.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the council text operation and used when producing its result.</param>
        /// <param name="isAiHostLab">Value indicating whether is AI host lab should apply to this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string GenerateSolutionManifest(
            string projectName,
            string solutionGuid,
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isAiHostLab, ILogger logger)
        {
            try
            {
                var sourceGoal = isAiHostLab
                ? ".NET 10 ASP.NET Core and DevExpress Blazor AI host control-plane lab with explicit provider, plugin, script, and native-runner adapter boundaries"
                : "LocalGPT/TacosPortalOpen-style .NET 10 Blazor and DevExpress generation";

                return
                $$"""
            {
              "projectName": "{{EscapeJsonString(projectName, logger)}}",
              "solutionGuid": "{{EscapeJsonString(solutionGuid, logger)}}",
              "generatedAtUtc": "{{DateTime.UtcNow:O}}",
              "modelNames": "{{EscapeJsonString(string.Join(", ", result.ModelNames), logger)}}",
              "artifactKind": "WholeSolutionZip",
              "sourceGoal": "{{EscapeJsonString(sourceGoal, logger)}}",
              "designContract": "Bootstrap v5 layout, DevExpress Blazor controls, and paired line/solid SVG navigation icons.",
              "validationStatus": "GeneratedFilesValidatedOnly",
              "buildTestResultProvenance": "Required files and contract metadata were validated before zipping. No generated-project build success is claimed.",
              "request": "{{EscapeJsonString(TrimForCodeComment(request.Prompt, 1400, logger), logger)}}",
              "finalAnswer": "{{EscapeJsonString(TrimForCodeComment(result.FinalAnswer, 1400, logger), logger)}}",
              "safety": "Sandbox artifact only. Integration requires explicit user approval."
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GenerateLocalGptGenerationJson");
                return string.Empty;
            }
        }

    }
}
