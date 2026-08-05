using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public sealed record SafeTextDocument(string Name, string Text, string ContentHash, string EncodingName, string ContentType, IReadOnlyList<string> Warnings);

public interface ISafeTextDocumentService
{
    Task<SafeTextDocument> ReadAsync(string filePath, int maxCharacters = 500_000, CancellationToken cancellationToken = default);
    Task<ProjectDocumentImport> ImportAsync(Guid projectId, Guid? revisionId, string filePath, bool userConfirmed, CancellationToken cancellationToken = default);
}
