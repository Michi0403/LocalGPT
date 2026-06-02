using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Data;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services
{
    public partial class CouncilKnowledgeService(
        IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
        ILogger<CouncilKnowledgeService> logger) : ICouncilKnowledgeService
    {
        public string DatabasePath => EfChatMemoryService.GetDefaultDatabasePath();

        public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await CouncilKnowledgeSchema.EnsureCreatedAsync(db, cancellationToken);
            await SeedKnowledgeAsync(db, cancellationToken);
        }

        public async Task<IReadOnlyList<CouncilKnowledgeEntry>> GetEntriesAsync(bool includeArchived = false, int take = 100, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await CouncilKnowledgeSchema.EnsureCreatedAsync(db, cancellationToken);
            await SeedKnowledgeAsync(db, cancellationToken);

            var query = db.CouncilKnowledgeEntries.AsNoTracking();
            if (!includeArchived)
                query = query.Where(entry => !entry.IsArchived);

            return await query
                .OrderByDescending(entry => entry.IsPinned)
                .ThenByDescending(entry => entry.IsUserApproved)
                .ThenByDescending(entry => entry.UpdatedAtUtc)
                .Take(Math.Clamp(take, 1, 500))
                .ToListAsync(cancellationToken);
        }

        public async Task<CouncilKnowledgeEntry> SaveEntryAsync(CouncilKnowledgeEntry entry, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await CouncilKnowledgeSchema.EnsureCreatedAsync(db, cancellationToken);
            await SeedKnowledgeAsync(db, cancellationToken);

            var now = DateTime.UtcNow;
            var existing = await db.CouncilKnowledgeEntries.SingleOrDefaultAsync(item => item.Id == entry.Id, cancellationToken);
            if (existing is null)
            {
                entry.CreatedAtUtc = entry.CreatedAtUtc == default ? now : entry.CreatedAtUtc;
                entry.UpdatedAtUtc = now;
                Normalize(entry);
                db.CouncilKnowledgeEntries.Add(entry);
            }
            else
            {
                existing.Topic = entry.Topic;
                existing.Scope = entry.Scope;
                existing.Content = entry.Content;
                existing.Source = entry.Source;
                existing.HelpfulSources = entry.HelpfulSources;
                existing.Tags = entry.Tags;
                existing.Confidence = entry.Confidence;
                existing.VerificationStatus = entry.VerificationStatus;
                existing.IsUserApproved = entry.IsUserApproved;
                existing.IsPinned = entry.IsPinned;
                existing.IsArchived = entry.IsArchived;
                existing.UpdatedAtUtc = now;
                Normalize(existing);
                entry = existing;
            }

            await db.SaveChangesAsync(cancellationToken);
            return entry;
        }

        public async Task DeleteEntryAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await CouncilKnowledgeSchema.EnsureCreatedAsync(db, cancellationToken);
            await SeedKnowledgeAsync(db, cancellationToken);
            var entry = await db.CouncilKnowledgeEntries.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (entry is null)
                return;

            db.CouncilKnowledgeEntries.Remove(entry);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<Guid> SaveFromCouncilRunAsync(MultiModelCouncilResult result, CancellationToken cancellationToken = default)
        {
            var nonSubstantive = IsNonSubstantiveCouncilKnowledge(result);
            var entry = new CouncilKnowledgeEntry
            {
                Topic = BuildTopic(result.Prompt),
                Scope = "AI Council",
                Source = $"AI Council {result.RunId}",
                Content = BuildCouncilKnowledgeContent(result),
                HelpfulSources = ExtractHelpfulSources(result.FinalAnswer),
                Tags = BuildTags(result, nonSubstantive),
                Confidence = nonSubstantive ? 20 : result.Warnings.Count == 0 ? 75 : 55,
                VerificationStatus = nonSubstantive ? "Archived" : "ModelSuggested",
                IsUserApproved = false,
                IsPinned = result.UserPoll is not null && !nonSubstantive,
                IsArchived = nonSubstantive
            };

            await SaveEntryAsync(entry, cancellationToken);
            logger.LogInformation("Saved council knowledge entry {KnowledgeEntryId} for council run {RunId}.", entry.Id, result.RunId);
            return entry.Id;
        }

        public async Task<string> BuildKnowledgeBriefingAsync(int take = 8, CancellationToken cancellationToken = default)
        {
            var entries = await GetEntriesAsync(includeArchived: false, take, cancellationToken);
            if (entries.Count == 0)
                return string.Empty;

            var builder = new StringBuilder()
                .AppendLine("AI Council maintained knowledge database:");

            var briefingEntries = entries
                .Where(entry => !LooksLikeNonSubstantiveContent(entry.Content))
                .OrderByDescending(entry => entry.IsUserApproved)
                .GroupBy(entry => $"{entry.Scope}|{entry.Topic}|{entry.Source}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First());

            foreach (var entry in briefingEntries)
            {
                var trust = BuildTrustLabel(entry);
                builder
                    .Append("- ")
                    .Append(entry.Topic)
                    .Append(" [")
                    .Append(entry.Scope)
                    .Append(", ")
                    .Append(trust)
                    .Append(", confidence ")
                    .Append(entry.Confidence)
                    .Append("%]: ")
                    .AppendLine(TrimForPrompt(entry.Content, 420));

                if (!string.IsNullOrWhiteSpace(entry.HelpfulSources))
                    builder.AppendLine($"  Helpful sources requested: {TrimForPrompt(entry.HelpfulSources, 240)}");
            }

            return builder.ToString().Trim();
        }

        private static void Normalize(CouncilKnowledgeEntry entry)
        {
            entry.Topic = TrimOrFallback(entry.Topic, 240, "Untitled knowledge entry");
            entry.Scope = TrimOrFallback(entry.Scope, 120, "AI Council");
            entry.Source = TrimOrFallback(entry.Source, 240, "Manual");
            entry.Tags = Trim(entry.Tags, 400);
            entry.Confidence = Math.Clamp(entry.Confidence, 0, 100);
            entry.VerificationStatus = NormalizeVerificationStatus(entry);
        }

        private static string BuildTrustLabel(CouncilKnowledgeEntry entry)
        {
            return entry.VerificationStatus switch
            {
                "SourceBacked" => "source-backed seed",
                "UserVerified" => "verified by user",
                "ModelSuggested" => "model-suggested; treat as hypothesis until user approves",
                "Archived" => "archived; do not use as active evidence",
                _ => entry.IsUserApproved
                    ? "verified by user"
                    : "needs verification"
            };
        }

        private static string NormalizeVerificationStatus(CouncilKnowledgeEntry entry)
        {
            if (entry.IsArchived)
                return "Archived";

            var requested = Trim(entry.VerificationStatus, 80).Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
            if (IsKnownVerificationStatus(requested))
                return requested;

            if (entry.Source.Contains("seed", StringComparison.OrdinalIgnoreCase))
                return "SourceBacked";

            if (entry.IsUserApproved)
                return "UserVerified";

            if (entry.Source.StartsWith("AI Council ", StringComparison.OrdinalIgnoreCase))
                return "ModelSuggested";

            return "NeedsVerification";
        }

        private static bool IsKnownVerificationStatus(string value)
        {
            return value is "SourceBacked" or "UserVerified" or "ModelSuggested" or "NeedsVerification" or "Archived";
        }

        private static async Task SeedKnowledgeAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken)
        {
            await SeedBuiltInKnowledgeAsync(db, cancellationToken);
            await SeedSqlKnowledgeAsync(db, cancellationToken);
        }

        private static async Task SeedBuiltInKnowledgeAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken)
        {
            const string seedSource = "LocalGPT built-in seed";
            var existingSeedIds = await db.CouncilKnowledgeEntries
                .Where(entry => entry.Source == seedSource)
                .Select(entry => entry.Id)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var entries = new[]
            {
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("157ea50f-093d-43dc-b7f6-546d74d8ad22"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "LocalGPT Blazor/DevExpress page generation rules",
                    Scope = "Blazor frontend",
                    Source = seedSource,
                    Content = "When generating LocalGPT UI, produce real .razor artifacts instead of C# classes that only build strings. " +
                        "Use @page, @rendermode InteractiveServer, @code blocks, dependency injection, and existing project styling such as main-container/top-container. " +
                        "Prefer known DevExpress Blazor components already used in this project: DxButton, DxCheckBox, DxComboBox, DxTextBox, DxMemo, DxSpinEdit, " +
                        "DxGrid, DxGridDataColumn, DxFormLayout, DxFormLayoutGroup, DxFormLayoutItem, DxLoadingPanel, DxMenu, DxGridLayout, and DXAiChat. " +
                        "Keep native commands and generated files in backend services with safe download routes.",
                    HelpfulSources = "- GET /__diag/devexpress for local DevExpress package/import/service inventory.\n- Local project pages: Components/Pages/Chat.razor, Database.razor, Install.razor, ModelCouncil.razor.\n- TacosPortalOpen sample zip inspected locally for server-interactive Razor + DevExpress patterns.",
                    Tags = "seed; blazor; devexpress; razor; dxaichat; artifacts",
                    Confidence = 90,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("dcf82c98-e535-453e-86c6-484a2795c140"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "TacosPortalOpen server-interactive architecture sample",
                    Scope = "Blazor frontend",
                    Source = seedSource,
                    Content = "TacosPortalOpen is a useful local architecture sample for Michi0403-style Blazor work. Relevant server-side patterns include Routes.razor " +
                        "with AuthorizeRouteView, pages using @rendermode InteractiveServer or new InteractiveServerRenderMode(prerender: true/false), AuthorizeView " +
                        "for protected UI, ToastWrapper/INotificationService for user feedback, and DevExpress DxGrid edit forms with EditFormTemplate + DxFormLayout. " +
                        "Treat the sample as architecture guidance, not code to copy blindly into LocalGPT.",
                    HelpfulSources = "- User-provided C:/Users/micha/Downloads/TacosPortalOpen-main.zip.\n- Inspected files: TacosPortal/Components/App.razor, Routes.razor, Pages/Index.razor, Pages/Admin/RoleAdministration.razor, Pages/GenericEditGrid.razor, Startup.cs.",
                    Tags = "seed; tacosportalopen; blazor; interactive-server; devexpress",
                    Confidence = 85,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("f1127721-1bc8-46a3-9d77-f0a89c92db37"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "EF Core and DevExpress business object generation",
                    Scope = "Entity Framework / DevExpress Web API",
                    Source = seedSource,
                    Content = "When generating EF Core business objects, first identify whether the user wants DevExpress Web API/XAF/OData-compatible business objects or a plain ASP.NET Core EF backend. " +
                        "For DevExpress Web API/XAF/OData, prefer explicit keys, scalar foreign keys, navigation properties, inverse relationships, attribute-visible validation/display/security metadata, and stable public properties for OData/model discovery. " +
                        "For plain EF backends, do not force the heavier DevExpress/XAF shape when services plus DTOs are simpler. " +
                        "Ask about snapshot/audit style, field-aware changes, backing fields, lazy loading, delete behavior, security system requirements, naming constraints, and migration nullability before emitting entities. " +
                        "Avoid accidental shadow properties by using consistent names, explicit FK scalar properties, [ForeignKey], [InverseProperty], and targeted ModelBuilder configuration. " +
                        "For reverse-engineered databases such as the user-supplied Telegram schema, preserve exact relationship semantics and naming; if field/property names may differ only by first-letter casing, do not casually rename them. " +
                        "When adding columns to populated databases, prefer nullable first migrations, semantic defaults, or backfill/multi-step migrations instead of blindly adding NOT NULL columns.",
                    HelpfulSources = "- Local guide: docs/EF_DEVEXPRESS_BUSINESS_OBJECTS.md.\n- DevExpress XAF Data Annotation Attributes: https://docs.devexpress.com/eXpressAppFramework/112701/business-model-design-orm/data-annotations-in-data-model.\n- DevExpress Backend Web API Service: https://docs.devexpress.com/eXpressAppFramework/403394/backend-web-api-service.\n- EF Core shadow properties: https://learn.microsoft.com/ef/core/modeling/shadow-properties.\n- EF Core relationship mapping attributes: https://learn.microsoft.com/ef/core/modeling/relationships/mapping-attributes.",
                    Tags = "seed; efcore; devexpress-web-api; xaf; odata; business-objects; shadow-properties; migrations; reverse-engineering",
                    Confidence = 94,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("243e82e1-9f6d-4b8c-abd8-75e7cda0c776"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Official docs and missing-source behavior",
                    Scope = "AI Council",
                    Source = seedSource,
                    Content = "If the council needs GitHub repository details, DevExpress APIs, .NET/Blazor version behavior, or official syntax rules and the local diagnostics " +
                        "do not provide enough evidence, it must say exactly which source is needed under Helpful sources requested or Missing feature report. " +
                        "Do not blame the user or hallucinate APIs. Prefer compact diagnostics first: /__diag/devexpress, /__diag/dxaichat-functions, " +
                        "/__diag/build-debug-files, /__diag/logs, and SQLite knowledge entries. Mark claims as Needs verification until the source or local package inventory confirms them.",
                    HelpfulSources = "- Official Microsoft Learn .NET/ASP.NET Core/Blazor docs when internet access is allowed.\n- DevExpress official Blazor docs matching the installed package version.\n- GitHub repository source files or local extracted zips supplied by Michi0403.",
                    Tags = "seed; sources; github; dotnet; devexpress; needs-verification",
                    Confidence = 95,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("46d5a4c1-c873-4285-b5e7-d2c58eb5b6be"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Minecraft Java mod and plugin source map",
                    Scope = "Minecraft Builder",
                    Source = seedSource,
                    Content = "Use this source map before generating Java Minecraft workspaces. Classic Forge uses the Forge MDK: download the MDK, extract it into an empty directory, " +
                        "import/open the Gradle project in Eclipse or IntelliJ, build with gradlew build, and test with generated run configs or gradlew runClient/runServer. " +
                        "Fabric builds with ./gradlew build or ./gradlew.bat build; use the shortest jar in build/libs for distribution and make sure the terminal/IDE Java version matches the project. " +
                        "Paper is the server-side plugin path for users who do not want a modded client; include plugin.yml and use Paper's plugin project setup guidance. " +
                        "Use Gradle Java toolchains or explicit IDE Gradle JVM settings to avoid inconsistent JDK behavior. Java syntax should be grounded in the Java Language Specification/JDK docs; " +
                        "Microsoft OpenJDK is a supported JDK distribution, not a separate Java syntax.",
                    HelpfulSources = "- Forge getting started: https://docs.minecraftforge.net/en/latest/gettingstarted/\n- NeoForge getting started: https://docs.neoforged.net/docs/gettingstarted/\n- Fabric building a mod: https://docs.fabricmc.net/develop/getting-started/building-a-mod\n- Paper getting started: https://docs.papermc.io/paper/dev/getting-started/\n- Gradle JVM toolchains: https://docs.gradle.org/current/userguide/toolchains.html\n- Oracle JDK 21 documentation: https://docs.oracle.com/en/java/javase/21/",
                    Tags = "seed; minecraft; forge; fabric; neoforge; paper; gradle; java; sources",
                    Confidence = 92,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("75c6cc67-958d-4b66-bfb2-f8f98eccd0c4"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Minecraft datapack generation source rules",
                    Scope = "Minecraft Builder",
                    Source = seedSource,
                    Content = "For vanilla Java datapacks, generate a zip/folder whose root contains pack.mcmeta and data/. The data folder contains namespaces; function entry points " +
                        "for modern 1.21-style generated packs should use singular folders such as data/<namespace>/function and data/minecraft/tags/function. " +
                        "Add data/minecraft/tags/function/load.json and tick.json to call namespace functions; minecraft:load runs after /reload or server load, and minecraft:tick runs each tick, " +
                        "so tick functions must stay tiny and delegate scheduled aggregate work. pack_format is required and version-sensitive; LocalGPT should use its datapack version catalog " +
                        "or source-check the target version before claiming compatibility. supported_formats and overlays exist for multi-format packs, but basic generated starters should keep one target version unless the user asks for overlays.",
                    HelpfulSources = "- Minecraft Wiki data pack structure and pack.mcmeta: https://minecraft.wiki/w/Data_pack\n- Minecraft Wiki Java function tags: https://minecraft.wiki/w/Function_tag_(Java_Edition)\n- Minecraft Java snapshot 23w31a pack metadata supported_formats/overlays: https://feedback.minecraft.net/hc/en-us/articles/18619031671821-Minecraft-Java-Edition-Snapshot-23w31a\n- Minecraft Wiki pack_format table: https://minecraft.wiki/w/Pack_format",
                    Tags = "seed; minecraft; datapack; pack.mcmeta; function-tags; pack-format; sources",
                    Confidence = 88,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("1b84f98e-3c07-4f8b-ac3d-2a42bd9fb0c5"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Living Cities datapack benchmark acceptance",
                    Scope = "Minecraft Builder",
                    Source = seedSource,
                    Content = "Use /__diag/minecraft/datapack-benchmark?minecraftVersion=1.21.4 as the low-context Living Cities datapack benchmark. " +
                        "A useful result must generate real .mcfunction files, no .mcfunction.txt placeholders, pack.mcmeta, minecraft load/tick function tags, namespace functions, " +
                        "JSON validation, function-reference validation, and a zip under build/. Compare against the friend's early living_cities.zip for preserved traits: namespace living_cities, " +
                        "core/load and core/tick entry points, scoreboards for year/population/food/security/prestige/birth year, storage areas for city/chronicle/personalities, " +
                        "and a town hall/admin workflow. Do not tell the user it was game-tested until /reload and in-game commands were actually run in Minecraft.",
                    HelpfulSources = "- Local route: GET /__diag/minecraft/datapack-benchmark?minecraftVersion=1.21.4\n- User-provided benchmark: C:/Users/micha/Downloads/living_cities.zip\n- User-provided design prompt: C:/Users/micha/Downloads/message (1).txt",
                    Tags = "seed; minecraft; datapack; living-cities; benchmark; validation",
                    Confidence = 90,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("eaf4e13b-9a10-42fa-9280-fd0fe5c01f9f"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Whole solution Blazor/DevExpress generation acceptance",
                    Scope = "LocalGPT generation",
                    Source = seedSource,
                    Content = "When a user asks for more than a snippet, LocalGPT should create a downloadable whole-solution zip. " +
                        "A useful .NET 10 Blazor/DevExpress solution artifact includes a .sln, .csproj, Program.cs, _Imports.razor, App/Routes, routable .razor pages, CSS, service/model code, README, and manifest. " +
                        "For LocalGPT/TacosPortalOpen-style requests, generate real Razor components with DevExpress controls and backend/service boundaries, then expose the zip through /__artifacts/council/. " +
                        "Do not send entire repositories as giant model context; use compact source-corpus metadata and official/source-backed knowledge first.",
                    HelpfulSources = "- Local artifact route: GET /__diag/council/artifact-smoke?target=solution\n- LocalGPT source tree for current Blazor/DevExpress patterns\n- User-provided TacosPortalOpen-main.zip for server-interactive architecture patterns",
                    Tags = "seed; whole-solution; blazor; devexpress; tacosportalopen; localgpt; artifacts",
                    Confidence = 90,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("3c0f8c1e-7a27-466d-9f8e-11ff4a3a7a22"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Ollama .NET Blazor experiment feasibility",
                    Scope = "LocalGPT generation",
                    Source = seedSource,
                    Content = "The user-provided ollama-main.zip is mostly Go plus native runtime/build layers: about 1,214 source entries, about 722 .go files, and major areas such as app, docs, template, model, server, cmd, convert, discover, llm, api, llama, runner, native C/C++ headers, and CMake. " +
                        "A pure .NET/Blazor replacement should be treated as a fun feasibility lab, not a real drop-in Ollama replacement. Feasible target: generate a .NET 10 ASP.NET Core control plane and DevExpress Blazor UI that mimics selected Ollama REST routes, model catalog/status, runner health, logs, and compatibility notes. " +
                        "Infeasible without deeper work: replacing Ollama's native inference/runtime, GGML/GPU backends, CMake payload, CUDA/ROCm/Vulkan/Metal paths, tokenizer/model conversion, and full manifest/model storage semantics. " +
                        "Generated work must be a sandbox solution zip and must not claim real inference unless an actual .NET/native inference backend is supplied and tested.",
                    HelpfulSources = "- User-provided source archive: C:/Users/micha/Downloads/ollama-main.zip\n- Local note: docs/OLLAMA_DOTNET_EXPERIMENT.md\n- Useful generated route: GET /__diag/council/artifact-smoke?target=ollama",
                    Tags = "seed; ollama; dotnet; blazor; devexpress; feasibility; whole-solution; artifacts",
                    Confidence = 88,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("77d1c42f-1c72-47bb-a08e-522ce742126d"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Ollama .NET lab generated stack constraint",
                    Scope = "LocalGPT generation",
                    Source = seedSource,
                    Content = "For the Ollama-inspired .NET/Blazor/DevExpress lab, the generated downloadable project must stay in .NET, C#, ASP.NET Core, Razor, EF/SQLite, and DevExpress Blazor. " +
                        "Do not propose generated Go or Python projects for this lab. If inference is discussed, describe it as a generic external/native backend contract, an existing service adapter, or a future approved .NET/native integration. " +
                        "The generated solution should include selected Ollama-style route stubs and UI, but must clearly say native model inference is not implemented by the all-.NET lab.",
                    HelpfulSources = "- Local note: docs/OLLAMA_DOTNET_EXPERIMENT.md\n- Local artifact route: GET /__diag/council/artifact-smoke?target=ollama",
                    Tags = "seed; ollama; dotnet-only; blazor; devexpress; constraints; artifacts",
                    Confidence = 92,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("a6c530d8-844f-4df6-a6bf-bb96c85b4af2"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "DXAiChat council runtime budget and GPU safety",
                    Scope = "DXAiChat AI Council",
                    Source = seedSource,
                    Content = "For Michi0403's 7900 XTX/14700K machine, prefer one active Ollama model at a time for council phases and order lightweight/known-stable models first. " +
                        "gpt-oss:20b has been the preferred first test model; deepseek-r1:8b can be useful but may be slow to produce final visible text; qwen/gwen/gemma should not be auto-selected for GPU-heavy smoke tests. " +
                        "Use limited GPU layers and compact prompts for diagnostics, but allow large user-configurable answer/context budgets for code generation. " +
                        "Defaults should not clamp source generation to tiny 2K/8K answers; LocalGPT now allows up to 131K answer/context tokens while still warning for large requests. " +
                        "If a council request stalls, stream visible phase/status updates and ask for a user poll instead of silently spinning.",
                    HelpfulSources = "- Local UI: Components/Pages/Chat.razor council token and model controls.\n- Local service: MultiModelCouncilService model ordering, max output/context, timeout, and warnings.\n- Local diagnostics: /__diag/council/artifact-smoke and /__diag/dxaichat-smoke.",
                    Tags = "seed; dxaichat; council; ollama; gpu-safety; gpt-oss; tokens; performance",
                    Confidence = 90,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("e82329d7-c8f9-47d4-9c7b-63a0fc338663"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Harmony and model thinking display",
                    Scope = "DXAiChat AI Council",
                    Source = seedSource,
                    Content = "Some Ollama-hosted OpenAI-style models stream Harmony channel markers such as analysis/commentary/final instead of plain Markdown or <think> tags. " +
                        "LocalGPT should adapt by model name and render model-supplied analysis/commentary in a visible Model thinking block while keeping final text readable. " +
                        "If a model returns thinking but no final answer, close the model-thinking details/pre block before rendering the incomplete-answer notice so DXAiChat never looks like it is still only thinking. " +
                        "Prompt Harmony models to keep analysis bounded and always emit user-visible final-channel text. " +
                        "Do not expose hidden chain-of-thought invented by the application; only display text actually supplied by the local model stream. " +
                        "When the user presses Stop, treat cancellation as a quiet user action and avoid unhandled TaskCanceledException in DXAiChat.",
                    HelpfulSources = "- Local service: OllamaThinkingChatClient VisibleThinkingStreamFormatter.\n- Local CSS: wwwroot/css/site.css model-thinking styles.\n- User observation: Harmony formatting sometimes broke in DXAiChat until adaptive parsing was added.",
                    Tags = "seed; harmony; thinking; dxaichat; streaming; cancellation",
                    Confidence = 90,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("b2af09bb-98a2-4ce1-85d5-f6aa06cc2c6e"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Minecraft datapack download contract",
                    Scope = "Minecraft Builder",
                    Source = seedSource,
                    Content = "When DXAiChat or the council is asked for a Minecraft Java datapack/modpack, LocalGPT should create a downloadable artifact instead of printing zip bytes as text. " +
                        "For vanilla datapacks the HTTP artifact must be a zip whose root has pack.mcmeta and data/ directly, with no wrapper folder. " +
                        "For Minecraft 1.21+ use singular data/<namespace>/function and data/minecraft/tags/function folders; use plural functions only for older targets. " +
                        "Reject .mcfunction.txt, uppercase namespaces, leading slash commands inside mcfunction files, broken function references, invalid tag JSON, and root storage removal syntax. " +
                        "Prefer data modify storage <id> set value {} for reset/debug operations and include a harmless visible debug command such as say LC register_banner loaded.",
                    HelpfulSources = "- Local service: MinecraftModWorkspaceService validation rules.\n- Local service: CouncilArtifactService datapack artifact branch.\n- Local route: GET /__diag/council/artifact-smoke?target=datapack.\n- User-provided datapack troubleshooting prompt in Codex attachment 91c5282e.",
                    Tags = "seed; minecraft; datapack; dxaichat; download; artifact; validation",
                    Confidence = 92,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("44fd504b-c6de-4a8b-a4e0-aea5a53200d9"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "LocalGPT static web assets direct-debug repair",
                    Scope = "ASP.NET Core host",
                    Source = seedSource,
                    Content = "Direct backend debugging can fail before WebApplication.CreateBuilder when LocalGPT.staticwebassets.runtime.json references generated obj static asset roots that no longer exist, especially obj/.../compressed. " +
                        "This surfaces as DirectoryNotFoundException from PhysicalFileProvider/StaticWebAssetsLoader and can leave WebView2 showing unstyled fallback HTML if assets are not reachable. " +
                        "Before CreateBuilder, LocalGPT should inspect its static web asset runtime manifest and recreate only missing generated obj roots such as compressed and scopedcss/bundle. " +
                        "Do not create missing NuGet/DevExpress package roots, because that would hide real package restore problems.",
                    HelpfulSources = "- Local code: Program.EnsureGeneratedStaticWebAssetContentRoots.\n- Verified probes: /, /Chat, /_framework/blazor.web.js, /_content/DevExpress.Blazor/dx-blazor.svg returned HTTP 200 after the repair.",
                    Tags = "seed; static-web-assets; aspnetcore; devexpress; direct-debug; webview2",
                    Confidence = 90,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("8bbd9161-5406-4d2d-bce5-b92c0fd10216"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "SQLite table editing and diagnostics",
                    Scope = "SQLite database",
                    Source = seedSource,
                    Content = "The SQLite database page lets the frontend user inspect and edit all LocalGPT memory/knowledge/log tables, but generated table editing must respect required columns, primary keys, and SQLite constraints. " +
                        "Validate insert/update requests before executing SQL, reject nulls for required non-PK columns without defaults, and wrap SqliteException with a user-readable message that names the table and operation. " +
                        "Application log warnings from HTTP loopback HTTPS redirection are noise for the desktop host and should not be treated as user action items. " +
                        "Useful runtime errors should be written to the database logger/ApplicationLogs so the council can explain local setup fixes such as missing Java, Ollama not running, or static asset failures.",
                    HelpfulSources = "- Local service: SqliteTableEditorService validation.\n- Local page: Components/Pages/Database.razor.\n- Local table: ApplicationLogs.",
                    Tags = "seed; sqlite; database; logs; diagnostics; table-editor",
                    Confidence = 88,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("1920341b-ae13-438d-a8c2-6d57e588e4a3"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "WebView2 packaged smoke path",
                    Scope = "WinUI WebView2 wrapper",
                    Source = seedSource,
                    Content = "Do not use the loose LocalGPTWebviewWrapper.exe as the primary WebView2 smoke test for WinUI. " +
                        "Loose launch can fail with REGDB_E_CLASSNOTREG at Microsoft.UI.Xaml.Application.Start when the Windows App SDK package context is missing. " +
                        "Use the registered MSIX/package activation path or Visual Studio package project for WebView2 frontend smoke tests. " +
                        "The wrapper should write startup diagnostics and runtime server/snapshot files under LocalGPT runtime folders, including package-local LocalCache paths when running packaged.",
                    HelpfulSources = "- Local code: LocalGPTWebviewWrapper/App.xaml.cs and MainWindow.xaml.cs runtime flag lookup.\n- Windows Application log showed REGDB_E_CLASSNOTREG for loose WinUI launch.\n- Preferred activation: shell:AppsFolder/<package-family-name>!App.",
                    Tags = "seed; webview2; winui; package; msix; smoke-test; windowsappsdk",
                    Confidence = 88,
                    IsUserApproved = true,
                    IsPinned = true
                }
            };

            foreach (var entry in entries)
                Normalize(entry);

            var missingEntries = entries
                .Where(entry => !existingSeedIds.Contains(entry.Id))
                .ToArray();

            if (missingEntries.Length == 0)
            {
                await MarkApprovedBuiltInSeedsAsVerifiedAsync(db, seedSource, cancellationToken);
                return;
            }

            try
            {
                db.CouncilKnowledgeEntries.AddRange(missingEntries);
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Another startup request may have inserted the same stable seed IDs first.
            }

            await MarkApprovedBuiltInSeedsAsVerifiedAsync(db, seedSource, cancellationToken);
        }

        private static Task MarkApprovedBuiltInSeedsAsVerifiedAsync(
            LocalGptMemoryDbContext db,
            string seedSource,
            CancellationToken cancellationToken)
        {
            return db.Database.ExecuteSqlRawAsync(
                """
                UPDATE "CouncilKnowledgeEntries"
                SET "VerificationStatus" = 'UserVerified'
                WHERE "Source" = {0}
                  AND "IsUserApproved" = 1
                  AND (
                      "VerificationStatus" IS NULL
                      OR "VerificationStatus" = ''
                      OR "VerificationStatus" = 'NeedsVerification'
                  );
                """,
                [seedSource],
                cancellationToken);
        }

        private static async Task SeedSqlKnowledgeAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken)
        {
            var path = FindSqlSeedPath();
            if (path is null)
                return;

            var sql = await File.ReadAllTextAsync(path, cancellationToken);
            if (string.IsNullOrWhiteSpace(sql))
                return;

            await db.Database.ExecuteSqlRawAsync(EscapeSqlFormatBraces(sql), cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """
                UPDATE "CouncilKnowledgeEntries"
                SET "VerificationStatus" =
                    CASE
                        WHEN "Source" = 'User-approved generation advice' THEN 'UserVerified'
                        ELSE 'SourceBacked'
                    END
                WHERE "Source" IN (
                    'LocalGPT SQL seed',
                    'Microsoft Learn source-backed seed',
                    'User-approved generation advice'
                )
                  AND ("VerificationStatus" IS NULL OR trim("VerificationStatus") = '' OR "VerificationStatus" = 'NeedsVerification');
                """,
                cancellationToken);
        }

        private static string EscapeSqlFormatBraces(string sql)
        {
            return sql.Replace("{", "{{", StringComparison.Ordinal)
                .Replace("}", "}}", StringComparison.Ordinal);
        }

        private static string? FindSqlSeedPath()
        {
            const string seedFileName = "COUNCIL_KNOWLEDGE_SEED.sql";
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "docs", seedFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "docs", seedFileName)
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "docs", seedFileName);
                if (File.Exists(candidate))
                    return candidate;

                directory = directory.Parent;
            }

            return null;
        }

        private static string BuildCouncilKnowledgeContent(MultiModelCouncilResult result)
        {
            var builder = new StringBuilder()
                .AppendLine($"Council members: {string.Join(", ", result.ModelNames)}")
                .AppendLine($"Prompt: {TrimForPrompt(result.Prompt, 900)}")
                .AppendLine()
                .AppendLine("Final answer:")
                .AppendLine(TrimForPrompt(result.FinalAnswer, 2400));

            if (result.Warnings.Count > 0)
            {
                builder.AppendLine().AppendLine("Warnings:");
                foreach (var warning in result.Warnings.Take(10))
                    builder.AppendLine($"- {warning}");
            }

            if (result.UserPoll is not null)
            {
                builder.AppendLine().AppendLine("User decision poll:");
                builder.AppendLine(result.UserPoll.Question);
                foreach (var option in result.UserPoll.Options)
                    builder.AppendLine($"- {option.Label}: {option.FollowUpPrompt}");
            }

            return builder.ToString().Trim();
        }

        private static string BuildTopic(string prompt)
        {
            var normalized = WhitespacePattern().Replace(prompt, " ").Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return "AI Council run";

            return normalized.Length <= 120 ? normalized : $"{normalized[..117].TrimEnd()}...";
        }

        private static string BuildTags(MultiModelCouncilResult result, bool nonSubstantive)
        {
            var tags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "council",
                "auto"
            };

            foreach (var model in result.ModelNames)
                tags.Add(model);
            if (result.Artifacts.Count > 0)
                tags.Add("artifact");
            if (result.UserPoll is not null)
                tags.Add("poll");
            if (nonSubstantive)
            {
                tags.Add("non-substantive");
                tags.Add("thinking-only");
            }

            return string.Join("; ", tags);
        }

        private static bool IsNonSubstantiveCouncilKnowledge(MultiModelCouncilResult result)
        {
            if (result.UserPoll is not null)
                return false;

            return LooksLikeNonSubstantiveContent(result.FinalAnswer);
        }

        private static bool LooksLikeNonSubstantiveContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return true;

            return content.Contains("returned thinking but no final visible answer", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("did not return a visible answer", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("did not return a substantive consensus answer", StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractHelpfulSources(string text)
        {
            var matches = HelpfulSourceLinePattern()
                .Matches(text)
                .Select(match => match.Groups["line"].Value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();

            return matches.Count == 0
                ? "None explicitly requested."
                : string.Join(Environment.NewLine, matches.Select(item => $"- {item}"));
        }

        private static string TrimForPrompt(string text, int maxLength)
        {
            var normalized = WhitespacePattern().Replace(text, " ").Trim();
            return normalized.Length <= maxLength
                ? normalized
                : $"{normalized[..maxLength].TrimEnd()}...";
        }

        private static string TrimOrFallback(string value, int maxLength, string fallback)
        {
            var trimmed = Trim(value, maxLength);
            return string.IsNullOrWhiteSpace(trimmed) ? fallback : trimmed;
        }

        private static string Trim(string value, int maxLength)
        {
            var trimmed = value?.Trim() ?? string.Empty;
            return trimmed.Length <= maxLength ? trimmed : $"{trimmed[..maxLength].TrimEnd()}";
        }

        [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
        private static partial Regex WhitespacePattern();

        [GeneratedRegex("(?im)^\\s*(?:[-*]\\s*)?(?<line>(?:helpful sources?|source request|needed sources?|references?|docs?|documentation|official docs?|examples?|sample projects?|spec(?:ification)?s?|tutorials?)\\s*[:\\-].+)$", RegexOptions.CultureInvariant)]
        private static partial Regex HelpfulSourceLinePattern();
    }
}
