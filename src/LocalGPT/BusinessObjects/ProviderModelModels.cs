using System.Security.Cryptography;
using System.Text;

namespace LocalGPT.BusinessObjects;

internal sealed class ProviderModelKinds
{
    private ProviderModelKinds() { }
    public const string Ollama = "ollama";
    public const string OpenAICompatible = "openai-compatible";
    public const string OpenAI = "openai";
    public const string AzureOpenAI = "azure-openai";
}

/// <summary>
/// Provider-qualified model identity. No credential is stored in this object; credentials are resolved
/// from the matching configured provider at execution time.
/// </summary>
public sealed class ProviderModelReference
{
    public string ProviderKind { get; set; } = ProviderModelKinds.Ollama;
    public string ProviderName { get; set; } = "Ollama";
    public string Endpoint { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public bool IsLocal { get; set; } = true;
    public bool IsReachable { get; set; }
    public bool IsConfigured { get; set; }
    public bool IsLoaded { get; set; }
    public bool SupportsBenchmark { get; set; } = true;
    public string Details { get; set; } = string.Empty;

    public string SelectionKey => new ProviderModelIdentity().CreateSelectionKey(ProviderName, Endpoint, ModelName);
    public string DisplayName => $"{ModelName} — {ProviderName}";
    public string EndpointLabel => new ProviderModelIdentity().GetEndpointLabel(Endpoint);
    public string StableId => new ProviderModelIdentity().CreateStableId(ProviderKind, Endpoint, ModelName);
}

internal readonly struct ProviderModelIdentity
{
    public string CreateSelectionKey(string providerName, string endpoint, string modelName)
    {
        var provider = string.IsNullOrWhiteSpace(providerName) ? "AI provider" : providerName.Trim();
        var model = string.IsNullOrWhiteSpace(modelName) ? "unnamed-model" : modelName.Trim();
        var normalizedEndpoint = NormalizeEndpoint(endpoint);
        return string.IsNullOrWhiteSpace(normalizedEndpoint)
            ? $"{provider} — {model}"
            : $"{provider} — {model} @ {normalizedEndpoint}";
    }

    public string GetEndpointLabel(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return string.Empty;
        var normalized = endpoint.Trim().TrimEnd('/');
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            return normalized;
        var host = string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            ? "127.0.0.1"
            : uri.Host;
        var defaultPort = (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && uri.Port == 80)
            || (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && uri.Port == 443);
        var authority = defaultPort ? host : $"{host}:{uri.Port}";
        var path = uri.AbsolutePath.TrimEnd('/');
        return string.IsNullOrWhiteSpace(path) || path == "/"
            ? authority
            : authority + path;
    }

    public string CreateStableId(string? providerKind, string? endpoint, string? modelName)
    {
        var value = $"{providerKind?.Trim().ToLowerInvariant() ?? string.Empty}|{NormalizeEndpoint(endpoint)}|{modelName?.Trim() ?? string.Empty}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16].ToLowerInvariant();
    }


    public bool LooksProviderQualified(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains(" — ", StringComparison.Ordinal)
        && value.Contains(" @ ", StringComparison.Ordinal);

    public bool TryParseSelectionKey(string? value, out ProviderModelReference reference)
    {
        reference = new ProviderModelReference();
        if (!LooksProviderQualified(value))
            return false;

        var text = value!.Trim();
        var providerSeparator = text.IndexOf(" — ", StringComparison.Ordinal);
        var endpointSeparator = text.LastIndexOf(" @ ", StringComparison.Ordinal);
        if (providerSeparator <= 0 || endpointSeparator <= providerSeparator + 3)
            return false;

        var providerName = text[..providerSeparator].Trim();
        var modelName = text[(providerSeparator + 3)..endpointSeparator].Trim();
        var endpoint = text[(endpointSeparator + 3)..].Trim();
        if (string.IsNullOrWhiteSpace(providerName) || string.IsNullOrWhiteSpace(modelName) || string.IsNullOrWhiteSpace(endpoint))
            return false;

        var providerKind = InferProviderKind(providerName);
        endpoint = providerKind == ProviderModelKinds.OpenAICompatible || providerKind == ProviderModelKinds.OpenAI
            ? NormalizeOpenAiCompatibleEndpoint(endpoint)
            : NormalizeEndpoint(endpoint);
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            return false;

        reference = new ProviderModelReference
        {
            ProviderKind = providerKind,
            ProviderName = providerName,
            Endpoint = endpoint,
            ModelName = modelName,
            IsLocal = uri.IsLoopback,
            IsConfigured = false,
            IsReachable = false,
            SupportsBenchmark = true,
            Details = "Provider-qualified route reconstructed from the saved Council selection."
        };
        return true;
    }

    private string InferProviderKind(string providerName)
    {
        if (providerName.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            return ProviderModelKinds.Ollama;
        if (providerName.Equals("Azure OpenAI", StringComparison.OrdinalIgnoreCase))
            return ProviderModelKinds.AzureOpenAI;
        if (providerName.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            return ProviderModelKinds.OpenAI;
        return ProviderModelKinds.OpenAICompatible;
    }

    public string NormalizeOpenAiCompatibleEndpoint(string? endpoint)
    {
        var normalized = NormalizeEndpoint(endpoint);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            return normalized;
        var builder = new UriBuilder(uri);
        if (string.IsNullOrWhiteSpace(builder.Path) || builder.Path == "/")
            builder.Path = "/v1";
        return builder.Uri.ToString().TrimEnd('/');
    }

    public string NormalizeEndpoint(string? endpoint)
    {
        var value = endpoint?.Trim().TrimEnd('/') ?? string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return value;
        var builder = new UriBuilder(uri);
        if (string.Equals(builder.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            builder.Host = "127.0.0.1";
        return builder.Uri.ToString().TrimEnd('/');
    }
}
