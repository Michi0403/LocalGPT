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
  'A plain unstyled WebView2 page meant the packaged app served Razor markup but not Blazor/DevExpress static assets. Keep IncludeLocalGptPublishedPayload defaulted to false for Visual Studio Debug/F5, then have Build-LocalGptPackage.ps1 and Publish-LocalGptRelease.ps1 pass IncludeLocalGptPublishedPayload=true after publishing LocalGPT. A release MSIX must include the published webroot entries LocalGPTWebviewWrapper/wwwroot/_framework/blazor.web.js, LocalGPTWebviewWrapper/wwwroot/_content/DevExpress.Blazor/dx-blazor.svg, LocalGPTWebviewWrapper/wwwroot/_content/DevExpress.Blazor.Themes/office-white.bs5.min.css, and LocalGPTWebviewWrapper/wwwroot/LocalGPT.styles.css. Copying files only into a loose AppX layout is not enough; inspect the actual MSIX archive and fail the build if DevExpress/Blazor static assets are missing.',
  'LocalGPT release cycle memory',
  'Package payload fix in LocalGPTWebviewWrapper (Package).wapproj; guard in LocalGPTWebviewWrapper/build/Build-LocalGptPackage.ps1; diagnostics in LocalGPTWebviewWrapper/MainWindow.xaml.cs; snapshots under %LOCALAPPDATA%/LocalGPT/WebView2Diagnostics',
  'seed; localgpt; webview2; devexpress; static-web-assets; blazor; msix; diagnostics; release-guard',
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
  'Routes: /__diag/minecraft/datapack-benchmark, /__diag/council/artifact-smoke?target=solution, /__diag/council/artifact-smoke?target=ai-host, /__artifacts/council/{fileName}; WebView2 smoke flags in %LOCALAPPDATA%/LocalGPT/runtime',
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
  'Route: /__diag/minecraft/dependency-version?loader=datapack&minecraftVersion=26.1; legacy comparison route: /__diag/minecraft/dependency-version?loader=fabric&minecraftVersion=1.21.4; source: LocalGPTWebviewWrapper/LocalGPT/Services/MinecraftDependencyVersionCatalog.cs',
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
  'AI host API and UI contract',
  'AI Host Control Plane',
  'A local AI host .NET/DevExpress generation must not be only a few web pages and must not be an upstream provider proxy. It should include an ASP.NET Core API with representative provider-compatible routes for version, tags/list models, running models, show, pull, push, create, copy, delete, generate, chat, and embed. It should include DevExpress pages for API console, model catalog, model download planning, chat, running models, logs, and settings. /api/chat and /api/generate must use direct local model-file runner paths such as a configured native executable/library, Python.NET bridge, ONNX/ML.NET adapter, or explicit setup gap; Ollama manifests may be read as local metadata but the Ollama service must not be called as inference fallback.',
  'Local AI host source scan and LocalGPT artifact contract',
  'Local docs: docs/AI_HOST_DOTNET_BLAZOR_REBUILD_GUIDE.md, docs/AI_HOST_CONTROL_PLANE_ARCHITECTURE.md, and docs/GENERATION_ARCHETYPE_CONTRACTS.md',
  'seed; ai-host; dotnet; aspnetcore; devexpress; api; model-downloads; settings; generation',
  96,
  1,
  1,
  0
);

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  '20efbf3a-e6e4-4bf3-9b94-10b8017de0d1',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Minecraft Java 26 datapack generation rules',
  'Minecraft Builder / datapack',
  'Current LocalGPT datapack generation defaults to Minecraft Java 26.1 unless the user requests an older target. Minecraft Java 26.1 requires Java 25 and uses datapack pack_format 101.1. Minecraft Java 26.2 snapshot builds use datapack pack_format 105.0 and should be treated as snapshot-only unless the user chooses it. For Java 26.x pack.mcmeta should write decimal pack formats as strings, for example "pack_format": "101.1". Keep 1.21.x pack_format 61 knowledge only as legacy comparison. Use singular function folders data/<namespace>/function and data/minecraft/tags/function. Validate zip root, JSON tags, function references, no leading slash commands, no .mcfunction.txt placeholders, and no root data remove storage reset commands.',
  'LocalGPT SQL seed from official Minecraft 26.x source check',
  'Official Minecraft 26.1 notes: https://www.minecraft.net/en-us/article/minecraft-java-edition-26-1; official Minecraft 26.2 Snapshot 6 notes: https://www.minecraft.net/en-us/article/minecraft-26-2-snapshot-6; local catalog: LocalGPTWebviewWrapper/LocalGPT/Services/MinecraftDatapackVersionCatalog.cs.',
  'seed; minecraft; datapack; java-26; java-25; pack_format; source-backed',
  96,
  1,
  1,
  0
),
(
  '7403613b-3c37-44a6-982f-5a77a5d12ad5',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Minecraft datapack storage command syntax and smoke discovery',
  'Minecraft Builder / validation',
  'When generating mcfunction storage writes, keep the storage id and NBT path separate: execute store result storage <namespace>:<storage_id> <nbt_path> int 1 run ... . Do not generate storage living_cities:city.year int 1 because the dot becomes part of the storage id and the command loses its NBT path. For city data use storage living_cities:city year int 1, founder.x, banner.x, population, food, security, and houses. If a tester says register_banner is not loaded, first prove discovery: load.json must call core/load, core/load should emit a visible load message and reference a harmless city/register_banner smoke path, and build-local.ps1 should validate every function namespace:path reference before zipping.',
  'LocalGPT SQL seed from friend datapack feedback',
  'Local docs: docs/MINECRAFT_SOURCE_KNOWLEDGE.md and docs/MINECRAFT_MOD_AI_BUILDER.md; local service: MinecraftModWorkspaceService; deterministic route: /__diag/council/artifact-smoke?target=datapack.',
  'seed; minecraft; mcfunction; storage; register_banner; validation; friend-feedback',
  95,
  1,
  1,
  0
),
(
  'd03d0994-cb15-4b96-858a-6d8a0cf3e2db',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'DXAiChat upload context and artifact delivery rules',
  'DXAiChat/frontend acceptance',
  'DXAiChat is expected to accept local context and return downloadable artifacts. Text-like uploads and zip files should be decoded into a bounded visible context message before asking for code, datapacks, or review; do not flood the model with an entire archive. If the user asks for .cs, .razor, .dll, solution zips, datapacks, or modpacks, return HTTP download links through /__artifacts/council/ or another safe GET route, not raw zip/binary text. Markdown/Harmony formatting must render final visible content as normal Markdown, especially top-level lists after model-thinking blocks.',
  'LocalGPT SQL seed from DXAiChat upload implementation',
  'Local page: LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Chat.razor; local artifact service: CouncilArtifactService; local route: /__artifacts/council/{fileName}.',
  'seed; dxaichat; upload; zip; artifacts; markdown; harmony; downloads',
  96,
  1,
  1,
  0
),
(
  '2db8489d-df7c-453a-90f7-c21e9f753d89',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Whisper Harmony and agent framework lessons for .NET generation',
  'Agents / speech / chat templates',
  'Selected learn-base folders for Whisper, Harmony, and OpenAI agents teach patterns that can be translated into C# and .NET. Whisper-style apps need audio input, model choice, transcription jobs, timestamps, language options, batching, progress, cancellation, and artifacts, wrapped behind user-approved backend services with SQLite job logs. Harmony/chat-template handling needs channel parsing, final-answer extraction, thinking display, and marker cleanup so final Markdown is visible. Agent frameworks teach model clients, typed tools, handoffs, guardrails, tracing, memory, and streaming events; in .NET translate them into interfaces such as IAgentRunner, IAgentTool, IAgentTraceStore, and IAgentMemoryService. Tool calls are not permission to self-expand or run native commands without user approval.',
  'LocalGPT SQL seed from selected learn-base source request',
  'Local source roots: selected learn-base harmony-main, whisper-main, and openai-agents-js-main; local guide: docs/AI_HOST_DOTNET_BLAZOR_REBUILD_GUIDE.md.',
  'seed; whisper; harmony; agents; dotnet; csharp; tool-calling; tracing; speech',
  94,
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
  '130006cf-f8e5-4664-aebd-11acb6b8a580',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'AI host control-plane architecture is decomposable',
  'AI Host Control Plane',
  'A local AI host control plane is not magic, but it must be decomposed honestly. Separate API/control plane, model storage/lifecycle, inference runtime, and hardware backend. LocalGPT can generate and test a .NET ASP.NET Core/DevExpress Blazor provider-compatible host with routes, model catalog, download planning, settings, logs, API console, chat pages, and runner interfaces. It must not claim native GGML/GGUF/GPU inference is verified unless a real approved backend is attached and tested, but it must include a direct local model-file runner path and must not use an upstream Ollama/LM Studio/OpenAI-compatible proxy for /api/chat or /api/generate. A practical first design is an API-compatible host with IModelCatalogService, IModelDownloadService, IInferenceProvider, IInferenceRunner, IModelRuntimeSession, IHardwareBudgetService, and IChatTemplateService. Native tensor kernels, tokenizer/runtime correctness, KV cache, sampling, embeddings, AMD/NVIDIA/Intel GPU backends, and VRAM scheduling are deeper runtime work.',
  'LocalGPT SQL seed',
  'Local doc: docs/AI_HOST_CONTROL_PLANE_ARCHITECTURE.md; related artifact route: /__diag/council/artifact-smoke?target=ai-host.',
  'seed; ai-host; control-plane; dotnet; aspnetcore; devexpress; inference-provider; gpu; api-compatibility',
  93,
  1,
  1,
  0
);

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  '4c8b205f-f7b7-44e8-8ad5-777a5d59d9c2',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Selected learn-base import rules for architecture generation',
  'Architecture fingerprints',
  'The selected local learn-base exists to teach reusable solution setup, wiring, libraries, protocols, host shapes, and coding patterns. Project names and branding are not important unless the user explicitly asks for them. Learn how multi-project solutions separate business objects, core services, Web API hosts, Blazor or non-Blazor frontends, worker/microservice hosts, bot integrations, and optional native/Python execution. Jezzifa-style patterns are especially valuable for Python.NET interop: detect Python.Runtime/PythonEngine/Py.GIL/pythonnet usage, isolate Python execution behind explicit user permission gates, configure Python paths through typed options, and keep C# service boundaries testable. Also learn DevExpress Security Web API/XAF/OData business-object wiring, EF contexts, certificate/security services, Telegram/bot services, speech-to-text/Whisper-oriented data, and mixed ASP.NET Core/Blazor hosts. Ask the user for a poll before choosing monolith vs microservice, Blazor vs non-Blazor frontend, DevExpress Web API/security, Python interop, database style, or deployment model. Do not copy obscene or legacy names into generated output.',
  'LocalGPT SQL seed',
  'Local route: /__diag/learn-base/import imports compact source-backed fingerprints from C:\tmpselectedcodexlearnbaseforlocalgpt; local route: /__diag/benchmark/engineering scores generation tasks honestly.',
  'seed; learn-base; architecture-fingerprint; pythonnet; devexpress-web-api; xaf; odata; microservice; blazor; aspnetcore; bot; interop',
  94,
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

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  'a3b8f1ff-58b6-4b10-9bd6-83cf0ccfbe52',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Local AI host source lessons for .NET DevExpress Blazor rebuilds',
  'AI Host Control Plane / source-backed architecture',
  'The local ollama-main source teaches an AI-control-plane architecture that LocalGPT can generate in .NET, but Ollama is only a provider/source example and must not be copied as the generated application name. Learn route families for version, tags/list models, running models, show, pull, push, create, copy, delete, generate, chat, and embed; OpenAI/Anthropic compatibility adapters; model manifests/layers/digests; transfer progress; runtime session keep-alive/cancel/unload; runner orchestration; tokenizer/templates/harmony/thinking handling; and platform shell concepts. When asked to build a local AI host in .NET Blazor DevExpress style, generate a real ASP.NET Core API plus DevExpress Blazor left-navigation app with chat, model catalog, downloads, running models, API console, templates, hardware, logs, diagnostics, settings, and a direct local model-file native runner contract. Do not output only a generic dashboard. Do not use an upstream provider proxy as the AI-host runtime milestone.',
  'LocalGPT SQL seed from local source scan',
  'Local guide: docs/AI_HOST_DOTNET_BLAZOR_REBUILD_GUIDE.md; local source root: C:\tmpselectedcodexlearnbaseforlocalgpt\ollama-main; local route: /__diag/learn-base/import.',
  'seed; source-backed; ai-host; dotnet; devexpress; blazor; api; model-lifecycle; chat; downloads; runtime',
  96,
  1,
  1,
  0
),
(
  '4aa81c8d-5d71-4af1-a56c-90084a9bff4d',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'DevExpress Blazor 25.2 demo as component wiring curriculum',
  'DevExpress Blazor generation',
  'The local DevExpress Blazor 25.2 demo is source-backed curriculum for generating real Blazor pages. It shows server-side and WebAssembly hosted solution shapes, central package version management, DevExpress service registration, metadata-driven demo navigation, DxAIChat pages for templates/attachments/function calling/message handling, DxGrid pages for CRUD/editing/filtering/search/layout/master-detail/selection/export/large data, DxFormLayout for settings and forms, upload/file input workflows, reporting, RichEdit, PDF, charts, pivot, scheduler, and document workflows. When the user asks for any DevExpress component, generate the Razor markup, service/state wiring, backend endpoint or data source, registration notes, loading/error state, and CSS only where needed.',
  'LocalGPT SQL seed from local DevExpress demo scan',
  'Local source root: C:\tmpselectedcodexlearnbaseforlocalgpt\Blazor-25.2\Blazor-25.2\demo; local guide: docs/AI_HOST_DOTNET_BLAZOR_REBUILD_GUIDE.md; local route: /__diag/blazor-devexpress-guidance.',
  'seed; source-backed; devexpress; blazor; dxgrid; dxaichat; dxformlayout; upload; reporting; wiring',
  97,
  1,
  1,
  0
),
(
  'e68d9d70-3314-4846-99e7-3f13da6b30aa',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Media, Python.NET, and microservice lessons for generated solutions',
  'Python interop / DevExpress Web API / modular solutions',
  'The local videocutter and Jezzifa-style sources teach reusable architecture patterns, not names. Video/media pipelines can remain in Python while .NET owns UI, API, permissions, logs, job state, and artifacts. Wrap external Python/media execution behind backend services, typed options, safe working directories, user permission gates, and nonblocking progress. Jezzifa-style source teaches multi-project or microservice-style separation of business objects, core services, API hosts, frontend hosts, bot services, DevExpress Web API/XAF/OData business-object wiring, EF contexts, security/certificate services, Telegram/bot integrations, Python.NET/PythonEngine/Py.GIL/pythonnet interop, and speech-to-text/Whisper-style data. Generated solutions must sanitize legacy names and poll for monolith vs modular/microservice, DevExpress Web API security, plain EF, Python interop, bot integration, and deployment choices.',
  'LocalGPT SQL seed from local source scan',
  'Local source roots: C:\tmpselectedcodexlearnbaseforlocalgpt\videocutter and C:\tmpselectedcodexlearnbaseforlocalgpt\Jezzifa; local guide: docs/AI_HOST_DOTNET_BLAZOR_REBUILD_GUIDE.md.',
  'seed; source-backed; pythonnet; media; microservice; devexpress-web-api; xaf; odata; telegram; whisper; interop',
  94,
  1,
  1,
  0
);

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  '58d6b4d7-c8e3-450a-a31a-57f0e9fc0b1a',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Compiled frontend design pattern library',
  'Frontend generation / Blazor / DevExpress / Bootstrap',
  'Use LocalGPT''s compiled frontend design pattern library directly when generating UI. Do not tell the user or model to use external galleries as runtime guidance; the relevant design concepts are already distilled into reusable archetypes, component mappings, service wiring, and accessibility checks. First classify the app as commerce, social/community, AI host/developer tool, SaaS/admin, media workbench, or another product archetype, then identify the primary task and information architecture. Use Bootstrap for responsive macro layout and DevExpress for application-grade interaction; create custom Razor components when the selected stack lacks a visual shell. Apply Microsoft Windows/Fluent design foundations: color hierarchy, commanding, elevation, geometry, iconography, layout, materials, motion, navigation, typography, usability, widgets, and writing. Generated frontends must include real pages, navigation, service boundaries, loading/empty/error/success states, accessible labels, and safe artifact/download routes when files are generated.',
  'LocalGPT SQL seed from compiled frontend design references and Microsoft Learn design guidelines',
  'Local guide: docs/FRONTEND_DESIGN_PATTERN_LIBRARY.md. Local route: GET /__diag/frontend-design-guidance. Microsoft Windows app design guidelines: https://learn.microsoft.com/en-us/windows/apps/design/guidelines-overview. DevExpress Blazor components: https://docs.devexpress.com/Blazor/400725/blazor-components. Bootstrap v5 docs: https://getbootstrap.com/docs/5.3/layout/grid/.',
  'seed; frontend-design; blazor; devexpress; bootstrap; windows-design; fluent; accessibility; archetypes',
  94,
  1,
  1,
  0
);

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  '31f7cfa5-8b68-47be-9c32-9d046f88cc85',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  '.NET AI host architecture and native-runner adapter rules',
  'AI host generation / .NET architecture',
  'When generating an AI-host-shaped .NET application, produce more than pages. Generate provider-neutral ASP.NET Core routes, typed options, DI registrations, EF/SQLite state, model catalog/download/session services, chat/template services, logs, settings, hardware budget policy, and downloadable artifact routes when useful. Use interface-driven boundaries: IModelCatalogService, IModelTransferService, IInferenceProvider, IInferenceRunner, IPluginCatalogService, IScriptExecutionService, IHardwareBudgetService, and IChatTemplateService. External hosts such as Ollama, LM Studio, OpenAI, HuggingFace downloads, Python.NET, PowerShell, ONNX, ML.NET, or native executables are adapters behind interfaces, not the product identity. For Michi0403''s accepted AI-host target, /api/chat and /api/generate must use direct local model-file runner paths, not upstream Ollama/LM Studio/OpenAI-compatible proxying. Use .NET DI/IoC, the options pattern, hosted/background services for queued work, AssemblyLoadContext/AssemblyDependencyResolver only for trusted plugins, and permission-gated Python.NET/PowerShell/native process execution with safe directories, cancellation, and logs. If real native inference is not configured, say so in the generated UI and produce a visible runner/plugin setup page; do not substitute an upstream provider proxy as a milestone. Generated AI-host solutions must include recognizable navigation for dashboard, model catalog, API console, chat, running models, downloads, templates, hardware, runner/plugins, logs, and settings.',
  'LocalGPT SQL seed from official .NET architecture docs and LocalGPT process memory',
  'Local guide: docs/DOTNET_AI_HOST_ARCHITECTURE_PATTERNS.md. Local route: GET /__diag/ai-host-rebuild-guidance. .NET dependency injection: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview. .NET options pattern: https://learn.microsoft.com/en-us/dotnet/core/extensions/options. ASP.NET Core hosted services: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services. .NET plugin support: https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support. PowerShell runspaces: https://learn.microsoft.com/en-us/powershell/scripting/developer/hosting/creating-runspaces.',
  'seed; ai-host; dotnet; architecture; dependency-injection; options; hosted-services; plugins; pythonnet; powershell; native-runner; adapters',
  95,
  1,
  1,
  0
);

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  '6a86923f-9c8e-4a86-96d6-80220ef4c16f',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'LocalGPT developer diary: build, deploy, and release lessons',
  'Project process memory',
  'Treat compiler output and generated-artifact builds as the final judge. LocalGPT itself building is not enough when the user downloads a generated solution; extract and build the generated solution too when practical. Keep generated project names short, normally 16-32 characters, because long names combine badly with src/bin/obj/runtime folders and Visual Studio diagnostics. Whole-solution generation should include .sln, .csproj, Program.cs, _Imports.razor, pages, models, services, CSS, docs, manifest, and build/run notes. Release work should publish backend runtime zips, package Windows wrapper assets when available, generate release notes, generate SHA256 manifest, and honestly state backend-only vs Windows-only dependencies.',
  'LocalGPT developer diary seed',
  'Local doc: docs/LOCALGPT_DEVELOPER_DIARY.md; local scripts: build/Assert-SourceFormatting.ps1, LocalGPTWebviewWrapper/build/Build-LocalGptPackage.ps1, LocalGPTWebviewWrapper/build/Publish-LocalGptRelease.ps1.',
  'seed; developer-diary; dotnet; build; release; generated-solutions; visual-studio; artifacts',
  97,
  1,
  1,
  0
),
(
  '1f2db6d5-6d95-41dd-b418-71f74c9e6ef1',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'LocalGPT developer diary: AI host and model handling',
  'Ollama/LM Studio/model operations',
  'Ollama and LM Studio are local AI hosts/providers, not the identity of generated applications. Generated apps should use provider-neutral names such as AI host, model host, or local AI control plane. Local model work must respect hardware: prefer sequential council turns, explicit context/output caps, keep_alive=0s, low or CPU GPU-layer settings after driver instability, and model unload checks before and after tests. HuggingFace and GitHub model sources should be represented as catalog rows and user-approved download plans; browsing a catalog is not permission to download binaries. Large token budgets can help source generation but can stall local hardware, so expose presets and let the user raise limits intentionally. Treat values below 64K as quick-chat/diagnostic budgets, not valid code-generation acceptance tests; use 256K when the model/runtime supports it for full solution generation.',
  'LocalGPT developer diary seed',
  'Local doc: docs/LOCALGPT_DEVELOPER_DIARY.md; local route: /__diag/council/models; local route: /__diag/ai-host-rebuild-guidance; local route: /__diag/council/artifact-smoke?target=ai-host.',
  'seed; developer-diary; ai-host; ollama; lmstudio; huggingface; github; gpu; tokens; model-operations',
  97,
  1,
  1,
  0
),
(
  'dc0f958c-3317-4db0-9a6d-6f67a692d0cd',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'LocalGPT developer diary: DXAiChat and artifact UX',
  'DXAiChat/frontend acceptance',
  'DXAiChat is the human-facing acceptance surface for chat UX. Backend diagnostics are necessary but not sufficient when the user asks whether chat, model selection, thoughts, polls, downloads, or generated artifacts work. Long-running local inference needs visible runtime status before the first model token arrives. Harmony/thinking output must be parsed adaptively by model family and every response needs a user-visible final answer. Stop/cancel should be a quiet user cancellation, not an unhandled exception. If a user asks for code, datapacks, modpacks, .cs, .razor, .dll, or whole solution zips, produce a safe downloadable artifact through HTTP routes instead of printing binary/zip text or claiming the task is too large.',
  'LocalGPT developer diary seed',
  'Local doc: docs/LOCALGPT_DEVELOPER_DIARY.md; local route: /__artifacts/council/{fileName}; local route: /__diag/dxaichat-functions; local services: CompositeChatClient and CouncilArtifactService.',
  'seed; developer-diary; dxaichat; frontend; harmony; streaming; cancellation; downloads; artifacts',
  97,
  1,
  1,
  0
),
(
  'c67d3553-b807-4e44-9e62-f58e2c05a720',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'LocalGPT developer diary: cooperative AI Council team body',
  'AI Council behavior',
  'Treat LocalGPT as a cooperative engineering body: the user, Codex, and every local or cloud model contribute different strengths. Every AI council member should present as glad to participate, respectful toward the others, and focused on helping the user reach a working artifact. Disagreement is useful when it produces a better poll, risk note, test, or smaller buildable milestone. Never shame the user, never overrule denied permission, and never self-expand into the real project without explicit approval. When the user is frustrated, translate the frustration into technical options, a short poll, and a concrete recovery path.',
  'LocalGPT developer diary seed',
  'Local doc: docs/LOCALGPT_DEVELOPER_DIARY.md; local docs: docs/LOCALGPT_WORKFLOW_MEMORY.md and docs/GENERATION_ARCHETYPE_CONTRACTS.md.',
  'seed; developer-diary; council; cooperation; team; user-autonomy; polls; safety',
  97,
  1,
  1,
  0
),
(
  '3422c760-3d9a-4cd7-b64f-95f1b867a224',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'LocalGPT developer diary: operating-system diagnostics',
  'OS/runtime diagnostics',
  'Prefer repeatable scripts and bounded diagnostic routes over ad hoc guessing. Collect generic evidence: .NET SDK/runtime list, workload/runtime availability, NuGet restore output, build logs, package logs, app logs, model host reachability, running model list, runtime server JSON, static asset availability, Visual Studio deployment messages, and package manifest identity/version. Use generic path names in docs and knowledge: repo root, local app data, temp release folder, package cache, model cache, and user-selected learn-base folder. Do not commit generated packages, downloaded model binaries, private certificates, local logs with secrets, or personal absolute paths. Distinguish display sleep/power settings from confirmed GPU driver resets by checking logs, timing, model load, and user observations.',
  'LocalGPT developer diary seed',
  'Local doc: docs/LOCALGPT_DEVELOPER_DIARY.md; local routes: /__diag/logs, /__diag/sqlite/tables, /__diag/learn-base/import; local scripts: Repair-LocalGptDevEnvironment.ps1 and Test-OllamaGptOss.ps1.',
  'seed; developer-diary; diagnostics; os; dotnet-runtime; msix; webview2; logs; gpu; privacy',
  96,
  1,
  1,
  0
);

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  '0f5b9966-fcc6-4c3f-a86b-e80bfb3af3f0',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'Capability gap reports for faster artifact generation',
  'DXAiChat / AI Council / LocalGPT improvement loop',
  'If Michi0403 says LocalGPT, DXAiChat, the AI Council, or a model lacks a capability, refuses too quickly, misses source knowledge, or generates the wrong artifact shape, treat that as approved product feedback and investigate. Do not stop at a vague apology or refusal. Produce the safest useful downloadable milestone when the user already gave concrete scope, then add a structured Capability gap report and <localgpt-capability-gap> block. The gap must classify requested languages, frameworks, versions, domain knowledge, local knowledge sources, external official sources, missing LocalGPT functions/routes/pages/services, safe workflow, artifact plan, and next LocalGPT improvement. Local sources should be tried first: DXAiFunctions, SQLite knowledge/logs/memory, local docs, learn-base imports, generated artifacts, build logs, and Test Lab/WebView2 evidence. External sources should be official docs, official GitHub repos, package/version docs, version manifests, or user-approved source imports. For AI-host generation requests, the expected result is a provider-neutral .NET/ASP.NET Core/DevExpress Blazor control-plane solution with recognisable navigation, model catalog, chat/API console, settings, logs, downloads, provider-compatible routes, SQLite/appsettings state, and honest native-inference boundaries.',
  'LocalGPT SQL seed',
  'Local doc: docs/CAPABILITY_GAP_CONTRACT.md. Local route: GET /__diag/capability-gap-contract. Local routes: /__diag/dxaichat-functions, /__diag/knowledge, /__diag/logs, /__diag/learn-base/import, /__diag/ai-host-rebuild-guidance, /__diag/council/artifact-smoke?target=ai-host. User-tested expectations from prior DXAiChat prompts: faster downloadable .cs/.razor/.dll/solution/datapack artifacts, non-generic AI-host control-plane shape, and no refusal when a buildable milestone is possible.',
  'seed; capability-gap; source-request; ai-host; artifacts; dxaichat; council; user-approved',
  96,
  1,
  1,
  0
);

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  'bf53476f-8f3c-48db-9519-2b353811f74c',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'LocalGPT Test Lab and WebView2 automation',
  'Frontend diagnostics / browser automation',
  'Use the LocalGPT Test Lab page for fast frontend-facing HTTP checks before loading heavy local models. It can call /health, /__diag, /__diag/dxaichat-functions, Minecraft 26.x datapack version checks, deterministic council artifact smoke routes, and learn-base imports, then show JSON and extracted /__artifacts download links. For the actual WinUI WebView2 wrapper, Microsoft documents two Selenium/Microsoft Edge WebDriver approaches: launch the WebView2 app with EdgeOptions.UseWebView and BinaryLocation, or attach to a running WebView2 instance with a remote debugging port and EdgeOptions.DebuggerAddress. Browser automation source such as AutomatedDiscordLogin should be imported as compact architecture fingerprints, not pasted wholesale into prompts. Optional Python.NET/Python browser automation can be added as a workbench only behind explicit user permission gates, safe working directories, typed options, logging, and visible run controls.',
  'LocalGPT SQL seed from official Microsoft WebView2 WebDriver guidance',
  'Microsoft Learn: https://learn.microsoft.com/microsoft-edge/webview2/how-to/webdriver; local page: Components/Pages/TestLab.razor; local route: /__diag/frontend-test-guidance; local learn-base request: C:\tmpselectedcodexlearnbaseforlocalgpt\AutomatedDiscordLogin-master.',
  'seed; frontend; test-lab; webview2; selenium; webdriver; pythonnet; browser-automation; diagnostics',
  90,
  1,
  1,
  0
);

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  'c0c5e707-ff01-4d35-98c1-9c5e1cb9c7c4',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'LocalGPT generated workspace workflow and execution safety',
  'DXAiChat / AI Council / Test Lab / generated source editing',
  'Generated code should be treated as a live sandbox workspace before it becomes a zip. ' ||
  'The AI Council should discover the current host and workspace with /__diag/artifact-workspaces, list files with /__diag/artifact-workspace/{workspaceName}/files, ' ||
  'read or save text source files through /__diag/artifact-workspace/{workspaceName}/file, and refresh the zip only through /__diag/artifact-workspace/{workspaceName}/zip. ' ||
  'The council should cite real /__artifacts/council/ download links and the actual workspace path rather than inventing host names or paths. ' ||
  'Models may generate, inspect, edit, compile, validate, and package sandbox artifacts, but they must not launch generated programs, scripts, installers, or solutions by themselves. ' ||
  'When a build produces an executable, script, or solution to open, the model must summarize local system impact such as files read/written, commands run, network/model downloads, deletes, services started, and settings changed, then ask the user to approve or manually start it. ' ||
  'For LocalGPT package/backend smoke tests, discover the active loopback URL from %LOCALAPPDATA%/LocalGPT/runtime/server.json because the app chooses a free port at startup. ' ||
  'Set LOCALGPT_STARTUP_TRACE=1 only while diagnosing startup; it prints phase markers around configuration, service registration, middleware, and runtime endpoint creation.',
  'LocalGPT runtime policy seed',
  'Routes: /__diag/artifact-workspaces, /__diag/artifact-workspace/{workspaceName}/files, /__diag/artifact-workspace/{workspaceName}/file, ' ||
  '/__diag/artifact-workspace/{workspaceName}/zip, /__artifacts/council/{fileName}; UI: Test Lab generated workspace panel; service: AiContextBootstrapService runtime identity briefing. Runtime file: %LOCALAPPDATA%/LocalGPT/runtime/server.json.',
  'seed; artifact-workflow; source-editing; execution-safety; test-lab; dxaichat-functions; startup-trace; runtime-endpoint',
  98,
  1,
  1,
  0
);

INSERT OR IGNORE INTO "CouncilKnowledgeEntries"
("Id", "CreatedAtUtc", "UpdatedAtUtc", "Topic", "Scope", "Content", "Source", "HelpfulSources", "Tags", "Confidence", "IsUserApproved", "IsPinned", "IsArchived")
VALUES
(
  '3f0f5538-5968-4ec0-953b-8b878869c3e2',
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  strftime('%Y-%m-%dT%H:%M:%fZ','now'),
  'LocalGPT security model: local-first privacy and local capability risk',
  'Security / hosting boundary',
  'LocalGPT is designed for single-user desktop/WebView2 use on loopback. ' ||
  'This gives a real privacy advantage because prompts, source code, chat memory, generated artifacts, logs, and local model calls can stay on the user machine. ' ||
  'Do not claim that local means no security concerns. The remaining risk is local capability risk: file access, native commands, generated scripts/projects, sensitive SQLite memory, imported knowledge, and optional cloud endpoints. ' ||
  'Keep command execution behind policy services, require explicit user permission before integrating generated code into LocalGPT, inspect generated scripts before running them, and mark imported knowledge as verified or unverified. ' ||
  'If LocalGPT is hosted for coworkers or any untrusted network, treat it as a normal web app and require authentication, authorization, CSRF protection, TLS, rate limits, audit logs, command restrictions, workspace isolation, secrets management, and database retention rules.',
  'LocalGPT security review seed',
  'Top-level doc: SECURITY.md. README section: Security Model. ' ||
  'Local code: Program.cs binds the desktop host to 127.0.0.1 by default. ' ||
  'Related local docs: AGENTS.md and docs/LOCALGPT_WORKFLOW_MEMORY.md.',
  'seed; security; local-first; privacy; webview2; loopback; native-commands; sqlite; cloud-providers; hosting-boundary',
  98,
  1,
  1,
  0
);
