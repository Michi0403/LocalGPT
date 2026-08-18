namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents the machine-derived coverage truth for one provider-model benchmark report.
/// </summary>
/// <remarks>
/// Coverage is calculated only from provider-qualified target results. Council/model prose is not an input.
/// </remarks>
public sealed class ProviderModelBenchmarkCoverageSnapshot
{
    /// <summary>Gets the distinct provider-qualified targets returned by the benchmark engine.</summary>
    public int AttemptedTargetCount { get; init; }

    /// <summary>Gets the distinct targets that produced a successful measured recommendation.</summary>
    public int SuccessfulTargetCount { get; init; }

    /// <summary>Gets the number of attempted targets without a successful measured recommendation.</summary>
    public int UnresolvedTargetCount => UnresolvedSelectionKeys.Count;

    /// <summary>Gets the exact provider-qualified identities returned by the measurement engine.</summary>
    public List<string> AttemptedSelectionKeys { get; init; } = [];

    /// <summary>Gets the exact provider-qualified identities with successful measured recommendations.</summary>
    public List<string> SuccessfulSelectionKeys { get; init; } = [];

    /// <summary>Gets the exact provider-qualified identities without successful measured recommendations.</summary>
    public List<string> UnresolvedSelectionKeys { get; init; } = [];

    /// <summary>Gets a value indicating whether the mechanically derived arithmetic invariant is satisfied.</summary>
    public bool IsArithmeticConsistent => AttemptedTargetCount - SuccessfulTargetCount == UnresolvedTargetCount;

    /// <summary>
    /// Initializes a deterministic coverage snapshot from benchmark results without consulting Council reviewer text.
    /// </summary>
    /// <param name="report">The provider benchmark report whose measured target results are authoritative.</param>
    public ProviderModelBenchmarkCoverageSnapshot(ProviderModelBenchmarkReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var targets = report.Targets
            .GroupBy(target => target.Model.SelectionKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        AttemptedSelectionKeys = targets
            .Select(target => target.Model.SelectionKey)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        SuccessfulSelectionKeys = targets
            .Where(HasSuccessfulMeasuredRecommendation)
            .Select(target => target.Model.SelectionKey)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var successfulSet = SuccessfulSelectionKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        UnresolvedSelectionKeys = AttemptedSelectionKeys
            .Where(value => !successfulSet.Contains(value))
            .ToList();
        AttemptedTargetCount = AttemptedSelectionKeys.Count;
        SuccessfulTargetCount = SuccessfulSelectionKeys.Count;
    }

    /// <summary>Applies the deterministic successful-measured-recommendation rule to one target.</summary>
    /// <param name="target">A provider-qualified benchmark target result.</param>
    /// <returns><see langword="true"/> only when the target completed without a target error and produced a recommendation profile.</returns>
    private bool HasSuccessfulMeasuredRecommendation(ProviderModelBenchmarkTargetResult target)
    {
        return string.IsNullOrWhiteSpace(target.Error)
            && !string.IsNullOrWhiteSpace(target.Recommendation.ProfileName);
    }
}
