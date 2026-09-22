using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Combines curator-approved file evidence, approved knowledge and available Council teams into one advisory upload-processing recommendation.</summary>
public sealed class UploadProcessingRecommendationService(
    IProjectIngestionService ingestion,
    IChatUploadWorkspaceService workspaces,
    ICouncilKnowledgeService knowledge,
    ICouncilTeamConfigurationService teams,
    IChatClientFactory chatClientFactory,
    IUploadFileProcessingCapabilityService processingCapabilities,
    LocalGptCatalogService catalog,
    ILogger<UploadProcessingRecommendationService> logger) : IUploadProcessingRecommendationService
{
    /// <inheritdoc />
    public async Task<UploadProcessingRecommendation> RecommendAsync(UploadProcessingRecommendationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.WorkspaceName);
            var gate = await ingestion.GetAsync(request.WorkspaceName, cancellationToken).ConfigureAwait(false)
                ?? await ingestion.InspectAsync(request.WorkspaceName, cancellationToken).ConfigureAwait(false);
            var availableTeams = await teams.GetTeamsAsync(false, cancellationToken).ConfigureAwait(false);
            var approvedKnowledge = await knowledge.GetEntriesAsync(false, 500, cancellationToken).ConfigureAwait(false);
            var relevantKnowledge = SelectKnowledge(gate, approvedKnowledge);
            var teamCandidates = RankTeams(gate, availableTeams);
            var processingRoutes = processingCapabilities.Evaluate(gate.Files);
            var fallback = BuildFallback(request, gate, relevantKnowledge, teamCandidates, processingRoutes);

            try
            {
                using var client = chatClientFactory.Build();
                client.SuppressBootstrapContext = true;
                client.ForcedMaxPromptCharacters = 48_000;
                client.ForcedMaxOutputTokens = 1_600;
                if (client.SelectedSession is null)
                    return await PersistAsync(fallback, cancellationToken).ConfigureAwait(false);

                var prompt = BuildPrompt(request, gate, relevantKnowledge, teamCandidates, processingRoutes);
                var response = await client.GetResponseAsync(
                    [
                        new ChatMessage(ChatRole.System,
                            "You are LocalGPT's bounded upload-processing advisor. Recommend; do not execute. Use only the supplied quarantine evidence, approved knowledge and listed Council teams. Never claim a file was extracted, executed, built, published or trusted. Return one JSON object only."),
                        new ChatMessage(ChatRole.User, prompt)
                    ],
                    new ChatOptions { MaxOutputTokens = 1_600, Temperature = 0.1f, Tools = [] },
                    cancellationToken).ConfigureAwait(false);
                var ai = ParseRecommendation(response.Text ?? string.Empty, fallback);
                ValidateSuggestedTeam(ai, teamCandidates);
                ai.Source = "AI-assisted from approved LocalGPT evidence";
                return await PersistAsync(ai, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AI upload-processing recommendation failed; using deterministic evidence fallback for {WorkspaceName}.", request.WorkspaceName);
                return await PersistAsync(fallback, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Building upload-processing recommendation failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<UploadProcessingRecommendation?> GetAsync(string workspaceName, CancellationToken cancellationToken = default)
    {
        try
        {
            var root = workspaces.ResolveWorkspacePath(workspaceName);
            if (root is null)
                return null;
            var path = Path.Combine(root, "processing-recommendation.json");
            if (!File.Exists(path))
                return null;
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<UploadProcessingRecommendation>(json, catalog.JsonOptions);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Loading upload-processing recommendation failed for {WorkspaceName}.", workspaceName);
            throw;
        }
    }

    private IReadOnlyList<CouncilKnowledgeEntry> SelectKnowledge(ProjectIngestionGateRecord gate, IReadOnlyList<CouncilKnowledgeEntry> entries)
    {
        try
        {
            var terms = gate.Domains.Concat(gate.ProjectKinds).Concat(gate.Toolchains)
                .Concat(gate.Repositories.SelectMany(repository => new[] { repository.Name, repository.Kind, repository.Version }.Concat(repository.Markers)))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return entries
                .Where(entry => entry.IsUserApproved && !entry.IsArchived)
                .Select(entry => new
                {
                    Entry = entry,
                    Score = terms.Count(term =>
                        entry.Topic.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || entry.Scope.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || entry.Tags.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || entry.Content.Contains(term, StringComparison.OrdinalIgnoreCase))
                })
                .Where(item => item.Score > 0 || terms.Length == 0)
                .OrderByDescending(item => item.Score)
                .ThenByDescending(item => item.Entry.IsPinned)
                .ThenByDescending(item => item.Entry.Confidence)
                .Take(12)
                .Select(item => item.Entry)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Selecting approved knowledge for upload recommendation failed.");
            throw;
        }
    }

    private IReadOnlyList<OrganicCouncilTeamDefinition> RankTeams(ProjectIngestionGateRecord gate, IReadOnlyList<OrganicCouncilTeamDefinition> availableTeams)
    {
        try
        {
            var terms = gate.Domains.Concat(gate.ProjectKinds).Concat(gate.Toolchains)
                .Concat(gate.Repositories.SelectMany(repository => new[] { repository.Name, repository.Kind, repository.Version }.Concat(repository.Markers)))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return availableTeams
                .Where(team => team.IsEnabled && !team.IsDeleted)
                .Select(team => new
                {
                    Team = team,
                    Haystack = string.Join(" ", new[] { team.DisplayName, team.Purpose }
                        .Concat(team.PreferredCapabilities)
                        .Concat(team.Roles.Select(role => $"{role.Role} {role.Expertise} {role.Responsibility}"))),
                })
                .Select(item => new { item.Team, Score = terms.Count(term => item.Haystack.Contains(term, StringComparison.OrdinalIgnoreCase)) })
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Team.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .Select(item => item.Team)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ranking Council teams for upload recommendation failed.");
            throw;
        }
    }

    private UploadProcessingRecommendation BuildFallback(
        UploadProcessingRecommendationRequest request,
        ProjectIngestionGateRecord gate,
        IReadOnlyList<CouncilKnowledgeEntry> relevantKnowledge,
        IReadOnlyList<OrganicCouncilTeamDefinition> teamCandidates,
        IReadOnlyList<UploadFileProcessingRoute> processingRoutes)
    {
        try
        {
            var repository = gate.Repositories.FirstOrDefault();
            var repositoryDetected = repository is not null;
            var repositoryTeam = repositoryDetected
                ? teamCandidates.FirstOrDefault(item => item.DisplayName.Contains("Repository", StringComparison.OrdinalIgnoreCase)
                    || item.Purpose.Contains("repository", StringComparison.OrdinalIgnoreCase)
                    || item.PreferredCapabilities.Any(capability => capability.Contains("repository", StringComparison.OrdinalIgnoreCase)))
                : null;
            var team = repositoryTeam
                ?? teamCandidates.FirstOrDefault(item => string.Equals(item.Key, "general", StringComparison.OrdinalIgnoreCase))
                ?? teamCandidates.FirstOrDefault();
            var domains = gate.Domains.Count > 0 ? string.Join(", ", gate.Domains) : "unclassified material";
            var repositoryLabel = repositoryDetected
                ? $"{repository!.Name}{(string.IsNullOrWhiteSpace(repository.Version) ? string.Empty : $" {repository.Version}")}{(repository.GitMetadataDetected ? " (Git metadata detected)" : string.Empty)}"
                : string.Empty;
            return new UploadProcessingRecommendation
            {
                WorkspaceName = gate.WorkspaceName,
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                Source = "Deterministic evidence fallback",
                Confidence = gate.DeterministicChecksPassed ? 55 : 20,
                Summary = repositoryDetected
                    ? $"Recognized {repositoryLabel} as a source repository inside bounded quarantine. Deterministic checks are {(gate.DeterministicChecksPassed ? "passing" : "not passing")}; repository identity does not bypass review."
                    : $"Quarantined upload currently classifies as {domains}. Deterministic checks are {(gate.DeterministicChecksPassed ? "passing" : "not passing")}; recommendation is advisory and does not bypass review.",
                RecommendedAction = !gate.DeterministicChecksPassed
                    ? "Resolve the quarantine rejection reasons before any processing or promotion."
                    : repositoryDetected
                        ? "Review and promote the repository into its canonical LocalGPT project structure. Promotion records a source snapshot, compares it with the previous revision, and stages source/delta Knowledge plus repository regex candidates for explicit review before they can become trusted runtime knowledge."
                        : "Review the quarantined files against the listed evidence and approved knowledge, then choose whether to process them in solo Chat or with the suggested Council team before promotion.",
                ProcessingMode = team is null ? "Solo" : "SoloOrTeam",
                SuggestedTeamKey = team?.Key ?? string.Empty,
                SuggestedTeamName = team?.DisplayName ?? string.Empty,
                SuggestedConfiguration = team is null ? "Use the currently selected Chat model with automatic consequential actions disabled." : $"Use the existing '{team.DisplayName}' definition without changing its saved role or safety policy.",
                SuggestedPrompt = BuildSuggestedPrompt(request, gate),
                RequiresClarification = string.IsNullOrWhiteSpace(request.UserGoal),
                ClarifyingQuestions = string.IsNullOrWhiteSpace(request.UserGoal) ? ["What outcome do you want from these files: understand, maintain, transform, compare, import as knowledge, build, or publish?"] : [],
                Domains = gate.Domains.ToList(),
                ProjectKinds = gate.ProjectKinds.ToList(),
                Toolchains = gate.Toolchains.ToList(),
                Repositories = gate.Repositories.Select(CloneRepository).ToList(),
                ProcessingRoutes = processingRoutes.Select(CloneProcessingRoute).ToList(),
                Evidence = gate.Files.Take(40).Select(file => $"{file.RelativePath} · {file.Kind} · {file.Length:n0} bytes · {string.Join(", ", file.ApprovedRegexMatches)}").ToList(),
                KnowledgeReferences = relevantKnowledge.Select(entry => $"{entry.Topic} [{entry.Id:N}]").ToList(),
                AlternativeActions = repositoryDetected
                    ? ["Inspect only", "Review repository delta", "Run a Council repository review", "Stage reviewed source knowledge/regex candidates", "Promote into the canonical project workspace after review", "Plan build/publish only after promotion"]
                    : ["Inspect only", "Summarize into Chat", "Run a Council review", "Import reviewed facts into Knowledge", "Promote into a project workspace after review"]
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Building deterministic upload recommendation fallback failed.");
            throw;
        }
    }

    private string BuildPrompt(
        UploadProcessingRecommendationRequest request,
        ProjectIngestionGateRecord gate,
        IReadOnlyList<CouncilKnowledgeEntry> relevantKnowledge,
        IReadOnlyList<OrganicCouncilTeamDefinition> teamCandidates,
        IReadOnlyList<UploadFileProcessingRoute> processingRoutes)
    {
        try
        {
            var builder = new StringBuilder();
            builder.AppendLine("Produce a general-purpose processing recommendation for this quarantined LocalGPT upload.");
            builder.AppendLine($"User goal: {(string.IsNullOrWhiteSpace(request.UserGoal) ? "not supplied" : request.UserGoal.Trim())}");
            builder.AppendLine($"Gate status: {gate.Status}; deterministic checks: {gate.DeterministicChecksPassed}.");
            builder.AppendLine($"Domains: {string.Join(", ", gate.Domains)}");
            builder.AppendLine($"Project kinds: {string.Join(", ", gate.ProjectKinds)}");
            builder.AppendLine($"Toolchains: {string.Join(", ", gate.Toolchains)}");
            if (gate.Repositories.Count > 0)
            {
                builder.AppendLine("Detected source repositories:");
                foreach (var repository in gate.Repositories.Take(12))
                    builder.AppendLine($"- kind={repository.Kind}; name={repository.Name}; version={repository.Version}; git={repository.GitMetadataDetected}; root={repository.RootHint}; markers={string.Join(",", repository.Markers.Take(8))}");
            }
            builder.AppendLine("Current deterministic processor routes:");
            foreach (var route in processingRoutes.Take(40))
                builder.AppendLine($"- {route.RelativePath} | processor={route.Processor} | capability={route.CapabilityKey} | available={route.IsAvailable} | reason={route.Reason}");
            builder.AppendLine("Files/evidence:");
            foreach (var file in gate.Files.Take(40))
                builder.AppendLine($"- {file.RelativePath} | {file.Kind} | {file.Length} bytes | rules={string.Join(",", file.ApprovedRegexMatches)}");
            builder.AppendLine("Approved knowledge excerpts:");
            foreach (var entry in relevantKnowledge)
                builder.AppendLine($"- {entry.Topic} | scope={entry.Scope} | tags={entry.Tags} | confidence={entry.Confidence} | {Bound(entry.Content, 900)}");
            builder.AppendLine("Available Council teams:");
            foreach (var team in teamCandidates)
                builder.AppendLine($"- key={team.Key}; name={team.DisplayName}; purpose={Bound(team.Purpose, 500)}; capabilities={string.Join(",", team.PreferredCapabilities.Take(12))}; roles={string.Join(",", team.Roles.Select(role => role.Role).Take(12))}");
            builder.AppendLine("Return JSON fields: confidence (0..100), summary, recommendedAction, processingMode (Solo|Team|SoloOrTeam), suggestedTeamKey, suggestedTeamName, suggestedConfiguration, suggestedPrompt, requiresClarification, clarifyingQuestions[], alternativeActions[]. Use an existing team key only. If no listed team is appropriate, leave team fields blank. Keep quarantine/review/approval as authoritative boundaries.");
            return builder.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Building upload advisor prompt failed.");
            throw;
        }
    }

    private void ValidateSuggestedTeam(UploadProcessingRecommendation recommendation, IReadOnlyList<OrganicCouncilTeamDefinition> candidates)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(recommendation.SuggestedTeamKey))
            {
                recommendation.SuggestedTeamName = string.Empty;
                return;
            }

            var configured = candidates.FirstOrDefault(team =>
                team.IsEnabled
                && !team.IsDeleted
                && string.Equals(team.Key, recommendation.SuggestedTeamKey, StringComparison.OrdinalIgnoreCase));
            if (configured is null)
            {
                recommendation.SuggestedTeamKey = string.Empty;
                recommendation.SuggestedTeamName = string.Empty;
                return;
            }

            recommendation.SuggestedTeamKey = configured.Key;
            recommendation.SuggestedTeamName = configured.DisplayName;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Validating the AI-suggested Council team against persisted LocalGPT configuration failed.");
            throw;
        }
    }

    private UploadProcessingRecommendation ParseRecommendation(string text, UploadProcessingRecommendation fallback)
    {
        try
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end <= start)
                return fallback;
            using var document = JsonDocument.Parse(text[start..(end + 1)]);
            var root = document.RootElement;
            fallback.Confidence = ReadInt(root, "confidence", fallback.Confidence);
            fallback.Summary = ReadString(root, "summary", fallback.Summary);
            fallback.RecommendedAction = ReadString(root, "recommendedAction", fallback.RecommendedAction);
            fallback.ProcessingMode = ReadString(root, "processingMode", fallback.ProcessingMode);
            fallback.SuggestedTeamKey = ReadString(root, "suggestedTeamKey", fallback.SuggestedTeamKey);
            fallback.SuggestedTeamName = ReadString(root, "suggestedTeamName", fallback.SuggestedTeamName);
            fallback.SuggestedConfiguration = ReadString(root, "suggestedConfiguration", fallback.SuggestedConfiguration);
            fallback.SuggestedPrompt = ReadString(root, "suggestedPrompt", fallback.SuggestedPrompt);
            fallback.RequiresClarification = ReadBool(root, "requiresClarification", fallback.RequiresClarification);
            fallback.ClarifyingQuestions = ReadStrings(root, "clarifyingQuestions", fallback.ClarifyingQuestions);
            fallback.AlternativeActions = ReadStrings(root, "alternativeActions", fallback.AlternativeActions);
            fallback.GeneratedAtUtc = DateTimeOffset.UtcNow;
            return fallback;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Parsing AI upload-processing recommendation failed; deterministic fallback was retained.");
            return fallback;
        }
    }

    private async Task<UploadProcessingRecommendation> PersistAsync(UploadProcessingRecommendation recommendation, CancellationToken cancellationToken)
    {
        try
        {
            var root = workspaces.ResolveWorkspacePath(recommendation.WorkspaceName);
            if (root is not null)
                await File.WriteAllTextAsync(Path.Combine(root, "processing-recommendation.json"), JsonSerializer.Serialize(recommendation, catalog.JsonOptions), cancellationToken).ConfigureAwait(false);
            return recommendation;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Persisting upload-processing recommendation failed for {WorkspaceName}.", recommendation.WorkspaceName);
            throw;
        }
    }

    private string BuildSuggestedPrompt(UploadProcessingRecommendationRequest request, ProjectIngestionGateRecord gate)
    {
        try
        {
            var goal = string.IsNullOrWhiteSpace(request.UserGoal) ? "Help me decide what to do with these files" : request.UserGoal.Trim();
            var repositoryInstruction = gate.Repositories.Count == 0
                ? string.Empty
                : " The upload contains a recognized source repository; preserve its repository root, compare the promoted canonical revision with its previous revision, and keep generated Knowledge/regex evidence review-required.";
            return $"{goal}. Use quarantined workspace '{gate.WorkspaceName}'. Start from the reviewed evidence and approved knowledge.{repositoryInstruction} Do not promote, execute, build, install, publish, network, or write outside the bounded workspace without the corresponding explicit user approval.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Building suggested upload prompt failed.");
            throw;
        }
    }

    private UploadFileProcessingRoute CloneProcessingRoute(UploadFileProcessingRoute source)
    {
        try
        {
            return new UploadFileProcessingRoute
            {
                RelativePath = source.RelativePath,
                Processor = source.Processor,
                CapabilityKey = source.CapabilityKey,
                IsAvailable = source.IsAvailable,
                Reason = source.Reason
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cloning upload processor route failed.");
            throw;
        }
    }

    private RepositoryEvidenceIdentity CloneRepository(RepositoryEvidenceIdentity source)
    {
        try
        {
            return new RepositoryEvidenceIdentity
            {
                Kind = source.Kind,
                Name = source.Name,
                Version = source.Version,
                RootHint = source.RootHint,
                GitMetadataDetected = source.GitMetadataDetected,
                Markers = source.Markers.ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cloning upload repository identity failed.");
            throw;
        }
    }

    private string Bound(string? value, int maximum)
    {
        try
        {
            var normalized = value?.Replace('\r', ' ').Replace('\n', ' ').Trim() ?? string.Empty;
            return normalized[..Math.Min(normalized.Length, maximum)];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bounding upload-advisor text failed.");
            throw;
        }
    }

    private int ReadInt(JsonElement root, string name, int fallback)
    {
        try { return root.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed) ? Math.Clamp(parsed, 0, 100) : fallback; }
        catch (Exception ex) { logger.LogError(ex, "Reading upload-advisor integer failed."); return fallback; }
    }

    private string ReadString(JsonElement root, string name, string fallback)
    {
        try { return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? Bound(value.GetString(), 4000) : fallback; }
        catch (Exception ex) { logger.LogError(ex, "Reading upload-advisor text failed."); return fallback; }
    }

    private bool ReadBool(JsonElement root, string name, bool fallback)
    {
        try { return root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : fallback; }
        catch (Exception ex) { logger.LogError(ex, "Reading upload-advisor boolean failed."); return fallback; }
    }

    private List<string> ReadStrings(JsonElement root, string name, IReadOnlyList<string> fallback)
    {
        try
        {
            if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
                return fallback.ToList();
            return value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => Bound(item.GetString(), 600)).Where(item => item.Length > 0).Take(12).ToList();
        }
        catch (Exception ex) { logger.LogError(ex, "Reading upload-advisor text collection failed."); return fallback.ToList(); }
    }
}
