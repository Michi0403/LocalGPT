using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore;
namespace LocalGPT.Services;

/// <summary>Maintains searchable role/model tables as indexes of the authoritative saved team roles.</summary>
public sealed class CouncilRoleAssignmentIndexService
{
    public async Task SynchronizeAsync(LocalGptMemoryDbContext db, OrganicCouncilTeamDefinition team,
        CancellationToken cancellationToken = default)
    {
        var oldRoles = await db.CouncilRoleIndexes.Where(item => item.TeamKey == team.Key)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var oldBindings = await db.CouncilRoleModelBindings.Where(item => item.TeamKey == team.Key)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        OrganicCouncilRoleDefinition[] roles = team.IsDeleted ? [] : team.Roles
            .Where(role => !string.IsNullOrWhiteSpace(role.Role))
            .DistinctBy(role => role.Role, StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var binding in oldBindings)
        {
            var role = roles.FirstOrDefault(item => string.Equals(item.Role, binding.RoleName, StringComparison.OrdinalIgnoreCase));
            if (role is null || !(role.AssignedModelKeys ?? []).Contains(binding.ModelKey, StringComparer.OrdinalIgnoreCase))
                db.CouncilRoleModelBindings.Remove(binding);
        }
        foreach (var old in oldRoles)
            if (!roles.Any(role => string.Equals(role.Role, old.RoleName, StringComparison.OrdinalIgnoreCase)))
                db.CouncilRoleIndexes.Remove(old);
        foreach (var role in roles)
        {
            var indexed = oldRoles.FirstOrDefault(item => string.Equals(item.RoleName, role.Role, StringComparison.OrdinalIgnoreCase));
            if (indexed is null)
                db.CouncilRoleIndexes.Add(new CouncilRoleIndex { TeamKey = team.Key, RoleName = role.Role,
                    Responsibility = role.Responsibility ?? string.Empty, SelectionMode = role.AiSelectionMode });
            else
            {
                indexed.Responsibility = role.Responsibility ?? string.Empty;
                indexed.SelectionMode = role.AiSelectionMode;
            }
            foreach (var modelKey in (role.AssignedModelKeys ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item) && item.Length <= 1024)
                .Distinct(StringComparer.OrdinalIgnoreCase))
                if (!oldBindings.Any(binding => string.Equals(binding.RoleName, role.Role, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(binding.ModelKey, modelKey, StringComparison.OrdinalIgnoreCase)))
                    db.CouncilRoleModelBindings.Add(new CouncilRoleModelBinding
                    { TeamKey = team.Key, RoleName = role.Role, ModelKey = modelKey });
        }
    }
}
