using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using LocalGptConfigurationRoot = LocalGPT.BusinessObjects.ConfigurationRoot;

namespace LocalGPT.Services;

/// <summary>Owns safe workspace-image resolution and Ollama capability probing before delegating inference to the shared local vision OCR service.</summary>
public sealed class WorkspaceVisionOcrService(
    IOptionsMonitor<LocalGptConfigurationRoot> options,
    IChatUploadWorkspaceService workspaces,
    ILocalVisionOcrService ocr,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    CouncilRuntimeService councilRuntime,
    ILogger<WorkspaceVisionOcrService> logger) : IWorkspaceVisionOcrService
{
    private readonly Version deepSeekOcrMinimumOllamaVersion = new(0, 13, 0);
    private readonly HashSet<string> imageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".tif", ".tiff"
    };

    /// <inheritdoc />
    public async Task<LocalVisionOcrCapability> GetCapabilityAsync(string? requestedModel = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var requested = string.IsNullOrWhiteSpace(requestedModel) ? "deepseek-ocr" : requestedModel.Trim();
            var hosts = ConfiguredOllamaHosts();
            if (hosts.Count == 0)
                return Unavailable(requested, "No Ollama host is configured in LocalGPT.");

            LocalVisionOcrCapability? bestReachable = null;
            foreach (var host in hosts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var capability = await ProbeHostAsync(host, requested, cancellationToken).ConfigureAwait(false);
                if (capability.Available)
                    return capability;
                if (!string.IsNullOrWhiteSpace(capability.OllamaVersion))
                    bestReachable ??= capability;
            }
            return bestReachable ?? Unavailable(requested, "No configured Ollama host answered the OCR capability probe.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Probing local workspace OCR capability failed.");
            return Unavailable(requestedModel ?? "deepseek-ocr", "The OCR capability probe failed. Review local diagnostics.");
        }
    }

    /// <inheritdoc />
    public async Task<WorkspaceImageOcrResult> RecognizeWorkspaceImageAsync(WorkspaceImageOcrRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.WorkspaceName);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.RelativePath);
            var workspaceRoot = workspaces.ResolveWorkspacePath(request.WorkspaceName)
                ?? throw new KeyNotFoundException($"Upload workspace '{request.WorkspaceName}' was not found.");
            var normalizedRelative = request.RelativePath.Replace('\\', '/').TrimStart('/');
            if (!normalizedRelative.StartsWith("original/", StringComparison.OrdinalIgnoreCase)
                && !normalizedRelative.StartsWith("extracted/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Workspace OCR accepts only quarantined original/ or reviewed extracted/ image evidence.");

            var imagePath = councilRuntime.ResolveWorkspaceFile(workspaceRoot, normalizedRelative, logger)
                ?? throw new InvalidOperationException("The requested workspace image path is outside the bounded workspace.");
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("The requested workspace image does not exist.");
            var extension = Path.GetExtension(imagePath);
            if (!imageExtensions.Contains(extension))
                throw new InvalidOperationException("The requested workspace file is not a supported local OCR image format.");

            var maximumImageBytes = Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.LocalVisionMaximumImageBytes));
            var info = new FileInfo(imagePath);
            if (info.Length <= 0 || info.Length > maximumImageBytes)
                throw new InvalidOperationException($"The OCR image must be between 1 byte and the configured LocalVisionMaximumImageBytes value ({maximumImageBytes} bytes).");

            var capability = await GetCapabilityAsync(request.ModelName, cancellationToken).ConfigureAwait(false);
            if (!capability.Available)
                throw new InvalidOperationException(capability.Reason);

            var bytes = await File.ReadAllBytesAsync(imagePath, cancellationToken).ConfigureAwait(false);
            var dataUrl = $"data:{MediaType(extension)};base64,{Convert.ToBase64String(bytes)}";
            var result = await ocr.RecognizeAsync(new LocalVisionOcrRequest
            {
                ImageDataUrl = dataUrl,
                ModelName = capability.ModelName,
                Prompt = request.Prompt,
                MaximumOutputTokens = request.MaximumOutputTokens
            }, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Completed bounded workspace OCR for {WorkspaceName}/{RelativePath} using {ModelName}; image bytes were omitted from diagnostics.",
                request.WorkspaceName,
                normalizedRelative,
                result.ModelName);
            return new WorkspaceImageOcrResult
            {
                WorkspaceName = request.WorkspaceName,
                RelativePath = normalizedRelative,
                Ocr = result
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Workspace image OCR failed; image content was omitted from diagnostics.");
            throw;
        }
    }

    private async Task<LocalVisionOcrCapability> ProbeHostAsync(string host, string requestedModel, CancellationToken cancellationToken)
    {
        try
        {
            var configuredTimeoutSeconds = runtimePolicy.GetInt(LocalGptRuntimeValue.LocalVisionRequestTimeoutSeconds);
            var normalizedHost = host.EndsWith('/') ? host : string.Concat(host, '/');
            var baseAddress = new Uri(normalizedHost, UriKind.Absolute);
            using var client = new HttpClient
            {
                BaseAddress = baseAddress,
                Timeout = configuredTimeoutSeconds <= 0 ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(Math.Min(configuredTimeoutSeconds, 10))
            };
            using var versionResponse = await client.GetAsync("api/version", cancellationToken).ConfigureAwait(false);
            if (!versionResponse.IsSuccessStatusCode)
                return Unavailable(requestedModel, $"Ollama at {host} did not return a usable version response.", host);
            using var versionDocument = JsonDocument.Parse(await versionResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            var versionText = versionDocument.RootElement.TryGetProperty("version", out var versionElement) ? versionElement.GetString() ?? string.Empty : string.Empty;

            using var tagsResponse = await client.GetAsync("api/tags", cancellationToken).ConfigureAwait(false);
            if (!tagsResponse.IsSuccessStatusCode)
                return Unavailable(requestedModel, $"Ollama at {host} did not return its installed model list.", host, versionText);
            using var tagsDocument = JsonDocument.Parse(await tagsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            var installedModels = ReadInstalledModelNames(tagsDocument.RootElement);
            var installed = installedModels.FirstOrDefault(model => ModelMatches(model, requestedModel));
            if (string.IsNullOrWhiteSpace(installed))
                return new LocalVisionOcrCapability
                {
                    Available = false,
                    ProviderUri = host,
                    ModelName = requestedModel,
                    OllamaVersion = versionText,
                    ModelInstalled = false,
                    RuntimeCompatible = RuntimeCompatible(requestedModel, versionText),
                    Requirement = RequirementFor(requestedModel),
                    Reason = $"Model '{requestedModel}' is not installed on Ollama host {host}."
                };

            var compatible = RuntimeCompatible(installed, versionText);
            return new LocalVisionOcrCapability
            {
                Available = compatible,
                ProviderUri = host,
                ModelName = installed,
                OllamaVersion = versionText,
                ModelInstalled = true,
                RuntimeCompatible = compatible,
                Requirement = RequirementFor(installed),
                Reason = compatible ? "OCR capability is ready." : $"{installed} requires {RequirementFor(installed)}; detected Ollama {versionText}."
            };
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or UriFormatException)
        {
            logger.LogDebug(exception, "Ollama OCR capability probe did not complete for one configured host.");
            return Unavailable(requestedModel, "Configured Ollama host did not answer the bounded OCR probe.", host);
        }
    }

    private List<string> ConfiguredOllamaHosts()
    {
        try
        {
            var ai = options.CurrentValue.AICore ?? new AICoreOptions();
            return new[] { ai.OllamaCore }
                .Concat(ai.OllamaCores ?? [])
                .Where(item => item is not null && !string.IsNullOrWhiteSpace(item.Uri))
                .Select(item => item.Uri.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving configured Ollama hosts for OCR failed.");
            throw;
        }
    }

    private List<string> ReadInstalledModelNames(JsonElement root)
    {
        try
        {
            var result = new List<string>();
            if (!root.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
                return result;
            foreach (var model in models.EnumerateArray())
            {
                if (model.TryGetProperty("name", out var name) && !string.IsNullOrWhiteSpace(name.GetString()))
                    result.Add(name.GetString()!);
                else if (model.TryGetProperty("model", out var modelName) && !string.IsNullOrWhiteSpace(modelName.GetString()))
                    result.Add(modelName.GetString()!);
            }
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading installed Ollama model names failed.");
            throw;
        }
    }

    private bool RuntimeCompatible(string modelName, string versionText)
    {
        try
        {
            if (!IsDeepSeekOcr(modelName))
                return true;
            var numeric = versionText.Split('-', '+')[0];
            return Version.TryParse(numeric, out var version) && version >= deepSeekOcrMinimumOllamaVersion;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Checking OCR runtime compatibility failed.");
            return false;
        }
    }

    private bool ModelMatches(string installed, string requested)
    {
        try
        {
            return string.Equals(installed, requested, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(RemoveModelTag(installed), RemoveModelTag(requested), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Comparing OCR model identities failed.");
            throw;
        }
    }

    private string RemoveModelTag(string modelName)
    {
        try
        {
            var separator = modelName.LastIndexOf(':');
            return separator > 0 ? modelName[..separator] : modelName;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing OCR model identity failed.");
            throw;
        }
    }

    private bool IsDeepSeekOcr(string modelName)
    {
        try
        {
            return RemoveModelTag(modelName).Equals("deepseek-ocr", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Checking whether an OCR model is DeepSeek OCR failed.");
            throw;
        }
    }

    private string RequirementFor(string modelName)
    {
        try
        {
            return IsDeepSeekOcr(modelName) ? "Ollama 0.13.0 or later" : "An installed Ollama-compatible vision/OCR model";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving the OCR runtime requirement failed.");
            throw;
        }
    }

    private LocalVisionOcrCapability Unavailable(string modelName, string reason, string providerUri = "", string ollamaVersion = "")
    {
        try
        {
            return new LocalVisionOcrCapability
            {
                Available = false,
                ProviderUri = providerUri,
                ModelName = modelName,
                OllamaVersion = ollamaVersion,
                ModelInstalled = false,
                RuntimeCompatible = false,
                Requirement = RequirementFor(modelName),
                Reason = reason
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building an unavailable OCR capability result failed.");
            throw;
        }
    }

    private string MediaType(string extension)
    {
        try
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".tif" or ".tiff" => "image/tiff",
                _ => "image/png"
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving OCR image media type failed.");
            throw;
        }
    }
}
