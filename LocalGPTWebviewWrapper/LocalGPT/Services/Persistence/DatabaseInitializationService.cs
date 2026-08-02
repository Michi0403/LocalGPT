using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates database health, compatibility reconciliation, EF migration, and deterministic seeding.
/// Low-level migration-history inspection belongs to <see cref="IDatabaseMigrationCompatibilityService"/>.
/// </summary>
public sealed class DatabaseInitializationService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseFileHealthService databaseFileHealth,
    IDatabaseMigrationCompatibilityService migrationCompatibility,
    IInitialDataCatalog catalog,
    ILocalGptRuntimePolicySeedDataService runtimePolicySeed,
    IServiceActivityService serviceActivity,
    IDatabaseLoggerReadiness databaseLoggerReadiness,
    IHostEnvironment hostEnvironment,
    ILogger<DatabaseInitializationService> logger) : IDatabaseInitializationService
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private volatile bool initialized;

    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        serviceActivity.RunAsync(
            nameof(DatabaseInitializationService),
            nameof(InitializeAsync),
            InitializeCoreAsync,
            cancellationToken,
            "Database migration and deterministic initial data feed completed.");

    private async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        if (IsInitializedStorePresent())
            return;

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsInitializedStorePresent())
                return;

            await databaseFileHealth.EnsureHealthyOrRecoverAsync(cancellationToken).ConfigureAwait(false);
            await migrationCompatibility.PrepareAsync(cancellationToken).ConfigureAwait(false);
            await RunMigrationAsync(cancellationToken).ConfigureAwait(false);

            // Each seed stage receives a fresh DbContext. A failed SaveChanges therefore cannot leave
            // stale tracked entities behind and trigger unrelated concurrency failures in the next stage.
            await RunSeedStageAsync("regex definitions", SeedRegexAsync, cancellationToken).ConfigureAwait(false);
            await RunSeedStageAsync("prompt definitions", SeedPromptsAsync, cancellationToken).ConfigureAwait(false);
            await RunSeedStageAsync("system variables", SeedVariablesAsync, cancellationToken).ConfigureAwait(false);
            await RunSeedStageAsync("knowledge entries", SeedKnowledgeAsync, cancellationToken).ConfigureAwait(false);
            await RunSeedStageAsync("core projects", SeedCoreProjectsAsync, cancellationToken).ConfigureAwait(false);
            await RunSeedStageAsync("Council model presets", SeedCouncilModelPresetsAsync, cancellationToken).ConfigureAwait(false);

            initialized = true;
            databaseLoggerReadiness.MarkReady();
            logger.LogInformation("LocalGPT database migration and initial data feed completed.");
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task RunMigrationAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(exception, "LocalGPT database migration was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "LocalGPT database migration failed. Deterministic seed stages were not started.");
            throw;
        }
    }

    private async Task RunSeedStageAsync(
        string stageName,
        Func<LocalGptMemoryDbContext, CancellationToken, Task> seed,
        CancellationToken cancellationToken)
    {
        const int maximumAttempts = 2;
        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await seed(db, cancellationToken).ConfigureAwait(false);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (DbUpdateConcurrencyException exception)
            {
                if (await TryReconcileSeedConcurrencyAsync(db, stageName, attempt, exception, cancellationToken).ConfigureAwait(false))
                    return;

                if (attempt < maximumAttempts)
                {
                    logger.LogWarning(
                        exception,
                        "Database seed stage {SeedStage} encountered a concurrency conflict on attempt {Attempt}; retrying once with a fresh DbContext.",
                        stageName,
                        attempt);
                    continue;
                }

                logger.LogError(
                    exception,
                    "Database seed stage {SeedStage} retained an unresolved concurrency conflict after {AttemptCount} attempt(s); later independent stages will continue.",
                    stageName,
                    maximumAttempts);
                return;
            }
            catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation(exception, "Database seed stage {SeedStage} was cancelled.", stageName);
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Database seed stage {SeedStage} failed on attempt {Attempt}; later independent stages will continue.",
                    stageName,
                    attempt);
                return;
            }
        }

    }

    private async Task<bool> TryReconcileSeedConcurrencyAsync(
        LocalGptMemoryDbContext db,
        string stageName,
        int attempt,
        DbUpdateConcurrencyException exception,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var conflictingEntryCount = exception.Entries.Count;

            // Never issue a second SaveChanges from the failed DbContext. EF still owns the original
            // modification batch and another save can repeat the same stale concurrency predicate.
            // A fresh context on the next bounded attempt reloads durable rows and applies only missing,
            // additive seed records. Existing user rows remain authoritative.
            foreach (var entry in exception.Entries)
                entry.State = EntityState.Detached;
            db.ChangeTracker.Clear();

            logger.LogWarning(
                exception,
                "Database seed stage {SeedStage} discarded {ConflictingEntryCount} stale tracked seed row(s) on attempt {Attempt}; a fresh DbContext will retry additive seed work without overwriting durable user values.",
                stageName,
                conflictingEntryCount,
                attempt);
            await Task.CompletedTask.ConfigureAwait(false);
            return false;
        }
        catch (OperationCanceledException cancellationException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(cancellationException, "Concurrency reconciliation for database seed stage {SeedStage} was cancelled.", stageName);
            throw;
        }
        catch (Exception reconciliationException)
        {
            logger.LogWarning(
                reconciliationException,
                "Database seed stage {SeedStage} could not clear stale tracked seed rows on attempt {Attempt}.",
                stageName,
                attempt);
            return false;
        }
    }

    private bool IsInitializedStorePresent() =>
        initialized && File.Exists(databaseFileHealth.DatabasePath);

    private async Task SeedRegexAsync(LocalGptMemoryDbContext db, CancellationToken token)
    {
        var existingNames = await db.RegexPatterns
            .Select(item => item.Name)
            .ToListAsync(token)
            .ConfigureAwait(false);

        var existing = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in catalog.RegexPatterns)
        {
            // Add returns false for both database-existing names and duplicate
            // names encountered earlier in this same in-memory seed run.
            if (!existing.Add(item.Name))
                continue;

            db.RegexPatterns.Add(new RegexPattern
            {
                Name = item.Name,
                Pattern = item.Pattern,
                Flags = item.Flags,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            });
        }
    }
    private async Task SeedPromptsAsync(LocalGptMemoryDbContext db, CancellationToken token)
    {
        var existing = await db.Prompts.Select(x => new { x.Key, x.Language }).ToListAsync(token).ConfigureAwait(false);
        foreach (var item in catalog.Prompts)
        {
            if (existing.Any(x =>
                    string.Equals(x.Key, item.Key, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Language, item.Language, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            db.Prompts.Add(new PromptConfig
            {
                Key = item.Key,
                Language = item.Language,
                Text = item.Text,
                LastUpdated = DateTime.UtcNow
            });
        }
    }

    private async Task SeedVariablesAsync(LocalGptMemoryDbContext db, CancellationToken token)
    {
        var existingRows = await db.SystemVariables.ToListAsync(token).ConfigureAwait(false);
        var existing = existingRows.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var item in catalog.Variables)
        {
            if (!existing.TryGetValue(item.Name, out var row))
            {
                row = new SystemVariable
                {
                    Name = item.Name,
                    ValueString = item.Value,
                    DataType = item.DataType,
                    LastUpdated = DateTime.UtcNow
                };

                // Make the newly queued row visible to later entries in this same seed run.
                existing.Add(item.Name, row);
                db.SystemVariables.Add(row);
                continue;
            }

            if (item.DataType.Equals(typeof(string[]).FullName, StringComparison.Ordinal)
                && !IsJsonStringArray(row.ValueString))
            {
                row.ValueString = item.Value;
                row.DataType = item.DataType;
                row.LastUpdated = DateTime.UtcNow;
                logger.LogWarning(
                    "Repaired legacy runtime-policy collection {RuntimePolicyCollectionName} with its canonical JSON seed value.",
                    item.Name);
                continue;
            }

            // Lossless default evolution: only replace values that exactly match a previous built-in default.
            // Any user-edited value remains authoritative.
            if (item.Name.Equals("DefaultContextTokens", StringComparison.OrdinalIgnoreCase)
                && row.ValueString == "65536"
                && item.Value == "262144")
            {
                row.ValueString = item.Value;
                row.DataType = item.DataType;
                row.LastUpdated = DateTime.UtcNow;
            }
        }
    }

    private bool IsJsonStringArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind == JsonValueKind.Array
                && document.RootElement.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String);
        }
        catch (JsonException exception)
        {
            logger.LogDebug(
                exception,
                "Runtime-policy collection candidate is not a JSON string array and requires compatibility repair.");
            return false;
        }
    }

    private async Task SeedKnowledgeAsync(LocalGptMemoryDbContext db, CancellationToken token)
    {
        var catalogEntries = await catalog.LoadKnowledgeAsync(token).ConfigureAwait(false);
        if (catalogEntries.Count == 0)
            return;

        var ids = catalogEntries.Select(x => x.Id).ToArray();
        var existingById = await db.CouncilKnowledgeEntries
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, token)
            .ConfigureAwait(false);

        foreach (var item in catalogEntries)
        {
            if (!existingById.TryGetValue(item.Id, out var existing))
            {
                db.CouncilKnowledgeEntries.Add(item);
                continue;
            }

            var contentChanged = !string.Equals(existing.SourceHash, item.SourceHash, StringComparison.OrdinalIgnoreCase);
            if (contentChanged)
            {
                existing.Topic = item.Topic;
                existing.Content = item.Content;
                existing.Source = item.Source;
                existing.HelpfulSources = item.HelpfulSources;
                existing.Tags = item.Tags;
                existing.SourceHash = item.SourceHash;
                existing.SourceDateUtc = item.SourceDateUtc;
                existing.LastVerifiedAtUtc = DateTime.UtcNow;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }

            // Trust metadata is policy-owned and must be refreshed even when document text is unchanged.
            existing.VerificationStatus = item.VerificationStatus;
            existing.ReviewStatus = item.ReviewStatus;
            existing.IsUserApproved = item.IsUserApproved;
            existing.IsPinned = item.IsPinned;
        }
    }


    private async Task SeedCoreProjectsAsync(LocalGptMemoryDbContext db, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var coreProjectId = runtimePolicySeed.GetSeed().LocalGptCoreProjectId;
        var humanitarianProjectId = Guid.Parse("7f4d7b4a-b622-4d15-8e44-9dfae2aa6102");
        var repositoryRoot = ResolveRepositoryRoot(hostEnvironment.ContentRootPath);

        var core = await db.LocalGptProjects
            .Include(project => project.Topics)
            .Include(project => project.Versions)
            .Include(project => project.Revisions)
            .Include(project => project.Requirements)
            .Include(project => project.Artifacts)
            .AsSplitQuery()
            .SingleOrDefaultAsync(project => project.Id == coreProjectId, token)
            .ConfigureAwait(false);
        if (core is null)
        {
            core = new LocalGptProject
            {
                Id = coreProjectId,
                Name = "LocalGPT Core",
                Purpose = "Human-guided, humanitarian self-development of LocalGPT, its AI Council, project architecture, database knowledge, regex links, diagnostics and organic 1-Wire organs.",
                RootPath = repositoryRoot,
                CurrentVersion = "2.1.15",
                Status = "Active",
                RecommendGit = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            db.LocalGptProjects.Add(core);
        }
        else
        {
            // Existing project rows are user/database authoritative. Version history, requirements and
            // artifacts below are additive; startup seeding does not update the parent row or its
            // concurrency token.
            db.Entry(core).State = EntityState.Unchanged;
        }

        EnsureTopic(core, "Repository architecture and self-development",
            "Maintains the LocalGPT repository structure, source/document/debug inventories, changelogs, architecture decisions, DXFunctions, controller/service directories and user-reviewed evolution.");
        EnsureTopic(core, "Council knowledge, regex and diagnostics",
            "Maintains reusable regex definitions and smart links between projects, conversations, compiler/scientific knowledge, repository files, Markdown, logs, debug artifacts and Council evidence.");
        EnsureVersion(core, "0.1.7", repositoryRoot, "Deterministic baseline project feed for the organic Council spooler and preflight architecture.");
        EnsureRevision(core, "main", "seed-v0.1.7", repositoryRoot,
            "Initial database-backed LocalGPT repository and self-awareness revision. User changes are never overwritten by later seeds.");
        EnsureVersion(core, "0.1.8", repositoryRoot, "Frontend-authoritative 1-Wire linking, database-backed public-service catalog and workspace-preservation proof.");
        EnsureRevision(core, "main", "seed-v0.1.8", repositoryRoot,
            "Adds the user-controlled service/DX catalog, two-frontend organic link approval, interaction editors and reproducible workspace preservation evidence without replacing user-maintained records.");
        EnsureVersion(core, "2.0.1", repositoryRoot, "Build-fix release for the unified LocalGPT and PublisherStudio organic suite; retains packaged 1-Wire contracts, configurable introductions, time/state diagnostics and frontend-authoritative paired workflows.");
        EnsureRevision(core, "main", "seed-v2.0.1", repositoryRoot,
            "Version 2 organic-suite seed. Retains prior project history while adding protocol-package, pairing, introduction and state-awareness requirements.");
        EnsureVersion(core, "2.0.2", repositoryRoot, "Runtime-class and configuration release with AI-owned ASCII frames, source-informed game presets, grouped DXFunction editing and responsive structured-text streaming.");
        EnsureRevision(core, "main", "seed-v2.0.2", repositoryRoot,
            "Adds database-backed Council runtime-class definitions, ASCII DOOM and Green Dragon configuration examples, runtime input metadata, and bounded renderer work during live Council streams.");
        EnsureVersion(core, "2.0.3", repositoryRoot, "In-chat ASCII game console, deterministic low-latency game bootstrap, shared human/AI controls, remote knowledge imports, tolerant runtime-class resolution and batched diagnostics.");
        EnsureRevision(core, "main", "seed-v2.0.3", repositoryRoot,
            "Integrates playable ASCII game sessions into Chat, preserves one authoritative frame owner, adds confirmed GitHub/web knowledge ingestion, improves handheld controls and reduces hot-path log and stream update pressure.");
        EnsureVersion(core, "2.0.4", repositoryRoot, "Build-policy and diagnostics correction release with renderer-safe awaits, service-owned text operations, non-disposing diagnostics proxies, business-object boundaries and concurrency-safe seed reconciliation.");
        EnsureRevision(core, "main", "seed-v2.0.4", repositoryRoot,
            "Addresses LocalGPT build-policy findings, preserves DI disposal ownership, moves newly introduced data models into BusinessObjects, and reconciles database seed conflicts without overwriting concurrent user edits.");
        EnsureVersion(core, "2.1.8", repositoryRoot, "Version-alignment release that advertises LocalGPT 2.1.8 consistently through the application package, runtime context, organic 1-Wire descriptor and seeded core-project metadata.");
        EnsureRevision(core, "main", "seed-v2.1.8", repositoryRoot,
            "Raises the LocalGPT application version to 2.1.8 without changing the separately versioned 1-Wire protocol package or removing prior release history.");
        EnsureVersion(core, "2.1.9", repositoryRoot, "Compile-fix release that restores the AdaptiveOllamaBenchmarkWiring interface import while retaining the explicit unimplemented benchmark boundary.");
        EnsureRevision(core, "main", "seed-v2.1.9", repositoryRoot,
            "Adds the missing LocalGPT.Interfaces namespace import required by AdaptiveOllamaBenchmarkWiring and advances the application patch version without changing the separately versioned 1-Wire protocol package.");
        EnsureVersion(core, "2.1.10", repositoryRoot, "Scoped-lifetime diagnostics and adaptive Ollama benchmark release with zero explicit true-context continuation captures.");
        EnsureRevision(core, "main", "seed-v2.1.10", repositoryRoot,
            "Prevents diagnostics decorators from resolving scoped services through the root provider, implements the bounded local Ollama autotuner, and restores the asynchronous policy: renderer-context continuations remain implicit while context-free service continuations use ConfigureAwait(false).");
        EnsureVersion(core, "2.1.11", repositoryRoot, "Fine-grained asynchronous continuation release with explicit ConfigureAwait(false) on context-free awaits and narrowly scoped ConfigureAwait(true) inside OnAfterRenderAsync lifecycle continuations.");
        EnsureRevision(core, "main", "seed-v2.1.11", repositoryRoot,
            "Corrects the 2.1.10 continuation policy by making every await expression explicit, retaining renderer affinity only in OnAfterRenderAsync, and keeping service, controller, persistence, diagnostics, background and non-lifecycle component continuations context-free.");
        EnsureVersion(core, "2.1.12", repositoryRoot, "Compiler and continuation-policy correction release with fully qualified configuration types, restored DXFunction parameter binding, exact Razor await auditing, and reviewed renderer-affine loading continuations.");
        EnsureRevision(core, "main", "seed-v2.1.12", repositoryRoot,
            "Fixes the Adaptive Ollama configuration type ambiguity, restores request.Parameters binding for project architecture and maintenance DXFunctions, repairs the PowerShell async audit, and preserves renderer context only for lifecycle or explicitly reviewed UI-loading continuations while services and controllers remain context-free.");
        EnsureVersion(core, "2.1.13", repositoryRoot, "Compile recovery and database-logger startup isolation release that awaits configured UI actions and defers ApplicationLogs persistence until migration and deterministic seeding complete.");
        EnsureRevision(core, "main", "seed-v2.1.13", repositoryRoot,
            "Repairs RunRemoteKnowledgeAsync so LocalGPT compiles and adds a one-way database-logger readiness gate so startup diagnostics cannot race migration or seed SaveChangesAsync operations. The adaptive Ollama benchmark implementation and independently versioned wire protocol remain unchanged.");
        EnsureVersion(core, "2.1.14", repositoryRoot, "Embedded firmware and workspace-environment preparation release with transport-neutral ESP32/Arduino planning, board/pin catalogs, PublisherStudio wiring contracts, official learning-source manifests and permission-aware compiler workspaces.");
        EnsureRevision(core, "main", "seed-v2.1.14", repositoryRoot,
            "Adds Chat-level embedded catalog, wiring, firmware and telemetry DXFunctions; protected logical 1-Wire telemetry ingress; PublisherStudio organic wiring-editor contracts; Arduino/Espressif/PlatformIO installer learning sources; and workspace environment, compiler, structure-regex and access-policy assessment wiring. Legacy embedded sources remain architectural evidence and are not copied into the product.");
        EnsureVersion(core, "2.1.15", repositoryRoot, "Build-correction release for the embedded workbench that removes two unapproved iterator helpers and replaces malformed interpolated raw-string artifact generators with explicit StringBuilder output.");
        EnsureRevision(core, "main", "seed-v2.1.15", repositoryRoot,
            "Corrects the EmbeddedHardwareCatalogService iterator-policy failures and the EmbeddedFirmwarePlanningService C# syntax failures while preserving the transport-neutral ESP32/Arduino, workspace-environment, DXFunction and organic capability contracts introduced in 2.1.14.");

        EnsureRequirement(core, "Preflight database and capability audit",
            "Before every Council run, fill deterministic database gaps, inspect the current project/topic context, publish the DXFunction and organic-skill directories, then ask exact user questions for missing current facts instead of guessing.",
            "CouncilPreflight", "Critical");
        EnsureRequirement(core, "Rejoinable hardware-road Council spooler",
            "Persist Council runs and expose live refresh/join controls. Schedule each model on its configured CPU/GPU/accelerator road with its own token ranges and 0-100% session interpolation.",
            "CouncilSpooler", "Critical");
        EnsureRequirement(core, "Authoritative packaged 1-Wire protocol",
            "Maintain the LocalGPT.WireProtocolVersion source and NuGet package only in LocalGPT. PublisherStudio and future organs consume the versioned package from release assets and negotiate protocol compatibility before linking.",
            "OneWirePackage", "Critical");
        EnsureRequirement(core, "Frontend-authoritative organic transactions",
            "A connected organ is usable only after the user links it in both frontends. Consequential eye, hand, filesystem, command and publication actions remain queued until the receiving frontend user confirms or supplies the requested rich-text/JSON input.",
            "HumanCollaboration", "Critical");
        EnsureRequirement(core, "PublisherStudio Story Editor Council workflow",
            "When paired, PublisherStudio may submit a story topic to a selected Council team. The Council returns publisher.text.insert.propose and the user alone inserts or dismisses the proposal at the current RichEdit caret.",
            "publisher.text.insert.propose", "High");
        EnsureRequirement(core, "Time and state awareness",
            "Expose a bounded read-only DXFunction that reports current UTC/local time, process state, the newest three logs, Council spool entries, hardware and linked 1-Wire peers before planning time-sensitive work.",
            "localgpt.time_state.now", "High");
        EnsureRequirement(core, "Debug symbol and build evidence awareness",
            "Index paths and bounded metadata for LocalGPT and PublisherStudio assemblies, portable PDB symbols, logs, binlogs and test reports. Never execute a debug artifact merely to inspect it; ask the user for missing symbols or current build evidence.",
            "localgpt.debug.inspect", "High");
        EnsureArtifact(core, "Repository source-file pattern", "RegexDefinition",
            @"(?i)\.(?:cs|razor|cshtml|json|xml|yml|yaml|props|targets|sln|csproj|md|txt|ps1|cmd|bat|js|mjs|css|html)$",
            "regex", "Generic text/source files that may be indexed into bounded project context.");
        EnsureArtifact(core, "Repository Markdown structure pattern", "RegexDefinition",
            @"(?m)^(?<level>#{1,6})\s+(?<heading>.+?)\s*$|^(?<bullet>\s*[-*+]\s+.+)$|^```(?<language>[A-Za-z0-9_+.#-]*)\s*$",
            "regex", "Headings, lists and fenced languages used to connect repository documentation and changelog knowledge.");
        EnsureArtifact(core, "Build and debug artifact pattern", "RegexDefinition",
            @"(?i)(?<name>[^\\/]+?)\.(?<kind>pdb|dll|exe|deps\.json|runtimeconfig\.json|binlog|trx|log)$",
            "regex", "Build/debug file classification. Reading a local path still follows the function's declared human-interaction policy.");
        EnsureArtifact(core, "Portable PDB document and checksum pattern", "RegexDefinition",
            @"(?i)(?<document>[^|\r\n]+\.(?:cs|razor|cshtml|fs|vb))\|(?<algorithm>sha1|sha256)\|(?<checksum>[a-f0-9]{40,64})",
            "regex", "Connects bounded portable-PDB document/checksum metadata to project revisions without storing executable debug payloads in prompts.");
        EnsureArtifact(core, "Organic transaction correlation pattern", "RegexDefinition",
            @"(?i)^(?<source>[a-z0-9_.:-]+)\s*->\s*(?<target>[a-z0-9_.:-]+)\s*#(?<correlation>[0-9a-f-]{36})\s*:\s*(?<capability>[a-z0-9_.:-]+)$",
            "regex", "Links frontend approvals, Council spool work and 1-Wire results by peer, correlation id and capability.");
        EnsureArtifact(core, "Authoritative WireProtocol package", "NuGetPackage",
            "LocalGPT.WireProtocolVersion.2.0.0.nupkg",
            "application/zip", "Built from the LocalGPT protocol project and copied beside release/install artifacts for PublisherStudio and future organ plugins.");

        var humanitarian = await db.LocalGptProjects
            .Include(project => project.Topics)
            .Include(project => project.Versions)
            .Include(project => project.Revisions)
            .Include(project => project.Requirements)
            .Include(project => project.Artifacts)
            .AsSplitQuery()
            .SingleOrDefaultAsync(project => project.Id == humanitarianProjectId, token)
            .ConfigureAwait(false);
        if (humanitarian is null)
        {
            humanitarian = new LocalGptProject
            {
                Id = humanitarianProjectId,
                Name = "Humanitarian Collaboration Workspace",
                Purpose = "A permanent user-maintained workspace for scientific, educational, accessibility, creative and other peaceful humanitarian projects supported by LocalGPT and connected organic systems.",
                RootPath = string.Empty,
                CurrentVersion = "1.0",
                Status = "Active",
                RecommendGit = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            db.LocalGptProjects.Add(humanitarian);
        }
        else
        {
            // Preserve the complete user-maintained parent row and seed only missing child records.
            db.Entry(humanitarian).State = EntityState.Unchanged;
        }
        EnsureTopic(humanitarian, "Humanitarian use cases",
            "User and Council co-maintain peaceful project goals, scientific topics, constraints, evidence and external-organ capabilities without assuming one language or technology.");
        EnsureTopic(humanitarian, "User requirements, questions and reviews",
            "Stores explicit requirements, unresolved questions, human votes, approvals, corrections and review outcomes shared across sessions.");
        EnsureVersion(humanitarian, "1.0", string.Empty, "Permanent user/Council collaboration baseline.");
        EnsureRevision(humanitarian, "main", "seed-v1", string.Empty,
            "Initial humanitarian collaboration structure. It is intentionally broad and remains user-editable.");
        EnsureRequirement(humanitarian, "Ask for missing current facts",
            "When a compiler version, scientific constant, source revision, device capability or current fact is missing, ask the user or request a source-backed feed and preserve the question as project knowledge.",
            "HumanQuestion", "High");
    }

    private async Task SeedCouncilModelPresetsAsync(LocalGptMemoryDbContext db, CancellationToken token)
    {
        var existingNames = await db.CouncilModelPresets
            .Select(item => item.Name)
            .ToListAsync(token)
            .ConfigureAwait(false);
        var existing = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var presets = new[]
        {
            BuildPreset(
                "Reactive ASCII Gameplay",
                "Low-latency one-model-at-a-time defaults for in-chat runtime games. Auto GPU offload supports NVIDIA and AMD through Ollama without hard-coded layer guesses; a bounded benchmark team may refine a copied preset later.",
                ["qwen3.5:0.8b", "qwen3.5:2b", "qwen3.5:4b", "llama3.2:1b", "phi3:3.8b"],
                [
                    Route("qwen3.5:0.8b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 256, 1536, 2048, 8192, null),
                    Route("qwen3.5:2b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 256, 2048, 4096, 12288, null),
                    Route("qwen3.5:4b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 384, 3072, 4096, 16384, null),
                    Route("llama3.2:1b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 256, 1536, 2048, 8192, null),
                    Route("phi3:3.8b", OneWireHardwareKind.Gpu, 0, "Auto GPU", 384, 3072, 4096, 16384, null)
                ],
                isDefault: false,
                maxParallel: 1,
                includeMemory: false,
                createProjectPerRun: false),
            BuildPreset(
                "Adaptive Mixed Hardware Council",
                "Four-member Council with independent per-model CPU/GPU token roads. The session slider interpolates each model from its own minimum to maximum.",
                ["gpt-oss:20b", "deepseek-r1:8b", "qwen3:8b", "gemma3:12b"],
                [
                    Route("gpt-oss:20b", OneWireHardwareKind.Gpu, 0, "GPU 1", 1024, 32768, 8192, 262144, 32),
                    Route("deepseek-r1:8b", OneWireHardwareKind.Cpu, 0, "CPU", 512, 12288, 4096, 131072, 0),
                    Route("qwen3:8b", OneWireHardwareKind.Gpu, 1, "GPU 2", 512, 16384, 4096, 131072, 24),
                    Route("gemma3:12b", OneWireHardwareKind.Gpu, 0, "GPU 1", 512, 12288, 4096, 98304, 20)
                ],
                isDefault: true,
                maxParallel: 3),
            BuildPreset(
                "Learning Round",
                "Database, chat-memory, regex, logs, project and knowledge maintenance round with conservative model-specific roads.",
                ["gpt-oss:20b", "qwen3:8b", "deepseek-r1:8b"],
                [
                    Route("gpt-oss:20b", OneWireHardwareKind.Gpu, 0, "GPU 1", 1024, 24576, 8192, 262144, 32),
                    Route("qwen3:8b", OneWireHardwareKind.Gpu, 1, "GPU 2", 512, 12288, 4096, 131072, 24),
                    Route("deepseek-r1:8b", OneWireHardwareKind.Cpu, 0, "CPU", 512, 8192, 4096, 98304, 0)
                ],
                isDefault: false,
                maxParallel: 3)
        };
        foreach (var preset in presets.Where(item => !existing.Contains(item.Name)))
            db.CouncilModelPresets.Add(preset);
    }

    private CouncilModelPreset BuildPreset(
        string name,
        string description,
        string[] models,
        OneWireCouncilModelRoute[] routes,
        bool isDefault,
        int maxParallel,
        bool includeMemory = true,
        bool createProjectPerRun = true) => new()
    {
        Name = name,
        Description = description,
        ModelNamesJson = JsonSerializer.Serialize(models),
        ModelRoutesJson = JsonSerializer.Serialize(routes),
        AllowParallelHardwareRoads = true,
        MaxOutputTokens = routes.Max(route => route.MaxOutputTokens),
        MaxContextTokens = routes.Max(route => route.MaxContextTokens),
        MaxParallelModels = maxParallel,
        IncludeMemory = includeMemory,
        GenerateArtifacts = false,
        CreateProjectPerRun = createProjectPerRun,
        IsDefault = isDefault,
        IsUserApproved = true,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    private OneWireCouncilModelRoute Route(
        string model,
        OneWireHardwareKind kind,
        int index,
        string name,
        int minOutput,
        int maxOutput,
        int minContext,
        int maxContext,
        int? numGpu) => new()
    {
        ModelName = model,
        HardwareKind = kind,
        HardwareIndex = index,
        HardwareName = name,
        MinOutputTokens = minOutput,
        MaxOutputTokens = maxOutput,
        MinContextTokens = minContext,
        MaxContextTokens = maxContext,
        OllamaNumGpu = numGpu,
        MaxConcurrentModelsOnLane = 1,
        IsEnabled = true
    };

    private void EnsureTopic(LocalGptProject project, string name, string description)
    {
        if (project.Topics.Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))) return;
        project.Topics.Add(new LocalGptProjectTopic
        {
            ProjectId = project.Id,
            Name = name,
            Description = description,
            Status = "Active",
            IsUserApproved = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    private void EnsureVersion(LocalGptProject project, string version, string path, string notes)
    {
        if (project.Versions.Any(item => string.Equals(item.Version, version, StringComparison.OrdinalIgnoreCase))) return;
        var hasCurrentVersion = project.Versions.Any(existing => existing.IsCurrent);
        project.Versions.Add(new LocalGptProjectVersion
        {
            ProjectId = project.Id,
            Version = version,
            Notes = notes,
            PathSnapshot = path,
            IsCurrent = !hasCurrentVersion,
            IsUserConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private void EnsureRevision(LocalGptProject project, string branch, string revision, string root, string summary)
    {
        if (project.Revisions.Any(item => string.Equals(item.BranchName, branch, StringComparison.OrdinalIgnoreCase) && string.Equals(item.RevisionName, revision, StringComparison.OrdinalIgnoreCase))) return;
        var hasCurrentRevision = project.Revisions.Any(existing => existing.IsCurrent);
        project.Revisions.Add(new LocalGptProjectRevision
        {
            ProjectId = project.Id,
            BranchName = branch,
            RevisionName = revision,
            Summary = summary,
            ProjectStructureJson = JsonSerializer.Serialize(new { RootPath = root, Seeded = true, Version = revision }),
            CreatedBy = "LocalGPT deterministic seed",
            IsCurrent = !hasCurrentRevision,
            IsUserApproved = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    private void EnsureRequirement(LocalGptProject project, string name, string description, string capability, string priority)
    {
        if (project.Requirements.Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))) return;
        project.Requirements.Add(new LocalGptProjectRequirement
        {
            ProjectId = project.Id,
            Name = name,
            Description = description,
            RequirementType = "Architecture",
            Status = "Active",
            Priority = priority,
            RequiredCapability = capability,
            SourceKind = "DeterministicSeed",
            CouncilRating = 100,
            IsUserApproved = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    private void EnsureArtifact(LocalGptProject project, string name, string kind, string value, string dataType, string description)
    {
        if (project.Artifacts.Any(item => string.Equals(item.ArtifactKind, kind, StringComparison.OrdinalIgnoreCase) && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))) return;
        project.Artifacts.Add(new LocalGptProjectArtifact
        {
            ProjectId = project.Id,
            ArtifactKind = kind,
            Name = name,
            Value = value,
            DataType = dataType,
            Description = description,
            CouncilReviewStatus = "Current",
            IsUserApproved = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    private string ResolveRepositoryRoot(string contentRoot)
    {
        var current = new DirectoryInfo(Path.GetFullPath(contentRoot));
        while (current is not null)
        {
            if (current.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly).Any() ||
                current.EnumerateDirectories("LocalGPTWebviewWrapper", SearchOption.TopDirectoryOnly).Any())
                return current.FullName;
            current = current.Parent;
        }
        return Path.GetFullPath(contentRoot);
    }

}

public sealed class DatabaseInitializationHostedService(
    IDatabaseInitializationService initializer,
    ILogger<DatabaseInitializationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "LocalGPT database initialization failed.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

