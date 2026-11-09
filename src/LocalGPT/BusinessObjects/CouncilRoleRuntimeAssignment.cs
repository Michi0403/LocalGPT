using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a council role runtime assignment application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="RoleName">Role name value supplied to the council role runtime assignment operation and used when producing its result.</param>
/// <param name="Definition">Definition value supplied to the council role runtime assignment operation and used when producing its result.</param>
/// <param name="AiParticipants">String dependency used by the council role runtime assignment workflow to provide the corresponding application capability.</param>
internal sealed record CouncilRoleRuntimeAssignment(
    string RoleName,
    OrganicCouncilRoleDefinition? Definition,
    IReadOnlyList<string> AiParticipants)
{
    /// <summary>
    /// Gets the human participation mode value that forms part of the council role runtime assignment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The human participation mode value exposed by <see cref="CouncilRoleRuntimeAssignment"/>.</value>
    public HumanParticipationMode HumanParticipationMode =>
        Definition?.HumanParticipationMode ?? global::LocalGPT.BusinessObjects.HumanParticipationMode.None;

    /// <summary>
    /// Gets the AI selection description value that forms part of the council role runtime assignment state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The AI selection description value exposed by <see cref="CouncilRoleRuntimeAssignment"/>.</value>
    public string AiSelectionDescription => HumanParticipationMode == global::LocalGPT.BusinessObjects.HumanParticipationMode.HumanOnly
        ? "no AI members (human-only role)"
        : Definition is null || Definition.AiSelectionMode == CouncilRoleAiSelectionMode.AllSelected
            ? $"all {AiParticipants.Count} selected AI member(s)"
            : Definition.AiSelectionMode == CouncilRoleAiSelectionMode.AssignedModels
                ? $"{AiParticipants.Count} provider-bound AI member(s)"
                : Definition.AiSelectionMode == CouncilRoleAiSelectionMode.AssignedModelsRandomRange
                    ? $"{AiParticipants.Count} deterministic-random provider-bound AI invocation(s)"
                    : $"{AiParticipants.Count} deterministic-random AI member(s)";
}
