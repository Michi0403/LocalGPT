namespace LocalGPT.BusinessObjects;

/// <summary>One reserved source-file assignment and the bounded, project-owned evidence needed to author it.</summary>
/// <param name="Assignment">Persisted attempt identity and provider route.</param>
/// <param name="Goal">Original draft goal.</param>
/// <param name="SharedContract">Common interface and dependency conventions established by the architect.</param>
/// <param name="WorkOrder">Instructions for the assigned path only.</param>
/// <param name="Dependencies">Exact previously accepted source of declared direct dependencies.</param>
/// <param name="PreviousSource">Prior candidate source when repairing an existing draft file.</param>
public sealed record CodeGenerationDraftWorkPacket(
    CodeGenerationDraftAssignment Assignment,
    string Goal,
    string SharedContract,
    string WorkOrder,
    IReadOnlyList<CodeGenerationFileSpec> Dependencies,
    string PreviousSource);
