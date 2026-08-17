using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates remote knowledge import behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class RemoteKnowledgeImportService
    {
    /// <summary>
    /// Performs extract ZIP safely as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="zipPath">Zip path value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="targetRoot">Target root value supplied to the remote knowledge import operation and used when producing its result.</param>
    private void ExtractZipSafely(string zipPath, string targetRoot)
    {
    try
    {
            using var archive = ZipFile.OpenRead(zipPath);
            var maximumZipEntries = Math.Max(1, catalog.MaxZipEntries);
            var maximumExtractedBytes = Math.Max(1L, catalog.MaxExtractedBytes);
            var maximumZipEntryBytes = Math.Max(1L, catalog.MaxZipEntryBytes);
            if (archive.Entries.Count > maximumZipEntries)
                throw new InvalidDataException($"Archive has more entries than the database-backed MaxZipEntries policy ({maximumZipEntries:n0}).");
            var normalizedRoot = Path.GetFullPath(targetRoot) + Path.DirectorySeparatorChar;
            long total = 0;
            foreach (var entry in archive.Entries)
            {
                if (entry.Length > maximumZipEntryBytes)
                    throw new InvalidDataException($"Archive entry exceeds the database-backed MaxZipEntryBytes policy ({maximumZipEntryBytes:n0} bytes).");
                total += Math.Max(0, entry.Length);
                if (total > maximumExtractedBytes)
                    throw new InvalidDataException($"Archive exceeds the database-backed MaxExtractedBytes policy ({maximumExtractedBytes:n0} bytes).");
                var destination = Path.GetFullPath(Path.Combine(targetRoot, entry.FullName));
                if (!destination.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Archive contains an unsafe traversal path.");
                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destination);
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                entry.ExtractToFile(destination, overwrite: true);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ExtractZipSafely)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ExtractZipSafely)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds file result list as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="sourceUri">Source uri value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="result">Result value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="includeRegex">Include regex value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="maxFiles">Max files value supplied to the remote knowledge import operation and used when producing its result.</param>
    private void BuildFileResultList(string root, Uri sourceUri, RemoteKnowledgeImportResult result, Regex includeRegex, int maxFiles)
    {
    try
    {
            foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Take(maxFiles))
            {
                var relative = Path.GetRelativePath(result.CacheRoot, path);
                var matched = includeRegex.IsMatch(relative.Replace(Path.DirectorySeparatorChar, '/'));
                result.Files.Add(new RemoteKnowledgeImportFile
                {
                    RelativePath = relative,
                    SourceUrl = sourceUri.AbsoluteUri,
                    Length = new FileInfo(path).Length,
                    MatchesFilePolicy = matched,
                    Status = matched ? "Extracted; matches file policy" : "Extracted; excluded by file policy"
                });
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildFileResultList)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildFileResultList)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs prepare matched import root as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="result">Result value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string PrepareMatchedImportRoot(RemoteKnowledgeImportResult result)
    {
    try
    {
            var selectedRoot = Path.Combine(result.CacheRoot, "selected-for-import");
            if (Directory.Exists(selectedRoot)) Directory.Delete(selectedRoot, recursive: true);
            Directory.CreateDirectory(selectedRoot);
            var cacheFull = Path.GetFullPath(result.CacheRoot) + Path.DirectorySeparatorChar;
            var selectedFull = Path.GetFullPath(selectedRoot) + Path.DirectorySeparatorChar;
            foreach (var file in result.Files.Where(item => item.MatchesFilePolicy))
            {
                var source = Path.GetFullPath(Path.Combine(result.CacheRoot, file.RelativePath));
                if (!source.StartsWith(cacheFull, StringComparison.OrdinalIgnoreCase) ||
                    source.StartsWith(selectedFull, StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(source))
                    continue;
                var relative = Path.GetRelativePath(result.CacheRoot, source);
                var destination = Path.GetFullPath(Path.Combine(selectedRoot, relative));
                if (!destination.StartsWith(selectedFull, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("A selected import path escaped the bounded staging directory.");
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, overwrite: true);
            }
            return selectedRoot;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(PrepareMatchedImportRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(PrepareMatchedImportRoot)} failed.");
        throw;
    }
}

    }
}
