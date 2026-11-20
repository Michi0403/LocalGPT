namespace LocalGPT.BusinessObjects;

/// <summary>Server-owned correlation for one Council execution, independent of the mutable Chat selection.</summary>
/// <param name="ChatRunId">Stable parent Chat run identifier, not a database conversation or project identifier.</param>
/// <param name="ProjectId">Persisted project selected or provisioned for this execution, or null when unbound.</param>
/// <param name="ProjectRevisionId">Persisted revision of the bound project, if available.</param>
public sealed record CouncilExecutionIdentity(Guid ChatRunId, Guid? ProjectId, Guid? ProjectRevisionId);
