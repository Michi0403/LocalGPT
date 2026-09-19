using System.Text;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services
{
    /// <summary>Owns the explicit prompt-context boundary used by self-contained Council presets.</summary>
    public sealed partial class MultiModelCouncilService
    {
        /// <summary>Returns whether the persisted team explicitly requests role-isolated context.</summary>
        private bool UsesRoleIsolatedContext(OrganicCouncilTeamDefinition team)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(team);
                return team.PreferredCapabilities.Contains(CouncilContextCapabilities.RoleIsolated, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Resolving the Council role-context boundary failed for team {TeamKey}.", team?.Key);
                throw;
            }
        }

        /// <summary>Builds the deliberately narrow bootstrap supplied to self-contained role workflows.</summary>
        private string BuildRoleIsolatedBootstrap(OrganicCouncilTeamDefinition team)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(team);
                var builder = new StringBuilder()
                    .AppendLine("Council role-isolated context")
                    .AppendLine("This persisted team is self-contained. Execute only the assigned role and workflow evidence for this run.")
                    .AppendLine("Do not infer a repository, project, Minecraft/modding task, prior chat objective, Markdown/file inventory or external project context unless the current workflow role explicitly supplies it as task evidence.")
                    .AppendLine("Do not change roles because unrelated capabilities, model names or remembered project material happen to be available elsewhere in LocalGPT.")
                    .Append("Team: ").AppendLine(team.DisplayName)
                    .Append("Purpose: ").AppendLine(team.Purpose);
                if (team.ArchitectureContracts.Count > 0)
                {
                    builder.AppendLine("Team contracts:");
                    foreach (var contract in team.ArchitectureContracts)
                        builder.Append("- ").AppendLine(contract);
                }
                return builder.ToString().Trim();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Building role-isolated Council context failed for team {TeamKey}.", team?.Key);
                throw;
            }
        }
    }
}
