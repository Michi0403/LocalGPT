using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>
/// Initial Council team choice for a goal. Keyword overlap alone is weak evidence (a help question once matched a
/// game team), so without a semantic plan the requested team is kept unless required capabilities or a strong,
/// configurable match demand another one. Teams that cannot be staffed with the run's models are never auto-selected.
/// </summary>
public sealed partial class GoalExecutionService
{
    /// <summary>Runtime value: minimum keyword score for leaving the requested team without a semantic plan.</summary>
    private const string CatalogFallbackMinimumTeamScoreKey = "GoalCatalogFallbackMinimumTeamScore";

    /// <summary>Chooses the initial goal team from the saved catalog, the caller's requested team and the run's model count.</summary>
    /// <param name="request">Goal request including the requested team and selection preservation flag.</param>
    /// <param name="configuredTeams">Saved, enabled Council teams.</param>
    /// <param name="ranked">Teams ranked by missing required capabilities, then keyword score.</param>
    /// <param name="hasRequirements">Whether the caller required specific functions or capabilities.</param>
    /// <param name="availableModelCount">Distinct AI models the run can use; 0 when none are known.</param>
    /// <param name="missing">Missing-requirement list that receives explicit selection problems.</param>
    /// <returns>The selected team, or <see langword="null"/> when no team is available.</returns>
    private OrganicCouncilTeamDefinition? SelectGoalTeam(
        GoalExecutionRequest request,
        IReadOnlyList<OrganicCouncilTeamDefinition> configuredTeams,
        IReadOnlyList<(OrganicCouncilTeamDefinition Team, int Missing, int Score)> ranked,
        bool hasRequirements,
        int availableModelCount,
        List<string> missing)
    {
        try
        {
            var requestedKey = request.CouncilRequest.CouncilTeamKey;
            var requested = configuredTeams.FirstOrDefault(team => string.Equals(team.Key, requestedKey, StringComparison.OrdinalIgnoreCase));
            if (request.PreserveSelectedTeam)
            {
                if (requested is null)
                {
                    missing.Add("SelectedCouncilTeam");
                    return null;
                }
                if (availableModelCount > 0 && !staffing.CanStaff(requested, availableModelCount))
                {
                    var estimate = staffing.Estimate(requested);
                    logger.LogWarning(
                        "Selected Council team {TeamKey} needs at least {Required} distinct AI models (group {Group}: {Roles}) but the run has {Available}; the goal is blocked before Council role assignment fails.",
                        requested.Key, estimate.MinimumDistinctModels, estimate.LimitingGroup, string.Join(", ", estimate.LimitingRoles), availableModelCount);
                    missing.Add("SelectedCouncilTeamNeedsMoreDistinctModels");
                }
                return requested;
            }

            bool Staffable(OrganicCouncilTeamDefinition team) => availableModelCount <= 0 || staffing.CanStaff(team, availableModelCount);
            var candidates = ranked.Where(entry => Staffable(entry.Team)).ToList();
            var minimumScore = ReadCatalogFallbackMinimumTeamScore();
            var chosen = (hasRequirements ? candidates.Where(entry => entry.Missing == 0).Select(entry => entry.Team).FirstOrDefault() : null)
                ?? candidates.Where(entry => entry.Score >= minimumScore).Select(entry => entry.Team).FirstOrDefault()
                ?? (requested is not null && Staffable(requested) ? requested : null)
                ?? configuredTeams.FirstOrDefault(team => string.Equals(team.Key, "general", StringComparison.OrdinalIgnoreCase) && Staffable(team))
                ?? candidates.Select(entry => entry.Team).FirstOrDefault();
            if (requested is not null && chosen is not null && !string.Equals(requested.Key, chosen.Key, StringComparison.OrdinalIgnoreCase))
                logger.LogInformation(
                    "Goal routes from requested team {Requested} to {Chosen} (requirements {HasRequirements}, available models {Available}, minimum keyword score {MinimumScore}).",
                    requested.Key, chosen.Key, hasRequirements, availableModelCount, minimumScore);
            var skipped = ranked.Count - candidates.Count;
            if (skipped > 0)
                logger.LogInformation("Goal team selection skipped {Skipped} team(s) that need more than {Available} distinct AI model(s).", skipped, availableModelCount);
            return chosen;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Selecting the goal Council team failed.");
            throw;
        }
    }

    /// <summary>Reads the configured minimum keyword score for leaving the requested team.</summary>
    /// <returns>The configured score, or 6 when the runtime value is unavailable.</returns>
    private int ReadCatalogFallbackMinimumTeamScore()
    {
        try
        {
            return Math.Max(1, runtimePolicy.GetInt(CatalogFallbackMinimumTeamScoreKey));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Runtime value {Key} is unavailable; the goal team selection uses a minimum keyword score of 6.", CatalogFallbackMinimumTeamScoreKey);
            return 6;
        }
    }
}
