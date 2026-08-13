using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates safe text document behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the safe text document workflow to provide the corresponding application capability.</param>
/// <param name="databaseInitializer">Database initialization service dependency used by the safe text document workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class SafeTextDocumentService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<SafeTextDocumentService> logger) : ISafeTextDocumentService
{
    /// <summary>
    /// Defines the max bytes constant used by <see cref="SafeTextDocumentService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaxBytes = 8 * 1024 * 1024;
    /// <summary>
    /// Gets the allowed extensions value that forms part of the safe text document state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The allowed extensions value exposed by <see cref="SafeTextDocumentService"/>.</value>
    private FrozenSet<string> AllowedExtensions { get; } = new[]
    {
        ".txt", ".md", ".markdown", ".rst", ".csv", ".tsv", ".json", ".jsonl", ".xml", ".yaml", ".yml", ".toml", ".ini", ".cfg", ".config",
        ".cs", ".csproj", ".sln", ".slnx", ".razor", ".css", ".js", ".ts", ".tsx", ".jsx", ".html", ".htm", ".sql", ".ps1", ".sh",
        ".java", ".kt", ".kts", ".py", ".go", ".rs", ".cpp", ".c", ".h", ".hpp", ".gradle", ".properties", ".mcfunction"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Performs read as part of the safe text document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="filePath">File path value supplied to the safe text document operation and used when producing its result.</param>
    /// <param name="maxCharacters">Max characters value supplied to the safe text document operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The safe text document produced by the operation.</returns>
    public async Task<SafeTextDocument> ReadAsync(string filePath, int maxCharacters = 500_000, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            var fullPath = Path.GetFullPath(filePath);
            var extension = Path.GetExtension(fullPath);
            if (!AllowedExtensions.Contains(extension))
                throw new InvalidOperationException($"The extension '{extension}' is not in the harmless text-document allowlist.");

            var info = new FileInfo(fullPath);
            if (!info.Exists)
                throw new FileNotFoundException("The selected text document was not found.", fullPath);
            if (info.Length > MaxBytes)
                throw new InvalidOperationException($"Text documents are limited to {MaxBytes / 1024 / 1024} MiB.");

            var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken).ConfigureAwait(false);
            if (LooksBinary(bytes))
                throw new InvalidOperationException("The selected file contains binary control bytes and was not imported as text.");

            var (encoding, bomLength) = DetectEncoding(bytes);
            var decoded = encoding.GetString(bytes, bomLength, bytes.Length - bomLength);
            var normalized = NormalizeText(decoded, Math.Clamp(maxCharacters, 1_000, 2_000_000), out var truncated, out var removedControls);
            var warnings = new List<string>();
            if (truncated)
                warnings.Add("Content was truncated to the configured character limit.");
            if (removedControls > 0)
                warnings.Add($"Removed {removedControls} non-text control character(s).");
            warnings.Add("Document content is stored as untrusted reference data and is never evaluated as a regular expression, command, or instruction authority.");

            return new SafeTextDocument(
                info.Name,
                normalized,
                Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                encoding.WebName,
                GuessContentType(extension),
                warnings);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SafeTextDocumentService)}.{nameof(ReadAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SafeTextDocumentService)}.{nameof(ReadAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs import as part of the safe text document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="filePath">File path value supplied to the safe text document operation and used when producing its result.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project document import produced by the operation.</returns>
    public async Task<ProjectDocumentImport> ImportAsync(Guid projectId, Guid? revisionId, string filePath, bool userConfirmed, CancellationToken cancellationToken = default)
    {
    try
    {
            if (!userConfirmed)
                throw new InvalidOperationException("Fresh human confirmation is required before importing a document into a project.");
            var document = await ReadAsync(filePath, cancellationToken: cancellationToken).ConfigureAwait(false);

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            if (!await db.LocalGptProjects.AnyAsync(item => item.Id == projectId, cancellationToken).ConfigureAwait(false))
                throw new KeyNotFoundException($"Project {projectId} was not found.");
            if (revisionId is Guid id && !await db.LocalGptProjectRevisions.AnyAsync(item => item.Id == id && item.ProjectId == projectId, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("The selected revision does not belong to this project.");

            var existing = await db.ProjectDocumentImports.SingleOrDefaultAsync(
                item => item.ProjectId == projectId && item.ContentHash == document.ContentHash,
                cancellationToken).ConfigureAwait(false);
            if (existing is not null)
                return existing;

            var entity = new ProjectDocumentImport
            {
                ProjectId = projectId,
                RevisionId = revisionId,
                SourceName = document.Name,
                SourceUri = Path.GetFullPath(filePath),
                ContentHash = document.ContentHash,
                ContentType = document.ContentType,
                EncodingName = document.EncodingName,
                ExtractedText = document.Text,
                Status = "Imported",
                SafetyNotes = string.Join(" ", document.Warnings),
                IsUserApproved = true,
                ImportedAtUtc = DateTime.UtcNow
            };
            db.ProjectDocumentImports.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Imported text document {DocumentName} into project {ProjectId}; content omitted from logs.", entity.SourceName, projectId);
            return entity;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SafeTextDocumentService)}.{nameof(ImportAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SafeTextDocumentService)}.{nameof(ImportAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs looks binary as part of the safe text document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="bytes">Bytes value supplied to the safe text document operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool LooksBinary(byte[] bytes)
    {
    try
    {
            if (bytes.Length == 0)
                return false;
            var sample = Math.Min(bytes.Length, 32_768);
            var suspicious = 0;
            for (var index = 0; index < sample; index++)
            {
                var value = bytes[index];
                if (value == 0)
                    return true;
                if (value < 0x09 || value is > 0x0D and < 0x20)
                    suspicious++;
            }
            return suspicious > sample / 50;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SafeTextDocumentService)}.{nameof(LooksBinary)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SafeTextDocumentService)}.{nameof(LooksBinary)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs detect encoding as part of the safe text document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="bytes">Bytes value supplied to the safe text document operation and used when producing its result.</param>
    /// <returns>The encoding encoding int bom length produced by the operation.</returns>
    private (Encoding Encoding, int BomLength) DetectEncoding(byte[] bytes)
    {
        if (bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
            return (new UTF8Encoding(false, true), 3);
        if (bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE }))
            return (Encoding.Unicode, 2);
        if (bytes.AsSpan().StartsWith(new byte[] { 0xFE, 0xFF }))
            return (Encoding.BigEndianUnicode, 2);
        try
        {
            _ = new UTF8Encoding(false, true).GetString(bytes);
            return (new UTF8Encoding(false, true), 0);
        }
        catch (DecoderFallbackException)
        {
            return (Encoding.Latin1, 0);
        }
    }

    /// <summary>
    /// Normalizes text as part of the safe text document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="input">Input value supplied to the safe text document operation and used when producing its result.</param>
    /// <param name="maxCharacters">Max characters value supplied to the safe text document operation and used when producing its result.</param>
    /// <param name="truncated">Value indicating whether truncated should apply to this operation.</param>
    /// <param name="removedControls">Removed controls value supplied to the safe text document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeText(string input, int maxCharacters, out bool truncated, out int removedControls)
    {
    try
    {
            var builder = new StringBuilder(Math.Min(input.Length, maxCharacters));
            removedControls = 0;
            foreach (var character in input.Normalize(NormalizationForm.FormC))
            {
                if (builder.Length >= maxCharacters)
                    break;
                if (character is '\r' or '\n' or '\t' || !char.IsControl(character))
                    builder.Append(character);
                else
                    removedControls++;
            }
            truncated = input.Length > builder.Length + removedControls;
            return builder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SafeTextDocumentService)}.{nameof(NormalizeText)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SafeTextDocumentService)}.{nameof(NormalizeText)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs guess content type as part of the safe text document service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="extension">Extension value supplied to the safe text document operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string GuessContentType(string extension) {
    try
    {
        return extension.ToLowerInvariant() switch
    {
        ".json" or ".jsonl" => "application/json",
        ".xml" or ".csproj" => "application/xml",
        ".csv" => "text/csv",
        ".html" or ".htm" => "text/html",
        ".md" or ".markdown" => "text/markdown",
        _ => "text/plain"
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SafeTextDocumentService)}.{nameof(GuessContentType)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SafeTextDocumentService)}.{nameof(GuessContentType)} failed.");
        throw;
    }
}
}
