# Microsoft .NET Sample Curriculum

Use this guide when LocalGPT or the AI Council needs official Microsoft sample
and learning-path grounding for .NET, C#, ASP.NET Core, Blazor, EF, DevOps,
cloud-native architecture, and technician-level troubleshooting.

## Source Baseline

This guide is grounded in official Microsoft and dotnet sources:

- `dotnet/samples`: https://github.com/dotnet/samples
- Microsoft Learn for .NET: https://learn.microsoft.com/en-ca/training/dotnet/
- Microsoft Learn C# path:
  https://learn.microsoft.com/en-us/training/paths/get-started-c-sharp-part-1/
- Microsoft Learn Blazor path:
  https://learn.microsoft.com/en-us/training/paths/build-web-apps-with-blazor/
- ASP.NET Core fundamentals path:
  https://learn.microsoft.com/en-us/training/paths/aspnet-core-fundamentals/
- ASP.NET Core documentation hub:
  https://learn.microsoft.com/en-us/aspnet/core/
- Create a web API with ASP.NET Core controllers:
  https://learn.microsoft.com/en-us/training/modules/build-web-api-aspnet-core/
- EF Core for Beginners:
  https://learn.microsoft.com/en-us/shows/entity-framework-core-for-beginners/
- .NET DevOps, testing, and deployment docs:
  https://learn.microsoft.com/en-us/dotnet/navigate/devops-testing/
- GitHub Actions and .NET:
  https://learn.microsoft.com/en-us/dotnet/devops/github-actions-overview
- .NET architecture guides:
  https://learn.microsoft.com/en-us/dotnet/architecture/
- .NET Aspire quickstart:
  https://learn.microsoft.com/en-us/training/modules/create-aspire-applications/

Prefer these official sources and the local SQLite knowledge database before
asking a model to infer modern .NET architecture from memory.

## dotnet/samples Map

Treat `dotnet/samples` as the official sample-code base referenced by .NET
documentation. It is not one application template; it is a repository of focused
examples. Use it to teach generation by sample family:

- `csharp`: language syntax, LINQ, delegates/events, nullable reasoning,
  pattern matching, records, async examples, and small focused demonstrations.
- `async/async-and-await`: async control flow, awaited operations, task
  composition, cancellation, and avoiding blocking calls.
- `core`: .NET runtime, CLI, hosting, configuration, libraries, and modern
  SDK-style project structure.
- `standard/data/sqlite`: small SQLite data-access examples and provider usage.
- `github-actions/DotNet.GitHubAction`: build automation and custom GitHub
  Action shape for .NET.
- `msbuild`: project file, target, property, and build customization examples.
- `azure`: Azure-adjacent samples. Use these only when the generated feature
  really needs cloud deployment or managed service integration.
- `orleans`: distributed/cloud actor examples. Use only when the archetype calls
  for distributed state or service boundaries.
- `machine-learning`, `iot`, `windowsforms`, `wpf`, and `framework`: specialized
  samples. Mark legacy or platform-specific assumptions clearly.

Generation rule: use focused samples as evidence for API and project shape, not
as copy-paste cargo. When a generated app needs a full architecture, combine the
sample family with Microsoft Learn architecture guidance and a LocalGPT
archetype contract.

## Microsoft Learn Curriculum Layers

Use the Microsoft Learn .NET page as the broad learning map:

1. .NET fundamentals and CLI/project structure.
2. C# syntax, types, strings, numbers, data flow, methods, collections, LINQ,
   exceptions, async/await, nullable, and object-oriented design.
3. Web apps with ASP.NET Core and Blazor.
4. Mobile and desktop when the target is MAUI, WinUI, WPF, or WebView2 hybrid.
5. Cloud-native and microservices only when the feature has real deployment,
   scaling, resiliency, or integration boundaries.
6. Generative AI with .NET when building AI chat, model adapters, tool calling,
   retrieval, or agent-like workflows.

For LocalGPT and TacosPortalOpen, the default curriculum emphasis is:

- C# and modern .NET fundamentals.
- Blazor Web App structure, render modes, routing, layouts, forms, validation,
  reusable components, lifecycle events, JavaScript interop, and data display.
- ASP.NET Core services, Minimal APIs/controllers, middleware, static assets,
  SignalR where real-time UI is needed, health checks, and secure downloads.
- EF Core and SQLite with short-lived DbContext usage, factories for Blazor
  Server/event work, migrations or idempotent schema upgrades, and efficient
  queries.
- DevOps basics: restore, build, test, publish, source hygiene, GitHub Actions,
  package/release artifacts, and honest validation evidence.
- Architecture: cohesive app first, service boundaries only where justified,
  diagnostics by default, and persistent user state in EF/SQLite rather than
  transient appsettings.

## Blazor Generation Checklist

When generating Blazor for LocalGPT-like apps:

- Generate real `.razor` files, not C# string builders pretending to be Razor.
- Include `@page` for routable pages and the correct render mode for the chosen
  hosting model.
- Use route parameters, layouts, and navigation intentionally.
- Put component state and event handlers in `@code`, but move growing business
  logic into services.
- Use forms, validation, and clear submit/cancel paths for user input.
- Use JavaScript interop only for browser APIs or UI behavior that Blazor cannot
  own cleanly.
- Add loading, empty, error, success, and cancelled states for long work.
- Back generated file downloads with explicit HTTP GET routes.
- Keep WebView2/desktop-specific behavior in the host/wrapper layer and
  web/application behavior in the ASP.NET Core/Blazor project.

## ASP.NET Core And EF Checklist

When generating backend code:

- Choose Minimal APIs for focused diagnostic, health, and artifact routes.
- Choose controllers when route grouping, filters, model binding conventions, or
  larger CRUD APIs make the code clearer.
- Use DI and options. Do not new up infrastructure services inside Razor pages.
- Keep native commands, file generation, report/PDF/Office output, and database
  writes in backend services.
- Use EF Core with explicit lifetime rules. Blazor Server UI events should prefer
  `IDbContextFactory` or short-lived contexts.
- Add indexes, projections, paging, and cancellation tokens for large tables.
- Log warnings and errors with enough context for the AI Council to help the
  user fix the local machine.

## Technician-Level Baseline

The council should be able to help a user or technician with:

- Installing and verifying the correct .NET SDK/runtime/workloads.
- Reading `dotnet --info`, `dotnet --list-sdks`, and `dotnet --list-runtimes`.
- Restoring NuGet packages and explaining package feed/license issues.
- Running `dotnet restore`, `dotnet build`, `dotnet test`, and `dotnet publish`.
- Checking static web asset output and `_content` paths for Blazor/DevExpress.
- Inspecting logs, launch profiles, ports, appsettings, environment variables,
  and runtime endpoint files.
- Understanding Windows-only workloads such as WinUI/WebView2 separately from
  cross-platform ASP.NET Core/Blazor backends.
- Explaining CI/CD with GitHub Actions or Azure Pipelines without hiding build
  failures behind vague release language.

## Council Behavior

When the council uses this curriculum:

- Prefer source-backed rows from the SQLite knowledge database.
- Ask for the specific official source when the local brief is not enough.
- Do not invent package APIs, template names, or target frameworks.
- Mark platform-specific or legacy samples clearly.
- For whole-solution generation, use `docs/GENERATION_ARCHETYPE_CONTRACTS.md`
  and produce visibly different architectures for different project kinds.
- Treat compiler output, endpoint smoke tests, and generated artifact contents as
  stronger evidence than model confidence.
