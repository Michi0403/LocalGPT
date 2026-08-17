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
    /// Parses string array as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="json">Json value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> ParseStringArray(string? json)
    {
    try
    {
            try { return JsonSerializer.Deserialize<List<string>>(string.IsNullOrWhiteSpace(json) ? "[]" : json)?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(100).ToList() ?? []; }
            catch (JsonException) { return []; }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ParseStringArray)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ParseStringArray)} failed.");
        throw;
    }
}
    /// <summary>
    /// Parses access policy as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="json">Json value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<WorkspaceAccessPolicyRule> ParseAccessPolicy(string? json)
    {
    try
    {
            try { return JsonSerializer.Deserialize<List<WorkspaceAccessPolicyRule>>(string.IsNullOrWhiteSpace(json) ? "[]" : json, new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true })?.Take(200).ToList() ?? []; }
            catch (JsonException) { return []; }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ParseAccessPolicy)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ParseAccessPolicy)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs enumerate relative entries as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="findings">Findings value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> EnumerateRelativeEntries(string root, int maximum, List<WorkspacePermissionFinding> findings)
    {
    try
    {
            var result = new List<string>();
            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0 && result.Count < maximum)
            {
                var current = pending.Pop();
                try
                {
                    foreach (var directory in Directory.EnumerateDirectories(current))
                    {
                        result.Add(Path.GetRelativePath(root, directory).Replace('\\', '/') + "/");
                        if (result.Count >= maximum) break;
                        pending.Push(directory);
                    }
                    if (result.Count >= maximum) break;
                    foreach (var file in Directory.EnumerateFiles(current))
                    {
                        result.Add(Path.GetRelativePath(root, file).Replace('\\', '/'));
                        if (result.Count >= maximum) break;
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    findings.Add(new("Warning", "ENUMERATION_PARTIAL", "Part of the workspace could not be inspected.", Path.GetRelativePath(root, current).Replace('\\', '/')));
                }
            }
            if (result.Count >= maximum)
                findings.Add(new("Warning", "ENTRY_LIMIT", $"Workspace assessment stopped after {maximum} entries."));
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(EnumerateRelativeEntries)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(EnumerateRelativeEntries)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs evaluate access policy rule as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="rule">Rule value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="entries">String dependency used by the project maintenance workflow to provide the corresponding application capability.</param>
    /// <param name="root">Root value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="rootWriteAccess">Value indicating whether root write access should apply to this operation.</param>
    /// <param name="findings">Findings value supplied to the project maintenance operation and used when producing its result.</param>
    private void EvaluateAccessPolicyRule(WorkspaceAccessPolicyRule rule, IReadOnlyList<string> entries, string root, bool rootWriteAccess, List<WorkspacePermissionFinding> findings)
    {
    try
    {
            var regex = CompileRegex(rule.RelativePathRegex, nameof(rule.RelativePathRegex), @"(?!)");
            var matches = entries.Where(entry => regex.IsMatch(entry)).Take(100).ToArray();
            if (rule.Required && matches.Length == 0)
            {
                findings.Add(new(rule.Severity, "POLICY_NO_MATCH", $"Required workspace policy '{Trim(rule.Name, 160)}' matched no file or directory."));
                return;
            }
            foreach (var relative in matches)
            {
                var isDirectory = relative.EndsWith("/", StringComparison.Ordinal);
                if ((rule.ExpectedEntryKind == "File" && isDirectory) || (rule.ExpectedEntryKind == "Directory" && !isDirectory))
                    findings.Add(new(rule.Severity, "POLICY_KIND", $"Workspace policy '{Trim(rule.Name, 160)}' matched the wrong entry kind.", relative));
                var fullPath = Path.GetFullPath(Path.Combine(root, relative.TrimEnd('/').Replace('/', Path.DirectorySeparatorChar)));
                if (!IsPathInside(root, fullPath))
                {
                    findings.Add(new("Danger", "POLICY_ESCAPE", "A workspace policy match escaped the configured root.", relative));
                    continue;
                }
                if (rule.RequiredAccess.Contains("Read", StringComparison.OrdinalIgnoreCase) && !(isDirectory ? CanEnumerateDirectory(fullPath) : CanOpenRead(fullPath)))
                    findings.Add(new(rule.Severity, "POLICY_READ_DENIED", $"Workspace policy '{Trim(rule.Name, 160)}' requires read access that is unavailable.", relative));
                if (rule.RequiredAccess.Contains("Write", StringComparison.OrdinalIgnoreCase) && !rootWriteAccess)
                    findings.Add(new(rule.Severity, "POLICY_WRITE_UNPROVEN", $"Workspace policy '{Trim(rule.Name, 160)}' requires write access, but the bounded workspace write probe did not succeed.", relative));
                if (rule.RequiredAccess.Contains("Execute", StringComparison.OrdinalIgnoreCase) && isDirectory)
                    findings.Add(new("Warning", "POLICY_EXECUTE_DIRECTORY", $"Execute access for directory policy '{Trim(rule.Name, 160)}' is not inferred; validate the assigned compiler/tool explicitly.", relative));
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(EvaluateAccessPolicyRule)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(EvaluateAccessPolicyRule)} failed.");
        throw;
    }
}
    /// <summary>
    /// Determines whether enumerate directory as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool CanEnumerateDirectory(string path)
    {
    try
    {
            try { _ = Directory.EnumerateFileSystemEntries(path).Take(1).ToArray(); return true; } catch { return false; }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(CanEnumerateDirectory)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(CanEnumerateDirectory)} failed.");
        throw;
    }
}
    /// <summary>
    /// Determines whether open read as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool CanOpenRead(string path)
    {
    try
    {
            try { using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete); return true; } catch { return false; }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(CanOpenRead)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(CanOpenRead)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs probe directory write as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private async Task<bool> ProbeDirectoryWriteAsync(string root, CancellationToken cancellationToken)
    {
    try
    {
            var probe = Path.Combine(root, $".localgpt-rights-probe-{Guid.NewGuid():N}.tmp");
            try
            {
                await File.WriteAllTextAsync(probe, "LocalGPT bounded workspace rights probe.", Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                File.Delete(probe);
                return true;
            }
            catch
            {
                try { if (File.Exists(probe)) File.Delete(probe); } catch { }
                return false;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ProbeDirectoryWriteAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ProbeDirectoryWriteAsync)} failed.");
        throw;
    }
}
    /// <summary>
    /// Determines whether broad or system root as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsBroadOrSystemRoot(string path)
    {
    try
    {
            var normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            var root = Path.TrimEndingDirectorySeparator(Path.GetPathRoot(normalized) ?? string.Empty);
            if (string.Equals(normalized, root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) return true;
            var protectedRoots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            }.Where(item => !string.IsNullOrWhiteSpace(item));
            return protectedRoots.Any(item => string.Equals(normalized, Path.TrimEndingDirectorySeparator(Path.GetFullPath(item)), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(IsBroadOrSystemRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(IsBroadOrSystemRoot)} failed.");
        throw;
    }
}
    /// <summary>
    /// Normalizes relative policy path as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeRelativePolicyPath(string value)
    {
    try
    {
            var normalized = (value ?? string.Empty).Trim().Replace('\\', '/').Trim('/');
            return normalized.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(normalized) ? string.Empty : normalized;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(NormalizeRelativePolicyPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(NormalizeRelativePolicyPath)} failed.");
        throw;
    }
}

    /// <summary>
    /// Validates JSON object as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="json">Json value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="parameter">Parameter value supplied to the project maintenance operation and used when producing its result.</param>
    private void ValidateJsonObject(string? json, string parameter)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(json)) return;
            try { using var doc = JsonDocument.Parse(json); if (doc.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("A JSON object is required.", parameter); }
            catch (JsonException ex) { throw new ArgumentException("The JSON object is invalid.", parameter, ex); }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ValidateJsonObject)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ValidateJsonObject)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs merge environment JSON as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="compilerJson">Compiler json value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="workspaceJson">Workspace json value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string MergeEnvironmentJson(string? compilerJson, string? workspaceJson)
    {
    try
    {
            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var json in new[] { compilerJson, workspaceJson })
            {
                if (string.IsNullOrWhiteSpace(json)) continue;
                try
                {
                    foreach (var pair in JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [])
                        merged[pair.Key] = pair.Value;
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException("A compiler or workspace environment JSON object is invalid.", ex);
                }
            }
            return JsonSerializer.Serialize(merged);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(MergeEnvironmentJson)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(MergeEnvironmentJson)} failed.");
        throw;
    }
}

    }
}
