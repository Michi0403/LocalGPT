using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates database initialization behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed partial class DatabaseInitializationService
{
    /// <summary>
    /// Performs seed knowledge as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="db">Database value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SeedKnowledgeAsync(LocalGptMemoryDbContext db, CancellationToken token)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(SeedKnowledgeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(SeedKnowledgeAsync)} failed.");
        throw;
    }
}


    /// <summary>
    /// Performs seed core projects as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="db">Database value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SeedCoreProjectsAsync(LocalGptMemoryDbContext db, CancellationToken token)
    {
    try
    {
            var now = DateTime.UtcNow;
            var coreProjectId = runtimePolicySeed.GetSeed().LocalGptCoreProjectId;
            var humanitarianProjectId = Guid.Parse("7f4d7b4a-b622-4d15-8e44-9dfae2aa6102");
            var repositoryRoot = ResolveRepositoryRoot(hostEnvironment.ContentRootPath);

            var core = await db.LocalGptProjects
                .AsNoTracking()
                .Include(project => project.Topics)
                .Include(project => project.Versions)
                .Include(project => project.Revisions)
                .Include(project => project.Requirements)
                .Include(project => project.Artifacts)
                .AsSplitQuery()
                .SingleOrDefaultAsync(project => project.Id == coreProjectId, token)
                .ConfigureAwait(false);
            var coreIsNew = core is null;
            core ??= new LocalGptProject
            {
                Id = coreProjectId,
                Name = "LocalGPT Core",
                Purpose = "Human-guided, humanitarian self-development of LocalGPT, its AI Council, project architecture, database knowledge, regex links, diagnostics and organic 1-Wire organs.",
                RootPath = repositoryRoot,
                CurrentVersion = "source-pending",
                Status = "Active",
                RecommendGit = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            var coreTopicIds = core.Topics.Select(item => item.Id).ToHashSet();
            var coreVersionIds = core.Versions.Select(item => item.Id).ToHashSet();
            var coreRevisionIds = core.Revisions.Select(item => item.Id).ToHashSet();
            var coreRequirementIds = core.Requirements.Select(item => item.Id).ToHashSet();
            var coreArtifactIds = core.Artifacts.Select(item => item.Id).ToHashSet();

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
            EnsureVersion(core, "2.1.16", repositoryRoot, "Build-correction release for workspace access-policy evaluation that resolves the Regex.IsMatch method-group ambiguity on .NET 10 without changing policy behavior.");
            EnsureRevision(core, "main", "seed-v2.1.16", repositoryRoot,
                "Resolves the .NET 10 LINQ overload ambiguity in workspace access-policy matching by wrapping Regex.IsMatch in an explicit single-argument lambda while preserving the existing bounded policy behavior.");
            EnsureVersion(core, "2.1.17", repositoryRoot, "Responsive workbench and customizable LearnBase release with the final workspace path-overload correction, full-width operational pages, optional ASCII sessions and selectable fullscreen scaling.");
            EnsureRevision(core, "main", "seed-v2.1.17", repositoryRoot,
                "Applies the string-based EndsWith workspace policy fix, adds editable LearnBase endings/regex/import modes with embedded source profiles, expands OneWire/Projects/Project Maintenance layouts, and makes the original ASCII corridor optional with side-by-side controls and Fit/Width/Native fullscreen modes.");
            EnsureVersion(core, "2.1.18", repositoryRoot, "Authoritative GameDirector, generated XML-comment documentation, startup seed-concurrency correction and large responsive Chat configuration release.");
            EnsureRevision(core, "main", "seed-v2.1.18", repositoryRoot,
                "Routes every game control proposal through the GameDirector and bounded creature/object subdirectors, adds DocFX HTML/PDF output with version-enriched XML-comment APIs, seeds existing projects through additive no-tracking inserts, and expands Chat configuration surfaces for 4K and 100-percent zoom use.");
            EnsureVersion(core, "2.1.19", repositoryRoot, "Documentation-build invocation correction release that preserves generated DocFX HTML/PDF output while making Windows PowerShell parameter passing deterministic.");
            EnsureRevision(core, "main", "seed-v2.1.19", repositoryRoot,
                "Prevents the trailing repository-root backslash from absorbing DocFX build arguments, normalizes all documentation input paths, and retains the 2.1.18 GameDirector, startup, translator and responsive Chat behavior.");
            EnsureVersion(core, "2.1.20", repositoryRoot, "First-run councils, recursive-prompt cleanup, canonical Harmony/Markdown rendering and resilient DocFX/XML-documentation release.");
            EnsureRevision(core, "main", "seed-v2.1.20", repositoryRoot,
                "Adds visible benchmark, GameDirector and language-specific development teams; introduces installer/documentation quick starts; prevents full Council transcripts and model-owned HTML from being recursively reinjected; and makes DocFX restore failures non-fatal for diagnostic builds while maintaining XML-commented changed APIs.");
            EnsureVersion(core, "2.1.22", repositoryRoot, "Open localization catalogs, persistent installer onboarding access and resilient DocFX metadata fallback.");
            EnsureRevision(core, "main", "seed-v2.1.22", repositoryRoot,
                "Keeps onboarding and benchmark quick starts accessible from Install, supports validated user language JSON catalogs and prevents DocFX metadata failures from breaking Debug builds.");
            EnsureVersion(core, "2.1.23", repositoryRoot, "Release-safe documentation fallback, direct Council autostart, compiler discovery UI and durable feature records.");
            EnsureRevision(core, "main", "seed-v2.1.23", repositoryRoot,
                "Added static HTML/PDF documentation fallback, fixed Council starter dispatch, restored normal quick prompts, exposed toolchains, and persisted newer feature records with CRUD APIs.");
            EnsureVersion(core, "2.2.1", repositoryRoot, "Maintenance release with reliable localization, installed-documentation discovery, grouped responsive Test Lab panels, stable AI-provider selection and GitHub Pages API navigation.");
            EnsureRevision(core, "main", "seed-v2.2.1", repositoryRoot,
                "Restores end-to-end language switching after interactive Blazor attachment, removes forbidden service statics, replaces the clipped Council/provider combo, keeps recursive installed documentation discovery and fixes the generated API reference link.");
            EnsureVersion(core, "2.2.2", repositoryRoot, "Patch release preserving the published 2.2.1 history while completing culture switching, installed PDF discovery, centered ASCII-game guide behavior, resilient DX catalog synchronization and generated API navigation.");
            EnsureRevision(core, "main", "seed-v2.2.2", repositoryRoot,
                "Advances the application package after 2.2.1 was published. Retains all frontend features and applies the follow-up localization, game-layout, documentation, persistence and build-policy corrections without changing the separately versioned 1-Wire protocol package.");
            EnsureVersion(core, "2.2.4", repositoryRoot, "Kawaii DocFX website-shell and release-routing patch with complete light/dark theming, validated generated links and shipped GitHub Pages output.");
            EnsureRevision(core, "main", "seed-v2.2.4", repositoryRoot,
                "Carries the post-2.2.2 documentation website corrections under a new application version. Preserves working localization and frontend behavior while applying the full Kawaii HTML shell, cat branding, light/dark mode support, generated-link validation and release-payload GitHub Pages publishing.");
            EnsureVersion(core, "2.2.5", repositoryRoot, "GitHub Pages deployment reliability patch that publishes the exact shipped Kawaii DocFX tree, verifies theme assets and automatically refreshes Pages after workflow changes.");
            EnsureRevision(core, "main", "seed-v2.2.5", repositoryRoot,
                "Added automatic latest-release Pages deployment, strict Kawaii asset selection, deployment diagnostics and a clear legacy-source guard without changing application features.");
            EnsureVersion(core, "2.2.6", repositoryRoot, "GitHub Actions Node.js 24 maintenance release that updates the Pages checkout action while preserving the shipped Kawaii DocFX deployment pipeline.");
            EnsureRevision(core, "main", "seed-v2.2.6", repositoryRoot,
                "Updates actions/checkout from v4 to the Node.js 24-based v6 release. The generated documentation payload, Kawaii theme, Pages extraction rules and application functionality remain unchanged.");
            EnsureVersion(core, "2.2.7", repositoryRoot, "GitHub Pages ZIP path-normalization release that correctly extracts PowerShell-created Windows archive members while preserving the shipped Kawaii DocFX site.");
            EnsureRevision(core, "main", "seed-v2.2.7", repositoryRoot,
                "Fixes release documentation extraction by retaining the exact stored ZIP member names after portable slash normalization. Windows backslash paths now publish correctly without weakening theme, API, PDF or path-safety validation.");
            EnsureVersion(core, "2.2.8", repositoryRoot, "Provider-qualified multi-provider Council and dynamic Benchmark Council release with reusable accessible model panels.");
            EnsureRevision(core, "main", "seed-v2.2.8", repositoryRoot,
                "Adds provider-and-endpoint-safe model identities, cross-provider Council execution, per-model benchmark/property controls in Chat, Install and Council, all-selected Benchmark Council runs, and user-approved provider-qualified recommendation presets.");
            EnsureVersion(core, "2.2.9", repositoryRoot, "Build-policy compliance and Council final-answer accounting patch for provider-qualified benchmarking.");
            EnsureRevision(core, "main", "seed-v2.2.9", repositoryRoot,
                "Moves provider-panel text composition into the text service, materializes configured-provider enumeration, fixes configuration type ambiguity and benchmark route initialization, removes obsolete Council dependencies, and records failed final-answer recovery honestly without attaching it as peer verification.");
            EnsureVersion(core, "2.3.2", repositoryRoot, "Chat ASCII-console close-action release with fullscreen-safe removal in every display scale mode.");
            EnsureRevision(core, "main", "seed-v2.3.2", repositoryRoot,
                "Adds an always-visible in-console close action, exits browser fullscreen before unmounting the interactive panel, and restores the Chat conversation without requiring a page refresh or session rejoin.");
            EnsureVersion(core, "2.3.3", repositoryRoot, "Documentation viewer and normalized source-layout maintenance release.");
            EnsureRevision(core, "main", "seed-v2.3.3", repositoryRoot,
                "Opens generated documentation in a contained in-app viewer, corrects the canonical src/LocalGPT documentation output path, and aligns active maintenance scripts with the normalized repository layout.");
            EnsureVersion(core, "2.3.6", repositoryRoot, "Responsive Chat and joinable live benchmark-session release.");
            EnsureRevision(core, "main", "seed-v2.3.6", repositoryRoot,
                "Keeps benchmark work inside the maintained live Council-session path, restores reliable join and transcript visibility in Chat, and makes configuration controls responsive at high display scaling without changing provider-qualified execution boundaries.");


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
            EnsureArtifact(core, "Canonical repository source", "SourceRepository",
                "https://github.com/Michi0403/LocalGPT",
                "uri", "Canonical public LocalGPT repository supplied by the user. Councils may inspect it read-only and explicit refresh pipelines may update local source-backed project knowledge.");
            EnsureRequirement(core, "Canonical LocalGPT and PublisherStudio repository knowledge",
                "LocalGPT Councils may inspect the canonical public LocalGPT and PublisherStudio/BlazorPublisher repositories for current source facts. Explicit user-invoked refresh pipelines may persist those retrieved repository versions, revisions, framework requirements, workspaces and complete tracked-file structures into their separate canonical projects.",
                "localgpt.repository.knowledge.refresh", "Critical");

            var reconciledCoreVersion = PrepareLocalGptReleaseHistory(core, repositoryRoot);
            TrackMissingProjectSeedRecords(
                db,
                core,
                coreIsNew,
                coreTopicIds,
                coreVersionIds,
                coreRevisionIds,
                coreRequirementIds,
                coreArtifactIds);
            await ReconcilePersistedCoreProjectAsync(db, core, coreIsNew, reconciledCoreVersion, repositoryRoot, token).ConfigureAwait(false);
            await SeedPublisherStudioProjectAsync(db, token).ConfigureAwait(false);
            await SeedRepositoryRefreshPipelinesAsync(db, token).ConfigureAwait(false);

            var humanitarian = await db.LocalGptProjects
                .AsNoTracking()
                .Include(project => project.Topics)
                .Include(project => project.Versions)
                .Include(project => project.Revisions)
                .Include(project => project.Requirements)
                .Include(project => project.Artifacts)
                .AsSplitQuery()
                .SingleOrDefaultAsync(project => project.Id == humanitarianProjectId, token)
                .ConfigureAwait(false);
            var humanitarianIsNew = humanitarian is null;
            humanitarian ??= new LocalGptProject
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
            var humanitarianTopicIds = humanitarian.Topics.Select(item => item.Id).ToHashSet();
            var humanitarianVersionIds = humanitarian.Versions.Select(item => item.Id).ToHashSet();
            var humanitarianRevisionIds = humanitarian.Revisions.Select(item => item.Id).ToHashSet();
            var humanitarianRequirementIds = humanitarian.Requirements.Select(item => item.Id).ToHashSet();
            var humanitarianArtifactIds = humanitarian.Artifacts.Select(item => item.Id).ToHashSet();
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

            TrackMissingProjectSeedRecords(
                db,
                humanitarian,
                humanitarianIsNew,
                humanitarianTopicIds,
                humanitarianVersionIds,
                humanitarianRevisionIds,
                humanitarianRequirementIds,
                humanitarianArtifactIds);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(SeedCoreProjectsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(SeedCoreProjectsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs track missing project seed records as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="db">Database value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="project">Project value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="isNewProject">Value indicating whether is new project should apply to this operation.</param>
    /// <param name="existingTopicIds">Guid dependency used by the database initialization workflow to provide the corresponding application capability.</param>
    /// <param name="existingVersionIds">Guid dependency used by the database initialization workflow to provide the corresponding application capability.</param>
    /// <param name="existingRevisionIds">Guid dependency used by the database initialization workflow to provide the corresponding application capability.</param>
    /// <param name="existingRequirementIds">Guid dependency used by the database initialization workflow to provide the corresponding application capability.</param>
    /// <param name="existingArtifactIds">Guid dependency used by the database initialization workflow to provide the corresponding application capability.</param>
    private void TrackMissingProjectSeedRecords(
        LocalGptMemoryDbContext db,
        LocalGptProject project,
        bool isNewProject,
        IReadOnlySet<Guid> existingTopicIds,
        IReadOnlySet<Guid> existingVersionIds,
        IReadOnlySet<Guid> existingRevisionIds,
        IReadOnlySet<Guid> existingRequirementIds,
        IReadOnlySet<Guid> existingArtifactIds)
    {
    try
    {
            if (isNewProject)
            {
                db.LocalGptProjects.Add(project);
                return;
            }

            db.LocalGptProjectTopics.AddRange(project.Topics.Where(item => !existingTopicIds.Contains(item.Id)));
            db.LocalGptProjectVersions.AddRange(project.Versions.Where(item => !existingVersionIds.Contains(item.Id)));
            db.LocalGptProjectRevisions.AddRange(project.Revisions.Where(item => !existingRevisionIds.Contains(item.Id)));
            db.LocalGptProjectRequirements.AddRange(project.Requirements.Where(item => !existingRequirementIds.Contains(item.Id)));
            db.LocalGptProjectArtifacts.AddRange(project.Artifacts.Where(item => !existingArtifactIds.Contains(item.Id)));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(TrackMissingProjectSeedRecords)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(TrackMissingProjectSeedRecords)} failed.");
        throw;
    }
}

}
