using System;
using System.Collections.Generic;
using System.Linq;

namespace LocalGPT.Services;

/// <summary>
/// Provides the Council team editor with normalized PublisherStudio capability names.
/// This is a catalog projection only; it does not grant invocation permissions.
/// </summary>
public sealed partial class CouncilTextService
{
    /// <summary>
    /// Selects PublisherStudio protocol capability keys from saved team preferences or
    /// live peer advertisements, ignoring missing keys and eliminating duplicates.
    /// </summary>
    /// <param name="keys">Capability names from the persisted and live registries.</param>
    /// <returns>Unique PublisherStudio capability names in stable presentation order.</returns>
    public IReadOnlyList<string> FilterPublisherOrganicCapabilityKeys(IEnumerable<string>? keys)
    {
        try
        {
            serviceLogger.LogTrace("Council text operation {Operation} started.", nameof(FilterPublisherOrganicCapabilityKeys));
            return (keys ?? Enumerable.Empty<string>())
                .Where(key => !string.IsNullOrWhiteSpace(key) &&
                              key.StartsWith("publisher.", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception exception)
        {
            serviceLogger.LogError(exception, "Council text operation {Operation} failed.", nameof(FilterPublisherOrganicCapabilityKeys));
            throw;
        }
    }
}
