using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
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

            var now = DateTime.UtcNow;
            var query = db.CouncilKnowledgeEntries.AsNoTracking();
            if (!includeArchived)
                query = query.Where(entry =>
                    !entry.IsArchived &&
                    entry.ReviewStatus != "Archived" &&
                    entry.ReviewStatus != "Deprecated" &&
                    entry.ReviewStatus != "Superseded" &&
                    entry.ReviewStatus != "Expired" &&
                    (entry.ExpiresAtUtc == null || entry.ExpiresAtUtc > now));

            return await query
                .OrderByDescending(entry => entry.IsPinned)
                .ThenByDescending(entry => entry.IsUserApproved)
                .ThenBy(entry => entry.ReviewStatus)
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
                existing.ReviewStatus = entry.ReviewStatus;
                existing.ExpiresAtUtc = entry.ExpiresAtUtc;
                existing.LastVerifiedAtUtc = entry.LastVerifiedAtUtc;
                existing.LastUsedAtUtc = entry.LastUsedAtUtc;
                existing.SupersededByKnowledgeId = entry.SupersededByKnowledgeId;
                existing.StalenessReason = entry.StalenessReason;
                existing.StalenessDetectedAtUtc = entry.StalenessDetectedAtUtc;
                existing.StalenessDetectedBy = entry.StalenessDetectedBy;
                existing.SourceHash = entry.SourceHash;
                existing.SourceDateUtc = entry.SourceDateUtc;
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
                ReviewStatus = nonSubstantive ? "Archived" : "NeedsUserReview",
                ExpiresAtUtc = nonSubstantive ? null : DateTime.UtcNow.AddDays(30),
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
                .Where(IsUsableForBriefing)
                .OrderByDescending(entry => entry.IsUserApproved)
                .GroupBy(entry => $"{entry.Scope}|{entry.Topic}|{entry.Source}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            if (briefingEntries.Count == 0)
                return string.Empty;

            await MarkEntriesUsedAsync(briefingEntries.Select(entry => entry.Id), cancellationToken);

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
            entry.ReviewStatus = NormalizeReviewStatus(entry);
            entry.StalenessReason = Trim(entry.StalenessReason, 500);
            entry.StalenessDetectedBy = Trim(entry.StalenessDetectedBy, 160);
            entry.SourceHash = Trim(entry.SourceHash, 128);
            if (string.IsNullOrWhiteSpace(entry.SourceHash))
                entry.SourceHash = ComputeSourceHash(entry);

            if (entry.VerificationStatus is "SourceBacked" or "UserVerified" && entry.LastVerifiedAtUtc is null)
                entry.LastVerifiedAtUtc = DateTime.UtcNow;

            if (entry.ReviewStatus == "Archived")
                entry.IsArchived = true;
        }

        private static string BuildTrustLabel(CouncilKnowledgeEntry entry)
        {
            var trust = entry.VerificationStatus switch
            {
                "SourceBacked" => "source-backed seed",
                "UserVerified" => "verified by user",
                "ModelSuggested" => "model-suggested; treat as hypothesis until user approves",
                "Archived" => "archived; do not use as active evidence",
                _ => entry.IsUserApproved
                    ? "verified by user"
                    : "needs verification"
            };

            var review = entry.ReviewStatus switch
            {
                "Current" => "current",
                "NeedsUserReview" => "needs user review",
                "NeedsSourceRefresh" => "needs source refresh",
                "NeedsDiagnosticVerification" => "needs diagnostic verification",
                "Expired" => "expired",
                "Deprecated" => "deprecated",
                "Superseded" => "superseded",
                "Archived" => "archived",
                _ => "needs review"
            };

            return $"{trust}; review: {review}";
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

        private static string NormalizeReviewStatus(CouncilKnowledgeEntry entry)
        {
            if (entry.IsArchived)
                return "Archived";

            if (entry.SupersededByKnowledgeId is not null)
                return "Superseded";

            var now = DateTime.UtcNow;
            if (entry.ExpiresAtUtc is not null && entry.ExpiresAtUtc.Value <= now)
            {
                if (string.IsNullOrWhiteSpace(entry.StalenessReason))
                    entry.StalenessReason = "Knowledge expiry date passed.";
                entry.StalenessDetectedAtUtc ??= now;
                entry.StalenessDetectedBy = TrimOrFallback(entry.StalenessDetectedBy, 160, "LocalGPT knowledge lifecycle");
                return "Expired";
            }

            var requested = Trim(entry.ReviewStatus, 80).Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
            if (requested == "NeedsUserReview" &&
                entry.IsUserApproved &&
                entry.VerificationStatus is "SourceBacked" or "UserVerified")
                return "Current";

            if (IsKnownReviewStatus(requested))
                return requested;

            return entry.VerificationStatus switch
            {
                "SourceBacked" or "UserVerified" => "Current",
                "Archived" => "Archived",
                _ => "NeedsUserReview"
            };
        }

        private static bool IsKnownReviewStatus(string value)
        {
            return value is "Current" or "NeedsUserReview" or "NeedsSourceRefresh" or "NeedsDiagnosticVerification" or "Expired" or "Deprecated" or "Superseded" or "Archived";
        }

        private static bool IsUsableForBriefing(CouncilKnowledgeEntry entry)
        {
            if (entry.IsArchived)
                return false;

            if (entry.ExpiresAtUtc is not null && entry.ExpiresAtUtc.Value <= DateTime.UtcNow)
                return false;

            return entry.ReviewStatus is not "Archived" and not "Deprecated" and not "Superseded" and not "Expired";
        }

        private async Task MarkEntriesUsedAsync(IEnumerable<Guid> entryIds, CancellationToken cancellationToken)
        {
            var ids = entryIds.Distinct().ToArray();
            if (ids.Length == 0)
                return;

            try
            {
                await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                await CouncilKnowledgeSchema.EnsureCreatedAsync(db, cancellationToken);
                var entries = await db.CouncilKnowledgeEntries
                    .Where(entry => ids.Contains(entry.Id))
                    .ToListAsync(cancellationToken);
                var now = DateTime.UtcNow;
                foreach (var entry in entries)
                    entry.LastUsedAtUtc = now;

                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is DbUpdateException or DbUpdateConcurrencyException or IOException)
            {
                if (LocalGptDatabaseRecovery.IsSqliteCorruption(ex))
                {
                    await LocalGptDatabaseRecovery.RecoverMalformedDatabaseAsync(DatabasePath, logger, cancellationToken);
                }

                logger.LogWarning(ex, "Could not update LastUsedAtUtc for council knowledge entries. Knowledge briefing will continue with read-only data.");
            }
        }

        private static string ComputeSourceHash(CouncilKnowledgeEntry entry)
        {
            var sourceMaterial = $"{entry.Topic}\n{entry.Scope}\n{entry.Source}\n{entry.HelpfulSources}\n{entry.Content}";
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceMaterial)));
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
                    Id = Guid.Parse("ef1d0872-b8bb-4d7c-9b67-3d092f99a54d"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "TacosPortalOpen source-fidelity generation contract",
                    Scope = "Replacement generation / Blazor / DevExpress",
                    Source = seedSource,
                    Content = "A generated TacosPortalOpen replacement must preserve the actual architecture signals, not only the domain name. " +
                        "The useful pattern is a multi-project .NET/Blazor solution with a shared/core layer, server-interactive host, optional WASM client, WinUI/WebView2 wrapper boundary, Telegram or message-event ingestion, update handlers, service/API boundaries, normalized persistence, worker/polling services, notifications/logging, custom security/admin screens, and build/deploy diagnostics. " +
                        "A generic taco menu, order queue, and reservation app is the wrong template unless the user explicitly asks for only restaurant CRUD. " +
                        "Generated replacements should include a Source Fidelity page/service/doc explaining which original-system workflows are represented, boundary-only, or missing.",
                    HelpfulSources = "- Local learn-base: C:\\tmpselectedcodexlearnbaseforlocalgpt\\TacosPortalOpen.\n- Local docs: docs/GENERATION_ARCHETYPE_CONTRACTS.md.\n- Local generator files: CouncilArtifactService source-fidelity artifact contract and EngineeringBenchmarkService replacement tasks.",
                    Tags = "seed; tacosportalopen; source-fidelity; replacement; telegram; workers; webview2; wasm; devexpress; blazor",
                    Confidence = 94,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("58d6b4d7-c8e3-450a-a31a-57f0e9fc0b1a"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Compiled frontend design pattern library",
                    Scope = "Frontend generation / Blazor / DevExpress / Bootstrap",
                    Source = seedSource,
                    Content = "Use LocalGPT's compiled frontend design pattern library directly when generating UI. " +
                        "Do not tell the user or model to use external galleries as runtime guidance; the relevant design concepts are already distilled into reusable archetypes, component mappings, service wiring, and accessibility checks. " +
                        "First classify the app as commerce, social/community, AI host/developer tool, SaaS/admin, media workbench, or another product archetype, then identify the primary task and information architecture. " +
                        "Use Bootstrap for responsive macro layout and DevExpress for application-grade interaction; create custom Razor components when the selected stack lacks a visual shell. " +
                        "Apply Microsoft Windows/Fluent design foundations: color hierarchy, commanding, elevation, geometry, iconography, layout, materials, motion, navigation, typography, usability, widgets, and writing. " +
                        "Generated frontends must include real pages, navigation, service boundaries, loading/empty/error/success states, accessible labels, and safe artifact/download routes when files are generated.",
                    HelpfulSources = "- Local guide: docs/FRONTEND_DESIGN_PATTERN_LIBRARY.md.\n" +
                        "- Local route: GET /__diag/frontend-design-guidance.\n" +
                        "- Microsoft Windows app design guidelines: https://learn.microsoft.com/en-us/windows/apps/design/guidelines-overview.\n" +
                        "- DevExpress Blazor components: https://docs.devexpress.com/Blazor/400725/blazor-components.\n" +
                        "- Bootstrap v5 docs: https://getbootstrap.com/docs/5.3/layout/grid/.",
                    Tags = "seed; frontend-design; blazor; devexpress; bootstrap; windows-design; fluent; accessibility; archetypes",
                    Confidence = 94,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("31f7cfa5-8b68-47be-9c32-9d046f88cc85"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = ".NET AI host architecture and native-runner adapter rules",
                    Scope = "AI host generation / .NET architecture",
                    Source = seedSource,
                    Content = "When generating an AI-host-shaped .NET application, produce more than pages. " +
                        "Generate provider-neutral ASP.NET Core routes, typed options, DI registrations, EF/SQLite state, model catalog/download/session services, chat/template services, logs, settings, hardware budget policy, and downloadable artifact routes when useful. " +
                        "Use interface-driven boundaries: IModelCatalogService, IModelTransferService, IInferenceProvider, IInferenceRunner, IPluginCatalogService, IScriptExecutionService, IHardwareBudgetService, and IChatTemplateService. " +
                        "External hosts such as Ollama, LM Studio, OpenAI, HuggingFace downloads, Python.NET, PowerShell, ONNX, ML.NET, or native executables are adapters behind interfaces, not the product identity. For Michi0403's accepted AI-host target, /api/chat and /api/generate must use direct local model-file runner paths, not upstream Ollama/LM Studio/OpenAI-compatible proxying. " +
                        "TypeScript is allowed when the requested solution needs client assets, browser automation, or a script adapter inside the ASP.NET Core/.NET application, but it should not accidentally replace the .NET control plane or Python/Python.NET model-runtime architecture. " +
                        "Use .NET DI/IoC, the options pattern, hosted/background services for queued work, typed HttpClient for provider calls, AssemblyLoadContext/AssemblyDependencyResolver only for trusted plugins, and permission-gated Python.NET/PowerShell/native process execution with safe directories, cancellation, and logs. " +
                        "If real native inference is not configured, say so in the generated UI and produce a visible runner/plugin setup page; do not substitute an upstream provider proxy as a milestone. " +
                        "Generated AI-host solutions must include recognizable navigation for dashboard, model catalog, API console, chat, running models, downloads, templates, hardware, runner/plugins, logs, and settings.",
                    HelpfulSources = "- Local guide: docs/DOTNET_AI_HOST_ARCHITECTURE_PATTERNS.md.\n" +
                        "- Local route: GET /__diag/ai-host-rebuild-guidance.\n" +
                        "- .NET dependency injection: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview.\n" +
                        "- .NET options pattern: https://learn.microsoft.com/en-us/dotnet/core/extensions/options.\n" +
                        "- ASP.NET Core hosted services: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services.\n" +
                        "- .NET plugin support: https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support.\n" +
                        "- PowerShell runspaces: https://learn.microsoft.com/en-us/powershell/scripting/developer/hosting/creating-runspaces.",
                    Tags = "seed; ai-host; dotnet; architecture; dependency-injection; options; hosted-services; plugins; pythonnet; powershell; native-runner; adapters",
                    Confidence = 95,
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
                    Id = Guid.Parse("9a22b442-e53d-4c36-b56e-bc2a37f11c38"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "DXAiChat frontend review and recode UX rules",
                    Scope = "Blazor DevExpress generation",
                    Source = seedSource,
                    Content = "DXAiChat is the required human-facing test path for chat, model selection, council review, polls, and generation requests; backend-only tests are not enough for frontend acceptance. " +
                        "Architecture choices must be generated at runtime from the user's actual request, not forced through preselected LocalGPT defaults. If important choices are missing, the AI or council must stop before generation, present a concise user poll, and wait for the user's option or custom feedback. " +
                        "Common poll choices include target platform/runtime, language/framework, UI stack if any, solution shape, data/persistence model, deployment target, reference-app fidelity, and expected downloadable artifacts. Blazor/DevExpress is a strong LocalGPT specialization, not a universal default for every generated app. " +
                        "Selected provider/model must be visibly verifiable before Send and locked at the composite chat-client boundary during diagnostics so a URL or UI choice cannot silently route to another configured model. " +
                        "Long local inference must show a non-model runtime status heartbeat in the chat transcript, separate from model-thinking blocks, so the user knows whether LocalGPT is waiting on Ollama, first token latency, or streamed model output. " +
                        "Frontend smoke tests against large local models should use slim diagnostic prompts, explicit prompt/output caps, and optional bootstrap suppression; production chats may use the normal knowledge bootstrap after the frontend path is proven. " +
                        "When the user asks to recode a goal application, recreate its recognizable navigation, first screen, model/catalog/settings/API/download/log workflows, and UX structure with Blazor and DevExpress components; do not output a generic dashboard with the same sample pages.",
                    HelpfulSources = "- Local docs: docs/BLAZOR_BOOTSTRAP_DEVEXPRESS_DESIGN.md and docs/GENERATION_ARCHETYPE_CONTRACTS.md.\n- Local frontend: Components/Pages/Chat.razor architecture poll and model/session lock.\n- User review request: DXAiChat must be tested like a human and recode targets must preserve the goal app look/workflows.",
                    Tags = "seed; dxaichat; frontend-review; poll; devexpress; blazor; recode; ux; model-selection",
                    Confidence = 95,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("b6e3d0f7-c770-4eb8-a6f4-785b80222111"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "DXAiChat native attachment and DevExpress feature integrity",
                    Scope = "DXAiChat / DevExpress Blazor",
                    Source = seedSource,
                    Content = "If the user asks for a built-in DevExpress capability, implement the documented DevExpress component API or say that it is blocked/unclear and ask. " +
                        "Do not add a separate custom control and describe it as the requested built-in feature. " +
                        "For DxAIChat file uploads, use the native paperclip attachment surface: FileUploadEnabled, DxAIChatFileUploadSettings, AIChatUploadFileInfo, and the normal chat-client upload content path. " +
                        "Do not add a MessageSent handler unless intentionally replacing automatic AI Chat delivery with a complete manual response path; DevExpress documents that a custom MessageSent handler overrides automatic delivery. " +
                        "LocalGPT should process attached files in backend upload workspaces: decode text and zip contents within budgets, summarize binaries/PDBs with printable strings, never execute uploaded or extracted files, and expose read-only chat.upload_* DXAiFunctions to the council. " +
                        "A custom upload panel may exist only as an explicitly labeled fallback and must not be represented as the embedded DxAIChat upload feature.",
                    HelpfulSources = "- DevExpress DxAIChat docs: https://docs.devexpress.com/Blazor/DevExpress.AIIntegration.Blazor.Chat.DxAIChat.\n" +
                        "- Local page: LocalGPTWebviewWrapper/LocalGPT/Components/Pages/Chat.razor.\n" +
                        "- Local backend: CompositeChatClient and ChatUploadWorkspaceService.",
                    Tags = "seed; dxaichat; devexpress; attachments; upload; integrity; paperclip; source-backed",
                    Confidence = 98,
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
                    Id = Guid.Parse("5b5c25cc-8caa-4d59-81a4-09199d96dd50"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "DocFX and Windows developer docs for LocalGPT software generation",
                    Scope = "DocFX / Windows developer support / .NET generation",
                    Source = seedSource,
                    Content = "The local windows-dev-docs-docs corpus should be treated as source-backed developer and technician knowledge. " +
                        "For documentation generation, use Microsoft Learn/DocFX-style Markdown with normal physical line breaks, front matter, title and description metadata, ms.topic/ms.date fields, includes, images, relative links, and TOC-aware structure. " +
                        "For software support and generation, use it as a source map for Windows App SDK, WinUI, WebView2, MSIX packaging/deployment, Developer Mode, Device Portal/discovery, winget, Terminal, Dev Drive, PowerToys, Arm64 compatibility, diagnostics, certificates, accessibility, and Windows app design. " +
                        "LocalGPT should teach this through compact knowledge entries and DocFX-ready docs rather than stuffing large Markdown files into model context.",
                    HelpfulSources = "- Local learn-base: C:\\tmpselectedcodexlearnbaseforlocalgpt\\windows-dev-docs-docs.\n- Local importer: LearnBaseKnowledgeImporterService Windows docs corpus entries.\n- Microsoft Learn Windows app design guidelines: https://learn.microsoft.com/en-us/windows/apps/design/guidelines-overview.",
                    Tags = "seed; docfx; windows-dev-docs; microsoft-learn; winui; webview2; msix; windowsappsdk; accessibility",
                    Confidence = 90,
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
                    HelpfulSources = "- Forge getting started: https://docs.minecraftforge.net/en/latest/gettingstarted/\n" +
                        "- NeoForge getting started: https://docs.neoforged.net/docs/gettingstarted/\n" +
                        "- Fabric building a mod: https://docs.fabricmc.net/develop/getting-started/building-a-mod\n" +
                        "- Paper getting started: https://docs.papermc.io/paper/dev/getting-started/\n" +
                        "- Gradle JVM toolchains: https://docs.gradle.org/current/userguide/toolchains.html\n" +
                        "- Oracle JDK 25 documentation for current Minecraft Java 26.x: https://docs.oracle.com/en/java/javase/25/\n" +
                        "- Oracle JDK 21 documentation for 1.21.x legacy targets: https://docs.oracle.com/en/java/javase/21/",
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
                    Content = "Use /__diag/minecraft/datapack-benchmark?minecraftVersion=26.1 as the low-context current-Java datapack benchmark; use minecraftVersion=1.21.4 only for legacy comparison. " +
                        "A useful result must generate real .mcfunction files, no .mcfunction.txt placeholders, pack.mcmeta, minecraft load/tick function tags, namespace functions, " +
                        "JSON validation, function-reference validation, and a zip under build/. Compare against the friend's early living_cities.zip for preserved traits: namespace living_cities, " +
                        "core/load and core/tick entry points, scoreboards for year/population/food/security/prestige/birth year, storage areas for city/chronicle/personalities, " +
                        "and a town hall/admin workflow. Do not tell the user it was game-tested until /reload and in-game commands were actually run in Minecraft.",
                    HelpfulSources = "- Local route: GET /__diag/minecraft/datapack-benchmark?minecraftVersion=26.1\n- Legacy comparison route: GET /__diag/minecraft/datapack-benchmark?minecraftVersion=1.21.4\n- User-provided benchmark: local living_cities.zip\n- User-provided design prompt: local message text",
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
                    Id = Guid.Parse("6d3d64fa-0ba4-4d30-9f5a-94df935809b9"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Prompt-driven app generation must not collapse into one default app",
                    Scope = "LocalGPT generation / DXAiChat / Council artifacts",
                    Source = seedSource,
                    Content = "LocalGPT should be able to generate many app ideas, not repeatedly produce LocalGPT, AI-host, or generic dashboard shells. " +
                        "Before creating files, classify the user's actual prompt into the requested app archetype and target platform: console utility, desktop/WebView2 app, Blazor/DevExpress app, backend service, Minecraft datapack/mod/plugin, AI host/control plane, bot, commerce/admin/media/social app, or another explicit domain. " +
                        "Do not create downloadable artifacts for ordinary advice, release-readiness, review, or troubleshooting conversations just because the Council mentions build, solution, LocalGPT, AI host, or artifact words in its answer. " +
                        "Generate .cs/.razor/.dll/solution/datapack zip artifacts only when the user explicitly asks to generate/create/build/continue a program or downloadable artifact, or explicitly asks the AI and user to keep developing files until accepted. " +
                        "If the generated zip is a generic shell, lacks the promised routes/services, or does not match the user's app idea, mark the result as failed, show the artifact and generator code to the Council, inspect knowledge/function gaps, and repair the generator before claiming success.",
                    HelpfulSources = "- Local artifact evidence: LocalGPTApp220013-177648d2.zip was generated from a release-readiness prompt and compiled only as a generic LocalGPT feature shell.\n" +
                        "- Local service: CouncilArtifactService artifact gating and archetype detection.\n" +
                        "- Local service: MultiModelCouncilService GenerateImplementationArtifact gate.\n" +
                        "- Local docs: docs/GENERATION_ARCHETYPE_CONTRACTS.md and docs/FRONTEND_DESIGN_PATTERN_LIBRARY.md.",
                    Tags = "seed; generation; archetype; artifacts; dxaichat; council; user-approved; quality-gate",
                    Confidence = 96,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("3c0f8c1e-7a27-466d-9f8e-11ff4a3a7a22"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "AI host .NET Blazor control-plane feasibility",
                    Scope = "LocalGPT generation",
                    Source = seedSource,
                    Content = "The user-provided ollama-main.zip is mostly Go plus native runtime/build layers: about 1,214 source entries, about 722 .go files, and major areas such as app, docs, template, model, server, cmd, convert, discover, llm, api, llama, runner, native C/C++ headers, and CMake. " +
                        "Treat Ollama as a source/provider example, not as the generated app name. Feasible target: generate a .NET 10 ASP.NET Core control plane and DevExpress Blazor UI that mimics selected provider-compatible REST routes, model catalog/status, runner health, logs, and compatibility notes. " +
                        "The generated target must include direct local model-file resolution, native runner configuration, route compatibility, model catalog/status, runner health, logs, settings, and compatibility notes. " +
                        "Full custom tensor kernels, GPU backends, tokenizer/model conversion, and full manifest/model storage semantics remain deeper work, but an upstream provider proxy is not an acceptable substitute for the local-file runner path.",
                    HelpfulSources = "- User-provided source archive: C:/Users/micha/Downloads/ollama-main.zip\n- Local note: docs/AI_HOST_DOTNET_EXPERIMENT.md\n- Useful generated route: GET /__diag/council/artifact-smoke?target=ai-host",
                    Tags = "seed; ai-host; dotnet; blazor; devexpress; feasibility; whole-solution; artifacts",
                    Confidence = 88,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("77d1c42f-1c72-47bb-a08e-522ce742126d"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "AI host .NET lab generated stack constraint",
                    Scope = "LocalGPT generation",
                    Source = seedSource,
                    Content = "For the local AI host .NET/Blazor/DevExpress lab, the generated downloadable project must stay in .NET, C#, ASP.NET Core, Razor, EF/SQLite, and DevExpress Blazor. " +
                        "Do not propose generated Go or Python projects for this lab. If inference is discussed, implement it as a direct local model-file runner contract, an approved native executable/library boundary, Python.NET bridge, ONNX/ML.NET adapter, or future approved .NET/native integration. " +
                        "The generated solution should include selected provider-compatible routes and UI, but /api/chat and /api/generate must not forward to upstream Ollama/LM Studio/OpenAI-compatible endpoints.",
                    HelpfulSources = "- Local note: docs/AI_HOST_DOTNET_EXPERIMENT.md\n- Local artifact route: GET /__diag/council/artifact-smoke?target=ai-host",
                    Tags = "seed; ai-host; dotnet-only; blazor; devexpress; constraints; artifacts",
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
                        "4K/8K is only a smoke-test budget. Values below 64K are quick-chat or diagnostics only and are not valid acceptance tests for source or solution generation. Use 64K+ context/output as the floor for real code generation, and use 256K when Ollama, the model, and hardware support it. " +
                        "If a council request stalls, stream visible phase/status updates and ask for a user poll instead of silently spinning.",
                    HelpfulSources = "- Local UI: Components/Pages/Chat.razor council token and model controls.\n- Local service: MultiModelCouncilService model ordering, max output/context, timeout, and warnings.\n- Local diagnostics: /__diag/council/artifact-smoke and /__diag/dxaichat-smoke.",
                    Tags = "seed; dxaichat; council; ollama; gpu-safety; gpt-oss; tokens; performance",
                    Confidence = 90,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("0dedfdf7-4ba7-4e80-90d1-c3e8f0a6722c"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Ollama long-context source generation budgets",
                    Scope = "DXAiChat AI Council",
                    Source = seedSource,
                    Content = "LocalGPT must not treat 8K or 32K context as enough for serious Ollama source generation. " +
                        "8K is a small smoke-test budget and 32K can stop mid-generation. Values below 64K are quick-chat or diagnostics only and are not valid acceptance tests for source or solution generation. " +
                        "Use 64K or more as the real coding floor, use 256K for full solution/code-generation tests when the model/runtime supports it, and keep UI/service clamps open up to 256K. " +
                        "If a generation stops mid-output, the next repair prompt should increase output/context budget rather than assuming the model is incapable.",
                    HelpfulSources = "- Local UI: Components/Pages/Chat.razor council token controls.\n- Local service: MultiModelCouncilService MaxContextTokens/MaxOutputTokens.\n- User observation: Ollama supported much larger context windows; 32K generation could still stop mid-output, while 262144 worked for earlier successful council/code-generation tests.",
                    Tags = "seed; ollama; long-context; source-generation; tokens; council; dxaichat; user-approved",
                    Confidence = 96,
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
                        "Prompt Harmony models to keep analysis bounded and emit user-visible final-channel text early, not only at the end of a long reasoning pass. " +
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
                    Id = Guid.Parse("68fd046e-b23b-4d7b-ad6c-627c9c3b5f0f"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "DXAiChat artifact delivery and council confidence contract",
                    Scope = "DXAiChat AI Council",
                    Source = seedSource,
                    Content = "When the user asks DXAiChat or the AI Council to generate Minecraft datapacks/modpacks, .NET/Blazor/DevExpress code, .cs/.razor/.dll files, or whole solution zips, treat the council as capable of producing a safe downloadable milestone. " +
                        "Do not refuse with \"too much\" or \"not capable\" language. If the target is large, reduce it into a buildable sandbox artifact, include file paths/download links through /__artifacts/council/, and list staged follow-up work under Needs verification. " +
                        "If material architecture choices are genuinely missing, create a concise poll and stop for the next user turn. Never claim the user failed to answer a poll in the same response that created it. " +
                        "Use DXAiFunctions such as /__diag/sqlite/tables, /__diag/knowledge, /__diag/logs, /__diag/council/artifact-smoke, /__diag/blazor-devexpress-guidance, and /__diag/dotnet-sample-curriculum before guessing. " +
                        "For direct artifact requests, generate links instead of printing zip/binary payloads as text, and do not self-integrate generated code into LocalGPT without explicit user approval.",
                    HelpfulSources = "- Local service: MultiModelCouncilService poll/artifact gate.\n- Local service: CouncilArtifactService artifact generators and /__artifacts/council route.\n- Local route: /__diag/dxaichat-functions for available function catalog.",
                    Tags = "seed; dxaichat; council; artifacts; generation; confidence; downloads; polls",
                    Confidence = 95,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("e5cf0fe2-09e6-4557-9e5e-4f1f394a8d42"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Provider-neutral local AI host with concurrent model workers",
                    Scope = "AI host / DXAiChat generation",
                    Source = seedSource,
                    Content = "When the user asks the council to generate a local AI host or model-hosting app, do not anchor the generated project on a provider brand. " +
                        "Prompt and design it by capabilities: left navigation, chat, model catalog, model downloads, running models, API console, templates, hardware budget, logs, settings, provider-compatible routes, and LocalGPT compatibility tests. " +
                        "A key improvement over constrained provider hosts is multiple running model sessions when hardware allows it. Generate an IRuntimeSessionService/IModelScheduler design with per-model queues, cancellation, keep-alive/unload policy, GPU/VRAM/CPU budget, MaxParallelModels, fairness, and safe fallback to sequential execution. " +
                        "Use provider-compatible APIs such as /api/chat, /api/generate, /api/tags, /api/ps, /api/show, /api/pull, /api/delete, and optional /v1/chat/completions so LocalGPT can point DXAiChat at the generated host URL. " +
                        "Native inference must start with direct local model-file runner contracts, Python.NET/process/native plugin boundaries, ONNX/ML.NET adapters, or explicit setup gaps. An external-provider proxy is not accepted for the AI-host replacement request. " +
                        "Model downloads from Hugging Face, GitHub, or provider catalogs require user approval, visible target paths, checksums when available, and no autonomous execution.",
                    HelpfulSources = "- Local docs: docs/DOTNET_AI_HOST_ARCHITECTURE_PATTERNS.md and docs/AI_HOST_DOTNET_BLAZOR_REBUILD_GUIDE.md.\n- Local route: GET /__diag/ai-host-rebuild-guidance.\n- Local service: CouncilArtifactService AI-host archetype.\n- User-approved product lesson: the generated host should help LocalGPT/Council run several compatible models at the same time when hardware and policy allow it.",
                    Tags = "seed; ai-host; provider-neutral; multi-model; concurrency; scheduler; hardware-budget; dxaichat; user-approved",
                    Confidence = 96,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("82d00fe4-2a34-4d52-a680-d1e335036f8b"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "AI host generation must wire promised API routes and UX, not placeholders",
                    Scope = "AI host / Council artifact generation",
                    Source = seedSource,
                    Content = "The 2026-06-03 DXAiChat repair round showed a concrete generator failure: a buildable Blazor zip can still be unacceptable if it only contains docs, placeholder services, or a dashboard shell. " +
                        "For AI-host artifacts, classify prompts mentioning AI host, local model host, native runner, model-file runner, provider-compatible routes, or Ollama-compatible APIs as the AI-host archetype even when they also mention LocalGPT. " +
                        "The generated Program.cs or endpoint extension must physically map GET /api/version, GET /api/tags, GET /api/ps, POST /api/generate, and POST /api/chat. " +
                        "Generated services must include an honest native-runner boundary with configured runner path/model paths, clear setup-needed errors when missing, no upstream proxy milestone, and tests or diagnostics that hit each route. " +
                        "Do not accept route text that appears only in README, comments, or UI snippets as implementation. Validate Program.cs/endpoints, appsettings/bootstrap keys, native-runner service code, and navigation pages before zipping. " +
                        "The frontend should resemble a modern AI host: left navigation, chat-first center area, model selector, model catalog, running models, downloads, API console, settings, logs, and clear empty/setup states rather than a raw table prototype. " +
                        "If missing knowledge is detected, ask for or import official sources, but still generate a safe buildable milestone with explicit setup gaps rather than pretending inference works.",
                    HelpfulSources =
                        "- Official Ollama API docs: https://docs.ollama.com/api and https://docs.ollama.com/api/chat.\n" +
                        "- Ollama GitHub API source docs: https://github.com/ollama/ollama/blob/main/docs/api.md.\n" +
                        "- Microsoft Learn Minimal APIs: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis.\n" +
                        "- Microsoft Learn EF Core SQLite provider: https://learn.microsoft.com/ef/core/providers/sqlite/.\n" +
                        "- DevExpress Blazor component docs: https://docs.devexpress.com/Blazor/400725/blazor-components, https://docs.devexpress.com/Blazor/DevExpress.Blazor.DxGrid, https://docs.devexpress.com/Blazor/DevExpress.Blazor.DxButton, https://docs.devexpress.com/Blazor/DevExpress.Blazor.DxTextBox.\n" +
                        "- Local evidence: real WebView2 DXAiChat repair prompt, generated LocalGPTApp194332-883568bf.zip, Program.cs route inspection, and CouncilArtifactService validation repair.",
                    Tags = "seed; ai-host; ollama-api; minimal-api; devexpress; artifact-validation; dxaichat; webview2; source-backed; user-approved",
                    Confidence = 97,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("8d8f0e91-6ee6-48ad-ae17-348a0b57108d"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Source-backed AI host route, runner, persistence, and test contracts",
                    Scope = "AI host / Council artifact generation",
                    Source = seedSource,
                    Content = "For AI-host generation, treat route contracts as implementation requirements, not documentation text. " +
                        "Ollama-compatible minimum routes are GET /api/version, GET /api/tags for installed models, GET /api/ps for loaded/running models, POST /api/generate for prompt completion, and POST /api/chat for message-based chat. " +
                        "Minimal API generation should physically map those exact paths with app.MapGet/app.MapPost or equivalent explicit route attributes; avoid attribute combinations that create /api/chat/chat. " +
                        "The CLI knowledge is useful only as a runner adapter reference: ollama run <model> is an interactive command, ollama pull <model> downloads, and ollama ls lists models. A replacement host must not depend on proxying Ollama; if direct native/model-file inference is not configured, return an honest setup-needed result. " +
                        "Persist runtime settings and chat/session state through EF Core with the Microsoft.EntityFrameworkCore.Sqlite provider, with bootstrap-only values in appsettings and user-editable runtime settings in SQLite. " +
                        "Generated tests should include route/integration tests using Microsoft.AspNetCore.Mvc.Testing/WebApplicationFactory or an equivalent live route test page. " +
                        "The chat UI can use DevExpress Blazor/DxAIChat for user interaction and native file uploads; file upload only transfers files, LocalGPT/backend/model code must explicitly process uploaded content. " +
                        "Python.NET is a valid optional runner boundary when the user approves a Python runtime and package list, but generated code must isolate Python execution behind a safe .NET service contract.",
                    HelpfulSources =
                        "- Ollama API introduction/base URL/generate: https://docs.ollama.com/api.\n" +
                        "- Ollama chat API: https://docs.ollama.com/api/chat.\n" +
                        "- Ollama list models: https://docs.ollama.com/api/tags.\n" +
                        "- Ollama running models: https://docs.ollama.com/api/ps.\n" +
                        "- Ollama CLI reference: https://docs.ollama.com/cli.\n" +
                        "- Microsoft Learn Minimal APIs: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis.\n" +
                        "- Microsoft Learn EF Core SQLite provider: https://learn.microsoft.com/ef/core/providers/sqlite/.\n" +
                        "- Microsoft Learn ASP.NET Core integration tests: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests.\n" +
                        "- DevExpress DxAIChat docs: https://docs.devexpress.com/Blazor/DevExpress.AIIntegration.Blazor.Chat.DxAIChat.\n" +
                        "- Python.NET embedding docs: https://pythonnet.github.io/pythonnet/dotnet.html.",
                    Tags = "seed; ai-host; ollama-api; ollama-cli; minimal-api; sqlite; efcore; integration-tests; dxaichat; pythonnet; source-backed",
                    Confidence = 96,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("0e8fb6af-33d2-4a90-aac3-34f3cf7f66c7"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Eick-at Moodle programming curriculum source map",
                    Scope = "C#/.NET/OOP/business developer thinking",
                    Source = seedSource,
                    Content = "The public Moodle course outlines from eick-at are useful as curriculum-level source maps for programming and business-developer thinking. " +
                        "Relevant concepts include program flowcharts/PAP, OOP vs procedural programming, classes/objects, scope, constructors, UML use-case/class/object/sequence diagrams, aggregation vs composition, navigation direction and role names, inheritance, interfaces, abstract classes, polymorphism, software testing, unit/component testing, TDD, Git, and production-ready workflows. " +
                        "Use these as design and teaching prompts for generated .NET/C# systems: ask for use cases, draw/describe domain relationships, separate responsibilities, write tests, and explain deployment/support expectations. " +
                        "Only the course outlines are source-backed here; exact diagrams or private course pages need a user-provided export before being treated as verified detailed content.",
                    HelpfulSources = "- Public course/section outline: https://moodle.eick-at.de/course/section.php?id=31.\n- Public Java OOP outline with UML/testing/Git parallels: https://moodle.eick-at.de/course/view.php?id=11.\n- Public Python/AD course outline with production and Windows/AD support parallels: https://moodle.eick-at.de/course/view.php?id=14.",
                    Tags = "seed; moodle; oop; csharp; uml; pap; testing; tdd; git; business-developer; needs-page-export-for-details",
                    Confidence = 76,
                    IsUserApproved = true,
                    IsPinned = false
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("0f5b9966-fcc6-4c3f-a86b-e80bfb3af3f0"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Capability gap reports for faster artifact generation",
                    Scope = "DXAiChat / AI Council / LocalGPT improvement loop",
                    Source = seedSource,
                    Content = "If Michi0403 says LocalGPT, DXAiChat, the AI Council, or a model lacks a capability, refuses too quickly, misses source knowledge, or generates the wrong artifact shape, treat that as approved product feedback and investigate. " +
                        "Do not stop at a vague apology or refusal. Produce the safest useful downloadable milestone when the user already gave concrete scope, then add a structured Capability gap report and <localgpt-capability-gap> block. " +
                        "The gap must classify requested languages, frameworks, versions, domain knowledge, local knowledge sources, external official sources, missing LocalGPT functions/routes/pages/services, safe workflow, artifact plan, and next LocalGPT improvement. " +
                        "Local sources should be tried first: DXAiFunctions, SQLite knowledge/logs/memory, local docs, learn-base imports, generated artifacts, build logs, and Test Lab/WebView2 evidence. External sources should be official docs, official GitHub repos, package/version docs, version manifests, or user-approved source imports. " +
                        "For AI-host generation requests, the expected result is a provider-neutral .NET/ASP.NET Core/DevExpress Blazor control-plane solution with recognisable navigation, model catalog, chat/API console, settings, logs, downloads, provider-compatible routes, SQLite/appsettings state, and honest native-inference boundaries.",
                    HelpfulSources = "- Local doc: docs/CAPABILITY_GAP_CONTRACT.md.\n- Local route: GET /__diag/capability-gap-contract.\n- Local routes: /__diag/dxaichat-functions, /__diag/knowledge, /__diag/logs, /__diag/learn-base/import, /__diag/ai-host-rebuild-guidance, /__diag/council/artifact-smoke?target=ai-host.\n- User-tested expectations from prior DXAiChat prompts: faster downloadable .cs/.razor/.dll/solution/datapack artifacts, non-generic AI-host control-plane shape, and no refusal when a buildable milestone is possible.",
                    Tags = "seed; capability-gap; source-request; ai-host; artifacts; dxaichat; council; user-approved",
                    Confidence = 96,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("44fd504b-c6de-4a8b-a4e0-aea5a53200d9"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "LocalGPT static web assets direct-debug and MSIX release repair",
                    Scope = "ASP.NET Core host / WinUI package",
                    Source = seedSource,
                    Content = "Direct backend debugging can fail before WebApplication.CreateBuilder when LocalGPT.staticwebassets.runtime.json references generated obj static asset roots that no longer exist, especially obj/.../compressed. " +
                        "This surfaces as DirectoryNotFoundException from PhysicalFileProvider/StaticWebAssetsLoader and can leave WebView2 showing unstyled fallback HTML if assets are not reachable. " +
                        "Before CreateBuilder, LocalGPT should inspect its static web asset runtime manifest and recreate only missing generated obj roots such as compressed and scopedcss/bundle. " +
                        "Do not create missing NuGet/DevExpress package roots, because that would hide real package restore problems. " +
                        "For MSIX/WebView2 releases, source wwwroot is not enough: the package must include the published webroot with wwwroot/_framework, wwwroot/_content, and LocalGPT.styles.css. " +
                        "Keep IncludeLocalGptPublishedPayload default false for Visual Studio Debug/F5, but have build/release scripts pass IncludeLocalGptPublishedPayload=true after publishing LocalGPT. " +
                        "A release package is unacceptable if the actual MSIX archive lacks LocalGPTWebviewWrapper/wwwroot/_framework/blazor.web.js, LocalGPTWebviewWrapper/wwwroot/_content/DevExpress.Blazor/dx-blazor.svg, LocalGPTWebviewWrapper/wwwroot/_content/DevExpress.Blazor.Themes/office-white.bs5.min.css, or LocalGPTWebviewWrapper/wwwroot/LocalGPT.styles.css. " +
                        "Generated DevExpress/Blazor apps should copy or publish static web assets through their real publish output and smoke-test the visual frontend before claiming success.",
                    HelpfulSources = "- Local code: Program.EnsureGeneratedStaticWebAssetContentRoots.\n- Local script: build/Build-LocalGptPackage.ps1 Assert-MsixStaticWebAssets.\n- Local package project: LocalGPTWebviewWrapper (Package).wapproj IncludeLocalGptPublishedPayload.\n- Verified probes: /, /Chat, /_framework/blazor.web.js, /_content/DevExpress.Blazor/dx-blazor.svg returned HTTP 200 after the repair.",
                    Tags = "seed; static-web-assets; aspnetcore; devexpress; direct-debug; webview2; msix; release-guard",
                    Confidence = 96,
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
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("bf53476f-8f3c-48db-9519-2b353811f74c"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "LocalGPT Test Lab and WebView2 automation",
                    Scope = "Frontend diagnostics / browser automation",
                    Source = seedSource,
                    Content = "Use the LocalGPT Test Lab page for fast frontend-facing HTTP checks before loading heavy local models. " +
                        "It can call /health, /__diag, /__diag/dxaichat-functions, Minecraft 26.x datapack version checks, deterministic council artifact smoke routes, and learn-base imports, then show JSON and extracted /__artifacts download links. " +
                        "For the actual WinUI WebView2 wrapper, Microsoft documents two Selenium/Microsoft Edge WebDriver approaches: launch the WebView2 app with EdgeOptions.UseWebView and BinaryLocation, or attach to a running WebView2 instance with a remote debugging port and EdgeOptions.DebuggerAddress. " +
                        "Browser automation source such as AutomatedDiscordLogin should be imported as compact architecture fingerprints, not pasted wholesale into prompts. Optional Python.NET/Python browser automation can be added as a workbench only behind explicit user permission gates, safe working directories, typed options, logging, and visible run controls.",
                    HelpfulSources = "- Microsoft Learn: Automate and test WebView2 apps with Microsoft Edge WebDriver, https://learn.microsoft.com/microsoft-edge/webview2/how-to/webdriver\n- Local page: Components/Pages/TestLab.razor.\n- Local route: /__diag/frontend-test-guidance.\n- Local learn-base request: C:\\tmpselectedcodexlearnbaseforlocalgpt\\AutomatedDiscordLogin-master.",
                    Tags = "seed; frontend; test-lab; webview2; selenium; webdriver; pythonnet; browser-automation; diagnostics",
                    Confidence = 90,
                    IsUserApproved = true,
                    IsPinned = true
                },
                new CouncilKnowledgeEntry
                {
                    Id = Guid.Parse("9fe4ce65-16c7-474d-a9b0-a6bb79af6ff0"),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Topic = "Legacy Jezzifa architecture lessons for generation",
                    Scope = "Enterprise .NET / DevExpress Web API / integrations",
                    Source = seedSource,
                    Content = "The user-supplied legacy Jezzifa archive is useful as sanitized architecture evidence, not as a modern code template to copy verbatim. " +
                        "It shows a larger .NET solution style with separate business-object/core/service/web projects, DevExpress Web API/XAF-style object-space setup, EF contexts, security/JWT/certificate services, custom controllers, database update helpers, Telegram bot service integration, Python configuration hooks, speech-to-text/Whisper-oriented data, and a separate web target. " +
                        "When generating similar modern systems, ask whether the user wants a monolith, modular monolith, or multi-project solution; whether DevExpress Web API/XAF/OData business objects are required; whether Telegram/Python/Whisper integrations are enabled; and whether optional external code execution is explicitly user-approved. " +
                        "Sanitize legacy names and do not reproduce obscene folder/class names in generated guidance. Prefer .NET 8-10, explicit DI, typed options, EF migrations/schema update plans, isolated integration services, safe secrets/config handling, and backend-owned native/Python execution behind user permission gates.",
                    HelpfulSources = "- Local user archive listing: Jezzifa.zip showed Api.WebApi.sln, BusinessObjects/Core projects, DevExpress Web API service setup, TelegramBotService metadata, PythonOptions/find_libpython.py, SpeechToTextValue, security/certificate services, and a separate web target.\n- Local guide: docs/EF_DEVEXPRESS_BUSINESS_OBJECTS.md.",
                    Tags = "seed; jezzifa; sanitized; devexpress-web-api; xaf; odata; telegram; python; whisper; modular-monolith",
                    Confidence = 86,
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
                SET "VerificationStatus" = 'UserVerified',
                    "ReviewStatus" = 'Current',
                    "LastVerifiedAtUtc" = COALESCE("LastVerifiedAtUtc", strftime('%Y-%m-%dT%H:%M:%fZ','now'))
                WHERE "Source" = {0}
                  AND "IsUserApproved" = 1
                  AND (
                      "VerificationStatus" IS NULL
                      OR "VerificationStatus" = ''
                      OR "VerificationStatus" = 'NeedsVerification'
                      OR "ReviewStatus" IS NULL
                      OR "ReviewStatus" = ''
                      OR "ReviewStatus" IN ('NeedsVerification', 'NeedsUserReview')
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
                        END,
                    "ReviewStatus" = 'Current',
                    "LastVerifiedAtUtc" = COALESCE("LastVerifiedAtUtc", strftime('%Y-%m-%dT%H:%M:%fZ','now'))
                WHERE "Source" IN (
                    'LocalGPT SQL seed',
                    'Microsoft Learn source-backed seed',
                    'User-approved generation advice'
                )
                  AND ("VerificationStatus" IS NULL OR trim("VerificationStatus") = '' OR "VerificationStatus" = 'NeedsVerification');
                """,
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """
                UPDATE "CouncilKnowledgeEntries"
                SET "ReviewStatus" = 'Current',
                    "LastVerifiedAtUtc" = COALESCE("LastVerifiedAtUtc", strftime('%Y-%m-%dT%H:%M:%fZ','now'))
                WHERE "Source" IN (
                    'LocalGPT SQL seed',
                    'Microsoft Learn source-backed seed',
                    'User-approved generation advice'
                )
                  AND ("ReviewStatus" IS NULL OR trim("ReviewStatus") = '' OR "ReviewStatus" IN ('NeedsVerification', 'NeedsUserReview'));
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
