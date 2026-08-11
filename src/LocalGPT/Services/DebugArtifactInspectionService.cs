using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Reflection.Metadata;

namespace LocalGPT.Services;

/// <summary>Reads bounded metadata from portable PDB and other debug files without loading or executing them.</summary>
public sealed class DebugArtifactInspectionService(ILogger<DebugArtifactInspectionService> logger) : IDebugArtifactInspectionService
{
    private const long MaximumInspectionBytes = 1024L * 1024L * 1024L;

    /// <summary>
    /// Runs the inspect async operation.
    /// </summary>
    public async Task<DebugArtifactInspectionResult> InspectAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var fullPath = Path.GetFullPath(filePath);
        var file = new FileInfo(fullPath);
        if (!file.Exists)
            throw new FileNotFoundException("The debug artifact was not found.", fullPath);
        if (file.Length > MaximumInspectionBytes)
            throw new InvalidOperationException($"The debug artifact is larger than the bounded {MaximumInspectionBytes:n0}-byte inspection ceiling.");

        var result = new DebugArtifactInspectionResult
        {
            FileName = file.Name,
            FullPath = fullPath,
            SizeBytes = file.Length,
            LastWriteUtc = file.LastWriteTimeUtc,
            Format = file.Extension.TrimStart('.').ToUpperInvariant()
        };

        if (!file.Extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase))
        {
            result.Warnings.Add("Only portable PDB document metadata is decoded. Other debug formats are reported as bounded binary metadata.");
            result.Metadata.Add($"Extension={file.Extension}");
            return result;
        }

        await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        try
        {
            using var provider = MetadataReaderProvider.FromPortablePdbStream(stream, MetadataStreamOptions.LeaveOpen);
            var reader = provider.GetMetadataReader();
            result.Format = "Portable PDB";
            var documentCount = 0;
            foreach (var handle in reader.Documents)
            {
                if (documentCount++ >= 4096) break;
                cancellationToken.ThrowIfCancellationRequested();
                var document = reader.GetDocument(handle);
                var name = reader.GetString(document.Name);
                if (!string.IsNullOrWhiteSpace(name))
                    result.Documents.Add(name);
            }
            result.Metadata.Add($"Documents={result.Documents.Count}");
            result.Metadata.Add($"MethodDebugInformation={reader.MethodDebugInformation.Count}");
            result.Metadata.Add($"LocalScopes={reader.LocalScopes.Count}");
            result.Metadata.Add($"CustomDebugInformation={reader.CustomDebugInformation.Count}");
        }
        catch (BadImageFormatException ex)
        {
            logger.LogInformation(ex, "Debug artifact {FileName} is not a portable PDB.", file.Name);
            result.Format = "Windows/native or unknown PDB";
            result.Warnings.Add("This PDB is not portable. LocalGPT can record its file metadata, but source-document decoding needs an installed native symbol reader or a matching portable PDB.");
        }
        return result;
    }
}
