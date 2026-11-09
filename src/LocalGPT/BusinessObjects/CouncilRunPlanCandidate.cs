using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a council run plan candidate application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Plan">Plan value supplied to the council run plan candidate operation and used when producing its result.</param>
/// <param name="Revision">Revision value supplied to the council run plan candidate operation and used when producing its result.</param>
/// <param name="IsEnabled">Value indicating whether enabled should apply to this operation.</param>
internal sealed record CouncilRunPlanCandidate(
    CouncilHardwareRoadPlan Plan,
    long Revision,
    bool IsEnabled);
