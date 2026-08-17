using LocalGPT.BusinessObjects;
using System.IO.Compression;
using System.Text.Json;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates minecraft datapack behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class MinecraftDatapackService
    {
        /// <summary>
        /// Performs minecraft datapack version info resolve as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="minecraftVersion">Minecraft version value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The minecraft datapack version info produced by the operation.</returns>
        public MinecraftDatapackVersionInfo MinecraftDatapackVersionInfoResolve(string? minecraftVersion, ILogger logger)
        {
            try
            {
                var requested = string.IsNullOrWhiteSpace(minecraftVersion)
                ? catalog.DefaultMinecraftVersion
                : minecraftVersion.Trim();
                var knownVersions = MinecraftDatapackVersionKnownVersions(logger);
                var exact = knownVersions.FirstOrDefault(item =>
                    requested.Equals(item.MatchedVersion, StringComparison.OrdinalIgnoreCase));
                if (exact is not null)
                    return exact with { RequestedVersion = requested };

                var prefix = knownVersions
                    .Where(item => requested.StartsWith(item.MatchedVersion, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(item => item.MatchedVersion.Length)
                    .FirstOrDefault();
                if (prefix is not null)
                    return prefix with { RequestedVersion = requested, IsExactMatch = false, NeedsVerification = true, Notes = $"{prefix.Notes} Version matched by prefix; verify against the official Minecraft version manifest before friend testing." };

                var fallback = requested.StartsWith("26.", StringComparison.OrdinalIgnoreCase)
                    ? knownVersions.First(item => item.MatchedVersion == catalog.DefaultMinecraftVersion)
                    : requested.StartsWith("1.21", StringComparison.OrdinalIgnoreCase)
                    ? knownVersions.First(item => item.MatchedVersion == "1.21.4")
                    : knownVersions.First(item => item.MatchedVersion == catalog.DefaultMinecraftVersion);

                return fallback with
                {
                    RequestedVersion = requested,
                    IsExactMatch = false,
                    NeedsVerification = true,
                    Notes = $"No exact LocalGPT mapping for Minecraft {requested}. Using {fallback.MatchedVersion} as a cautious fallback; verify pack_format with the official version manifest."
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not resolve datapack metadata for Minecraft version {MinecraftVersion}.", minecraftVersion);
                var requested = string.IsNullOrWhiteSpace(minecraftVersion) ? catalog.DefaultMinecraftVersion : minecraftVersion.Trim();
                return new MinecraftDatapackVersionInfo(
                    RequestedVersion: requested,
                    MatchedVersion: catalog.DefaultMinecraftVersion,
                    PackFormat: "101.1",
                    FunctionRegistryFolder: "function",
                    IsExactMatch: false,
                    NeedsVerification: true,
                    Notes: "LocalGPT used a defensive datapack fallback. Verify pack_format before release.",
                    Source: "LocalGPT defensive fallback after version-resolution failure.");
            }
        }

        /// <summary>
        /// Performs minecraft datapack version known versions as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The collection produced by the operation.</returns>
        public List< MinecraftDatapackVersionInfo> MinecraftDatapackVersionKnownVersions (ILogger logger)
        {
    try
    {
                try
                {
                    return new()
                    {
                             MinecraftDatapackVersionInfoKnown("26.2", "105.0", "function", "Minecraft Java 26.2 snapshot family. Use only for snapshot worlds and verify against the installed launcher build.",logger),
            MinecraftDatapackVersionInfoKnown("26.2-snapshot-6", "105.0", "function", "Minecraft Java 26.2 Snapshot 6 datapack format.",logger),
            MinecraftDatapackVersionInfoKnown("26.1.2", "101.1", "function", "Minecraft Java 26.1 stable family; Java 25 runtime required.",logger),
            MinecraftDatapackVersionInfoKnown("26.1.1", "101.1", "function", "Minecraft Java 26.1 stable family; Java 25 runtime required.",logger),
            MinecraftDatapackVersionInfoKnown("26.1", "101.1", "function", "Minecraft Java 26.1 stable family; Java 25 runtime required.",logger),
            MinecraftDatapackVersionInfoKnown("1.21.4", 61.ToString(System.Globalization.CultureInfo.InvariantCulture), "function", "LocalGPT Living Cities benchmark target.",logger),
            MinecraftDatapackVersionInfoKnown("1.21.3", 57.ToString(System.Globalization.CultureInfo.InvariantCulture), "function", "Minecraft 1.21.2/1.21.3 datapack format family.",logger),
            MinecraftDatapackVersionInfoKnown("1.21.2", 57.ToString(System.Globalization.CultureInfo.InvariantCulture), "function", "Minecraft 1.21.2/1.21.3 datapack format family.",logger),
            MinecraftDatapackVersionInfoKnown("1.21.1", 48.ToString(System.Globalization.CultureInfo.InvariantCulture), "function", "Minecraft 1.21/1.21.1 datapack format family.",logger),
            MinecraftDatapackVersionInfoKnown("1.21",48.ToString(System.Globalization.CultureInfo.InvariantCulture), "function", "Minecraft 1.21/1.21.1 datapack format family.",logger)
                    };
                }
                catch (Exception)
                {
                    return new();
                }
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            serviceLogger.LogDebug(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(MinecraftDatapackVersionKnownVersions)} was canceled.");
        else
            serviceLogger.LogError(__serviceMethodException, $"Service method {nameof(MinecraftDatapackService)}.{nameof(MinecraftDatapackVersionKnownVersions)} failed.");
        throw;
    }
}

        /// <summary>
        /// Performs minecraft datapack version info known as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="version">Version value supplied to the council text operation and used when producing its result.</param>
        /// <param name="packFormat">Pack format value supplied to the council text operation and used when producing its result.</param>
        /// <param name="functionRegistryFolder">Function registry folder value supplied to the council text operation and used when producing its result.</param>
        /// <param name="notes">Notes value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The minecraft datapack version info produced by the operation.</returns>
        public MinecraftDatapackVersionInfo MinecraftDatapackVersionInfoKnown(string version, string packFormat, string functionRegistryFolder, string notes, ILogger logger) 
        {
            try
            {
                return new(
                RequestedVersion: version,
                MatchedVersion: version,
                PackFormat: packFormat,
                FunctionRegistryFolder: functionRegistryFolder,
                IsExactMatch: true,
                NeedsVerification: false,
                Notes: notes,
                Source: "LocalGPT curated datapack version catalog; verify unknown versions with the official Minecraft version manifest.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not create curated datapack metadata for version {Version}.", version);
                return new MinecraftDatapackVersionInfo(
                    RequestedVersion: version ?? string.Empty,
                    MatchedVersion: version ?? string.Empty,
                    PackFormat: string.IsNullOrWhiteSpace(packFormat) ? "unknown" : packFormat,
                    FunctionRegistryFolder: string.IsNullOrWhiteSpace(functionRegistryFolder) ? "function" : functionRegistryFolder,
                    IsExactMatch: false,
                    NeedsVerification: true,
                    Notes: string.IsNullOrWhiteSpace(notes) ? "Curated datapack metadata requires verification." : notes,
                    Source: "LocalGPT defensive fallback after curated metadata construction failure.");
            }
        }

        /// <summary>Executes the create datapack mcmeta operation.</summary>
        /// <param name="request">Input value for request.</param>
        /// <param name="context">Input value for context.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string CreateDatapackMcmeta(MinecraftModBuildRequest request, WorkspaceContext context, ILogger logger)
        {
            try
            {
                return $$"""
            {
              "pack": {
                "pack_format": {{GetPackFormatJsonValue(request.MinecraftVersion, logger)}},
                "description": "{{jsonText.EscapeStringValue(context.ProjectName)}} - LocalGPT generated Living Cities datapack"
              }
            }
            """;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "CreateDatapackMcmeta");
                return string.Empty;
            }
        }
       
        /// <summary>Executes the write datapack function async operation.</summary>
        /// <param name="context">Input value for context.</param>
        /// <param name="functionPath">Input value for functionPath.</param>
        /// <param name="content">Input value for content.</param>
        /// <param name="cancellationToken">Input value for cancellationToken.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task WriteDatapackFunctionAsync(
    WorkspaceContext context,
    string functionPath,
    string content,
    CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var normalizedPath = functionPath.Replace('/', Path.DirectorySeparatorChar);
                var path = Path.Combine(context.ProjectRoot, "data", context.ModId, "function", $"{normalizedPath}.mcfunction");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, content, catalog.Utf8NoBom, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "WriteDatapackFunctionAsync");
            }
        }
        /// <summary>Executes the datapack reference comparison missing operation.</summary>
        /// <param name="generatedZipPath">Input value for generatedZipPath.</param>
        /// <param name="referenceZipPath">Input value for referenceZipPath.</param>
        /// <param name="summary">Input value for summary.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public DatapackReferenceComparison? DatapackReferenceComparisonMissing(string generatedZipPath, string referenceZipPath, string summary, ILogger logger)
        {
            try
            {
               return new(
                    GeneratedZipPath: generatedZipPath,
                    ReferenceZipPath: referenceZipPath,
                    ReferenceExists: System.IO.File.Exists(referenceZipPath),
                    GeneratedFileCount: 0,
                    GeneratedFunctionFileCount: 0,
                    GeneratedPlaceholderCount: 0,
                    ReferenceFileCount: 0,
                    ReferenceFunctionFileCount: 0,
                    ReferencePlaceholderCount: 0,
                    GeneratedHasRootPackMcmeta: false,
                    ReferenceHasRootPackMcmeta: false,
                    ReferenceHasNestedPackMcmeta: false,
                    GeneratedHasLoadTag: false,
                    GeneratedHasTickTag: false,
                    ReferenceHasLoadTag: false,
                    ReferenceHasTickTag: false,
                    CriticalFileCount: 0,
                    PreservedCriticalFileCount: 0,
                    PreservedCriticalFiles: [],
                    ReferencePlaceholderSamples: [],
                    Summary: summary);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Missing generatedZipPath {generatedZipPath.ToString()} referenceZipPath {referenceZipPath.ToString()} summary {summary.ToString()}");
                return null;
            }
        }

        /// <summary>Executes the build datapack reference comparison operation.</summary>
        /// <param name="workspaceRoot">Input value for workspaceRoot.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public DatapackReferenceComparison? BuildDatapackReferenceComparison(string workspaceRoot, ILogger logger)
        {
            try
            {
                var generatedZip = Directory.Exists(Path.Combine(workspaceRoot, "build"))
      ? Directory.GetFiles(Path.Combine(workspaceRoot, "build"), "*.zip").Order(StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? string.Empty
      : string.Empty;
                var referenceZip = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    "living_cities.zip");

                if (string.IsNullOrWhiteSpace(generatedZip) || !System.IO.File.Exists(generatedZip))
                {
                    return DatapackReferenceComparisonMissing(
                        generatedZip,
                        referenceZip,
                        "Generated benchmark zip was not found.", logger);
                }

                if (!System.IO.File.Exists(referenceZip))
                {
                    return DatapackReferenceComparisonMissing(
                        generatedZip,
                        referenceZip,
                        "Reference living_cities.zip was not found in Downloads.", logger);
                }

                var generatedEntries = ReadZipFileEntries(generatedZip, logger);
                var referenceEntries = ReadZipFileEntries(referenceZip, logger);
                var normalizedReferenceEntries = referenceEntries
                    .Select(filter => NormalizeReferenceDatapackEntry(filter, logger))
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .ToArray();

                var generatedSet = generatedEntries.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var normalizedReferenceSet = normalizedReferenceEntries.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var criticalFiles = new[]
                {
                "pack.mcmeta",
                "data/minecraft/tags/function/load.json",
                "data/minecraft/tags/function/tick.json",
                "data/living_cities/function/core/load.mcfunction",
                "data/living_cities/function/core/tick.mcfunction",
                "data/living_cities/function/city/create.mcfunction",
                "data/living_cities/function/citizens/register.mcfunction",
                "data/living_cities/function/ui/status.mcfunction"
            };
                var preservedCriticalFiles = criticalFiles
                    .Where(file => generatedSet.Contains(file) && normalizedReferenceSet.Contains(file))
                    .ToArray();
                var generatedPlaceholders = generatedEntries
                    .Where(entry => entry.EndsWith(".mcfunction.txt", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var referencePlaceholders = referenceEntries
                    .Where(entry => entry.EndsWith(".mcfunction.txt", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                var summary = string.Join(" ", new[]
                {
                $"Generated zip has {generatedEntries.Length} files and {generatedEntries.Count(filter => IsMcFunctionPath(filter,logger))} functions.",
                $"Reference zip has {referenceEntries.Length} files and {referenceEntries.Count(filter => IsMcFunctionPath(filter,logger))} real functions plus {referencePlaceholders.Length} placeholders.",
                "Generated zip has root pack.mcmeta/load/tick tags; reference keeps those under a top-level folder, so it is useful as a design benchmark but less install-ready as a zip."
            });

                return new DatapackReferenceComparison(
                    GeneratedZipPath: generatedZip,
                    ReferenceZipPath: referenceZip,
                    ReferenceExists: true,
                    GeneratedFileCount: generatedEntries.Length,
                    GeneratedFunctionFileCount: generatedEntries.Count(filter => IsMcFunctionPath(filter, logger)),
                    GeneratedPlaceholderCount: generatedPlaceholders.Length,
                    ReferenceFileCount: referenceEntries.Length,
                    ReferenceFunctionFileCount: referenceEntries.Count(filter => IsMcFunctionPath(filter, logger)),
                    ReferencePlaceholderCount: referencePlaceholders.Length,
                    GeneratedHasRootPackMcmeta: generatedSet.Contains("pack.mcmeta"),
                    ReferenceHasRootPackMcmeta: referenceEntries.Contains("pack.mcmeta", StringComparer.OrdinalIgnoreCase),
                    ReferenceHasNestedPackMcmeta: normalizedReferenceSet.Contains("pack.mcmeta"),
                    GeneratedHasLoadTag: generatedSet.Contains("data/minecraft/tags/function/load.json"),
                    GeneratedHasTickTag: generatedSet.Contains("data/minecraft/tags/function/tick.json"),
                    ReferenceHasLoadTag: normalizedReferenceSet.Contains("data/minecraft/tags/function/load.json"),
                    ReferenceHasTickTag: normalizedReferenceSet.Contains("data/minecraft/tags/function/tick.json"),
                    CriticalFileCount: criticalFiles.Length,
                    PreservedCriticalFileCount: preservedCriticalFiles.Length,
                    PreservedCriticalFiles: preservedCriticalFiles,
                    ReferencePlaceholderSamples: referencePlaceholders.Take(12).ToArray(),
                    Summary: summary);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildDatapackReferenceComparison {ex.ToString()} workspaceRoot {workspaceRoot?.ToString()}");
                return null;
            }
        }
        /// <summary>Executes the read zip file entries operation.</summary>
        /// <param name="zipPath">Input value for zipPath.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string[] ReadZipFileEntries(string zipPath, ILogger logger)
        {
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                return archive.Entries
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
                    .Select(entry => entry.FullName.Replace('\\', '/'))
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ReadZipFileEntries {ex.ToString()} zipPath {zipPath?.ToString()}");
                return new string[0];
            }

        }

        /// <summary>Executes the normalize reference datapack entry operation.</summary>
        /// <param name="entry">Input value for entry.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string NormalizeReferenceDatapackEntry(string entry, ILogger logger)
        {
            try
            {
                var normalized = entry.Replace('\\', '/').TrimStart('/');
                const string nestedPrefix = "living_cities/";
                return normalized.StartsWith(nestedPrefix, StringComparison.OrdinalIgnoreCase)
                    ? normalized[nestedPrefix.Length..]
                    : normalized;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NormalizeReferenceDatapackEntry {ex.ToString()} entry {entry?.ToString()}");
                return string.Empty;
            }

        }

        /// <summary>Executes the is mc function path operation.</summary>
        /// <param name="entry">Input value for entry.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool IsMcFunctionPath(string entry, ILogger logger)
        {
            try
            {
                return entry.EndsWith(".mcfunction", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsMcFunctionPath {ex.ToString()} entry {entry?.ToString()}");
                return false;
            }
        }
        /// <summary>Executes the validate generated datapack workspace operation.</summary>
        /// <param name="rootPath">Input value for rootPath.</param>
        /// <param name="logger">Input value for logger.</param>
        public void ValidateGeneratedDatapackWorkspace(string rootPath, ILogger logger)
        {
            try
            {
                var packPath = Path.Combine(rootPath, "pack.mcmeta");
                var dataPath = Path.Combine(rootPath, "data");
                if (!File.Exists(packPath))
                    throw new InvalidOperationException("Generated datapack is missing root pack.mcmeta.");
                if (!Directory.Exists(dataPath))
                    throw new InvalidOperationException("Generated datapack is missing root data folder.");

                JsonDocument.Parse(File.ReadAllText(packPath));
                foreach (var tagPath in Directory.GetFiles(Path.Combine(dataPath, "minecraft", "tags", "function"), "*.json"))
                    JsonDocument.Parse(File.ReadAllText(tagPath));

                var nestedPack = Directory
                    .EnumerateDirectories(rootPath)
                    .Select(directory => Path.Combine(directory, "pack.mcmeta"))
                    .FirstOrDefault(File.Exists);
                if (nestedPack is not null)
                    throw new InvalidOperationException("Generated datapack has a nested wrapper folder containing pack.mcmeta.");

                var pluralFunctionsFolder = Directory
                    .EnumerateDirectories(dataPath, "functions", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (pluralFunctionsFolder is not null)
                    throw new InvalidOperationException("Generated datapack contains legacy plural functions folder; Minecraft 1.21+ uses function.");

                var txtPlaceholder = Directory
                    .EnumerateFiles(dataPath, "*.mcfunction.txt", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (txtPlaceholder is not null)
                    throw new InvalidOperationException("Generated datapack contains .mcfunction.txt placeholder files.");

                foreach (var functionFile in Directory.EnumerateFiles(dataPath, "*.mcfunction", SearchOption.AllDirectories))
                {
                    var content = File.ReadAllText(functionFile);
                    if (catalog.LeadingSlashCommandPattern.IsMatch(content))
                        throw new InvalidOperationException($"Generated function contains a leading slash command: {Path.GetRelativePath(rootPath, functionFile)}");
                    if (catalog.RootStorageRemovePattern.IsMatch(content))
                        throw new InvalidOperationException($"Generated function uses data remove storage root syntax: {Path.GetRelativePath(rootPath, functionFile)}");
                    if (catalog.MalformedStorageTargetPattern.IsMatch(content))
                        throw new InvalidOperationException($"Generated function appears to put an NBT path into the storage id instead of after it: {Path.GetRelativePath(rootPath, functionFile)}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateGeneratedDatapackWorkspace rootPath:{rootPath}");
             
            }
           
        }

        /// <summary>
        /// Determines whether minecraft datapack artifact target as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="prompt">Prompt value supplied to the council text operation and used when producing its result.</param>
        /// <param name="finalAnswer">Final answer value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The bool produced by the operation.</returns>
        public bool? IsMinecraftDatapackArtifactTarget(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {
                var text = prompt;
                return patterns.MinecraftPattern.IsMatch(text) && patterns.DatapackPattern.IsMatch(text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not detect whether the response targets a Minecraft datapack artifact.");
                return null;
            }
        }

        /// <summary>
        /// Determines whether minecraft skeleton matrix artifact target as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="prompt">Prompt value supplied to the council text operation and used when producing its result.</param>
        /// <param name="finalAnswer">Final answer value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The bool produced by the operation.</returns>
        public bool? IsMinecraftSkeletonMatrixArtifactTarget(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {
                var text = prompt;
                return patterns.MinecraftPattern.IsMatch(text) && patterns.MinecraftSkeletonMatrixPattern.IsMatch(text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not detect whether the response targets a Minecraft loader matrix artifact.");
                return null;
            }
        }

        /// <summary>
        /// Performs extract minecraft version as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ExtractMinecraftVersion(string text, ILogger logger)
        {
            try
            {
                var match = patterns.MinecraftVersionPattern.Match(text);
                return match.Success
                    ? match.Groups["version"].Value
                    : catalog.DefaultMinecraftVersion;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ExtractMinecraftVersion text:{text}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds minecraft datapack artifact identity as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="timestamp">Timestamp value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The minecraft datapack artifact identity produced by the operation.</returns>
        public MinecraftDatapackArtifactIdentity? BuildMinecraftDatapackArtifactIdentity(string text, string timestamp, ILogger logger)
        {
            try
            {
                var displayName = ExtractMinecraftProjectDisplayName(text,null, logger);
                var modId = ToMinecraftNamespace(displayName, logger);
                var projectName = _text.ToPascalIdentifier(displayName, logger);
                if (string.IsNullOrWhiteSpace(projectName))
                    projectName = "PromptedDatapack";
                if (string.IsNullOrWhiteSpace(modId))
                    modId = "prompted_datapack";

                return new MinecraftDatapackArtifactIdentity(
                    $"{projectName}Council{timestamp.Replace("-", string.Empty, StringComparison.Ordinal)}",
                    modId,
                    $"com.localgpt.{modId.Replace("_", string.Empty, StringComparison.Ordinal)}",
                    displayName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildMinecraftDatapackArtifactIdentity text:{text} timestamp:{timestamp}");
                return null;
            }
        }
        /// <summary>
        /// Performs extract minecraft project display name as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="text">Text value supplied to the council text operation and used when producing its result.</param>
        /// <param name="harmonyModel">Harmony model value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ExtractMinecraftProjectDisplayName(string text, bool? harmonyModel, ILogger logger)
        {
            try
            {
                harmonyModel=harmonyModel ?? false;
                var quoted = patterns.MinecraftQuotedProjectNamePattern.Match(text);
                if (quoted.Success)
                    return CleanMinecraftProjectDisplayName(quoted.Groups["name"].Value, logger);

                var explicitlyNamed = patterns.MinecraftExplicitProjectNamePattern.Match(text);
                if (explicitlyNamed.Success)
                    return CleanMinecraftProjectDisplayName(explicitlyNamed.Groups["name"].Value, logger);

                var named = patterns.MinecraftNamedProjectPattern.Match(text);
                if (named.Success)
                    return CleanMinecraftProjectDisplayName(named.Groups["name"].Value, logger);

                var heading = patterns.MarkdownHeadingProjectNamePattern.Match(text);
                if (heading.Success)
                    return CleanMinecraftProjectDisplayName(heading.Groups["name"].Value, logger);

                return "Prompted Datapack";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ExtractMinecraftProjectDisplayName text:{text}");
                return string.Empty;
            }
            
        }
        /// <summary>
        /// Performs clean minecraft project display name as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string CleanMinecraftProjectDisplayName(string value, ILogger logger)
        {
            try
            {
                var trimmed = value.Trim();
                foreach (var separator in new[] { " with ", " for ", " and ", " that ", " the ", " zip ", " pack " })
                {
                    var index = trimmed.IndexOf(separator, StringComparison.OrdinalIgnoreCase);
                    if (index > 2)
                        trimmed = trimmed[..index].Trim();
                }

                return string.IsNullOrWhiteSpace(trimmed) ? "Prompted Datapack" : trimmed;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CleanMinecraftProjectDisplayName value:{value}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Performs to minecraft namespace as part of the council text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the council text operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string ToMinecraftNamespace(string value, ILogger logger)
        {
            try
            {
                var normalized = patterns.IdentifierSeparatorPattern.Replace(value.ToLowerInvariant(), "_").Trim('_');
                return string.IsNullOrWhiteSpace(normalized) ? "prompted_datapack" : normalized;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToMinecraftNamespace value:{value}");
                return string.Empty;
            }
        }


    }
}
