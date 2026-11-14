using System.Globalization;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services.ConfigurationSteward;

/// <summary>
/// Inspects the live Council model catalog and persisted team bindings. Newly installed Ollama models are usable
/// even when they are not yet preferred in AICore; exact role assignments remain user-owned configuration.
/// The steward offers a compare-and-set, human-approved replacement only when one unambiguous role key is stale.
/// </summary>
/// <param name="teams">Saved Council team configuration.</param>
/// <param name="providerModels">Live provider model discovery.</param>
/// <param name="staffing">Distinct-model staffing estimator.</param>
/// <param name="dbContextFactory">Persisted roles JSON for exact approved compare-and-set repairs.</param>
/// <param name="logger">Check diagnostics.</param>
public sealed class CouncilTeamStaffingHealthCheck(
    ICouncilTeamConfigurationService teams,
    IProviderModelRuntimeService providerModels,
    ICouncilTeamStaffingEstimator staffing,
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    ILogger<CouncilTeamStaffingHealthCheck> logger) : IConfigurationHealthCheck
{
    /// <inheritdoc />
    public string CheckKey => "team-staffing";

    /// <inheritdoc />
    public string Category => "teams";

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigurationFinding>> InspectAsync(ConfigurationStewardPolicy policy, CancellationToken cancellationToken = default)
    {
        try
        {
            var discovered = await providerModels.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
            // Local provider discovery knows whether a model is installed. "IsConfigured" is a saved preference,
            // not an installed-model requirement. Cloud routes keep their explicit configuration requirement.
            var usable = discovered.Where(candidate => candidate.IsInstalled || (candidate.IsConfigured && !candidate.IsLocal))
                .GroupBy(candidate => candidate.SelectionKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First()).ToList();
            var usableKeys = usable.Select(candidate => candidate.SelectionKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var identity = new ProviderModelIdentity();
            var findings = new List<ConfigurationFinding>();
            if (usable.Count == 0)
            {
                findings.Add(new ConfigurationFinding
                {
                    Key = "team-staffing:no-models", CheckKey = CheckKey, Category = Category, Severity = "Warning",
                    Title = "No usable Council models discovered",
                    Summary = "No installed local model or configured remote provider model was discovered. LocalGPT and linked PublisherStudio Council requests cannot start without participants.",
                    VerificationNote = "Inspect the live provider endpoint and Ollama model list. The steward never installs a model or enables an unapproved provider.",
                    Evidence = [new() { Source = "IProviderModelRuntimeService.GetCandidatesAsync", ObservedValue = "0 usable models", Reference = "Installed models or explicitly configured remote models" }]
                });
            }

            var configured = await teams.GetTeamsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var persisted = (await db.CouncilTeamConfigurations.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false))
                .ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);
            foreach (var team in configured)
            {
                var estimate = staffing.Estimate(team);
                if (estimate.MinimumDistinctModels > usable.Count)
                {
                    findings.Add(new ConfigurationFinding
                    {
                        Key = $"{CheckKey}:{team.Key}", CheckKey = CheckKey, Category = Category, Severity = "Info",
                        Title = $"Team \"{team.DisplayName}\" needs {estimate.MinimumDistinctModels} distinct AI models; {usable.Count} usable",
                        Summary = $"Roles {string.Join(", ", estimate.LimitingRoles)} require distinct models (group {estimate.LimitingGroup}). The goal planner must not silently reuse a model when this team's roles forbid it.",
                        VerificationNote = "A team editor change or additional model installation requires your decision; no saved roles are changed by this diagnosis.",
                        Evidence =
                        [
                            new() { Source = $"CouncilTeamConfigurations.{team.Key}", ObservedValue = estimate.MinimumDistinctModels.ToString(CultureInfo.InvariantCulture), Reference = "Required distinct model count" },
                            new() { Source = "Live provider model discovery", ObservedValue = usable.Count.ToString(CultureInfo.InvariantCulture), Reference = "Installed local or configured remote models" }
                        ]
                    });
                }

                var assignedRoles = team.Roles.Where(role => role.HumanParticipationMode != HumanParticipationMode.HumanOnly &&
                    (role.AiSelectionMode is CouncilRoleAiSelectionMode.AssignedModels or CouncilRoleAiSelectionMode.AssignedModelsRandomRange)).ToList();
                var stale = assignedRoles.SelectMany(role => role.AssignedModelKeys
                    .Where(key => !string.IsNullOrWhiteSpace(key) && !usableKeys.Contains(key) &&
                        identity.ResolveEquivalentCandidate(key, usable, out _) is null)
                    .Select(key => (Role: role, Key: key)))
                    .ToList();
                if (stale.Count > 0)
                {
                    var finding = new ConfigurationFinding
                    {
                        Key = $"team-model-assignment:{team.Key}", CheckKey = CheckKey, Category = Category,
                        Severity = "Warning", Title = $"Team \"{team.DisplayName}\" has unavailable saved model assignments",
                        Summary = $"{stale.Count} exact provider-qualified assignment(s) no longer match usable models. Newly downloaded Ollama models do not automatically replace an explicitly saved role binding.",
                        VerificationNote = "Only a user-approved choice may rebind an exact role model. Maintained system teams remain under their seed/reset and team-editor workflow; the steward does not rewrite their definitions.",
                        Evidence = stale.Select(item => new ConfigurationFindingEvidence
                        {
                            Source = $"CouncilTeamConfigurations.{team.Key}.Roles[{item.Role.Role}]",
                            ObservedValue = item.Key,
                            Reference = "No matching usable provider-qualified model in the current discovery snapshot"
                        }).ToList()
                    };
                    // A narrowly bounded user-owned team edit is safe to offer as an approval choice:
                    // one stale exact key in one role. Complex or seeded team migrations remain editor-owned.
                    if (!team.IsSystemSeed && stale.Count == 1 &&
                        persisted.TryGetValue(team.Key, out var row) && !row.IsDeleted)
                    {
                        var staleRole = stale[0].Role;
                        var replacementCandidates = usable.Where(candidate => candidate.IsInstalled &&
                            !staleRole.AssignedModelKeys.Contains(candidate.SelectionKey, StringComparer.OrdinalIgnoreCase) &&
                            (string.IsNullOrWhiteSpace(staleRole.DistinctAiAssignmentGroup) ||
                             !assignedRoles.Where(role => role != staleRole &&
                                    string.Equals(role.DistinctAiAssignmentGroup, staleRole.DistinctAiAssignmentGroup, StringComparison.OrdinalIgnoreCase))
                                 .SelectMany(role => role.AssignedModelKeys)
                                 .Contains(candidate.SelectionKey, StringComparer.OrdinalIgnoreCase)))
                            .Take(8).ToList();
                        foreach (var candidate in replacementCandidates)
                        {
                            var replacement = JsonSerializer.Deserialize<List<OrganicCouncilRoleDefinition>>(
                                row.RolesJson, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
                            var target = replacement.FirstOrDefault(role => string.Equals(role.Role, staleRole.Role, StringComparison.OrdinalIgnoreCase));
                            if (target is null || !target.AssignedModelKeys.Contains(stale[0].Key, StringComparer.OrdinalIgnoreCase))
                                continue;
                            target.AssignedModelKeys = target.AssignedModelKeys
                                .Select(key => string.Equals(key, stale[0].Key, StringComparison.OrdinalIgnoreCase)
                                    ? candidate.SelectionKey : key)
                                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                            finding.Options.Add(new ConfigurationRepairOption
                            {
                                Key = "rebind-" + finding.Options.Count.ToString(CultureInfo.InvariantCulture),
                                Label = $"Assign {candidate.SelectionKey} to {staleRole.Role}",
                                Description = "Replaces only the unavailable exact assignment in this user-owned team after human approval; other role and tool policies remain unchanged.",
                                Changes = [new ConfigurationRepairChange
                                {
                                    TargetKind = ConfigurationRepairChange.CouncilTeamRolesKind,
                                    TargetKey = row.Key,
                                    ExpectedCurrentValue = row.RolesJson,
                                    NewValue = JsonSerializer.Serialize(replacement, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                                }]
                            });
                        }
                        if (finding.Options.Count > 0)
                            finding.Options.Add(new ConfigurationRepairOption { Key = "keep", Label = "Keep the current exact assignment", Description = "Do not modify this team's roles." });
                    }
                    findings.Add(finding);
                }
            }
            logger.LogInformation("Configuration check {CheckKey} found {Count} team model issue(s) with {Available} live usable model(s).",
                CheckKey, findings.Count, usable.Count);
            return findings;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Configuration check {CheckKey} failed.", CheckKey);
            throw;
        }
    }
}
