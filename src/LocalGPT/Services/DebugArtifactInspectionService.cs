using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Reflection.Metadata;

namespace LocalGPT.Services;

/// <summary>Reads bounded metadata from portable PDB and other debug files without loading or executing them.</summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
/// <param name="runtimePolicy">Local gpt runtime policy data service dependency used by the debug artifact inspection workflow to provide the corresponding application capability.</param>
public sealed class DebugArtifactInspectionService(
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<DebugArtifactInspectionService> logger) : IDebugArtifactInspectionService
{

    /// <summary>
    /// Performs inspect as part of the debug artifact inspection service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="filePath">File path value supplied to the debug artifact inspection operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The debug artifact inspection result produced by the operation.</returns>
    public async Task<DebugArtifactInspectionResult> InspectAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var fullPath = Path.GetFullPath(filePath);
        var file = new FileInfo(fullPath);
        if (!file.Exists)
            throw new FileNotFoundException("The debug artifact was not found.", fullPath);
        var maximumInspectionBytes = Math.Max(1L, runtimePolicy.GetLong(LocalGptRuntimeValue.DebugArtifactMaximumInspectionBytes));
        if (file.Length > maximumInspectionBytes)
            throw new InvalidOperationException($"The debug artifact exceeds the configured DebugArtifactMaximumInspectionBytes policy ({maximumInspectionBytes:n0} bytes).");

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

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
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
