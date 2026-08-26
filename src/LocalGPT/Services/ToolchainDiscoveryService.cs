using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services;

/// <summary>Performs bounded, cross-platform local toolchain discovery from PATH and knowledge-defined roots without performing network I/O.</summary>
/// <param name="knowledge">Toolchain knowledge service dependency used by the toolchain discovery workflow to provide the corresponding application capability.</param>
/// <param name="regexPatterns">Regex pattern service dependency used by the toolchain discovery workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ToolchainDiscoveryService(
    IToolchainKnowledgeService knowledge,
    IRegexPatternService regexPatterns,
    IPlatformRuntimeService platform,
    ILogger<ToolchainDiscoveryService> logger) : IToolchainDiscoveryService
{
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="ToolchainDiscoveryService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>
    /// Gets the current platform value that forms part of the toolchain discovery state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public ToolchainPlatformKind CurrentPlatform => platform.ToolchainPlatform;

    /// <summary>
    /// Performs discover as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<ToolchainDiscoveryCandidate>> DiscoverAsync(IReadOnlyList<string>? customRoots = null, int maximumCandidates = 128, CancellationToken cancellationToken = default)
    {
        try
        {
            maximumCandidates = Math.Clamp(maximumCandidates, 1, 512);
            var profiles = await knowledge.GetProfilesAsync(cancellationToken).ConfigureAwait(false);
            if (profiles.Count == 0)
                return [];
            var envTokenRegex = await regexPatterns.GetRegexAsync("builtin.toolchain-environment-token").ConfigureAwait(false);
            var foundPaths = new HashSet<string>(PathComparer());
            var result = new List<ToolchainDiscoveryCandidate>();
            var pathDirectories = SplitPathDirectories();

            foreach (var profile in profiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var directory in pathDirectories)
                {
                    AddDirectCandidates(profile, directory, "PATH", foundPaths, result, maximumCandidates);
                    if (result.Count >= maximumCandidates)
                        return Finish(result);
                }
            }

            foreach (var profile in profiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var variable in profile.EnvironmentRootVariables.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var value = Environment.GetEnvironmentVariable(variable);
                    if (string.IsNullOrWhiteSpace(value))
                        continue;
                    foreach (var environmentRoot in SplitEnvironmentRoots(value))
                    {
                        var root = NormalizeRoot(environmentRoot, envTokenRegex);
                        if (string.IsNullOrWhiteSpace(root))
                            continue;
                        if (File.Exists(root))
                            AddCandidate(profile, root, $"Environment:{variable}", foundPaths, result);
                        else
                            AddRootCandidates(profile, root, $"Environment:{variable}", foundPaths, result, maximumCandidates, cancellationToken);
                        if (result.Count >= maximumCandidates)
                            return Finish(result);
                    }
                }

                foreach (var configuredRoot in SelectProfileRoots(profile).Concat(profile.CommonSearchRoots))
                {
                    var root = NormalizeRoot(configuredRoot, envTokenRegex);
                    if (string.IsNullOrWhiteSpace(root))
                        continue;
                    AddRootCandidates(profile, root, "KnowledgeRoot", foundPaths, result, maximumCandidates, cancellationToken);
                    if (result.Count >= maximumCandidates)
                        return Finish(result);
                }
            }

            foreach (var customRoot in customRoots ?? [])
            {
                var root = NormalizeRoot(customRoot, envTokenRegex);
                if (string.IsNullOrWhiteSpace(root))
                    continue;
                foreach (var profile in profiles)
                {
                    AddRootCandidates(profile, root, "UserRoot", foundPaths, result, maximumCandidates, cancellationToken);
                    if (result.Count >= maximumCandidates)
                        return Finish(result);
                }
            }

            return Finish(result);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Cross-platform toolchain discovery was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cross-platform toolchain discovery failed; searched paths were omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Parses environment variables as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public IReadOnlyList<ToolchainEnvironmentVariableSetting> ParseEnvironmentVariables(string environmentVariablesJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(environmentVariablesJson))
                return [];
            using var document = JsonDocument.Parse(environmentVariablesJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return [];
            return document.RootElement.EnumerateObject()
                .Select(property => new ToolchainEnvironmentVariableSetting { Name = property.Name, Value = property.Value.GetString() ?? string.Empty, Source = "Stored", IsEnabled = true })
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Ignored malformed stored toolchain environment-variable JSON; values were omitted.");
            return [];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing structured toolchain environment variables failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs serialize environment variables as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public string SerializeEnvironmentVariables(IEnumerable<ToolchainEnvironmentVariableSetting>? environmentVariables)
    {
        try
        {
            var values = (environmentVariables ?? [])
                .Where(item => item.IsEnabled && !string.IsNullOrWhiteSpace(item.Name))
                .GroupBy(item => item.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last().Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            return JsonSerializer.Serialize(values, jsonOptions);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Serializing structured toolchain environment variables failed; values were omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs finish as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="result">Result value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<ToolchainDiscoveryCandidate> Finish(List<ToolchainDiscoveryCandidate> result)
    {
        try
        {
            logger.LogInformation("Discovered {CandidateCount} local toolchain executable candidate(s) on platform {Platform}; executable paths were omitted from logs.", result.Count, CurrentPlatform);
            return result.OrderBy(item => item.Language, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.ExecutablePath, PathComparer()).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ordering toolchain discovery candidates failed; paths were omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs select profile roots as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IEnumerable<string> SelectProfileRoots(ToolchainKnowledgeProfile profile)
    {
        try
        {
            return CurrentPlatform switch
            {
                ToolchainPlatformKind.Windows => profile.WindowsSearchRoots,
                ToolchainPlatformKind.Linux => profile.LinuxSearchRoots,
                ToolchainPlatformKind.MacOS => profile.MacOsSearchRoots,
                _ => []
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Selecting platform roots for toolchain profile {ProfileKey} failed.", profile.Key);
            throw;
        }
    }

    /// <summary>
    /// Adds direct candidates as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="directory">Directory value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="foundPaths">Found paths value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="result">Result value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="maximumCandidates">Maximum candidates value supplied to the toolchain discovery operation and used when producing its result.</param>
    private void AddDirectCandidates(ToolchainKnowledgeProfile profile, string directory, string source, HashSet<string> foundPaths, List<ToolchainDiscoveryCandidate> result, int maximumCandidates)
    {
        try
        {
            if (!Directory.Exists(directory))
                return;
            foreach (var executableName in profile.ExecutableNames.Where(item => !string.IsNullOrWhiteSpace(item)))
            {
                if (result.Count >= maximumCandidates)
                    return;
                var candidate = Path.Combine(directory, executableName.Trim());
                if (File.Exists(candidate))
                    AddCandidate(profile, candidate, source, foundPaths, result);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Direct toolchain discovery failed for profile {ProfileKey}; paths were omitted from logs.", profile.Key);
            throw;
        }
    }

    /// <summary>
    /// Adds root candidates as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="root">Root value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="foundPaths">Found paths value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="result">Result value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="maximumCandidates">Maximum candidates value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    private void AddRootCandidates(ToolchainKnowledgeProfile profile, string root, string source, HashSet<string> foundPaths, List<ToolchainDiscoveryCandidate> result, int maximumCandidates, CancellationToken cancellationToken)
    {
        try
        {
            if (!Directory.Exists(root))
                return;
            AddDirectCandidates(profile, root, source, foundPaths, result, maximumCandidates);
            if (result.Count >= maximumCandidates || profile.MaximumSearchDepth <= 0)
                return;
            var names = profile.ExecutableNames.Where(item => !string.IsNullOrWhiteSpace(item)).ToHashSet(PathComparer());
            var pending = new Queue<(string Path, int Depth)>();
            pending.Enqueue((root, 0));
            var visited = 0;
            while (pending.Count > 0 && result.Count < maximumCandidates && visited < 3000)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = pending.Dequeue();
                visited++;
                foreach (var file in SafeGetFiles(current.Path))
                {
                    if (!names.Contains(Path.GetFileName(file)))
                        continue;
                    AddCandidate(profile, file, source, foundPaths, result);
                    if (result.Count >= maximumCandidates)
                        return;
                }
                if (current.Depth >= profile.MaximumSearchDepth)
                    continue;
                foreach (var directory in SafeGetDirectories(current.Path))
                    pending.Enqueue((directory, current.Depth + 1));
            }
        
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Toolchain root traversal was cancelled for profile {ProfileKey}.", profile.Key);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Toolchain root traversal failed for profile {ProfileKey}; paths were omitted from logs.", profile.Key);
            throw;
        }
    }

    /// <summary>
    /// Adds candidate as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="candidatePath">Candidate path value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="foundPaths">Found paths value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="result">Result value supplied to the toolchain discovery operation and used when producing its result.</param>
    private void AddCandidate(ToolchainKnowledgeProfile profile, string candidatePath, string source, HashSet<string> foundPaths, List<ToolchainDiscoveryCandidate> result)
    {
        try
        {
            var fullPath = Path.GetFullPath(candidatePath);
            if (!foundPaths.Add(fullPath))
                return;
            var home = InferHome(profile, fullPath);
            result.Add(new ToolchainDiscoveryCandidate
            {
                ProfileKey = profile.Key,
                Name = profile.DisplayName,
                Language = profile.Language,
                Kind = profile.Kind,
                ExecutablePath = fullPath,
                ToolchainHomePath = home,
                DiscoverySource = source,
                Platform = CurrentPlatform,
                ValidationArguments = profile.ValidationArguments,
                VersionRegexPatternName = profile.VersionRegexPatternName,
                KnowledgeEntryId = profile.KnowledgeEntryId,
                EnvironmentVariables = BuildRelevantEnvironment(profile, home, source)
            });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            logger.LogDebug(exception, "Skipped one invalid toolchain candidate path; path content was omitted.");
        }
    }

    /// <summary>
    /// Builds relevant environment as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="home">Home value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<ToolchainEnvironmentVariableSetting> BuildRelevantEnvironment(ToolchainKnowledgeProfile profile, string home, string source)
    {
        try
        {
            var values = new List<ToolchainEnvironmentVariableSetting>();
            foreach (var variable in profile.EnvironmentRootVariables.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var value = Environment.GetEnvironmentVariable(variable);
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                values.Add(new ToolchainEnvironmentVariableSetting { Name = variable, Value = value, Source = "Environment", IsEnabled = true });
            }
            if (values.Count == 0 && profile.EnvironmentRootVariables.Count == 1 && !string.IsNullOrWhiteSpace(home) && source != "PATH")
                values.Add(new ToolchainEnvironmentVariableSetting { Name = profile.EnvironmentRootVariables[0], Value = home, Source = "KnowledgeInferred", IsEnabled = true });
            return values;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building structured environment variables for toolchain profile {ProfileKey} failed; values were omitted from logs.", profile.Key);
            throw;
        }
    }

    /// <summary>
    /// Performs infer home as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="profile">Profile value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="executablePath">Executable path value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string InferHome(ToolchainKnowledgeProfile profile, string executablePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(executablePath) ?? string.Empty;
            if (string.Equals(Path.GetFileName(directory), "bin", StringComparison.OrdinalIgnoreCase) && profile.EnvironmentRootVariables.Count > 0)
                return Directory.GetParent(directory)?.FullName ?? directory;
            return directory;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Inferring a toolchain home failed for profile {ProfileKey}; executable path omitted from logs.", profile.Key);
            throw;
        }
    }

    /// <summary>
    /// Normalizes root as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <param name="envTokenRegex">Env token regex value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeRoot(string? value, Regex? envTokenRegex)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var expanded = value.Trim();
        if (expanded.StartsWith("~", StringComparison.Ordinal))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            expanded = Path.Combine(home, expanded[1..].TrimStart('/', '\\'));
        }
        expanded = Environment.ExpandEnvironmentVariables(expanded);
        if (envTokenRegex is not null)
        {
            expanded = envTokenRegex.Replace(expanded, match =>
            {
                var name = match.Groups["name"].Value;
                var replacement = Environment.GetEnvironmentVariable(name);
                return string.IsNullOrEmpty(replacement) ? match.Value : replacement;
            });
        }
        try { return Path.GetFullPath(expanded); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            logger.LogDebug(exception, "Ignored an invalid toolchain search root; path content was omitted.");
            return string.Empty;
        }
    }

    /// <summary>
    /// Splits one environment-root variable into its platform-native path entries so list-valued variables are discovered as roots instead of being interpreted as one path blob.
    /// </summary>
    /// <param name="value">Environment-variable value containing one or more platform-native path entries.</param>
    /// <returns>The normalized input entries before filesystem expansion.</returns>
    private IReadOnlyList<string> SplitEnvironmentRoots(string value)
    {
        try
        {
            return value
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(PathComparer())
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Splitting a list-valued toolchain environment root failed; environment content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs split path directories as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<string> SplitPathDirectories()
    {
        try
        {
            return (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(Directory.Exists)
                .Distinct(PathComparer())
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading PATH directories for toolchain discovery failed; PATH content was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs safe get files as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="directory">Directory value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<string> SafeGetFiles(string directory)
    {
        try { return Directory.GetFiles(directory); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(exception, "Skipped inaccessible toolchain search files; path content was omitted.");
            return [];
        }
    }

    /// <summary>
    /// Performs safe get directories as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="directory">Directory value supplied to the toolchain discovery operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<string> SafeGetDirectories(string directory)
    {
        try { return Directory.GetDirectories(directory); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(exception, "Skipped inaccessible toolchain search directories; path content was omitted.");
            return [];
        }
    }

    /// <summary>
    /// Performs path comparer as part of the toolchain discovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The string comparer produced by the operation.</returns>
    private StringComparer PathComparer()
    {
        try
        {
            return platform.PathComparer;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Selecting the platform path comparer failed.");
            throw;
        }
    }
}
