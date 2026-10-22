namespace LocalGPT.Interfaces;

/// <summary>Identifies ZIP-compatible container formats and provides bounded, read-only semantic text extraction for learning.</summary>
public interface IArchivePackageService
{
    /// <summary>Checks a file's ZIP signature rather than assuming only a .zip extension can contain an archive.</summary>
    /// <param name="path">Candidate local file path.</param>
    /// <returns>True when the file starts with a recognized ZIP container signature.</returns>
    bool IsZipContainer(string path);

    /// <summary>Determines the semantic ZIP package family from its entries without executing or extracting content.</summary>
    /// <param name="path">ZIP-compatible local file path.</param>
    /// <returns>Format family for diagnostics and learning evidence.</returns>
    string IdentifyFamily(string path);

    /// <summary>Reads a bounded, resumable page of semantic text from one verified package.</summary>
    /// <param name="path">Local ZIP-compatible package.</param>
    /// <param name="characterOffset">Previous page's next character offset.</param>
    /// <param name="maxCharacters">Maximum visible characters in this page.</param>
    /// <param name="cancellationToken">Cancellation for safe reading.</param>
    /// <returns>One page and whether further semantic content remains.</returns>
    Task<ArchiveSemanticPage> ReadSemanticPageAsync(string path, long characterOffset, int maxCharacters, CancellationToken cancellationToken = default);

    /// <summary>Reads approved textual parts inside ZIP-compatible packages with configured bomb/entry limits.</summary>
    /// <param name="path">Local ZIP-compatible package.</param>
    /// <param name="maxCharacters">Maximum characters to return for model context.</param>
    /// <param name="cancellationToken">Cancellation for bounded extraction.</param>
    /// <returns>Semantic text annotated with the names of the package parts that supplied it.</returns>
    Task<string> ReadSemanticTextAsync(string path, int maxCharacters, CancellationToken cancellationToken = default);

    /// <summary>Rejects symbolic links or special Unix entries masquerading as ordinary ZIP files.</summary>
    /// <param name="externalAttributes">External attributes from one ZIP entry.</param>
    /// <returns>True for link or special-file modes that must not be extracted.</returns>
    bool IsUnsafeEntry(int externalAttributes);
}

/// <summary>Source-backed page whose offset is counted through normalized package text, not compressed ZIP bytes.</summary>
/// <param name="Text">Semantic page content.</param>
/// <param name="HasMore">Whether additional normalized package content remains.</param>
public sealed record ArchiveSemanticPage(string Text, bool HasMore);
