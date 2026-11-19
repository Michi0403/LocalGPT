using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Provides common identifier computation without granting filesystem, process or database write access.</summary>
public interface IGuidUtilityService
{
    /// <summary>Generates or validates an unbound GUID.</summary>
    /// <param name="request">Bound operation and optional value to validate.</param>
    /// <returns>Canonical computation result, not an entity lookup.</returns>
    GuidUtilityResult Execute(GuidUtilityRequest request);
}
