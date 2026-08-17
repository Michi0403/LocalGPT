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
    /// Builds include regex as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pattern">Pattern value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    private Regex BuildIncludeRegex(string? pattern)
    {
    try
    {
            var value = string.IsNullOrWhiteSpace(pattern)
                ? @"(?i)\.(cs|razor|csproj|sln|json|xml|md|txt|ps1|cmd|sh|py|js|ts|tsx|css|scss|html|htm|php|c|h|cpp|hpp|java|kt|go|rs|sql|yml|yaml)$"
                : pattern.Trim();
            return regexPatterns.Compile(value, "CultureInvariant");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildIncludeRegex)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildIncludeRegex)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds cache root as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sourceUri">Source uri value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildCacheRoot(Uri sourceUri)
    {
    try
    {
            var safeHost = SanitizeSegment(sourceUri.Host, allowLeadingDot: true);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceUri.AbsoluteUri)))[..16].ToLowerInvariant();
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocalGPT", "LearningBase", "RemoteSources", safeHost, hash);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildCacheRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(BuildCacheRoot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs safe segment as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SafeSegment(string value)
    {
    try
    {
            var safe = SanitizeSegment(value, allowLeadingDot: false).Trim('-', '.');
            if (string.IsNullOrWhiteSpace(safe)) throw new ArgumentException("A source path segment is empty or invalid.");
            return safe;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(SafeSegment)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(SafeSegment)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs sanitize segment as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="allowLeadingDot">Value indicating whether allow leading dot should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SanitizeSegment(string? value, bool allowLeadingDot)
    {
    try
    {
            var source = value ?? string.Empty;
            var builder = new StringBuilder(source.Length);
            foreach (var character in source)
            {
                var allowed = char.IsAsciiLetterOrDigit(character) || character is '_' or '-' || character == '.';
                builder.Append(allowed ? character : '-');
            }
            var result = builder.ToString();
            return allowLeadingDot ? result : result.TrimStart('.');
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(SanitizeSegment)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(SanitizeSegment)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs clear directory as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="directory">Directory value supplied to the remote knowledge import operation and used when producing its result.</param>
    private void ClearDirectory(string directory)
    {
    try
    {
            if (!Directory.Exists(directory)) return;
            foreach (var file in Directory.EnumerateFiles(directory)) File.Delete(file);
            foreach (var child in Directory.EnumerateDirectories(directory)) Directory.Delete(child, recursive: true);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ClearDirectory)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ClearDirectory)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs extension for as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="uri">Uri value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="mediaType">Media type value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ExtensionFor(Uri uri, string mediaType)
    {
    try
    {
            var extension = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(extension) && extension.Length <= 12) return extension;
            return mediaType switch
            {
                "text/html" => ".html",
                "application/json" => ".json",
                "text/plain" => ".txt",
                "text/markdown" => ".md",
                "application/xml" or "text/xml" => ".xml",
                _ => ".bin"
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ExtensionFor)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ExtensionFor)} failed.");
        throw;
    }
}

    }
}
