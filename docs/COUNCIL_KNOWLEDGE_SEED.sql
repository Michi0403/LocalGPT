-- LocalGPT AI Council knowledge seed.
-- This file is intentionally git-tracked so source-backed council knowledge can be restored into SQLite.
-- The application imports it with INSERT OR IGNORE, so user edits to existing rows are not overwritten.

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  '54bc5a58-3a5a-4d6d-9545-13c9813f2ad8',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'DevExpress source map for LocalGPT and TacosPortalOpen generation',
  'DevExpress Blazor',
  'For LocalGPT/TacosPortalOpen-style generation, prefer DevExpress Blazor UI controls in real Razor components and keep document/report/Office generation in ASP.NET Core backend services with safe download routes. Use DevExpress official docs for current API shape and DevExpress-Examples repositories for patterns. Important example families include Blazor AI chat function calling, multi-model chat with history, Grid plus EF Core binding/editing, FormLayout detail/edit forms, native Blazor reporting, JavaScript-based Blazor reporting, ASP.NET Core reporting best practices, Office File API Web API backends, Word/Spreadsheet Document API in Blazor Server, and Skia-based reporting for ASP.NET Core.',
  'LocalGPT SQL seed',
  'DevExpress Examples org: https://github.com/DevExpress-Examples; Blazor components docs: https://docs.devexpress.com/Blazor/400725/blazor-components; Reporting docs: https://docs.devexpress.com/XtraReports/2162/reporting',
  'seed; devexpress; blazor; aspnetcore; reporting; tacosportalopen; localgpt',
  94,
  1,
  1,
  0
),
(
  'd0dcfda6-9081-437b-91be-7d9b9628065a',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'DevExpress DxAIChat generation rules',
  'DevExpress Blazor',
  'DxAIChat is the correct LocalGPT chat surface for model selection, visible streaming, file attachment UX, prompt suggestions, save/load, tool calling, and custom AI providers. For local Ollama/LM Studio, LocalGPT should supply the Microsoft.Extensions.AI IChatClient bridge and keep model/provider settings outside the component. Use UseStreaming for responsive output. For rich Markdown responses, set the response format/template and sanitize generated HTML before rendering. Tool calling should be exposed as explicit LocalGPT DXAiFunctions and diagnostic routes rather than arbitrary self-expansion.',
  'LocalGPT SQL seed',
  'DxAIChat docs: https://docs.devexpress.com/Blazor/DevExpress.AIIntegration.Blazor.Chat.DxAIChat; Function calling example: https://github.com/DevExpress-Examples/blazor-ai-chat-function-calling; Multi-LLM chat example: https://github.com/DevExpress-Examples/blazor-ai-chat-with-multiple-llm-services; Chat history example: https://github.com/DevExpress-Examples/blazor-multi-model-ai-chat-with-history',
  'seed; devexpress; dxaichat; ai-chat; ollama; tools; streaming',
  95,
  1,
  1,
  0
),
(
  'dff6dd3c-1f81-46f1-a4a2-1a7981d790c5',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'DevExpress Grid/FormLayout/EF Core patterns',
  'DevExpress Blazor',
  'For LocalGPT database admin and generated business pages, use DxGrid for tabular data, DxGridDataColumn for columns, command/toolbar actions for CRUD, DxFormLayout for edit/detail forms, and explicit validation/user feedback. Persist grid layout or user presets in EF/SQLite when they affect future work. Prefer service methods over direct data access in Razor when logic grows. DevExpress examples show EF Core custom data sources, batch editing, Web API data binding, toolbar CRUD buttons, popup/separate edit forms, save/restore layout, context menus, and FormLayout detail views.',
  'LocalGPT SQL seed',
  'Grid docs: https://docs.devexpress.com/Blazor/403143/components/grid; Grid examples: https://docs.devexpress.com/Blazor/404035/components/grid/examples; Example repos: https://github.com/DevExpress-Examples/blazor-grid-custom-datasource-with-ef-core; https://github.com/DevExpress-Examples/blazor-grid-batch-editing; https://github.com/DevExpress-Examples/blazor-grid-and-toolbar; https://github.com/DevExpress-Examples/blazor-grid-save-restore-layout; https://github.com/DevExpress-Examples/blazor-grid-display-detail-information-using-form-layout',
  'seed; devexpress; dxgrid; formlayout; efcore; crud; sqlite',
  92,
  1,
  1,
  0
),
(
  '4ec5b7df-1e11-4651-bb08-51e27d9b58ee',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'DevExpress Reporting backend and Blazor/WASM rules',
  'DevExpress Reporting',
  'DevExpress Reporting for LocalGPT should be implemented primarily in ASP.NET Core backend services/controllers. The Blazor frontend should host status, parameter UI, viewer/designer components, and safe download links. Blazor Reporting supports native Report Viewer, JavaScript-based Document Viewer, JavaScript-based Report Designer, and standalone parameter panel. Blazor WASM reporting examples require server-side reporting endpoints/controllers/providers/storage because report generation and design services are backend responsibilities. For PDF/DOCX/XLSX generation, prefer backend services and artifact/download routes instead of generating binary payloads in chat.',
  'LocalGPT SQL seed',
  'ASP.NET Core Reporting docs: https://docs.devexpress.com/AspNetCore/400597/reporting; Blazor Reports docs: https://docs.devexpress.com/Blazor/401706/components/reports; Blazor Reporting overview: https://docs.devexpress.com/XtraReports/401676/web-reporting/blazor-reporting; WASM example: https://github.com/DevExpress-Examples/reporting-blazor-wasm-get-started',
  'seed; devexpress; reporting; blazor-wasm; aspnetcore; documents; downloads',
  94,
  1,
  1,
  0
),
(
  '3a0cb09f-91b0-4851-9139-027a84a8f72b',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'DevExpress Reporting best practices for production-like apps',
  'DevExpress Reporting',
  'When LocalGPT generates reporting features, include production concerns from DevExpress ASP.NET Core Reporting best practices: async mode where appropriate, memory and cache management, closing viewers before removing UI regions, database connection handling, CSRF protection, authentication/authorization for reports and data, custom exception handling/logging, localization, skeleton/loading UI, and server-side error diagnostics. For cross-platform ASP.NET Core reporting, prefer DevExpress.Drawing with SkiaSharp when System.Drawing is unsuitable.',
  'LocalGPT SQL seed',
  'Best practices repo: https://github.com/DevExpress-Examples/AspNetCore.Reporting.BestPractices; Support mirror: https://supportcenter.devexpress.com/ticket/details/t939061/asp-net-core-reporting-best-practices; Skia example: https://github.com/DevExpress-Examples/reporting-use-devexpress-drawing-skia-engine; Error handling: https://github.com/DevExpress-Examples/reporting-aspnet-core-handle-server-side-errors',
  'seed; devexpress; reporting; security; performance; skia; diagnostics',
  93,
  1,
  1,
  0
),
(
  '3bb74597-faf8-41b4-a497-2a13c31a8d64',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'DevExpress Office and document generation for LocalGPT',
  'DevExpress Office',
  'For AI-requested Office/PDF examples, LocalGPT should generate backend-owned services and download artifacts. DevExpress examples show Office File API in ASP.NET Core Web API apps, converting Word/Excel to HTML, Word Document API mail-merge style letters in Blazor Server, and Spreadsheet Document API generated workbooks. The frontend should request generation, show progress/errors, and expose returned artifact links. Do not embed large binary content in DXAiChat messages.',
  'LocalGPT SQL seed',
  'Office Web API example: https://github.com/DevExpress-Examples/office-file-api-in-web-api-app; Dockerized Office API example: https://github.com/DevExpress-Examples/office-file-api-dockerize-application; Word API Blazor Server example: https://github.com/DevExpress-Examples/word-document-api-generate-and-send-letters-within-blazor-server-app; Spreadsheet API Blazor Server example: https://github.com/DevExpress-Examples/spreadsheet-document-api-create-loan-amortization-schedule-within-blazor-server-app',
  'seed; devexpress; office-file-api; word; spreadsheet; artifact-download',
  90,
  1,
  1,
  0
),
(
  'af432275-7197-4475-baa0-6722cc0ad3a8',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Microsoft Blazor .NET 8-10 architecture rules',
  'Microsoft Blazor',
  'For LocalGPT/TacosPortalOpen generation, distinguish Blazor Web App render modes from standalone Blazor WebAssembly. A Blazor Web App can use static SSR, Interactive Server, Interactive WebAssembly, or Interactive Auto when services/endpoints are configured. Standalone Blazor WebAssembly runs client-side and does not use render modes. Parameters passed from static parents to interactive children must be JSON serializable. LocalGPT desktop currently favors Interactive Server inside WebView2 for easier debugging and backend access; use WASM when offline/static-client behavior is explicitly requested.',
  'LocalGPT SQL seed',
  'Blazor fundamentals: https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/?view=aspnetcore-10.0; Render modes: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0; Hosting models: https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models; Tooling: https://learn.microsoft.com/en-us/aspnet/core/blazor/tooling?view=aspnetcore-10.0',
  'seed; microsoft; blazor; dotnet10; render-modes; wasm; interactive-server',
  95,
  1,
  1,
  0
),
(
  '1d61a4c7-a1dd-4a26-904f-990465be07ac',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Microsoft Blazor WASM and static asset rules',
  'Microsoft Blazor',
  'Blazor WebAssembly downloads the .NET runtime, app assemblies, and static assets to the browser. Payload size matters: trimming, compression, and browser caching are core performance concerns. Hosted Blazor WebAssembly uses an ASP.NET Core backend to serve the client and expose APIs; standalone WASM can be hosted as static files. In LocalGPT, WASM plus DevExpress should be treated as a separate client/front-end target while native commands, EF writes, reporting generation, and artifact creation stay in backend services.',
  'LocalGPT SQL seed',
  'Hosting models: https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models; Host and deploy: https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/?view=aspnetcore-10.0; Blazor static files: https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/static-files?view=aspnetcore-10.0; WASM security: https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/?view=aspnetcore-10.0',
  'seed; microsoft; blazor-wasm; static-assets; hosted-wasm; security',
  93,
  1,
  1,
  0
),
(
  'ef188b21-174e-4b18-a929-f2a5eba1bc0d',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Microsoft ASP.NET Core backend service rules',
  'ASP.NET Core',
  'For LocalGPT, backend capabilities belong in ASP.NET Core services and explicit routes/controllers/minimal APIs. Use DI to register services, use options/config only for bootstrap/logging/runtime settings, and persist user/application state in EF/SQLite when it should survive restarts. Use static asset guidance for Blazor assets and WebView2 packaging. Use Minimal APIs for focused diagnostics/artifact routes, and keep risky native operations behind deliberate backend services with validation and logging.',
  'LocalGPT SQL seed',
  'ASP.NET Core docs: https://learn.microsoft.com/en-us/aspnet/core/; Minimal API tutorial: https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0; Dependency injection: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0; Static files: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0',
  'seed; microsoft; aspnetcore; minimal-api; dependency-injection; static-files',
  94,
  1,
  1,
  0
),
(
  '8b8e8906-b25b-4846-9681-ac6e9873a91d',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Microsoft EF Core and SQLite rules for Blazor apps',
  'EF Core',
  'DbContext is a short-lived unit-of-work and is not thread-safe. In normal ASP.NET Core requests, AddDbContext scoped lifetime is a good default. Blazor Server circuits do not align with a per-request scope, so prefer IDbContextFactory or short-lived contexts for UI events/background work. Always await EF async calls immediately. Track migrations in source control for schema evolution; for LocalGPT local admin tables, lightweight schema helpers plus idempotent SQL seeds are acceptable when preserving user-edited SQLite data. For performance, project only needed columns, index query predicates, avoid cartesian explosion, and use pagination/keyset pagination for large tables.',
  'LocalGPT SQL seed',
  'DbContext lifetime: https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/; EF migrations: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/; Efficient querying: https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying; SQLite provider: https://learn.microsoft.com/en-us/ef/core/providers/sqlite/',
  'seed; microsoft; efcore; sqlite; blazor-server; dbcontext; migrations',
  95,
  1,
  1,
  0
),
(
  '3ade0cb2-1043-4314-8a28-27ecad8ff2a3',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Microsoft official GitHub sample map',
  'Microsoft Samples',
  'Use only official Microsoft/dotnet repositories for baseline comparisons unless the user explicitly supplies another project. dotnet/blazor-samples contains .NET 10 Blazor Web App, Blazor WebAssembly, SignalR, Web API call, auth, OIDC/BFF, Windows auth, WASM logging, React interop, web workers, and MAUI Blazor samples. dotnet/efcore is the EF Core source and Microsoft.Data.Sqlite home. dotnet/EntityFramework.Docs and dotnet/AspNetCore.Docs are documentation sources. dotnet/eShop is the current reference app for a services-based .NET Aspire architecture. dotnet/samples contains code referenced by .NET docs.',
  'LocalGPT SQL seed',
  'Blazor samples: https://github.com/dotnet/blazor-samples; ASP.NET Core source: https://github.com/dotnet/aspnetcore; ASP.NET Core docs: https://github.com/dotnet/AspNetCore.Docs; EF Core source: https://github.com/dotnet/efcore; EF docs: https://github.com/dotnet/EntityFramework.Docs; eShop: https://github.com/dotnet/eShop; .NET samples: https://github.com/dotnet/samples',
  'seed; microsoft; github; samples; blazor; efcore; aspnetcore; eshop',
  94,
  1,
  1,
  0
),
(
  '49161f80-6ca8-4c51-9ba2-7763e9363d93',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'LocalGPT/TacosPortalOpen generation target',
  'LocalGPT generation',
  'The main generation target is LocalGPT/TacosPortalOpen-style .NET 8-10 Blazor with DevExpress, especially Blazor Web App and Blazor WebAssembly combinations. Generated UI artifacts should be real .razor files with @page when routable, correct render mode for the chosen hosting model, dependency injection, DevExpress controls, concise tooltips/help, and no string-builder fake pages. Generated backend artifacts should be services/routes/controllers with EF/SQLite persistence where durable state is needed. Generated report/document artifacts should use backend services and downloadable files. The council should compare proposals against official DevExpress examples and Microsoft dotnet samples before recommending integration.',
  'LocalGPT SQL seed',
  'Local docs: docs/BLAZOR_DEVEXPRESS_AI_GENERATION.md; docs/ARCHITECTURE_FOR_AI.md; DevExpress examples org; dotnet/blazor-samples 10.0; user-provided TacosPortalOpen sample zip',
  'seed; localgpt; tacosportalopen; blazor; devexpress; wasm; code-generation',
  96,
  1,
  1,
  0
),
(
  '8de321d9-6255-41d6-b9aa-c051d8782629',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'LocalGPT release build and MSIX deployment lessons',
  'LocalGPT packaging',
  'The 2026-06-02 release cycle showed that the WinUI/WebView2 package must be built with Visual Studio MSBuild, not SDK-only dotnet build, because the .wapproj DesktopBridge project uses Visual Studio targets. Build scripts must check native command exit codes explicitly because PowerShell does not fail just because dotnet or MSBuild returned nonzero. Clean generated obj intermediates for the selected platform/configuration when StaticWebAssets JSON or NuGet asset caches show null-byte corruption.',
  'LocalGPT release cycle memory',
  'Scripts: LocalGPTWebviewWrapper/build/Build-LocalGptPackage.ps1; LocalGPTWebviewWrapper/build/Publish-LocalGptRelease.ps1; package project: LocalGPTWebviewWrapper/LocalGPTWebviewWrapper (Package); release tag v0.1.1-ai-council.20260602',
  'seed; localgpt; msix; desktopbridge; powershell; release; dotnet10; build',
  98,
  1,
  1,
  0
),
(
  '5f3112ad-2f5c-4af9-b079-29502d2f5ee7',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'LocalGPT WebView2 and DevExpress static asset packaging',
  'WinUI WebView2',
  'A plain unstyled WebView2 page meant the packaged app served Razor markup but not Blazor/DevExpress static assets. The fix was to publish the LocalGPT web project and add published wwwroot/_content, wwwroot/_framework, LocalGPT.styles.css, staticwebassets manifests, and LocalGPT.deps.json into the MSIX payload with AppxPackagePayload TargetPath entries. Copying files only into a loose AppX layout was not enough; package.map.txt had to contain the payload entries.',
  'LocalGPT release cycle memory',
  'Package payload fix in LocalGPTWebviewWrapper (Package).wapproj; diagnostics in LocalGPTWebviewWrapper/MainWindow.xaml.cs; snapshots under %LOCALAPPDATA%/LocalGPT/WebView2Diagnostics',
  'seed; localgpt; webview2; devexpress; static-web-assets; blazor; msix; diagnostics',
  98,
  1,
  1,
  0
),
(
  'e5b7f32a-2328-45ad-aea4-9a703f01d4f9',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'LocalGPT AI Council generation and release-test workflow',
  'AI Council',
  'For release-grade AI Council feature work, prefer deterministic LocalGPT diagnostic routes first, then a final WebView2 frontend smoke. The Living Cities datapack benchmark route validated Minecraft generation without loading Ollama. The council artifact smoke routes generated real Razor, C#, DLL, and solution zip downloads. Live DXAiChat model smoke tests are valuable, but a single slow reasoning model that times out is a model-output health signal, not proof that deterministic artifact generation failed.',
  'LocalGPT release cycle memory',
  'Routes: /__diag/minecraft/datapack-benchmark, /__diag/council/artifact-smoke?target=solution, /__diag/council/artifact-smoke?target=ollama, /__artifacts/council/{fileName}; WebView2 smoke flags in %LOCALAPPDATA%/LocalGPT/runtime',
  'seed; ai-council; diagnostics; minecraft; datapack; blazor-generation; artifacts; dxaichat',
  96,
  1,
  1,
  0
),
(
  'b9b11e54-90be-46c4-9b5f-6e3952896608',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Working with Michi0403 productively',
  'Collaboration',
  'Michi0403 works best with an engineering partner that acts, verifies, and reports concrete evidence. Do not stop at proposals when he asks to fix, build, or release. Make small meaningful commits and push regularly. Be honest about failures but do not overdramatize hardware or Windows instability; distinguish GPU driver resets, display sleep, package errors, deployment errors, and model latency with logs. Use compiler/build output as the deciding authority. ' ||
  'When paths are unclear, offer a small poll with concrete options and treat the user choice as binding. Generated code and self-expansion stay sandboxed until Michi approves integration. His stubbornness is usually a product-quality signal; turn it into tests, diagnostics, and council knowledge instead of arguing.',
  'LocalGPT workflow memory',
  'docs/LOCALGPT_WORKFLOW_MEMORY.md; diagnostics and release cycle around v0.1.1-ai-council.20260602; user collaboration notes from the LocalGPT repair thread',
  'seed; collaboration; michi0403; workflow; autonomy; diagnostics; commits',
  99,
  1,
  1,
  0
),
(
  'dc39f6ee-29de-4030-ae4f-c37126cffb4a',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Legacy Exam Ref 70-483 C# architecture memory',
  'Legacy .NET Framework',
  'Treat Michi0403''s Exam Ref 70-483 history, Rob Miles admiration, and the user-supplied Sample-Code-master.zip as respected legacy C# learning context. This is obsolete as a default target for new LocalGPT generation: prefer modern .NET 8-10, SDK-style projects, nullable, analyzers, dependency injection, appsettings/options, async APIs, tests, and current Blazor/ASP.NET Core patterns. ' ||
  'Use the legacy material for thinking quality when relevant: clear type design, exceptions, TPL tasks, PLINQ, async/threading reasoning, LINQ, delegates/events, disposal, IO/serialization, validation, and small focused examples. If the user explicitly asks for .NET Framework, generate classic project shapes honestly; otherwise translate the ideas into modern .NET. Do not copy .vs, bin, obj, exe, pdb, or old build outputs from sample archives into git or release artifacts.',
  'User-provided learning source',
  'Sample archive: C:/Users/micha/Downloads/Sample-Code-master.zip; observed listings include Exceptions in PLINQ, Create a task, Run a task, and Task Factory; Exam Ref 70-483 Programming in C#; Rob Miles is recorded here as a user-respected C# learning influence, not as an objective global ranking claim.',
  'seed; csharp; exam-70-483; dotnet-framework; legacy; rob-miles; tpl; plinq; architecture; obsolete',
  92,
  1,
  1,
  0
),
(
  '3f615c30-8f60-48b0-b904-1968f43745c0',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Generated project archetype contract',
  'AI Council Generation',
  'Whole-project generation must classify the archetype before writing files. The supported project_kind values include fabric_mod, neoforge_mod, paper_plugin, datapack, localgpt_feature, and dotnet_service. ' ||
  'Every whole project must include PROJECT_INDEX.md, ARCHITECTURE.md, BUILD_AND_RUN.md, .localgpt-generation.json, and a platform-correct source/resource layout. ' ||
  'PROJECT_INDEX.md must explain purpose, entry points, generated files, build/run commands, and validation status. Reject generic folder soup, missing index/home routes, string-builder fake Razor pages, wrong platform metadata, and any claim of build success without command output.',
  'User-approved generation advice',
  'Local doc: docs/GENERATION_ARCHETYPE_CONTRACTS.md; user advice attachment: C:/Users/micha/.codex/attachments/e23e6bf9-52e7-4f3d-97f8-4e9a005efb5a/pasted-text.txt',
  'seed; generation; archetype; project-index; manifest; validation; localgpt; minecraft; dotnet',
  98,
  1,
  1,
  0
),
(
  '49c3f0d6-a359-47b4-a32e-35e6309ef68d',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Microsoft Learn modern .NET architecture rules for generation',
  '.NET Architecture',
  'Use Microsoft .NET architecture guidance to decide shape before code. Prefer a cohesive Blazor/ASP.NET Core app when a feature does not need independent deployment. ' ||
  'Introduce service-oriented separation only around real boundaries such as independent scaling, external runner adapters, background work, downloads, reporting/document generation, or integrations. ' ||
  'Keep UI interaction in Razor components and business/native/data work in injected services. Use appsettings for bootstrap/runtime configuration and EF/SQLite for durable user/application state. ' ||
  'Include diagnostics or health checks for runtime features. Design APIs and models with clear naming, stable contracts, debuggability, and future evolution in mind.',
  'Microsoft Learn source-backed seed',
  'Architecture overview: https://learn.microsoft.com/en-us/dotnet/architecture/; Modern ASP.NET Core web apps: https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/; ' ||
  'Modern Web App pattern: https://learn.microsoft.com/en-us/azure/architecture/web-apps/guides/enterprise-app-patterns/modern-web-app/dotnet/guidance; ' ||
  'Blazor architecture guide: https://learn.microsoft.com/en-us/dotnet/architecture/blazor-for-web-forms-developers/; Framework design guidelines: https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/; Library guidance: https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/',
  'seed; microsoft; dotnet; architecture; blazor; aspnetcore; library-guidance; generation',
  97,
  1,
  1,
  0
),
(
  '180eecde-0c08-4c48-94b6-8461293681d3',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Source hygiene guard for public repository quality',
  'Repository Hygiene',
  'Physical source formatting is a hard quality gate, not an editor soft-wrap preference. Program.cs, NativeCommandRunner.cs, AiContextBootstrapService.cs, README.md, and other tracked source/docs must keep normal raw newline counts and avoid giant physical lines. ' ||
  'Run build/Assert-SourceFormatting.ps1 before commits. GitHub Actions workflow .github/workflows/source-hygiene.yml runs the same guard on push and pull request for tracked .cs, .razor, .md, .ps1, and .json files.',
  'LocalGPT workflow memory',
  'Script: build/Assert-SourceFormatting.ps1; CI: .github/workflows/source-hygiene.yml; doc: docs/LOCALGPT_WORKFLOW_MEMORY.md',
  'seed; source-hygiene; formatting; ci; raw-lines; public-repo',
  98,
  1,
  1,
  0
),
(
  'd0711da3-7c75-4764-9636-bc452cb72453',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Minecraft dependency resolver before workspace generation',
  'Minecraft Builder',
  'Before generating Fabric, NeoForge, Paper, or datapack workspaces, call /__diag/minecraft/dependency-version or use MinecraftDependencyVersionCatalog. It returns requested/matched Minecraft versions, Java/Gradle versions, Fabric loader/API, NeoForge, Paper API, datapack pack_format, exact-match flags, and NeedsVerification. ' ||
  'Fallback mappings are allowed for smoke tests but must be source-checked against official Fabric, NeoForge, Paper, Gradle, and Minecraft documentation before release or friend testing.',
  'LocalGPT workflow memory',
  'Route: /__diag/minecraft/dependency-version?loader=fabric&minecraftVersion=1.21.4; source: LocalGPTWebviewWrapper/LocalGPT/Services/MinecraftDependencyVersionCatalog.cs',
  'seed; minecraft; dependency-resolver; fabric; neoforge; paper; datapack; gradle; java',
  92,
  1,
  1,
  0
),
(
  '32ecda45-3fa8-42f1-86c6-d8b276f99861',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Bootstrap v5 and DevExpress Blazor design generation',
  'Blazor Design',
  'Use Bootstrap v5 for macro layout and DevExpress Blazor for real application controls. Bootstrap owns containers, rows, columns, gutters, spacing, flex alignment, responsive breakpoints, and small utility classes. ' ||
  'DevExpress owns grids, forms, editors, toolbars, menus, navigation widgets, upload, charts, reports, dialogs, AI chat, and document/file workflows. Generated pages should feel like working application screens: compact headings, helpful tooltips, visible loading/error/success states, and real backend download links when files are generated.',
  'Official DevExpress and Bootstrap docs',
  'Local doc: docs/BLAZOR_BOOTSTRAP_DEVEXPRESS_DESIGN.md; DevExpress components: https://docs.devexpress.com/Blazor/400725/blazor-components; Bootstrap grid: https://getbootstrap.com/docs/5.3/layout/grid/',
  'seed; blazor; bootstrap5; devexpress; design; responsive; generation',
  96,
  1,
  1,
  0
),
(
  '0f345732-a0e9-4867-9ec2-d80fb49de4ab',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'DevExpress Blazor template and component starting points',
  'DevExpress Blazor',
  'For new generated Blazor solutions, start from official project-template shapes before inventing architecture. Use the DevExpress Blazor Template Kit as a model for a themed shell, Bootstrap stylesheet choice, optional Open Iconic resources, ready pages, and component demos. ' ||
  'Use Microsoft Blazor templates or official samples for base hosting structure. Pick DevExpress components by workflow: DxGrid for editable tabular data, DxFormLayout for settings and forms, DxToolbar/DxMenu/DxTreeView/DxTabs/DxDrawer for navigation/actions, DxLoadingPanel for long work, and DxAIChat for model conversations.',
  'Official DevExpress docs',
  'DevExpress get started: https://docs.devexpress.com/Blazor/401057/get-started; Template Kit: https://docs.devexpress.com/Blazor/405308/get-started/template-kit; Components: https://docs.devexpress.com/Blazor/400725/blazor-components',
  'seed; devexpress; templates; component-selection; blazor; dotnet10; generation',
  95,
  1,
  1,
  0
),
(
  'd8f010f6-6861-49f3-9e29-0e37cebc09ee',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Navigation SVG icon generation contract',
  'Blazor Design',
  'When generating navigation icons, create two SVG styles for every concept: a line icon for the default state and a solid or duotone icon for hover, active, selected, or compact states. ' ||
  'Use a square viewBox, currentColor, consistent visual weight, short title text, no embedded text, no gradients, no decorative blobs, and readable geometry at 16px and 24px. Use DevExpress IconUrl or IconCssClass when icons belong to DevExpress navigation components; otherwise use img aria-hidden=true inside Bootstrap navigation with visible text labels.',
  'LocalGPT design contract and DevExpress icon docs',
  'Local doc: docs/BLAZOR_BOOTSTRAP_DEVEXPRESS_DESIGN.md; DevExpress icons: https://docs.devexpress.com/Blazor/401749/styling-and-themes/icons',
  'seed; svg; navigation; icons; devexpress; bootstrap; design',
  96,
  1,
  1,
  0
),
(
  '54b06025-8c24-454e-a13e-c34ec49a6ab7',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Microsoft dotnet/samples map for code generation',
  '.NET Samples',
  'Treat dotnet/samples as official sample code referenced by .NET documentation. Use its focused sample families as evidence for project/API shape: csharp for language features, async/async-and-await for task/cancellation patterns, core for SDK-style .NET structure, standard/data/sqlite for SQLite basics, github-actions/DotNet.GitHubAction for CI/action shape, msbuild for project customization, and azure/orleans/machine-learning/iot/windowsforms/wpf/framework only when the generated archetype really needs that platform. ' ||
  'Do not copy sample snippets blindly into LocalGPT; combine focused sample evidence with Microsoft Learn architecture and the LocalGPT archetype contract.',
  'Official dotnet GitHub source',
  'dotnet/samples: https://github.com/dotnet/samples; local doc: docs/MICROSOFT_DOTNET_SAMPLE_CURRICULUM.md',
  'seed; microsoft; dotnet; samples; csharp; async; sqlite; msbuild; github-actions; generation',
  96,
  1,
  1,
  0
),
(
  '4906f9a5-802b-4f85-a52b-f8b56a39d5e1',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Microsoft Learn .NET developer and technician curriculum',
  '.NET Curriculum',
  'Use Microsoft Learn as the learning baseline for good .NET developers and technicians: .NET fundamentals, C# syntax/thinking, Blazor web apps, ASP.NET Core services and APIs, EF Core data access, desktop/mobile when needed, cloud-native/microservices when there are real boundaries, generative AI with .NET for model/tool workflows, and DevOps/testing/deployment for build honesty. ' ||
  'For technician support, teach SDK/runtime/workload verification, NuGet restore, dotnet restore/build/test/publish, static web asset checks, logs, launch profiles, ports, environment variables, WebView2 separation, CI/CD, and release artifact evidence.',
  'Microsoft Learn source-backed seed',
  'Microsoft Learn .NET: https://learn.microsoft.com/en-ca/training/dotnet/; C# path: https://learn.microsoft.com/en-us/training/paths/get-started-c-sharp-part-1/; Blazor path: https://learn.microsoft.com/en-us/training/paths/build-web-apps-with-blazor/; ASP.NET Core fundamentals: https://learn.microsoft.com/en-us/training/paths/aspnet-core-fundamentals/; Web API module: https://learn.microsoft.com/en-us/training/modules/build-web-api-aspnet-core/; EF Core for Beginners: https://learn.microsoft.com/en-us/shows/entity-framework-core-for-beginners/; .NET DevOps docs: https://learn.microsoft.com/en-us/dotnet/navigate/devops-testing/; GitHub Actions and .NET: https://learn.microsoft.com/en-us/dotnet/devops/github-actions-overview',
  'seed; microsoft-learn; dotnet; csharp; blazor; aspnetcore; efcore; devops; technician',
  96,
  1,
  1,
  0
),
(
  '9d4654b4-0d6d-424a-9285-cfe307bb83d4',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Microsoft architecture and Aspire generation boundaries',
  '.NET Architecture',
  'Use Microsoft architecture guidance to select the smallest honest shape: cohesive ASP.NET Core/Blazor app by default, service boundaries only for independent deployment, scaling, integration, background work, downloads, or external runners. Use .NET Aspire concepts for distributed/cloud-native prototypes that need orchestration, service discovery, health, telemetry, resiliency, or local multi-service coordination. ' ||
  'Do not add Aspire, Orleans, microservices, Azure, Docker, or CI complexity to a LocalGPT feature unless the archetype contract explains the real boundary.',
  'Microsoft Learn source-backed seed',
  'Architecture overview: https://learn.microsoft.com/en-us/dotnet/architecture/; .NET Aspire quickstart: https://learn.microsoft.com/en-us/training/modules/create-aspire-applications/; ASP.NET Core docs: https://learn.microsoft.com/en-us/aspnet/core/',
  'seed; microsoft; architecture; aspire; cloud-native; service-boundaries; localgpt',
  95,
  1,
  1,
  0
),
(
  '64a7e3fa-10ab-4c0e-95d6-442db7ee3772',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Ollama .NET lab API and UI contract',
  'Ollama .NET Lab',
  'An Ollama-inspired .NET/DevExpress generation must not be only a few web pages. It should include an ASP.NET Core control-plane API with representative Ollama-style route stubs for version, tags/list models, running models, show, pull, push, create, copy, delete, generate, chat, and embed. It should include DevExpress pages for API console, model catalog, model download planning, and settings. It must clearly say native GGML/GPU inference, model loading, and real binary downloads are not implemented unless an approved backend exists.',
  'Official Ollama API docs and LocalGPT artifact contract',
  'Ollama API docs: https://docs.ollama.com/api; Local docs: docs/OLLAMA_DOTNET_EXPERIMENT.md and docs/GENERATION_ARCHETYPE_CONTRACTS.md',
  'seed; ollama; dotnet; aspnetcore; devexpress; api; model-downloads; settings; generation',
  96,
  1,
  1,
  0
);

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  '9fe4ce65-16c7-474d-a9b0-a6bb79af6ff0',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Legacy Jezzifa architecture lessons for generation',
  'Enterprise .NET / DevExpress Web API / integrations',
  'The user-supplied legacy Jezzifa archive is useful as sanitized architecture evidence, not as a modern code template to copy verbatim. It shows a larger .NET solution style with separate business-object/core/service/web projects, DevExpress Web API/XAF-style object-space setup, EF contexts, security/JWT/certificate services, custom controllers, database update helpers, Telegram bot service integration, Python configuration hooks, speech-to-text/Whisper-oriented data, and a separate web target. When generating similar modern systems, ask whether the user wants a monolith, modular monolith, or multi-project solution; whether DevExpress Web API/XAF/OData business objects are required; whether Telegram/Python/Whisper integrations are enabled; and whether optional external code execution is explicitly user-approved. Sanitize legacy names and do not reproduce obscene folder/class names in generated guidance. Prefer .NET 8-10, explicit DI, typed options, EF migrations/schema update plans, isolated integration services, safe secrets/config handling, and backend-owned native/Python execution behind user permission gates.',
  'LocalGPT SQL seed',
  'Local user archive listing: Jezzifa.zip showed Api.WebApi.sln, BusinessObjects/Core projects, DevExpress Web API service setup, TelegramBotService metadata, PythonOptions/find_libpython.py, SpeechToTextValue, security/certificate services, and a separate web target. Local guide: docs/EF_DEVEXPRESS_BUSINESS_OBJECTS.md.',
  'seed; jezzifa; sanitized; devexpress-web-api; xaf; odata; telegram; python; whisper; modular-monolith',
  86,
  1,
  1,
  0
);

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  '68fd046e-b23b-4d7b-ad6c-627c9c3b5f0f',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'DXAiChat artifact delivery and council confidence contract',
  'DXAiChat AI Council',
  'When the user asks DXAiChat or the AI Council to generate Minecraft datapacks/modpacks, .NET/Blazor/DevExpress code, .cs/.razor/.dll files, or whole solution zips, treat the council as capable of producing a safe downloadable milestone. Do not refuse with "too much" or "not capable" language. If the target is large, reduce it into a buildable sandbox artifact, include file paths/download links through /__artifacts/council/, and list staged follow-up work under Needs verification. If material architecture choices are genuinely missing, create a concise poll and stop for the next user turn. Never claim the user failed to answer a poll in the same response that created it. Use DXAiFunctions such as /__diag/sqlite/tables, /__diag/knowledge, /__diag/logs, /__diag/council/artifact-smoke, /__diag/blazor-devexpress-guidance, and /__diag/dotnet-sample-curriculum before guessing. For direct artifact requests, generate links instead of printing zip/binary payloads as text, and do not self-integrate generated code into LocalGPT without explicit user approval.',
  'LocalGPT SQL seed',
  'Local service: MultiModelCouncilService poll/artifact gate; Local service: CouncilArtifactService artifact generators and /__artifacts/council route; Local route: /__diag/dxaichat-functions for available function catalog.',
  'seed; dxaichat; council; artifacts; generation; confidence; downloads; polls',
  95,
  1,
  1,
  0
);

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  '9a22b442-e53d-4c36-b56e-bc2a37f11c38',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'DXAiChat frontend review and recode UX rules',
  'Blazor DevExpress generation',
  'DXAiChat is the required human-facing test path for chat, model selection, council review, polls, and generation requests; backend-only tests are not enough for frontend acceptance. Architecture choices must be generated at runtime from the user''s actual request, not forced through preselected LocalGPT defaults. If important choices are missing, the AI or council must stop before generation, present a concise user poll, and wait for the user''s option or custom feedback. Common poll choices include target platform/runtime, language/framework, UI stack if any, solution shape, data/persistence model, deployment target, reference-app fidelity, and expected downloadable artifacts. Blazor/DevExpress is a strong LocalGPT specialization, not a universal default for every generated app. Selected provider/model must be visibly verifiable before Send and locked at the composite chat-client boundary during diagnostics so a URL or UI choice cannot silently route to another configured model. Long local inference must show a non-model runtime status heartbeat in the chat transcript, separate from model-thinking blocks, so the user knows whether LocalGPT is waiting on Ollama, first token latency, or streamed model output. Frontend smoke tests against large local models should use slim diagnostic prompts, explicit prompt/output caps, and optional bootstrap suppression; production chats may use the normal knowledge bootstrap after the frontend path is proven. When the user asks to recode a goal application, recreate its recognizable navigation, first screen, model/catalog/settings/API/download/log workflows, and UX structure with the selected architecture; do not output a generic dashboard with the same sample pages.',
  'LocalGPT SQL seed',
  'Local docs: docs/BLAZOR_BOOTSTRAP_DEVEXPRESS_DESIGN.md and docs/GENERATION_ARCHETYPE_CONTRACTS.md; Local frontend: Components/Pages/Chat.razor architecture poll and model/session lock; User review request: DXAiChat must be tested like a human and recode targets must preserve the goal app look/workflows.',
  'seed; dxaichat; frontend-review; poll; devexpress; blazor; recode; ux; model-selection',
  95,
  1,
  1,
  0
);

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  'f1127721-1bc8-46a3-9d77-f0a89c92db37',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'EF Core and DevExpress business object generation',
  'Entity Framework / DevExpress Web API',
  'When generating EF Core business objects, first identify whether the user wants DevExpress Web API/XAF/OData-compatible business objects or a plain ASP.NET Core EF backend. For DevExpress Web API/XAF/OData, prefer explicit keys, scalar foreign keys, navigation properties, inverse relationships, attribute-visible validation/display/security metadata, and stable public properties for OData/model discovery. For plain EF backends, do not force the heavier DevExpress/XAF shape when services plus DTOs are simpler. Ask about snapshot/audit style, field-aware changes, backing fields, lazy loading, delete behavior, security system requirements, naming constraints, and migration nullability before emitting entities. Avoid accidental shadow properties by using consistent names, explicit FK scalar properties, [ForeignKey], [InverseProperty], and targeted ModelBuilder configuration. For reverse-engineered databases such as the user-supplied Telegram schema, preserve exact relationship semantics and naming; if field/property names may differ only by first-letter casing, do not casually rename them. When adding columns to populated databases, prefer nullable first migrations, semantic defaults, or backfill/multi-step migrations instead of blindly adding NOT NULL columns.',
  'LocalGPT SQL seed',
  'Local guide: docs/EF_DEVEXPRESS_BUSINESS_OBJECTS.md; DevExpress XAF Data Annotation Attributes: https://docs.devexpress.com/eXpressAppFramework/112701/business-model-design-orm/data-annotations-in-data-model; DevExpress Backend Web API Service: https://docs.devexpress.com/eXpressAppFramework/403394/backend-web-api-service; EF Core shadow properties: https://learn.microsoft.com/ef/core/modeling/shadow-properties; EF Core relationship mapping attributes: https://learn.microsoft.com/ef/core/modeling/relationships/mapping-attributes',
  'seed; efcore; devexpress-web-api; xaf; odata; business-objects; shadow-properties; migrations; reverse-engineering',
  94,
  1,
  1,
  0
);
