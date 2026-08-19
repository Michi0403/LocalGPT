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
    /// <value>The attempted target count value exposed by <see cref="ProviderModelBenchmarkCoverageSnapshot"/>.</value>
    public int AttemptedTargetCount { get; init; }

    /// <summary>
    /// Gets or sets the successful target count that quantifies the associated provider model benchmark coverage snapshot data.
    /// </summary>
    /// <value>The successful target count value exposed by <see cref="ProviderModelBenchmarkCoverageSnapshot"/>.</value>
    public int SuccessfulTargetCount { get; init; }

    /// <summary>Gets the number of attempted targets without a successful measured recommendation.</summary>
    /// <value>The unresolved target count value exposed by <see cref="ProviderModelBenchmarkCoverageSnapshot"/>.</value>
    public int UnresolvedTargetCount => UnresolvedSelectionKeys.Count;

    /// <summary>Gets the exact provider-qualified identities returned by the measurement engine.</summary>
    /// <value>The attempted selection keys value exposed by <see cref="ProviderModelBenchmarkCoverageSnapshot"/>.</value>
    public List<string> AttemptedSelectionKeys { get; init; } = [];

    /// <summary>
    /// Gets or sets the successful selection keys collection maintained or exposed by this provider model benchmark coverage snapshot instance for downstream processing.
    /// </summary>
    /// <value>The successful selection keys value exposed by <see cref="ProviderModelBenchmarkCoverageSnapshot"/>.</value>
    public List<string> SuccessfulSelectionKeys { get; init; } = [];

    /// <summary>
    /// Gets or sets the unresolved selection keys collection maintained or exposed by this provider model benchmark coverage snapshot instance for downstream processing.
    /// </summary>
    /// <value>The unresolved selection keys value exposed by <see cref="ProviderModelBenchmarkCoverageSnapshot"/>.</value>
    public List<string> UnresolvedSelectionKeys { get; init; } = [];

    /// <summary>Gets a value indicating whether the mechanically derived arithmetic invariant is satisfied.</summary>
    /// <value>The is arithmetic consistent value exposed by <see cref="ProviderModelBenchmarkCoverageSnapshot"/>.</value>
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

    /// <summary>
    /// Determines whether successful measured recommendation for <see cref="ProviderModelBenchmarkCoverageSnapshot"/>, keeping the operation consistent with the state and invariants of the surrounding provider model benchmark coverage snapshot workflow.
    /// </summary>
    /// <param name="target">A provider-qualified benchmark target result.</param>
    /// <returns><see langword="true"/> only when the target completed without a target error and produced a recommendation profile.</returns>
    private bool HasSuccessfulMeasuredRecommendation(ProviderModelBenchmarkTargetResult target)
    {
        return string.IsNullOrWhiteSpace(target.Error)
            && !string.IsNullOrWhiteSpace(target.Recommendation.ProfileName);
    }
}
