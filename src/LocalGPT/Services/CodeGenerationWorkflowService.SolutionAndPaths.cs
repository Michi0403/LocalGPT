using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates code generation workflow behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CodeGenerationWorkflowService
    {
    /// <summary>
    /// Builds solution file as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="relativeProjectPath">Relative project path value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildSolutionFile(string name, string relativeProjectPath)
    {
    try
    {
            var projectGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();
            var solutionGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();
            var normalizedProjectPath = relativeProjectPath.Replace('/', '\\');
            var builder = new StringBuilder();

            builder.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            builder.AppendLine("# Visual Studio Version 17");
            builder.AppendLine("VisualStudioVersion = 17.0.31903.59");
            builder.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");
            builder.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{name}\", \"{normalizedProjectPath}\", \"{projectGuid}\"");
            builder.AppendLine("EndProject");
            builder.AppendLine("Global");
            builder.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            builder.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
            builder.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
            builder.AppendLine("\tEndGlobalSection");
            builder.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
            builder.AppendLine($"\t\t{projectGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
            builder.AppendLine($"\t\t{projectGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU");
            builder.AppendLine($"\t\t{projectGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU");
            builder.AppendLine($"\t\t{projectGuid}.Release|Any CPU.Build.0 = Release|Any CPU");
            builder.AppendLine("\tEndGlobalSection");
            builder.AppendLine("\tGlobalSection(ExtensibilityGlobals) = postSolution");
            builder.AppendLine($"\t\tSolutionGuid = {solutionGuid}");
            builder.AppendLine("\tEndGlobalSection");
            builder.AppendLine("EndGlobal");
            return builder.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildSolutionFile)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildSolutionFile)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds completed successfully as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="buildStatus">Build status value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool BuildCompletedSuccessfully(string buildStatus)
    {
    try
    {
            var statuses = buildStatus.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return statuses.Length > 0 && statuses.All(status => status.EndsWith(":BuildPassed", StringComparison.OrdinalIgnoreCase));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildCompletedSuccessfully)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(BuildCompletedSuccessfully)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether inside directory as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="directory">Directory value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsInsideDirectory(string path, string directory)
    {
    try
    {
            var normalizedDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
            var normalizedPath = Path.GetFullPath(path);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(normalizedPath, normalizedDirectory, comparison) ||
                   normalizedPath.StartsWith(normalizedDirectory + Path.DirectorySeparatorChar, comparison);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(IsInsideDirectory)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(IsInsideDirectory)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves inside root as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="relativePath">Relative path value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveInsideRoot(string root, string relativePath)
    {
    try
    {
            var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            var candidate = Path.GetFullPath(Path.Combine(normalizedRoot, relativePath));
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!string.Equals(candidate, normalizedRoot, comparison) &&
                !candidate.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, comparison))
            {
                throw new InvalidOperationException("The requested output path escapes the reviewed artifact workspace.");
            }
            return candidate;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ResolveInsideRoot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ResolveInsideRoot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes relative path as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeRelativePath(string? value)
    {
    try
    {
            var path = string.IsNullOrWhiteSpace(value) ? "." : value.Trim().Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(path))
                throw new ArgumentException("Only relative paths are allowed in reviewed generation payloads.");
            var parts = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Any(part => part is "." or ".."))
            {
                if (path == ".")
                    return ".";
                throw new ArgumentException("Relative paths may not contain traversal segments.");
            }
            foreach (var part in parts)
            {
                if (part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || part.IndexOfAny(['<', '>', ':', '"', '|', '?', '*', '\0']) >= 0)
                    throw new ArgumentException("A reviewed output path contains invalid path characters.");

                var baseName = Path.GetFileNameWithoutExtension(part).TrimEnd('.', ' ');
                if (IsWindowsReservedName(baseName))
                    throw new ArgumentException($"A reviewed output path uses the reserved Windows name '{baseName}'.");
            }

            return parts.Length == 0 ? "." : Path.Combine(parts);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeRelativePath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeRelativePath)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether windows reserved name as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsWindowsReservedName(string name)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return name.Equals("CON", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("AUX", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
                   Regex.IsMatch(name, "^(COM|LPT)[1-9]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(IsWindowsReservedName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(IsWindowsReservedName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes identifier as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeIdentifier(string? value, string fallback)
    {
    try
    {
            var source = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            var builder = new StringBuilder();
            foreach (var character in source)
            {
                if (char.IsLetterOrDigit(character) || character == '_')
                    builder.Append(character);
            }
            if (builder.Length == 0)
                builder.Append(fallback);
            if (!char.IsLetter(builder[0]) && builder[0] != '_')
                builder.Insert(0, '_');
            return builder.ToString();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeIdentifier)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeIdentifier)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes identifier path as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeIdentifierPath(string? value, string fallback) {
    try
    {
        return string.Join('.', (string.IsNullOrWhiteSpace(value) ? fallback : value)
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => NormalizeIdentifier(part, "Generated")));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeIdentifierPath)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(NormalizeIdentifierPath)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs escape c sharp as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string EscapeCSharp(string? value) {
    try
    {
        return (value ?? string.Empty).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(EscapeCSharp)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(EscapeCSharp)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs value or fallback as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ValueOrFallback(string? value, string fallback)
    {
    try
    {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ValueOrFallback)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(ValueOrFallback)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether h prefix as part of the code generation workflow service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="hash">Hash value supplied to the code generation workflow operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string HashPrefix(string hash) {
    try
    {
        return hash.Length <= 12 ? hash : hash[..12];
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(HashPrefix)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CodeGenerationWorkflowService)}.{nameof(HashPrefix)} failed.");
        throw;
    }
}

    }
}
