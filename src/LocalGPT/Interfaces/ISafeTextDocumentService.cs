using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Represents a safe text document.
/// </summary>
public sealed record SafeTextDocument(string Name, string Text, string ContentHash, string EncodingName, string ContentType, IReadOnlyList<string> Warnings);

/// <summary>
/// Defines the safe text document service contract.
/// </summary>
public interface ISafeTextDocumentService
{
    /// <summary>
    /// Reads async.
    /// </summary>
    Task<SafeTextDocument> ReadAsync(string filePath, int maxCharacters = 500_000, CancellationToken cancellationToken = default);
    /// <summary>
    /// Imports async.
    /// </summary>
    Task<ProjectDocumentImport> ImportAsync(Guid projectId, Guid? revisionId, string filePath, bool userConfirmed, CancellationToken cancellationToken = default);
}
