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
public sealed class OrganicAddonManifestService(
    IWebHostEnvironment environment,
    IOneWireConnectionRegistry connections,
    IOneWirePeerRegistry peers,
    ILogger<OrganicAddonManifestService> logger) : IOrganicAddonManifestService
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Gets manifests.
    /// </summary>
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
    /// Gets skill descriptors.
    /// </summary>
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
    /// Gets catalog entries.
    /// </summary>
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
    /// Determines whether peer online.
    /// </summary>
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
    /// Gets manifest directories.
    /// </summary>
    private IEnumerable<string> GetManifestDirectories()
    {
        logger.LogTrace("Organic add-on manifest directory enumeration started.");
        try
        {
            var contentDirectory = Path.Combine(environment.ContentRootPath, "Configuration", "OrganicAddons");
            yield return contentDirectory;

            var outputDirectory = Path.Combine(AppContext.BaseDirectory, "Configuration", "OrganicAddons");
            if (!string.Equals(outputDirectory, contentDirectory, StringComparison.OrdinalIgnoreCase))
                yield return outputDirectory;
        }
        finally
        {
            logger.LogTrace("Organic add-on manifest directory enumeration completed.");
        }
    }

    /// <summary>
    /// Runs the normalize operation.
    /// </summary>
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
    /// Normalizes list.
    /// </summary>
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
    /// Normalizes identifier.
    /// </summary>
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
