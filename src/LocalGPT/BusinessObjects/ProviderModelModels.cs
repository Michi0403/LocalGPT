using System.Security.Cryptography;
using System.Text;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a provider model kinds application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
internal sealed class ProviderModelKinds
{
    /// <summary>
    /// Initializes a new <see cref="ProviderModelKinds"/> instance and captures the dependencies or initial state required by its provider model kinds workflow.
    /// </summary>
    private ProviderModelKinds() { }
    /// <summary>
    /// Defines the Ollama constant used by <see cref="ProviderModelKinds"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string Ollama = "ollama";
    /// <summary>
    /// Defines the OpenAI compatible constant used by <see cref="ProviderModelKinds"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string OpenAICompatible = "openai-compatible";
    /// <summary>
    /// Defines the OpenAI constant used by <see cref="ProviderModelKinds"/> so callers and internal logic share the same stable value.
    /// </summary>
    public const string OpenAI = "openai";
    /// <summary>
    /// Defines the azure OpenAI constant used by <see cref="ProviderModelKinds"/> so callers and internal logic share the same stable value.
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
    /// Gets or sets the provider kind value that forms part of the provider model reference state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The provider kind value exposed by <see cref="ProviderModelReference"/>.</value>
    public string ProviderKind { get; set; } = ProviderModelKinds.Ollama;
    /// <summary>
    /// Gets or sets the provider name value that forms part of the provider model reference state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The provider name value exposed by <see cref="ProviderModelReference"/>.</value>
    public string ProviderName { get; set; } = "Ollama";
    /// <summary>
    /// Gets or sets the endpoint that identifies the network or application endpoint associated with this provider model reference state.
    /// </summary>
    /// <value>The endpoint value exposed by <see cref="ProviderModelReference"/>.</value>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the model name value that forms part of the provider model reference state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model name value exposed by <see cref="ProviderModelReference"/>.</value>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether local applies to the provider model reference state.
    /// </summary>
    /// <value>The is local value exposed by <see cref="ProviderModelReference"/>.</value>
    public bool IsLocal { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether reachable applies to the provider model reference state.
    /// </summary>
    /// <value>The is reachable value exposed by <see cref="ProviderModelReference"/>.</value>
    public bool IsReachable { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether configured applies to the provider model reference state.
    /// </summary>
    /// <value>The is configured value exposed by <see cref="ProviderModelReference"/>.</value>
    public bool IsConfigured { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether loaded applies to the provider model reference state.
    /// </summary>
    /// <value>The is loaded value exposed by <see cref="ProviderModelReference"/>.</value>
    public bool IsLoaded { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether benchmark applies to the provider model reference state.
    /// </summary>
    /// <value>The supports benchmark value exposed by <see cref="ProviderModelReference"/>.</value>
    public bool SupportsBenchmark { get; set; } = true;
    /// <summary>
    /// Gets or sets the details value that forms part of the provider model reference state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The details value exposed by <see cref="ProviderModelReference"/>.</value>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Gets the stable selection key used to identify or correlate this provider model reference instance with related application state.
    /// </summary>
    /// <value>The selection key value exposed by <see cref="ProviderModelReference"/>.</value>
    public string SelectionKey => new ProviderModelIdentity().CreateSelectionKey(ProviderName, Endpoint, ModelName);
    /// <summary>
    /// Gets the display name value that forms part of the provider model reference state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="ProviderModelReference"/>.</value>
    public string DisplayName => $"{ModelName} — {ProviderName}";
    /// <summary>
    /// Gets the endpoint label value that forms part of the provider model reference state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The endpoint label value exposed by <see cref="ProviderModelReference"/>.</value>
    public string EndpointLabel => new ProviderModelIdentity().GetEndpointLabel(Endpoint);
    /// <summary>
    /// Gets the stable stable identifier used to identify or correlate this provider model reference instance with related application state.
    /// </summary>
    /// <value>The stable identifier value exposed by <see cref="ProviderModelReference"/>.</value>
    public string StableId => new ProviderModelIdentity().CreateStableId(ProviderKind, Endpoint, ModelName);
}

/// <summary>
/// Represents a provider model identity application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
internal readonly struct ProviderModelIdentity
{
    /// <summary>
    /// Creates selection key for <see cref="ProviderModelIdentity"/>, keeping the operation consistent with the state and invariants of the surrounding provider model identity workflow.
    /// </summary>
    /// <param name="providerName">Provider name value supplied to the provider model identity operation and used when producing its result.</param>
    /// <param name="endpoint">Endpoint value supplied to the provider model identity operation and used when producing its result.</param>
    /// <param name="modelName">Model name value supplied to the provider model identity operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Retrieves endpoint label for <see cref="ProviderModelIdentity"/>, keeping the operation consistent with the state and invariants of the surrounding provider model identity workflow.
    /// </summary>
    /// <param name="endpoint">Endpoint value supplied to the provider model identity operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Creates stable identifier for <see cref="ProviderModelIdentity"/>, keeping the operation consistent with the state and invariants of the surrounding provider model identity workflow.
    /// </summary>
    /// <param name="providerKind">Provider kind value supplied to the provider model identity operation and used when producing its result.</param>
    /// <param name="endpoint">Endpoint value supplied to the provider model identity operation and used when producing its result.</param>
    /// <param name="modelName">Model name value supplied to the provider model identity operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string CreateStableId(string? providerKind, string? endpoint, string? modelName)
    {
        var value = $"{providerKind?.Trim().ToLowerInvariant() ?? string.Empty}|{NormalizeEndpoint(endpoint)}|{modelName?.Trim() ?? string.Empty}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16].ToLowerInvariant();
    }


    /// <summary>
    /// Performs looks provider qualified for <see cref="ProviderModelIdentity"/>, keeping the operation consistent with the state and invariants of the surrounding provider model identity workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the provider model identity operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool LooksProviderQualified(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains(" — ", StringComparison.Ordinal)
        && value.Contains(" @ ", StringComparison.Ordinal);

    /// <summary>
    /// Attempts to parse selection key for <see cref="ProviderModelIdentity"/>, keeping the operation consistent with the state and invariants of the surrounding provider model identity workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the provider model identity operation and used when producing its result.</param>
    /// <param name="reference">Reference value supplied to the provider model identity operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Performs infer provider kind for <see cref="ProviderModelIdentity"/>, keeping the operation consistent with the state and invariants of the surrounding provider model identity workflow.
    /// </summary>
    /// <param name="providerName">Provider name value supplied to the provider model identity operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Normalizes OpenAI compatible endpoint for <see cref="ProviderModelIdentity"/>, keeping the operation consistent with the state and invariants of the surrounding provider model identity workflow.
    /// </summary>
    /// <param name="endpoint">Endpoint value supplied to the provider model identity operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Normalizes endpoint for <see cref="ProviderModelIdentity"/>, keeping the operation consistent with the state and invariants of the surrounding provider model identity workflow.
    /// </summary>
    /// <param name="endpoint">Endpoint value supplied to the provider model identity operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
