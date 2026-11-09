using System.Globalization;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services.ConfigurationSteward;

/// <summary>
/// Reports enabled Council teams whose distinct-assignment roles need more AI models than are installed. Goal
/// planning already skips such teams automatically; this report-only finding tells the user why and what helps.
/// </summary>
/// <param name="teams">Saved Council teams.</param>
/// <param name="providerModels">Provider runtime that lists installed and configured models.</param>
/// <param name="staffing">Distinct-model staffing estimate.</param>
/// <param name="logger">Logger for check diagnostics.</param>
public sealed class CouncilTeamStaffingHealthCheck(
    ICouncilTeamConfigurationService teams,
    IProviderModelRuntimeService providerModels,
    ICouncilTeamStaffingEstimator staffing,
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
            var installed = (await providerModels.GetCandidatesAsync(cancellationToken).ConfigureAwait(false))
                .Count(candidate => candidate.IsInstalled && candidate.IsConfigured);
            var findings = new List<ConfigurationFinding>();
            foreach (var team in await teams.GetTeamsAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                var estimate = staffing.Estimate(team);
                if (estimate.MinimumDistinctModels <= installed)
                    continue;
                findings.Add(new ConfigurationFinding
                {
                    Key = $"{CheckKey}:{team.Key}",
                    CheckKey = CheckKey,
                    Category = Category,
                    Severity = "Info",
                    Title = $"Team \"{team.DisplayName}\" needs {estimate.MinimumDistinctModels} different AI models; {installed} installed",
                    Summary = $"Roles {string.Join(", ", estimate.LimitingRoles)} share the distinct model group \"{estimate.LimitingGroup}\" without fallback, so each needs its own model. Goal planning skips this team until enough models are available; selecting it explicitly stops before the run.",
                    VerificationNote = "The count is a lower bound computed from the saved roles; a run can still fail if fewer models are selected for it than are installed. Installing models or allowing shared models in the team editor are your decisions; the steward does not change team definitions.",
                    Evidence =
                    [
                        new() { Source = $"CouncilTeamConfigurations.{team.Key}", ObservedValue = $"{estimate.MinimumDistinctModels} distinct models (group {estimate.LimitingGroup})", Reference = "Council role assignment rule for distinct groups without fallback" },
                        new() { Source = "Installed and configured provider models", ObservedValue = installed.ToString(CultureInfo.InvariantCulture), Reference = "IProviderModelRuntimeService.GetCandidatesAsync" }
                    ]
                });
                if (findings.Count >= Math.Max(1, policy.MaximumFindings))
                    break;
            }
            logger.LogDebug("Configuration check {CheckKey} found {Count} team(s) that need more than {Installed} installed model(s).", CheckKey, findings.Count, installed);
            return findings;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Configuration check {CheckKey} was canceled.", CheckKey);
            else
                logger.LogError(exception, "Configuration check {CheckKey} failed.", CheckKey);
            throw;
        }
    }
}
