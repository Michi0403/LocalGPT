namespace LocalGPT.BusinessObjects;

/// <summary>Candidate file evidence for an independent reviewer; the source and dependency hashes bind the decision.</summary>
/// <param name="DraftId">Exact writable draft identity.</param>
/// <param name="Path">Canonical manifest path.</param>
/// <param name="Author">Actual provider-qualified author, if available.</param>
/// <param name="Sha256">Hash of the current candidate bytes.</param>
/// <param name="Source">Actual stored candidate content.</param>
/// <param name="Goal">Original development goal.</param>
/// <param name="WorkOrder">Manifest instructions for the file.</param>
/// <param name="SharedContract">Architect's shared interface contract.</param>
/// <param name="Dependencies">Actual accepted direct dependency source.</param>
/// <param name="DependencyHashes">Exact dependency hashes for this inspection.</param>
/// <param name="PreviousReviewers">Routes already attempted for these exact bytes and dependencies.</param>
public sealed record CodeGenerationDraftInspection(Guid DraftId, string Path, string Author, string Sha256,
    string Source, string Goal, string WorkOrder, string SharedContract, IReadOnlyList<CodeGenerationFileSpec> Dependencies,
    IReadOnlyDictionary<string, string> DependencyHashes, IReadOnlyList<string> PreviousReviewers);
