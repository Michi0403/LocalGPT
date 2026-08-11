using System.Security.Cryptography;
using System.Text;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a provider model kinds.
/// </summary>
internal sealed class ProviderModelKinds
{
    /// <summary>
    /// Runs the provider model kinds operation.
    /// </summary>
    private ProviderModelKinds() { }
    /// <summary>
    /// Stores ollama.
    /// </summary>
    public const string Ollama = "ollama";
    /// <summary>
    /// Stores open aicompatible.
    /// </summary>
    public const string OpenAICompatible = "openai-compatible";
    /// <summary>
    /// Stores open ai.
    /// </summary>
    public const string OpenAI = "openai";
    /// <summary>
    /// Stores azure open ai.
    /// </summary>
    public const string AzureOpenAI = "azure-openai";
}

/// <summary>
/// Provider-qualified model identity. No credential is stored in this object; credentials are resolved
/// from the matching configured provider at execution time.
/// </summary>
public sealed class ProviderModelReference
{
    /// <summary>
    /// Gets or sets provider kind.
    /// </summary>
    public string ProviderKind { get; set; } = ProviderModelKinds.Ollama;
    /// <summary>
    /// Gets or sets provider name.
    /// </summary>
    public string ProviderName { get; set; } = "Ollama";
    /// <summary>
    /// Gets or sets endpoint.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets model name.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets is local.
    /// </summary>
    public bool IsLocal { get; set; } = true;
    /// <summary>
    /// Gets or sets is reachable.
    /// </summary>
    public bool IsReachable { get; set; }
    /// <summary>
    /// Gets or sets is configured.
    /// </summary>
    public bool IsConfigured { get; set; }
    /// <summary>
    /// Gets or sets is loaded.
    /// </summary>
    public bool IsLoaded { get; set; }
    /// <summary>
    /// Gets or sets supports benchmark.
    /// </summary>
    public bool SupportsBenchmark { get; set; } = true;
    /// <summary>
    /// Gets or sets details.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets selection key.
    /// </summary>
    public string SelectionKey => new ProviderModelIdentity().CreateSelectionKey(ProviderName, Endpoint, ModelName);
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName => $"{ModelName} — {ProviderName}";
    /// <summary>
    /// Gets or sets endpoint label.
    /// </summary>
    public string EndpointLabel => new ProviderModelIdentity().GetEndpointLabel(Endpoint);
    /// <summary>
    /// Gets or sets stable identifier.
    /// </summary>
    public string StableId => new ProviderModelIdentity().CreateStableId(ProviderKind, Endpoint, ModelName);
}

/// <summary>
/// Represents a provider model identity.
/// </summary>
internal readonly struct ProviderModelIdentity
{
    /// <summary>
    /// Creates selection key.
    /// </summary>
    public string CreateSelectionKey(string providerName, string endpoint, string modelName)
    {
        var provider = string.IsNullOrWhiteSpace(providerName) ? "AI provider" : providerName.Trim();
        var model = string.IsNullOrWhiteSpace(modelName) ? "unnamed-model" : modelName.Trim();
        var normalizedEndpoint = NormalizeEndpoint(endpoint);
        return string.IsNullOrWhiteSpace(normalizedEndpoint)
            ? $"{provider} — {model}"
            : $"{provider} — {model} @ {normalizedEndpoint}";
    }

    /// <summary>
    /// Gets endpoint label.
    /// </summary>
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

    /// <summary>
    /// Creates stable identifier.
    /// </summary>
    public string CreateStableId(string? providerKind, string? endpoint, string? modelName)
    {
        var value = $"{providerKind?.Trim().ToLowerInvariant() ?? string.Empty}|{NormalizeEndpoint(endpoint)}|{modelName?.Trim() ?? string.Empty}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16].ToLowerInvariant();
    }


    /// <summary>
    /// Runs the looks provider qualified operation.
    /// </summary>
    public bool LooksProviderQualified(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains(" — ", StringComparison.Ordinal)
        && value.Contains(" @ ", StringComparison.Ordinal);

    /// <summary>
    /// Attempts to parse selection key.
    /// </summary>
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

    /// <summary>
    /// Runs the infer provider kind operation.
    /// </summary>
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

    /// <summary>
    /// Normalizes open ai compatible endpoint.
    /// </summary>
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

    /// <summary>
    /// Normalizes endpoint.
    /// </summary>
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
