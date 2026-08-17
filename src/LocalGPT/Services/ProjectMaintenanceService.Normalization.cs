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
    /// Normalizes scope as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="scope">Scope value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeScope(string? scope) {
    try
    {
        return (scope ?? string.Empty).Trim() switch { "Project" => "Project", "ProjectType" => "ProjectType", "Global" => "Global", _ => throw new ArgumentException("ScopeKind must be Project, ProjectType, or Global.", nameof(scope)) };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(NormalizeScope)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(NormalizeScope)} failed.");
        throw;
    }
}
    /// <summary>
    /// Normalizes absolute path as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="parameter">Parameter value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeAbsolutePath(string? value, string parameter)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A path is required.", parameter);
            try { return Path.GetFullPath(Environment.ExpandEnvironmentVariables(value.Trim())); }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { throw new ArgumentException("The path is invalid.", parameter, ex); }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(NormalizeAbsolutePath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(NormalizeAbsolutePath)} failed.");
        throw;
    }
}
    /// <summary>
    /// Normalizes optional path as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeOptionalPath(string? value) {
    try
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : NormalizeAbsolutePath(value, nameof(value));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(NormalizeOptionalPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(NormalizeOptionalPath)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs require confirmation as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="confirmed">Value indicating whether confirmed should apply to this operation.</param>
    /// <param name="operation">Operation value supplied to the project maintenance operation and used when producing its result.</param>
    private void RequireConfirmation(bool confirmed, string operation) {
    try
    {
     if (!confirmed) throw new InvalidOperationException($"Fresh human confirmation is required before {operation}."); 
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(RequireConfirmation)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(RequireConfirmation)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs require text as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="parameter">Parameter value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="max">Max value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RequireText(string? value, string parameter, int max) {
    try
    {
     var result = Trim(value, max); return string.IsNullOrWhiteSpace(result) ? throw new ArgumentException("A value is required.", parameter) : result; 
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(RequireText)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(RequireText)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs trim or fallback as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="max">Max value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string TrimOrFallback(string? value, int max, string fallback) {
    try
    {
     var result = Trim(value, max); return string.IsNullOrWhiteSpace(result) ? fallback : result; 
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(TrimOrFallback)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(TrimOrFallback)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs trim as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="max">Max value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Trim(string? value, int max) {
    try
    {
     var result = value?.Trim() ?? string.Empty; return result.Length <= max ? result : result[..max]; 
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(Trim)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(Trim)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs limit as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="max">Max value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Limit(string value, int max) {
    try
    {
        return value.Length <= max ? value : value[..max];
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(Limit)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(Limit)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs first non empty line as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="max">Max value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string FirstNonEmptyLine(string value, int max) {
    try
    {
        return Trim(value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), max);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(FirstNonEmptyLine)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(FirstNonEmptyLine)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs safe file name as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SafeFileName(string value) {
    try
    {
        return string.Concat(value.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(SafeFileName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(SafeFileName)} failed.");
        throw;
    }
}

    }
}
