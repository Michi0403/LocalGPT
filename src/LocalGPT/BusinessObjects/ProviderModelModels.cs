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
    /// Resolves one current provider/model candidate that is safely equivalent to a persisted binding.
    /// </summary>
    /// <param name="savedBinding">Persisted provider-qualified selection key or legacy bare model name.</param>
    /// <param name="candidates">Current provider-qualified model catalog.</param>
    /// <param name="isAmbiguous">Receives <see langword="true"/> when more than one current candidate could represent the saved binding.</param>
    /// <returns>The uniquely equivalent current candidate, or <see langword="null"/> when no safe unique match exists.</returns>
    public MultiModelCouncilModelCandidate? ResolveEquivalentCandidate(
        string? savedBinding,
        IReadOnlyList<MultiModelCouncilModelCandidate> candidates,
        out bool isAmbiguous)
    {
        isAmbiguous = false;
        if (string.IsNullOrWhiteSpace(savedBinding) || candidates.Count == 0)
            return null;

        var requested = savedBinding.Trim();
        var exact = candidates
            .Where(candidate => string.Equals(candidate.SelectionKey, requested, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (exact.Count == 1)
            return exact[0];
        if (exact.Count > 1)
        {
            isAmbiguous = true;
            return null;
        }

        if (TryParseSelectionKey(requested, out var savedReference))
        {
            var identity = this;
            var qualifiedMatches = candidates
                .Where(candidate => identity.ProviderKindsEquivalent(savedReference.ProviderKind, candidate.ProviderKind))
                .Where(candidate => identity.EndpointsEquivalent(savedReference.ProviderKind, savedReference.Endpoint, candidate.Endpoint))
                .Where(candidate => identity.ModelNamesEquivalent(savedReference.ModelName, candidate.ModelName))
                .ToList();
            if (qualifiedMatches.Count == 1)
                return qualifiedMatches[0];
            if (qualifiedMatches.Count > 1)
            {
                isAmbiguous = true;
                return null;
            }

            // LocalGPT 4.2.1 and earlier could persist Ollama's OpenAI-compatible /v1 facade
            // as a second logical provider. When the current catalog has canonicalized that facade
            // back to native Ollama, reconcile only the same model on the same confirmed runtime.
            if (string.Equals(savedReference.ProviderKind, ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase))
            {
                var ollamaFacadeMatches = candidates
                    .Where(candidate => string.Equals(candidate.ProviderKind, ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase))
                    .Where(candidate => identity.IsOllamaOpenAiCompatibilityFacade(candidate.Endpoint, savedReference.Endpoint))
                    .Where(candidate => identity.ModelNamesEquivalent(savedReference.ModelName, candidate.ModelName))
                    .ToList();
                if (ollamaFacadeMatches.Count == 1)
                    return ollamaFacadeMatches[0];
                if (ollamaFacadeMatches.Count > 1)
                    isAmbiguous = true;
            }
            return null;
        }

        var modelIdentity = this;
        var bareMatches = candidates
            .Where(candidate => modelIdentity.ModelNamesEquivalent(requested, candidate.ModelName))
            .ToList();
        if (bareMatches.Count == 1)
            return bareMatches[0];
        if (bareMatches.Count > 1)
            isAmbiguous = true;
        return null;
    }

    /// <summary>Determines whether two saved/current model keys can safely identify the same concrete model selection.</summary>
    /// <remarks>Provider-qualified keys keep host/provider boundaries intact. Legacy bare model names are accepted only as candidate matches; callers that resolve a set must still reject ambiguity.</remarks>
    public bool AreEquivalentSelectionKeys(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;
        if (string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        var leftQualified = TryParseSelectionKey(left, out var leftReference);
        var rightQualified = TryParseSelectionKey(right, out var rightReference);
        if (!leftQualified || !rightQualified)
        {
            var leftModel = leftQualified ? leftReference.ModelName : left;
            var rightModel = rightQualified ? rightReference.ModelName : right;
            return ModelNamesEquivalent(leftModel, rightModel);
        }

        if (ProviderKindsEquivalent(leftReference.ProviderKind, rightReference.ProviderKind)
            && EndpointsEquivalent(leftReference.ProviderKind, leftReference.Endpoint, rightReference.Endpoint)
            && ModelNamesEquivalent(leftReference.ModelName, rightReference.ModelName))
            return true;

        return (string.Equals(leftReference.ProviderKind, ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase)
                && string.Equals(rightReference.ProviderKind, ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase)
                && IsOllamaOpenAiCompatibilityFacade(rightReference.Endpoint, leftReference.Endpoint)
                && ModelNamesEquivalent(leftReference.ModelName, rightReference.ModelName))
            || (string.Equals(rightReference.ProviderKind, ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase)
                && string.Equals(leftReference.ProviderKind, ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase)
                && IsOllamaOpenAiCompatibilityFacade(leftReference.Endpoint, rightReference.Endpoint)
                && ModelNamesEquivalent(leftReference.ModelName, rightReference.ModelName));
    }

    /// <summary>
    /// Determines whether a current candidate represents the same persisted provider/model identity.
    /// </summary>
    /// <param name="savedBinding">Persisted provider-qualified selection key or legacy bare model name.</param>
    /// <param name="candidate">Current candidate to compare.</param>
    /// <returns><see langword="true"/> only when the supplied candidate is the unique safe reconciliation target.</returns>
    public bool IsEquivalentCandidate(string? savedBinding, MultiModelCouncilModelCandidate candidate)
    {
        var resolved = ResolveEquivalentCandidate(savedBinding, [candidate], out _);
        return resolved is not null;
    }

    /// <summary>
    /// Builds the protocol-independent runtime authority used for Council hardware and concurrency accounting.
    /// </summary>
    /// <param name="endpoint">Provider transport endpoint whose host and effective port identify the physical serving runtime.</param>
    /// <returns>A normalized <c>host:port</c> runtime key; transport path and protocol kind are intentionally excluded.</returns>
    public string GetRuntimeAuthorityKey(string? endpoint)
    {
        var normalized = NormalizeEndpoint(endpoint);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            return string.IsNullOrWhiteSpace(normalized) ? string.Empty : normalized.ToLowerInvariant();

        var host = string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            ? "127.0.0.1"
            : uri.Host.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(host))
            return string.Empty;
        if (host.Contains(":", StringComparison.Ordinal) && !host.StartsWith("[", StringComparison.Ordinal))
            host = $"[{host}]";
        return $"{host}:{uri.Port}";
    }

    /// <summary>
    /// Determines whether one OpenAI-compatible endpoint is the <c>/v1</c> transport facade of the same native Ollama runtime.
    /// </summary>
    /// <param name="ollamaEndpoint">Native Ollama endpoint whose runtime authority is canonical.</param>
    /// <param name="openAiEndpoint">OpenAI-compatible endpoint that may expose the same Ollama runtime.</param>
    /// <returns><see langword="true"/> only when both endpoints share scheme, normalized host and port and the compatibility route is exactly <c>/v1</c>.</returns>
    public bool IsOllamaOpenAiCompatibilityFacade(string? ollamaEndpoint, string? openAiEndpoint)
    {
        var nativeEndpoint = NormalizeEndpoint(ollamaEndpoint);
        var compatibilityEndpoint = NormalizeOpenAiCompatibleEndpoint(openAiEndpoint);
        if (!Uri.TryCreate(nativeEndpoint, UriKind.Absolute, out var nativeUri)
            || !Uri.TryCreate(compatibilityEndpoint, UriKind.Absolute, out var compatibilityUri))
        {
            return false;
        }

        var nativePath = nativeUri.AbsolutePath.TrimEnd('/');
        var compatibilityPath = compatibilityUri.AbsolutePath.TrimEnd('/');
        return (string.IsNullOrWhiteSpace(nativePath) || nativePath == "/")
            && string.Equals(compatibilityPath, "/v1", StringComparison.OrdinalIgnoreCase)
            && string.Equals(nativeUri.Scheme, compatibilityUri.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(nativeUri.Host, compatibilityUri.Host, StringComparison.OrdinalIgnoreCase)
            && nativeUri.Port == compatibilityUri.Port;
    }

    /// <summary>
    /// Determines whether two provider model tokens identify the same concrete model, treating only an explicit <c>:latest</c> suffix as optional.
    /// </summary>
    /// <param name="left">First provider model token.</param>
    /// <param name="right">Second provider model token.</param>
    /// <returns><see langword="true"/> when the concrete provider tokens are equivalent.</returns>
    public bool ModelNamesEquivalent(string? left, string? right)
    {
        var leftToken = NormalizeModelToken(left);
        var rightToken = NormalizeModelToken(right);
        return string.Equals(leftToken, rightToken, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether two provider kinds are equivalent for persisted binding reconciliation.
    /// </summary>
    /// <param name="left">First provider kind.</param>
    /// <param name="right">Second provider kind.</param>
    /// <returns><see langword="true"/> when both provider kinds address the same provider protocol family.</returns>
    private bool ProviderKindsEquivalent(string? left, string? right)
    {
        var leftKind = left?.Trim() ?? string.Empty;
        var rightKind = right?.Trim() ?? string.Empty;
        return string.Equals(leftKind, rightKind, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether two endpoints identify the same host route for the supplied provider kind.
    /// </summary>
    /// <param name="providerKind">Provider protocol family that owns the endpoint.</param>
    /// <param name="left">First endpoint.</param>
    /// <param name="right">Second endpoint.</param>
    /// <returns><see langword="true"/> when both endpoints normalize to the same provider route.</returns>
    private bool EndpointsEquivalent(string? providerKind, string? left, string? right)
    {
        if (string.Equals(providerKind, ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase))
            return EndpointAuthoritiesEquivalent(left, right);

        var leftEndpoint = string.Equals(providerKind, ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase)
            || string.Equals(providerKind, ProviderModelKinds.OpenAI, StringComparison.OrdinalIgnoreCase)
                ? NormalizeOpenAiCompatibleEndpoint(left)
                : NormalizeEndpoint(left);
        var rightEndpoint = string.Equals(providerKind, ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase)
            || string.Equals(providerKind, ProviderModelKinds.OpenAI, StringComparison.OrdinalIgnoreCase)
                ? NormalizeOpenAiCompatibleEndpoint(right)
                : NormalizeEndpoint(right);
        return string.Equals(leftEndpoint, rightEndpoint, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether two Ollama endpoint authorities refer to the same scheme, normalized host and port without allowing cross-host substitution.
    /// </summary>
    /// <param name="left">First Ollama endpoint.</param>
    /// <param name="right">Second Ollama endpoint.</param>
    /// <returns><see langword="true"/> when both authorities are equal.</returns>
    private bool EndpointAuthoritiesEquivalent(string? left, string? right)
    {
        var leftEndpoint = NormalizeEndpoint(left);
        var rightEndpoint = NormalizeEndpoint(right);
        if (!Uri.TryCreate(leftEndpoint, UriKind.Absolute, out var leftUri)
            || !Uri.TryCreate(rightEndpoint, UriKind.Absolute, out var rightUri))
        {
            return string.Equals(leftEndpoint, rightEndpoint, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(leftUri.Scheme, rightUri.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(leftUri.Host, rightUri.Host, StringComparison.OrdinalIgnoreCase)
            && leftUri.Port == rightUri.Port;
    }

    /// <summary>
    /// Normalizes one provider model token while preserving its concrete size and variant identity.
    /// </summary>
    /// <param name="modelName">Provider model token to normalize.</param>
    /// <returns>The normalized concrete provider model token.</returns>
    private string NormalizeModelToken(string? modelName)
    {
        var token = modelName?.Trim() ?? string.Empty;
        return token.EndsWith(":latest", StringComparison.OrdinalIgnoreCase)
            ? token[..^7]
            : token;
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
