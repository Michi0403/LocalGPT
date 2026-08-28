using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;

namespace LocalGPT.Services;

/// <summary>
/// Loads source-controlled organic add-on manifests. Offline manifests are discovery metadata only;
/// they become available for invocation only when the matching trusted 1-Wire peer is connected.
/// </summary>
/// <param name="environment">Web host environment dependency used by the organic addon manifest workflow to provide the corresponding application capability.</param>
/// <param name="connections">One wire connection registry dependency used by the organic addon manifest workflow to provide the corresponding application capability.</param>
/// <param name="peers">One wire peer registry dependency used by the organic addon manifest workflow to provide the corresponding application capability.</param>
/// <param name="platform">Platform runtime service used for cross-platform manifest path normalization and comparison.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OrganicAddonManifestService(
    IWebHostEnvironment environment,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    IPlatformRuntimeService platform,
    ILogger<OrganicAddonManifestService> logger) : IOrganicAddonManifestService
{
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="OrganicAddonManifestService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Retrieves manifests as part of the organic addon manifest service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<OrganicAddonManifest> GetManifests()
    {
        var manifests = new Dictionary<string, OrganicAddonManifest>(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in GetManifestDirectories())
        {
            if (!Directory.Exists(directory))
                continue;

            foreach (var path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
                         .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var manifest = JsonSerializer.Deserialize<OrganicAddonManifest>(File.ReadAllText(path), jsonOptions);
                    if (manifest is null || string.IsNullOrWhiteSpace(manifest.Key) || string.IsNullOrWhiteSpace(manifest.SourcePeerId))
                    {
                        logger.LogWarning("Ignored invalid organic add-on manifest {ManifestPath}.", path);
                        continue;
                    }

                    Normalize(manifest);
                    manifests[manifest.Key] = manifest;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
                {
                    logger.LogWarning(exception, "Could not read organic add-on manifest {ManifestPath}.", path);
                }
            }
        }

        var result = manifests.Values.OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        logger.LogInformation("Discovered {ManifestCount} offline organic add-on manifest(s).", result.Count);
        return result;
    }

    /// <summary>
    /// Retrieves skill descriptors as part of the organic addon manifest service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<OneWireSkillDescriptor> GetSkillDescriptors() {
    try
    {
        return GetManifests()
        .Select(manifest => new OneWireSkillDescriptor
        {
            Key = manifest.Key,
            DisplayName = manifest.DisplayName,
            Description = manifest.Description,
            SourcePeerId = manifest.SourcePeerId,
            Organs = [.. manifest.Organs],
            CapabilityKeys = [.. manifest.CapabilityKeys],
            UiActivationKeys = [.. manifest.UiActivationKeys],
            IsOnline = IsPeerOnline(manifest.SourcePeerId),
            IsEnabled = true,
            UpdatedUtc = DateTimeOffset.UtcNow
        })
        .ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicAddonManifestService)}.{nameof(GetSkillDescriptors)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicAddonManifestService)}.{nameof(GetSkillDescriptors)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves catalog entries as part of the organic addon manifest service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<DxAiFunctionCatalogEntry> GetCatalogEntries()
    {
    try
    {
            var entries = new List<DxAiFunctionCatalogEntry>();
            foreach (var manifest in GetManifests())
            {
                var online = IsPeerOnline(manifest.SourcePeerId);
                foreach (var method in manifest.ControllerMethods)
                {
                    var signature = $"{manifest.Key}|{method.Controller}|{method.MethodName}|{method.HttpMethod}|{method.Route}";
                    var shortHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signature))).ToLowerInvariant()[..12];
                    var functionName = $"organic.{NormalizeIdentifier(manifest.Key)}.{NormalizeIdentifier(method.Controller)}.{NormalizeIdentifier(method.MethodName)}.{shortHash}";
                    var entry = new DxAiFunctionCatalogEntry
                    {
                        CatalogKey = $"organic-controller:{shortHash}",
                        Kind = "OrganicAddonControllerMethod",
                        FunctionName = functionName,
                        DisplayName = $"{manifest.DisplayName}: {method.Controller}.{method.MethodName}",
                        Purpose = string.IsNullOrWhiteSpace(method.Purpose)
                            ? $"Discover the {method.HttpMethod} {method.Route} controller method exposed by the {manifest.DisplayName} organic add-on."
                            : method.Purpose,
                        Method = method.HttpMethod,
                        Route = method.Route,
                        ParameterSchemaJson = "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":true}",
                        Source = $"OrganicAddonManifest:{manifest.Key}",
                        ServiceContractTypeName = manifest.SourcePeerId,
                        ImplementationTypeName = method.Controller,
                        ServiceMethodName = method.MethodName,
                        ParameterTypeNamesJson = "[]",
                        IsReadOnly = method.IsReadOnly,
                        IsAvailable = online,
                        IsEnabled = true,
                        ExposeToAiChat = false,
                        ExposeToOneWire = false,
                        AllowRemoteInvocation = false,
                        RequiresFrontendConfirmation = true,
                        InteractionEditor = OneWireInteractionEditor.Json,
                        IsSystemSeed = true,
                        UpdatedBy = "Organic add-on manifest"
                    };
                    entry.DescriptorHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
                        entry.Kind,
                        entry.FunctionName,
                        entry.Method,
                        entry.Route,
                        entry.Source)))).ToLowerInvariant();
                    entries.Add(entry);
                }
            }

            logger.LogInformation(
                "Discovered {ControllerMethodCount} organic add-on controller method descriptor(s); offline methods remain discovery-only.",
                entries.Count);
            return entries;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicAddonManifestService)}.{nameof(GetCatalogEntries)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicAddonManifestService)}.{nameof(GetCatalogEntries)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether peer online as part of the organic addon manifest service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sourcePeerId">Identifier of the source peer to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsPeerOnline(string sourcePeerId)
    {
    try
    {
            if (connections.IsConnected(sourcePeerId))
                return true;

            return peers.GetPeers().Any(peer =>
                peer.IsConnected &&
                (string.Equals(peer.PeerId, sourcePeerId, StringComparison.OrdinalIgnoreCase) ||
                 peer.PeerId.StartsWith($"{sourcePeerId}:", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(peer.Application, sourcePeerId, StringComparison.OrdinalIgnoreCase)));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicAddonManifestService)}.{nameof(IsPeerOnline)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicAddonManifestService)}.{nameof(IsPeerOnline)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves manifest directories as part of the organic addon manifest service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private IEnumerable<string> GetManifestDirectories()
    {
        logger.LogTrace("Organic add-on manifest directory enumeration started.");
        try
        {
            var contentDirectory = Path.Combine(environment.ContentRootPath, "Configuration", "OrganicAddons");
            yield return contentDirectory;

            var outputDirectory = Path.Combine(AppContext.BaseDirectory, "Configuration", "OrganicAddons");
            if (!platform.PathsEqual(outputDirectory, contentDirectory))
                yield return outputDirectory;
        }
        finally
        {
            logger.LogTrace("Organic add-on manifest directory enumeration completed.");
        }
    }

    /// <summary>
    /// Performs normalize as part of the organic addon manifest service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="manifest">Manifest value supplied to the organic addon manifest operation and used when producing its result.</param>
    private void Normalize(OrganicAddonManifest manifest)
    {
    try
    {
            manifest.Key = manifest.Key.Trim().ToLowerInvariant();
            manifest.DisplayName = string.IsNullOrWhiteSpace(manifest.DisplayName) ? manifest.Key : manifest.DisplayName.Trim();
            manifest.Description = manifest.Description?.Trim() ?? string.Empty;
            manifest.SourcePeerId = manifest.SourcePeerId.Trim();
            manifest.Organs = NormalizeList(manifest.Organs);
            manifest.CapabilityKeys = NormalizeList(manifest.CapabilityKeys);
            manifest.UiActivationKeys = NormalizeList(manifest.UiActivationKeys);
            manifest.ControllerMethods = (manifest.ControllerMethods ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item.Controller) && !string.IsNullOrWhiteSpace(item.MethodName) && !string.IsNullOrWhiteSpace(item.Route))
                .GroupBy(item => $"{item.Controller}|{item.MethodName}|{item.HttpMethod}|{item.Route}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(item => item.Controller, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.MethodName, StringComparer.OrdinalIgnoreCase)
                .ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicAddonManifestService)}.{nameof(Normalize)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicAddonManifestService)}.{nameof(Normalize)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes list as part of the organic addon manifest service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="values">String dependency used by the organic addon manifest workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> NormalizeList(IEnumerable<string>? values) {
    try
    {
        return (values ?? [])
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicAddonManifestService)}.{nameof(NormalizeList)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicAddonManifestService)}.{nameof(NormalizeList)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes identifier as part of the organic addon manifest service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the organic addon manifest operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeIdentifier(string value)
    {
    try
    {
            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
                builder.Append(char.IsLetterOrDigit(character) || character == '_' ? char.ToLowerInvariant(character) : '_');
            return builder.ToString().Trim('_');
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicAddonManifestService)}.{nameof(NormalizeIdentifier)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicAddonManifestService)}.{nameof(NormalizeIdentifier)} failed.");
        throw;
    }
}
}
