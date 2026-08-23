using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines persisted relationships between Council knowledge and reusable regex patterns so recognition semantics remain
/// structured, queryable, and user-controlled rather than being hidden inside free-form knowledge text.
/// </summary>
public interface IKnowledgeRegexLinkService
{
    /// <summary>Lists the regex relationships currently associated with one Council knowledge entry.</summary>
    /// <param name="knowledgeEntryId">Identifier of the Council knowledge entry.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The persisted relationships ordered for deterministic presentation.</returns>
    Task<IReadOnlyList<CouncilKnowledgeRegexPatternLink>> GetForKnowledgeAsync(
        Guid knowledgeEntryId,
        CancellationToken cancellationToken = default);

    /// <summary>Tests enabled regex relationships for one knowledge entry against caller-supplied text without storing the text.</summary>
    /// <param name="knowledgeEntryId">Identifier of the Council knowledge entry whose enabled recognition links should be tested.</param>
    /// <param name="input">Caller-supplied text to evaluate against linked regex patterns.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The successful recognition matches in deterministic relationship order.</returns>
    Task<IReadOnlyList<KnowledgeRegexRecognitionMatch>> TestRecognitionAsync(
        Guid knowledgeEntryId,
        string input,
        CancellationToken cancellationToken = default);

    /// <summary>Persists the caller-confirmed semantic role of one reusable regex pattern for one Council knowledge note.</summary>
    /// <param name="request">User-confirmed relationship values to persist.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The saved relationship including its regex navigation.</returns>
    Task<CouncilKnowledgeRegexPatternLink> SaveAsync(
        SaveKnowledgeRegexPatternLinkRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes one explicitly confirmed knowledge-to-regex relationship without deleting either endpoint.</summary>
    /// <param name="knowledgeEntryId">Identifier of the Council knowledge entry.</param>
    /// <param name="regexPatternId">Identifier of the regex pattern.</param>
    /// <param name="userConfirmed">Whether the user explicitly confirmed the consequential unlink operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the link has been removed or was already absent.</returns>
    Task DeleteAsync(
        Guid knowledgeEntryId,
        int regexPatternId,
        bool userConfirmed,
        CancellationToken cancellationToken = default);
}
