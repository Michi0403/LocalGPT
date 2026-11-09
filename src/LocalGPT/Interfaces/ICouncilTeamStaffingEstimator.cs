using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Estimates whether a saved Council team can be staffed with a given number of distinct AI models.</summary>
public interface ICouncilTeamStaffingEstimator
{
    /// <summary>Computes the minimum distinct AI models the team's distinct-assignment groups reserve.</summary>
    /// <param name="team">Saved Council team.</param>
    /// <returns>The conservative staffing estimate.</returns>
    CouncilTeamStaffingEstimate Estimate(OrganicCouncilTeamDefinition team);

    /// <summary>Determines whether the team can be staffed with the given number of distinct AI models.</summary>
    /// <param name="team">Saved Council team.</param>
    /// <param name="availableModelCount">Distinct AI models available to the run.</param>
    /// <returns><see langword="true"/> when the estimate does not exceed the available models.</returns>
    bool CanStaff(OrganicCouncilTeamDefinition team, int availableModelCount);
}
