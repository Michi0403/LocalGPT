using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Provides the single reviewer-ranking policy shared by benchmark runtime and UI.</summary>
public interface IProviderModelReviewerPolicyService
{
    /// <summary>Returns the default reviewer priority for a provider-qualified model; lower values are preferred.</summary>
    /// <param name="model">Model value supplied to the provider model reviewer policy operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    int GetPriority(ProviderModelReference model);
}
