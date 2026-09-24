using LocalGPT.BusinessObjects;
using LocalGptConfigurationRoot = LocalGPT.BusinessObjects.ConfigurationRoot;
using LocalGPT.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LocalGPT.Services;

/// <summary>Searches Hugging Face model metadata without executing repository code.</summary>
public sealed class HuggingFaceModelCatalogService(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<LocalGptConfigurationRoot> options,
    ILogger<HuggingFaceModelCatalogService> logger) : IHuggingFaceModelCatalogService
{
    public async Task<IReadOnlyList<HuggingFaceModelSearchResult>> SearchAsync(HuggingFaceModelSearchRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(request);
            var query = (request.Query ?? string.Empty).Trim();
            if (query.Length > 300)
                throw new ArgumentException("Hugging Face search query cannot exceed 300 characters.", nameof(request));
            var limit = Math.Clamp(request.Limit, 1, 50);
            var route = $"https://huggingface.co/api/models?search={Uri.EscapeDataString(query)}&limit={limit}&sort=downloads&direction=-1&full=true";
            using var message = new HttpRequestMessage(HttpMethod.Get, route);
            var tokenName = options.CurrentValue.PythonCore?.HuggingFaceTokenEnvironmentVariable;
            if (!string.IsNullOrWhiteSpace(tokenName))
            {
                var token = Environment.GetEnvironmentVariable(tokenName.Trim());
                if (!string.IsNullOrWhiteSpace(token))
                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            message.Headers.UserAgent.ParseAdd("LocalGPT/4.9.0");

            var client = httpClientFactory.CreateClient("LocalGPTHuggingFace");
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var results = new List<HuggingFaceModelSearchResult>();
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return results;
            foreach (var item in document.RootElement.EnumerateArray())
            {
                var modelId = GetString(item, "modelId");
                if (string.IsNullOrWhiteSpace(modelId))
                    modelId = GetString(item, "id");
                if (string.IsNullOrWhiteSpace(modelId))
                    continue;
                var tags = item.TryGetProperty("tags", out var tagsNode) && tagsNode.ValueKind == JsonValueKind.Array
                    ? tagsNode.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString() ?? string.Empty).Where(value => value.Length > 0).ToList()
                    : [];
                var pipelineTag = GetString(item, "pipeline_tag");
                var capabilities = DetectCapabilities(modelId, pipelineTag, tags);
                if (request.Capability != LocalAiCapability.Unknown && !capabilities.Contains(request.Capability))
                    continue;
                results.Add(new HuggingFaceModelSearchResult
                {
                    ModelId = modelId,
                    Author = modelId.Contains('/') ? modelId[..modelId.IndexOf('/')] : string.Empty,
                    PipelineTag = pipelineTag,
                    LibraryName = GetString(item, "library_name"),
                    IsPrivate = GetBoolean(item, "private"),
                    IsGated = item.TryGetProperty("gated", out var gated) && gated.ValueKind is JsonValueKind.True or JsonValueKind.String,
                    Downloads = GetInt64(item, "downloads"),
                    Likes = (int)Math.Min(int.MaxValue, GetInt64(item, "likes")),
                    LastModifiedUtc = DateTime.TryParse(GetString(item, "lastModified"), out var modified) ? modified.ToUniversalTime() : null,
                    Tags = tags,
                    Capabilities = capabilities
                });
            }
            logger.LogInformation("Hugging Face metadata search returned {Count} compatible result(s); query text and credentials were omitted from logs.", results.Count);
            return results;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HuggingFaceModelCatalogService)}.{nameof(SearchAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HuggingFaceModelCatalogService)}.{nameof(SearchAsync)} failed.");
        throw;
    }
}

    private List<LocalAiCapability> DetectCapabilities(string modelId, string pipelineTag, IReadOnlyCollection<string> tags)
    {
    try
    {
            var evidence = string.Join(' ', new[] { modelId, pipelineTag }.Concat(tags)).ToLowerInvariant();
            var result = new HashSet<LocalAiCapability>();
            if (evidence.Contains("text-to-image") || evidence.Contains("image-generation") || evidence.Contains("diffusion") || evidence.Contains("flux") || evidence.Contains("qwen-image") || evidence.Contains("z-image"))
                result.Add(LocalAiCapability.ImageGeneration);
            if (evidence.Contains("image-to-image") || evidence.Contains("image-edit"))
                result.Add(LocalAiCapability.ImageEditing);
            if (evidence.Contains("text-to-video") || evidence.Contains("video-generation") || evidence.Contains("wan2") || evidence.Contains("wan-"))
                result.Add(LocalAiCapability.TextToVideo);
            if (evidence.Contains("image-to-video"))
                result.Add(LocalAiCapability.ImageToVideo);
            if (evidence.Contains("automatic-speech-recognition") || evidence.Contains("whisper") || evidence.Contains("speech-recognition") || evidence.Contains("asr"))
                result.Add(LocalAiCapability.SpeechRecognition);
            if (evidence.Contains("text-to-speech") || evidence.Contains("speech-synthesis"))
                result.Add(LocalAiCapability.SpeechSynthesis);
            if (evidence.Contains("text-to-audio") || evidence.Contains("audio-generation"))
                result.Add(LocalAiCapability.AudioGeneration);
            if (evidence.Contains("image-text-to-text") || evidence.Contains("visual-question-answering") || evidence.Contains("vision-language"))
                result.Add(LocalAiCapability.ImageUnderstanding);
            if (evidence.Contains("feature-extraction") || evidence.Contains("sentence-transformers") || evidence.Contains("embedding"))
                result.Add(LocalAiCapability.Embeddings);
            return result.OrderBy(value => value).ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HuggingFaceModelCatalogService)}.{nameof(DetectCapabilities)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HuggingFaceModelCatalogService)}.{nameof(DetectCapabilities)} failed.");
        throw;
    }
}

    private string GetString(JsonElement element, string name) {
    try
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HuggingFaceModelCatalogService)}.{nameof(GetString)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HuggingFaceModelCatalogService)}.{nameof(GetString)} failed.");
        throw;
    }
}
    private bool GetBoolean(JsonElement element, string name) {
    try
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HuggingFaceModelCatalogService)}.{nameof(GetBoolean)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HuggingFaceModelCatalogService)}.{nameof(GetBoolean)} failed.");
        throw;
    }
}
    private long GetInt64(JsonElement element, string name) {
    try
    {
        return element.TryGetProperty(name, out var value) && value.TryGetInt64(out var number) ? number : 0;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(HuggingFaceModelCatalogService)}.{nameof(GetInt64)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(HuggingFaceModelCatalogService)}.{nameof(GetInt64)} failed.");
        throw;
    }
}
}
