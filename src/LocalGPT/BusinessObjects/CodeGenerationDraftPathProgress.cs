namespace LocalGPT.BusinessObjects;

/// <summary>Source-path evidence for a draft progress display; a stored candidate is not a build receipt.</summary>
/// <param name="Path">Canonical manifest path.</param>
/// <param name="Sha256">Current stored candidate hash, or empty when absent.</param>
/// <param name="ModelName">Last assigned provider-qualified route.</param>
/// <param name="Attempt">Number of the last reserved attempt.</param>
/// <param name="InspectionAccepted">Whether an exact current candidate and dependency revision has an accepted inspection.</param>
/// <param name="Diagnostic">Last observed assignment or inspection diagnostic.</param>
/// <param name="Dependencies">Canonical prerequisite paths.</param>
/// <param name="Assignments">Persisted assignment history without source content.</param>
/// <param name="Inspections">Persisted inspection decisions without source content.</param>
public sealed record CodeGenerationDraftPathProgress(string Path, string Sha256, string ModelName, int Attempt,
    bool InspectionAccepted, string Diagnostic, IReadOnlyList<string> Dependencies,
    IReadOnlyList<CodeGenerationDraftAssignment> Assignments, IReadOnlyList<CodeGenerationDraftInspectionDecision> Inspections)
{
    /// <summary>Display-ready prerequisite paths prepared by the workflow service for the progress panel.</summary>
    public string DependencyDisplay { get; init; } = string.Empty;
}
