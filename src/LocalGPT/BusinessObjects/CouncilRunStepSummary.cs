namespace LocalGPT.BusinessObjects;

/// <summary>Compact authenticated execution evidence from an existing Council spool record, without provider output text.</summary>
/// <param name="SortOrder">Stable recorded step order.</param>
/// <param name="Round">Logical round number.</param>
/// <param name="Phase">Recorded workflow phase.</param>
/// <param name="Role">Assigned role.</param>
/// <param name="ModelName">Provider-qualified executing participant or service identity.</param>
/// <param name="StartedAtUtc">Recorded start in UTC.</param>
/// <param name="CompletedAtUtc">Recorded completion in UTC.</param>
/// <param name="DurationSeconds">Measured execution duration in seconds.</param>
/// <param name="ContextCeiling">Configured effective context ceiling, not measured context consumption.</param>
/// <param name="OutputCeiling">Configured effective output ceiling, not measured generated tokens.</param>
/// <param name="InfrastructureFailure">Whether the recorded error is infrastructure-attributed.</param>
/// <param name="Error">Recorded failure diagnostic, if present.</param>
public sealed record CouncilRunStepSummary(int SortOrder, int Round, string Phase, string Role, string ModelName,
    DateTime StartedAtUtc, DateTime CompletedAtUtc, double DurationSeconds, int ContextCeiling, int OutputCeiling,
    bool InfrastructureFailure, string Error)
{
    /// <summary>Function name and outcome from the recorded coordinator gateway receipt, not inferred model prose.</summary>
    public string FunctionActivity { get; init; } = string.Empty;
}
