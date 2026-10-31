using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace LocalGPT.Services;

/// <summary>Reverse model-to-role assignments; execution policy stays owned by confirmed Council team definitions.</summary>
public sealed class CouncilRoleAssignmentService(
    ICouncilTeamConfigurationService teams, IMultiModelCouncilService council,
    IDbContextFactory<LocalGptMemoryDbContext> database, CouncilRoleAssignmentIndexService index)
    : ICouncilRoleAssignmentService
{
    public async Task<IReadOnlyList<CouncilRoleAssignmentChoice>> ListForModelAsync(string modelKey, CancellationToken cancellationToken = default)
    {
        var saved = await teams.GetTeamsAsync(includeDisabled: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        await using var db = await database.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        foreach (var team in saved)
            await index.SynchronizeAsync(db, team, cancellationToken).ConfigureAwait(false);
        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return saved.Where(team => !team.IsDeleted)
            .OrderBy(team => team.DisplayName, StringComparer.OrdinalIgnoreCase)
            .SelectMany(team => team.Roles.Where(role => role.HumanParticipationMode != HumanParticipationMode.HumanOnly)
                .Select(role => new CouncilRoleAssignmentChoice(team.Key, team.DisplayName, role.Role,
                    role.AiSelectionMode, (role.AssignedModelKeys ?? []).Contains(modelKey, StringComparer.OrdinalIgnoreCase),
                    team.IsSystemSeed, team.IsEnabled))).ToArray();
    }

    public async Task<OrganicCouncilTeamDefinition> AssignAsync(CouncilRoleAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.UserConfirmed) throw new InvalidOperationException("Fresh operator confirmation is required.");
        if (string.IsNullOrWhiteSpace(request.ModelKey) || string.IsNullOrWhiteSpace(request.TeamKey))
            throw new ArgumentException("Exact provider-qualified model identity and team are required.");
        var candidates = await council.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
        if (!candidates.Any(item => item.IsInstalled && string.Equals(item.SelectionKey, request.ModelKey, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Selected model is not reachable on its exact host. Refresh model inventory.");
        var team = await teams.FindTeamAsync(request.TeamKey, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The team no longer exists or is disabled.");
        var selected = (request.RoleNames ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (selected.Any(name => !team.Roles.Any(role => string.Equals(role.Role, name, StringComparison.OrdinalIgnoreCase)
            && role.HumanParticipationMode != HumanParticipationMode.HumanOnly)))
            throw new InvalidOperationException("Selected role is unavailable or human-only.");
        var changed = false;
        foreach (var role in team.Roles.Where(item => item.HumanParticipationMode != HumanParticipationMode.HumanOnly))
        {
            role.AssignedModelKeys ??= [];
            var assigned = role.AssignedModelKeys.Contains(request.ModelKey, StringComparer.OrdinalIgnoreCase);
            var wanted = selected.Contains(role.Role);
            if (assigned && !wanted)
            {
                if (role.AssignedModelKeys.Count == 1 &&
                    role.AiSelectionMode is CouncilRoleAiSelectionMode.AssignedModels or CouncilRoleAiSelectionMode.AssignedModelsRandomRange)
                    throw new InvalidOperationException($"Cannot empty the required model pool for role '{role.Role}'. Change its mode in Council Teams first.");
                role.AssignedModelKeys.RemoveAll(key => string.Equals(key, request.ModelKey, StringComparison.OrdinalIgnoreCase));
                changed = true;
            }
            else if (!assigned && wanted)
            {
                role.AssignedModelKeys.Add(request.ModelKey);
                changed = true;
            }
            if (wanted && request.RestrictSelectedRolesToAssignedPool &&
                role.AiSelectionMode is not CouncilRoleAiSelectionMode.AssignedModels and not CouncilRoleAiSelectionMode.AssignedModelsRandomRange)
            {
                role.AiSelectionMode = CouncilRoleAiSelectionMode.AssignedModels;
                changed = true;
            }
        }
        if (!changed) return team;
        // The authoritative save creates a user-owned copy of any supplied seed, preserving the template.
        return await teams.SaveAsync(new SaveCouncilTeamConfigurationRequest
        { Team = team, IsEnabled = team.IsEnabled, UserConfirmed = true }, cancellationToken).ConfigureAwait(false);
    }
}
