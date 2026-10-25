using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// First-class goal orchestration boundary above Council. The live DI capability directory and persisted
/// teams are the only routing catalogs; no task-category switch or second executable tool registry is used.
/// Council still owns rounds, model roles, tools, approvals, recovery, persistence and streaming callbacks.
/// </summary>
/// <param name="directory">Current executable DXFunctions and available organic skills.</param>
/// <param name="teams">Persisted, user-configurable Council team definitions.</param>
/// <param name="council">The existing Council run engine.</param>
/// <param name="gameBootstrap">Ensures the configured game session before delegating to Council.</param>
/// <param name="logger">Execution diagnostics without user prompt or source contents.</param>
/// <param name="providerModels">Bounded planning-only inference with registered model routes.</param>
/// <param name="runtimePolicy">Database-configurable planner instructions and limits.</param>
/// <param name="ledger">Durable step checkpoints and per-goal coordination.</param>
/// <param name="wirePeers">Live advertised 1-Wire peer capabilities.</param>
/// <param name="wireConnections">Authoritative active 1-Wire connection state.</param>
public sealed partial class GoalExecutionService(
    IRuntimeCapabilityDirectoryService directory,
    ICouncilTeamConfigurationService teams,
    IMultiModelCouncilService council,
    CouncilGameWorkflowBootstrapService gameBootstrap,
    IProviderModelRuntimeService providerModels,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    IGoalExecutionLedgerService ledger,
    IOneWirePeerRegistry wirePeers,
    IOneWireConnectionRegistry wireConnections,
    ILogger<GoalExecutionService> logger) : IGoalExecutionService
{
    /// <inheritdoc />
    public async Task<GoalExecutionResult> PlanAsync(GoalExecutionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.CouncilRequest);
        cancellationToken.ThrowIfCancellationRequested();
        var text = string.IsNullOrWhiteSpace(request.GoalText) ? request.CouncilRequest.Prompt : request.GoalText;
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("A nonempty goal is required.", nameof(request));

        var snapshot = await directory.SynchronizeAsync(cancellationToken).ConfigureAwait(false);
        var configuredTeams = await teams.GetTeamsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var functions = snapshot.Functions
            .Where(function => function.AvailableToAi && !function.IsCoordinationOnly &&
                (function.SupportsAutomaticInvocation || function.SupportsDirectInvocation || function.SupportsDeferredApprovalRequest))
            .ToArray();
        var skills = snapshot.Skills.Where(skill => skill.IsEnabled && skill.IsOnline).ToArray();
        var requiredFunctions = request.RequiredFunctions.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var requiredCapabilities = request.RequiredCapabilities.Concat(request.CouncilRequest.RequestedOrganicCapabilities)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var knownFunctions = functions.Select(function => function.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // A publisher.* key is a capability of a CONNECTED 1-Wire peer, not a LocalGPT
        // organic skill key. Validate against advertised executable peers rather than
        // rejecting every PublisherStudio request at the goal-planning boundary.
        var peerCapabilities = wirePeers.GetPeers()
            .Where(peer => wireConnections.IsConnected(peer.PeerId))
            .SelectMany(peer => peer.Capabilities)
            .Where(capability => capability.IsEnabled && capability.IsOnline)
            .ToArray();
        var knownCapabilities = skills.Select(skill => skill.Key)
            .Concat(peerCapabilities.Select(capability => capability.Key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = requiredFunctions.Where(key => !knownFunctions.Contains(key))
            .Concat(requiredCapabilities.Where(key => !knownCapabilities.Contains(key)))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var goalTokens = Tokens(text);
        var matchingFunctions = functions
            .Select(function => new { function.Name, Score = Match(goalTokens, function.Name, function.Purpose, function.Parameters) })
            .Where(item => item.Score >= 2 || requiredFunctions.Contains(item.Name, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Score).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Name).ToList();
        var matchingSkills = skills
            .Select(skill => new { skill.Key, Score = Match(goalTokens, skill.Key) })
            .Where(item => item.Score >= 2 || requiredCapabilities.Contains(item.Key, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Score).ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Key).ToList();
        matchingSkills.AddRange(peerCapabilities
            .Select(capability => new { capability.Key,
                Score = Match(goalTokens, capability.Key, capability.DisplayName, capability.Description) })
            .Where(item => item.Score >= 2 || requiredCapabilities.Contains(item.Key, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Score).ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Key));
        matchingSkills = matchingSkills.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var ranked = configuredTeams
            .Select(team => new
            {
                Team = team,
                Missing = requiredFunctions.Except(team.AllowedAutomaticFunctions.Concat(team.RequiredCompletedFunctions), StringComparer.OrdinalIgnoreCase).Count()
                    + requiredCapabilities.Except(team.PreferredCapabilities, StringComparer.OrdinalIgnoreCase).Count(),
                Score = Match(goalTokens, team.Key, team.DisplayName, team.Purpose)
                    + team.PreferredCapabilities.Sum(key => Match(goalTokens, key))
                    + team.Roles.Sum(role => Match(goalTokens, role.Role))
                    + team.AllowedAutomaticFunctions.Count(key => matchingFunctions.Contains(key, StringComparer.OrdinalIgnoreCase)) * 2
            })
            .OrderBy(entry => entry.Missing).ThenByDescending(entry => entry.Score)
            .ThenBy(entry => entry.Team.Key, StringComparer.OrdinalIgnoreCase).ToArray();
        var requestedKey = request.CouncilRequest.CouncilTeamKey;
        var selected = request.PreserveSelectedTeam
            ? configuredTeams.FirstOrDefault(team => string.Equals(team.Key, requestedKey, StringComparison.OrdinalIgnoreCase))
            : ranked.FirstOrDefault(entry => entry.Score >= 2 ||
                (requiredFunctions.Length + requiredCapabilities.Length > 0 && entry.Missing == 0))?.Team;
        if (request.PreserveSelectedTeam && selected is null)
            missing.Add("SelectedCouncilTeam");
        if (!request.PreserveSelectedTeam)
            selected ??= configuredTeams.FirstOrDefault(team => string.Equals(team.Key, requestedKey, StringComparison.OrdinalIgnoreCase))
                ?? configuredTeams.FirstOrDefault(team => string.Equals(team.Key, "general", StringComparison.OrdinalIgnoreCase))
                ?? ranked.FirstOrDefault()?.Team;
        if (selected is null)
            missing.Add("CouncilTeam");

        // Preserve caller/provider-authoritative model assignments. Council applies configured role assignments;
        // the goal layer never silently switches to a different remote provider or grants new tool permissions.
        var models = request.CouncilRequest.ModelSelections.Select(model => model.SelectionKey)
            .Concat(request.CouncilRequest.ModelSelections.Count == 0
                ? request.CouncilRequest.ModelNames : [])
            .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var discoveredModels = new List<ProviderModelReference>();
        if (models.Count == 0)
        {
            // Provider candidates are supplied by the Council runtime, never inferred from catalog prose.
            var installed = (await council.GetCandidatesAsync(cancellationToken).ConfigureAwait(false))
                .Where(candidate => candidate.IsInstalled && candidate.IsConfigured)
                .OrderByDescending(candidate => candidate.IsLoaded)
                .ThenBy(candidate => candidate.SelectionKey, StringComparer.OrdinalIgnoreCase)
                .ToList();
            // Concurrency is not participant count. Let the persisted role assignment policy
            // determine participant needs independently of MaxParallelModels.
            var requiredParticipants = selected is null ? 1 : selected.Roles
                .Where(role => role.HumanParticipationMode != HumanParticipationMode.HumanOnly)
                .Select(role => Math.Max(1, role.MinimumAiParticipants))
                .DefaultIfEmpty(1).Max();
            var assignedKeys = selected?.Roles
                .SelectMany(role => role.AssignedModelKeys)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (assignedKeys.Count > 0)
            {
                discoveredModels = installed.Where(candidate => assignedKeys.Contains(candidate.SelectionKey))
                    .Select(candidate => candidate.ToReference()).ToList();
                if (discoveredModels.Count == 0)
                    missing.Add("AssignedModelUnavailable");
            }
            else
                discoveredModels = installed.Take(requiredParticipants).Select(candidate => candidate.ToReference()).ToList();
            models.AddRange(discoveredModels.Select(model => model.SelectionKey));
        }
        if (models.Count == 0)
            missing.Add("CouncilParticipants");

        var semantic = await TrySemanticPlanAsync(text, request, configuredTeams, functions, skills,
            models, cancellationToken).ConfigureAwait(false);
        var source = semantic is null ? "CatalogFallback" : "ModelSemantic";
        if (!request.PreserveSelectedTeam && semantic is not null)
        {
            var semanticTeam = configuredTeams.FirstOrDefault(team => string.Equals(team.Key,
                semantic.TeamKey, StringComparison.OrdinalIgnoreCase));
            if (semanticTeam is not null) selected = semanticTeam;
        }
        var validKeys = configuredTeams.Select(team => team.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var steps = semantic?.Steps
            .Where(step => !string.IsNullOrWhiteSpace(step.Objective)
                && validKeys.Contains(step.TeamKey)
                && (!request.PreserveSelectedTeam || string.Equals(step.TeamKey, selected?.Key, StringComparison.OrdinalIgnoreCase)))
            .Take(Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.GoalMaximumPlanSteps)))
            .Select((step, index) => new GoalExecutionPlanStep
            {
                Key = "goal-" + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                Objective = step.Objective.Trim(), TeamKey = step.TeamKey
            }).ToList() ?? [];
        if (steps.Count == 0 && selected is not null)
            steps.Add(new GoalExecutionPlanStep { Key = "goal-1", Objective = text, TeamKey = selected.Key });
        if (semantic is not null)
        {
            matchingFunctions = semantic.Functions.Where(knownFunctions.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            matchingSkills = semantic.Capabilities.Where(knownCapabilities.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        return new GoalExecutionResult
        {
            GoalId = request.CouncilRequest.RunId,
            GoalText = text,
            SelectedTeamKey = selected?.Key ?? string.Empty,
            SelectedModels = models,
            PlannedModelSelections = discoveredModels,
            AvailableFunctions = matchingFunctions,
            AvailableCapabilities = matchingSkills,
            MissingRequirements = missing.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            PlannerSource = source,
            PlanSteps = steps,
            State = GoalExecutionState.Blocked
        };
    }

    /// <inheritdoc />
    public async Task<GoalExecutionResult> ExecuteAsync(GoalExecutionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CouncilRequest.RunId == Guid.Empty)
            request.CouncilRequest.RunId = Guid.NewGuid();
        using var lease = await ledger.AcquireAsync(request.CouncilRequest.RunId, cancellationToken).ConfigureAwait(false);
        var existing = await ledger.LoadAsync(request.CouncilRequest.RunId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
            return await ContinueAsync(request, existing, retryInterrupted: false, cancellationToken).ConfigureAwait(false);
        var cachedPlan = request.CouncilRequest.PrecomputedGoalPlan;
        var plan = cachedPlan is not null && cachedPlan.GoalId == request.CouncilRequest.RunId
            && string.Equals(cachedPlan.GoalText, request.GoalText, StringComparison.Ordinal)
            && cachedPlan.MissingRequirements.Count == 0
                ? cachedPlan : await PlanAsync(request, cancellationToken).ConfigureAwait(false);
        request.CouncilRequest.PrecomputedGoalPlan = null;
        request.KeyaccountReviewEnabled = request.CouncilRequest.KeyaccountReviewEnabled;
        request.KeyaccountContinuationLimit = request.CouncilRequest.KeyaccountContinuationLimit;
        var checkpoint = CreateCheckpoint(request, plan);
        if (plan.MissingRequirements.Count > 0)
        {
            checkpoint.State = GoalExecutionState.Blocked;
            await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
            plan.Checkpoint = checkpoint;
            return plan;
        }
        await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
        if (request.CouncilRequest.ModelSelections.Count == 0 && request.CouncilRequest.ModelNames.Count == 0)
        {
            request.CouncilRequest.ModelSelections = plan.PlannedModelSelections.ToList();
            request.CouncilRequest.ModelNames = plan.PlannedModelSelections.Select(model => model.ModelName).ToList();
        }
        return await ContinueAsync(request, checkpoint, retryInterrupted: false, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<GoalExecutionCheckpoint?> GetCheckpointAsync(Guid goalId, CancellationToken cancellationToken = default) =>
        ledger.LoadAsync(goalId, cancellationToken);

    /// <inheritdoc />
    public async Task<GoalExecutionResult> ResumeAsync(Guid goalId, bool confirmInterruptedRetry, CancellationToken cancellationToken = default)
    {
        using var lease = await ledger.AcquireAsync(goalId, cancellationToken).ConfigureAwait(false);
        var checkpoint = await ledger.LoadAsync(goalId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("Goal checkpoint not found.");
        var request = RestoreRequest(checkpoint);
        return await ContinueAsync(request, checkpoint, confirmInterruptedRetry, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GoalExecutionResult> ContinueWithNewObjectiveAsync(Guid goalId, string? objective,
        int additionalRounds, CancellationToken cancellationToken = default)
    {
        if (additionalRounds < 1 || additionalRounds > 100)
            throw new ArgumentOutOfRangeException(nameof(additionalRounds), "Select between 1 and 100 new Council rounds per continuation request.");
        using var lease = await ledger.AcquireAsync(goalId, cancellationToken).ConfigureAwait(false);
        var checkpoint = await ledger.LoadAsync(goalId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("Goal checkpoint not found.");
        if (checkpoint.State is GoalExecutionState.Running or GoalExecutionState.Interrupted or GoalExecutionState.NeedsApproval)
            throw new InvalidOperationException("Resolve running or interrupted Council work before adding new objectives.");
        var newObjective = (string.IsNullOrWhiteSpace(objective) ? checkpoint.KeyaccountPendingObjective : objective).Trim();
        if (newObjective.Length < 4 || newObjective.Length > 2000)
            throw new ArgumentException("Specify a distinct objective between 4 and 2000 characters.", nameof(objective));
        if (checkpoint.Steps.Any(step => string.Equals(step.Objective.Trim(), newObjective, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("The objective already exists; do not replay an earlier Council operation.");
        // This method is invoked only from the human-approval-gated continuation endpoint.
        // It extends the original goal journal; earlier successful steps are immutable.
        checkpoint.KeyaccountContinuationLimit = checked(checkpoint.KeyaccountContinuationsScheduled + additionalRounds);
        checkpoint.KeyaccountContinuationApproved = true;
        checkpoint.KeyaccountPendingObjective = string.Empty;
        checkpoint.Steps.Add(new GoalExecutionPlanStep
        {
            Key = "keyaccount-manual-" + Guid.NewGuid().ToString("N"),
            Objective = newObjective,
            TeamKey = checkpoint.Steps.LastOrDefault()?.TeamKey ?? checkpoint.RequestedTeamKey
        });
        checkpoint.KeyaccountContinuationsScheduled++;
        checkpoint.State = GoalExecutionState.Running;
        await ledger.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);
        return await ContinueAsync(RestoreRequest(checkpoint), checkpoint, retryInterrupted: false, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Extracts comparable terms from user input and persisted capability/team metadata without an embedded task vocabulary.</summary>
    /// <param name="text">Untrusted task or catalog description.</param>
    /// <returns>Normalized significant terms.</returns>
    private HashSet<string> Tokens(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new(StringComparer.OrdinalIgnoreCase);
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var buffer = new System.Text.StringBuilder();
        foreach (var character in text)
        {
            if (char.IsLetterOrDigit(character)) buffer.Append(char.ToLowerInvariant(character));
            else Flush();
        }
        Flush();
        return terms;

        void Flush()
        {
            if (buffer.Length >= 4) terms.Add(buffer.ToString());
            buffer.Clear();
        }
    }

    /// <summary>Scores catalog descriptions by concrete token overlap; no application task categories are hardcoded.</summary>
    /// <param name="goalTokens">Significant original goal terms.</param>
    /// <param name="descriptions">Saved team or registered function descriptions.</param>
    /// <returns>The count of matching terms across the descriptors.</returns>
    private int Match(HashSet<string> goalTokens, params string?[] descriptions)
    {
        var searchable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var description in descriptions)
            searchable.UnionWith(Tokens(description));
        return goalTokens.Count(term => searchable.Contains(term)
            || searchable.Any(candidate => candidate.Length >= 5 && term.Length >= 5
                && string.Equals(candidate[..5], term[..5], StringComparison.OrdinalIgnoreCase)));
    }
}
