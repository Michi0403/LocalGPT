using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Represents safe text state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
/// <param name="Name">Name value supplied to the safe text operation and used when producing its result.</param>
/// <param name="Text">Text value supplied to the safe text operation and used when producing its result.</param>
/// <param name="ContentHash">Content hash value supplied to the safe text operation and used when producing its result.</param>
/// <param name="EncodingName">Encoding name value supplied to the safe text operation and used when producing its result.</param>
/// <param name="ContentType">Content type value supplied to the safe text operation and used when producing its result.</param>
/// <param name="Warnings">String dependency used by the safe text workflow to provide the corresponding application capability.</param>
public sealed record SafeTextDocument(string Name, string Text, string ContentHash, string EncodingName, string ContentType, IReadOnlyList<string> Warnings);

/// <summary>
/// Defines the contract for safe text document behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ISafeTextDocumentService
{
    /// <summary>
    /// Performs read as part of the safe text document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="filePath">File path value supplied to the safe text document operation and used when producing its result.</param>
    /// <param name="maxCharacters">Max characters value supplied to the safe text document operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The safe text document produced by the operation.</returns>
    Task<SafeTextDocument> ReadAsync(string filePath, int maxCharacters = 500_000, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs import as part of the safe text document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="filePath">File path value supplied to the safe text document operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project document import produced by the operation.</returns>
    Task<ProjectDocumentImport> ImportAsync(Guid projectId, Guid? revisionId, string filePath, bool userConfirmed, CancellationToken cancellationToken = default);
}
