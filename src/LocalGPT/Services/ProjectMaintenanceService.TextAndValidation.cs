using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates project maintenance behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class ProjectMaintenanceService
    {
    /// <summary>
    /// Determines whether text extension as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="extension">Extension value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsTextExtension(string extension) {
    try
    {
        return new[] { ".cs", ".razor", ".csproj", ".sln", ".slnx", ".json", ".xml", ".props", ".targets", ".ps1", ".cmd", ".md", ".yml", ".yaml", ".java", ".py", ".ino", ".pde", ".cpp", ".cc", ".cxx", ".c", ".h", ".hpp", ".ini", ".toml", ".cfg", ".conf", ".cmake", ".kconfig", ".sdkconfig", ".txt", ".css", ".js", ".ts", ".html" }.Contains(extension.ToLowerInvariant());
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(IsTextExtension)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(IsTextExtension)} failed.");
        throw;
    }
}
    /// <summary>
    /// Determines whether generated path as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="relative">Relative value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsGeneratedPath(string relative) {
    try
    {
        return Regex.IsMatch(relative, @"(?i)(^|/)(bin|obj|node_modules|artifacts|\.vs)(/|$)", RegexOptions.CultureInvariant, runtimePolicy.RegexTimeout);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(IsGeneratedPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(IsGeneratedPath)} failed.");
        throw;
    }
}
    /// <summary>
    /// Finds nearest project file as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="file">File value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string FindNearestProjectFile(string root, string file)
    {
        var directory = Path.GetDirectoryName(file);
        while (!string.IsNullOrWhiteSpace(directory) && directory.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var project = Directory.EnumerateFiles(directory, "*.*proj", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (project is not null) return project;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogDebug(ex, "Could not inspect project files in directory {DirectoryPath}.", directory);
                return string.Empty;
            }
            directory = Path.GetDirectoryName(directory);
        }
        return string.Empty;
    }
    /// <summary>
    /// Determines whether h file as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> HashFileAsync(string path, CancellationToken cancellationToken)
    {
    try
    {
            var stream = File.OpenRead(path);
            await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            return Convert.ToHexString(hash);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(HashFileAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(HashFileAsync)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs capture tracked source state as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="files">Local gpt project tracked file dependency used by the project maintenance workflow to provide the corresponding application capability.</param>
    /// <param name="requireStoredHashMatch">Value indicating whether require stored hash match should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project tracked source state produced by the operation.</returns>
    private async Task<ProjectTrackedSourceState> CaptureTrackedSourceStateAsync(IReadOnlyList<LocalGptProjectTrackedFile> files, bool requireStoredHashMatch, CancellationToken cancellationToken)
    {
    try
    {
            var entries = new List<ProjectSourceManifestEntry>(files.Count);
            foreach (var file in files.OrderBy(item => item.ProjectRelativePath, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!File.Exists(file.AbsolutePath)) throw new FileNotFoundException("A tracked project file is missing. Rescan the project before continuing.", file.AbsolutePath);
                var hash = await HashFileAsync(file.AbsolutePath, cancellationToken).ConfigureAwait(false);
                if (requireStoredHashMatch && !string.Equals(hash, file.ContentHash, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Tracked file '{file.ProjectRelativePath}' changed after the last approved scan. Rescan before building or approving the revision.");
                var size = new FileInfo(file.AbsolutePath).Length;
                entries.Add(new ProjectSourceManifestEntry(file.ProjectRelativePath.Replace('\\', '/'), hash, size));
            }
            var canonical = string.Join("\n", entries.Select(item => item.RelativePath + "|" + item.ContentHash + "|" + item.SizeBytes));
            var hashValue = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
            var manifestJson = JsonSerializer.Serialize(new { SourceHash = hashValue, Files = entries }, new JsonSerializerOptions { WriteIndented = true });
            return new ProjectTrackedSourceState(hashValue, manifestJson, entries);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(CaptureTrackedSourceStateAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(CaptureTrackedSourceStateAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether path inside as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="path">Path value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsPathInside(string root, string path)
    {
    try
    {
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
            var normalizedPath = Path.GetFullPath(path);
            return normalizedPath.StartsWith(normalizedRoot, comparison) || string.Equals(Path.TrimEndingDirectorySeparator(normalizedPath), Path.TrimEndingDirectorySeparator(root), comparison);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(IsPathInside)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(IsPathInside)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs regex matches as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pattern">Pattern value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="input">Input value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool RegexMatches(string pattern, string input) {
    try
    {
        return !string.IsNullOrWhiteSpace(pattern) && CompileRegex(pattern, nameof(pattern), @"(?!)").IsMatch(input ?? string.Empty);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(RegexMatches)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(RegexMatches)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs compile regex as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pattern">Pattern value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="parameter">Parameter value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    private Regex CompileRegex(string? pattern, string parameter, string fallback)
    {
    try
    {
            try
            {
                return regexCompilation.Compile(
                    string.IsNullOrWhiteSpace(pattern) ? fallback : pattern,
                    "cultureinvariant",
                    runtimePolicy.RegexTimeout,
                    $"Project maintenance parameter {parameter}");
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException("The regular expression is invalid.", parameter, ex);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(CompileRegex)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(CompileRegex)} failed.");
        throw;
    }
}
    /// <summary>
    /// Validates regex as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pattern">Pattern value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="parameter">Parameter value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="allowEmpty">Value indicating whether allow empty should apply to this operation.</param>
    private void ValidateRegex(string? pattern, string parameter, bool allowEmpty)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(pattern)) { if (allowEmpty) return; throw new ArgumentException("A regular expression is required.", parameter); }
            _ = CompileRegex(pattern, parameter, pattern);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ValidateRegex)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ValidateRegex)} failed.");
        throw;
    }
}
    /// <summary>
    /// Validates JSON array as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="json">Json value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="parameter">Parameter value supplied to the project maintenance operation and used when producing its result.</param>
    private void ValidateJsonArray(string? json, string parameter)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(json)) return;
            try { using var doc = JsonDocument.Parse(json); if (doc.RootElement.ValueKind != JsonValueKind.Array) throw new ArgumentException("A JSON array is required.", parameter); }
            catch (JsonException ex) { throw new ArgumentException("The JSON array is invalid.", parameter, ex); }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ValidateJsonArray)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ValidateJsonArray)} failed.");
        throw;
    }
}
    /// <summary>
    /// Validates workspace access policy JSON as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="json">Json value supplied to the project maintenance operation and used when producing its result.</param>
    private void ValidateWorkspaceAccessPolicyJson(string? json)
    {
    try
    {
            ValidateJsonArray(json, nameof(json));
            foreach (var rule in ParseAccessPolicy(json))
            {
                ValidateRegex(rule.RelativePathRegex, nameof(rule.RelativePathRegex), allowEmpty: false);
                if (rule.ExpectedEntryKind != "File" && rule.ExpectedEntryKind != "Directory" && rule.ExpectedEntryKind != "Either")
                    throw new ArgumentException("ExpectedEntryKind must be File, Directory, or Either.", nameof(json));
                if (rule.RequiredAccess != "Read" && rule.RequiredAccess != "ReadWrite" && rule.RequiredAccess != "Execute" && rule.RequiredAccess != "ReadWriteExecute")
                    throw new ArgumentException("RequiredAccess is invalid.", nameof(json));
                if (rule.Severity != "Warning" && rule.Severity != "Danger")
                    throw new ArgumentException("Severity must be Warning or Danger.", nameof(json));
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ValidateWorkspaceAccessPolicyJson)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ValidateWorkspaceAccessPolicyJson)} failed.");
        throw;
    }
}

    }
}
