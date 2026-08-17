using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Channels;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates multi model council behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class MultiModelCouncilService
    {
        /// <summary>
        /// Performs select participants as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="normalizedLegacyBaseUri">Normalized legacy base uri value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        private async Task<List<string>> SelectParticipantsAsync(
            MultiModelCouncilRequest request,
            string normalizedLegacyBaseUri,
            CancellationToken cancellationToken)
        {
            try
            {
                var useLegacyBaseUri = request.ModelSelections.Count == 0
                    && !string.IsNullOrWhiteSpace(request.BaseUri);
                var currentCandidates = await providerModels.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
                var currentBySelectionKey = currentCandidates
                    .GroupBy(candidate => candidate.SelectionKey, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
                var staleSelections = new List<string>();
                var references = new List<ProviderModelReference>();

                foreach (var requestedReference in request.ModelSelections
                    .Where(model => model is not null && !string.IsNullOrWhiteSpace(model.ModelName))
                    .GroupBy(model => model.SelectionKey, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .Take(catalog.MaxParticipants))
                {
                    if (currentBySelectionKey.TryGetValue(requestedReference.SelectionKey, out var currentCandidate))
                    {
                        references.Add(currentCandidate.ToReference());
                        continue;
                    }

                    if (IsConfiguredProviderEndpoint(requestedReference)
                        && !HasReachableProviderEndpoint(currentCandidates, requestedReference))
                    {
                        // The endpoint remains deliberately configured but the host itself is currently offline.
                        // Preserve the exact model route and let the real provider call report reachability. If the
                        // host is reachable and this model is absent, treat the model route as stale instead.
                        requestedReference.IsConfigured = true;
                        requestedReference.IsReachable = false;
                        references.Add(requestedReference);
                        continue;
                    }

                    staleSelections.Add(requestedReference.SelectionKey);
                }

                if (staleSelections.Count > 0)
                {
                    throw new KeyNotFoundException(
                        $"The following provider-qualified Council route(s) are no longer configured or discoverable: {string.Join("; ", staleSelections)}. Refresh provider models and reselect those exact hosts; LocalGPT will not substitute a same-name model from another provider.");
                }

                foreach (var requested in request.ModelNames
                    .Where(model => !string.IsNullOrWhiteSpace(model))
                    .Select(model => model.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (references.Count >= catalog.MaxParticipants)
                        break;
                    if (references.Any(model => model.SelectionKey.Equals(requested, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    if (!new ProviderModelIdentity().LooksProviderQualified(requested)
                        && references.Any(model => model.ModelName.Equals(requested, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Provider-qualified selections are authoritative. A parallel legacy ModelNames list may
                        // repeat their bare provider-native names; do not resolve those names again or guess another endpoint.
                        continue;
                    }
                    ProviderModelReference resolved;
                    if (new ProviderModelIdentity().LooksProviderQualified(requested))
                    {
                        if (!currentBySelectionKey.TryGetValue(requested, out var currentCandidate))
                        {
                            var identity = new ProviderModelIdentity();
                            if (identity.TryParseSelectionKey(requested, out var savedReference)
                                && IsConfiguredProviderEndpoint(savedReference)
                                && !HasReachableProviderEndpoint(currentCandidates, savedReference))
                            {
                                savedReference.IsConfigured = true;
                                savedReference.IsReachable = false;
                                resolved = savedReference;
                            }
                            else
                            {
                                throw new KeyNotFoundException(
                                    $"The provider-qualified Council model '{requested}' is no longer configured or discoverable. Refresh provider models and reselect that exact host; LocalGPT will not fall back to a same-name model on another endpoint.");
                            }
                        }
                        else
                        {
                            resolved = currentCandidate.ToReference();
                        }
                    }
                    else if (useLegacyBaseUri)
                    {
                        resolved = new ProviderModelReference
                        {
                            ProviderKind = ProviderModelKinds.Ollama,
                            ProviderName = "Ollama",
                            Endpoint = normalizedLegacyBaseUri,
                            ModelName = requested,
                            IsLocal = new Uri(normalizedLegacyBaseUri, UriKind.Absolute).IsLoopback,
                            IsConfigured = false,
                            IsReachable = false,
                            SupportsBenchmark = true,
                            Details = "Legacy bare model name bound to the explicitly requested Ollama BaseUri."
                        };
                    }
                    else
                    {
                        var bareMatches = currentCandidates
                            .Where(candidate => candidate.ModelName.Equals(requested, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        if (bareMatches.Count > 1)
                        {
                            throw new InvalidOperationException(
                                $"Model name '{requested}' is exposed by multiple provider hosts. Select the provider-qualified model entry instead of guessing an endpoint.");
                        }
                        resolved = bareMatches.Count == 1
                            ? bareMatches[0].ToReference()
                            : await providerModels.ResolveAsync(requested, cancellationToken).ConfigureAwait(false);
                    }
                    if (!references.Any(model => model.SelectionKey.Equals(resolved.SelectionKey, StringComparison.OrdinalIgnoreCase)))
                        references.Add(resolved);
                }

                if (references.Count == 0)
                {
                    var candidates = await providerModels.GetCandidatesAsync(cancellationToken).ConfigureAwait(false);
                    references = candidates
                        .Where(candidate => candidate.IsInstalled || candidate.IsConfigured)
                        .Take(catalog.MaxParticipants)
                        .Select(candidate => candidate.ToReference())
                        .ToList();
                }

                if (references.Count == 0)
                    references.Add(await providerModels.ResolveAsync("gpt-oss:20b", cancellationToken).ConfigureAwait(false));

                foreach (var reference in references)
                    providerModels.Remember(reference);

                request.ModelSelections = references;
                request.ModelNames = references.Select(model => model.SelectionKey).ToList();
                if (!string.IsNullOrWhiteSpace(request.CouncilLeaderModelName))
                {
                    var leader = references.FirstOrDefault(model =>
                        model.SelectionKey.Equals(request.CouncilLeaderModelName, StringComparison.OrdinalIgnoreCase));
                    if (leader is null)
                    {
                        var bareLeaderMatches = references
                            .Where(model => model.ModelName.Equals(request.CouncilLeaderModelName, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        leader = bareLeaderMatches.Count == 1 ? bareLeaderMatches[0] : null;
                        if (bareLeaderMatches.Count > 1)
                        {
                            logger.LogWarning(
                                "Council leader model name {LeaderModelName} is ambiguous across selected providers. The run will use its normal deterministic leader selection instead of guessing an endpoint.",
                                request.CouncilLeaderModelName);
                            request.CouncilLeaderModelName = string.Empty;
                        }
                    }
                    if (leader is not null)
                        request.CouncilLeaderModelName = leader.SelectionKey;
                }
                return request.ModelNames;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Provider-qualified Council participant selection failed; request content was omitted.");
                throw;
            }
        }

        /// <summary>
        /// Performs qualify model routes as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="routes">One wire council model route dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="references">Provider model reference dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <returns>The collection produced by the operation.</returns>
        private List<OneWireCouncilModelRoute> QualifyModelRoutes(
            IEnumerable<OneWireCouncilModelRoute>? routes,
            IReadOnlyList<ProviderModelReference> references)
        {
    try
    {
                var qualified = new List<OneWireCouncilModelRoute>();
                foreach (var route in routes ?? [])
                {
                    if (route is null || string.IsNullOrWhiteSpace(route.ModelName))
                        continue;
                    var matches = references.Where(model =>
                        model.SelectionKey.Equals(route.ModelName, StringComparison.OrdinalIgnoreCase)
                        || model.ModelName.Equals(route.ModelName, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (matches.Count > 1 && matches.All(model => !model.SelectionKey.Equals(route.ModelName, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    var reference = matches.FirstOrDefault();
                    if (reference is not null)
                    {
                        route.ModelName = reference.SelectionKey;
                        route.ProviderKind = reference.ProviderKind;
                        route.ProviderName = reference.ProviderName;
                        route.ProviderEndpoint = reference.Endpoint;
                        route.ProviderModelName = reference.ModelName;
                        if (!reference.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase))
                            route.OllamaNumGpu = null;
                    }
                    qualified.Add(route);
                }
                foreach (var reference in references)
                {
                    if (qualified.Any(route => route.ModelName.Equals(reference.SelectionKey, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    qualified.Add(new OneWireCouncilModelRoute
                    {
                        ModelName = reference.SelectionKey,
                        ProviderKind = reference.ProviderKind,
                        ProviderName = reference.ProviderName,
                        ProviderEndpoint = reference.Endpoint,
                        ProviderModelName = reference.ModelName,
                        HardwareKind = OneWireHardwareKind.Auto,
                        HardwareIndex = -1,
                        HardwareName = reference.IsLocal ? "Automatic local provider road" : "Remote provider route",
                        MinOutputTokens = 256,
                        MaxOutputTokens = 4096,
                        MinContextTokens = 2048,
                        MaxContextTokens = 32768,
                        OllamaNumGpu = null,
                        IsEnabled = true,
                        MaxConcurrentModelsOnLane = 1
                    });
                }

                return qualified
                    .GroupBy(route => route.ModelName, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToList();
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(QualifyModelRoutes)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(MultiModelCouncilService)}.{nameof(QualifyModelRoutes)} failed.");
        throw;
    }
}


        /// <summary>
        /// Determines whether reachable provider endpoint as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="candidates">Multi model council model candidate dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="model">Model value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        private bool HasReachableProviderEndpoint(
            IReadOnlyList<MultiModelCouncilModelCandidate> candidates,
            ProviderModelReference model)
        {
            try
            {
                var identity = new ProviderModelIdentity();
                var requestedEndpoint = model.ProviderKind.Equals(ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase)
                    || model.ProviderKind.Equals(ProviderModelKinds.OpenAI, StringComparison.OrdinalIgnoreCase)
                    ? identity.NormalizeOpenAiCompatibleEndpoint(model.Endpoint)
                    : identity.NormalizeEndpoint(model.Endpoint);
                return candidates.Any(candidate =>
                {
                    if (!candidate.IsInstalled || !candidate.ProviderKind.Equals(model.ProviderKind, StringComparison.OrdinalIgnoreCase))
                        return false;
                    var candidateEndpoint = candidate.ProviderKind.Equals(ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase)
                        || candidate.ProviderKind.Equals(ProviderModelKinds.OpenAI, StringComparison.OrdinalIgnoreCase)
                        ? identity.NormalizeOpenAiCompatibleEndpoint(candidate.Endpoint)
                        : identity.NormalizeEndpoint(candidate.Endpoint);
                    return candidateEndpoint.Equals(requestedEndpoint, StringComparison.OrdinalIgnoreCase);
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not evaluate provider endpoint reachability during Council route preflight.");
                throw;
            }
        }

        /// <summary>
        /// Determines whether configured provider endpoint as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="model">Model value supplied to the multi model council operation and used when producing its result.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        private bool IsConfiguredProviderEndpoint(ProviderModelReference model)
        {
            try
            {
                var options = optionsRoot.CurrentValue.AICore ?? new AICoreOptions();
                var identity = new ProviderModelIdentity();
                if (model.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase))
                {
                    var requestedEndpoint = identity.NormalizeEndpoint(model.Endpoint);
                    return new[] { options.OllamaCore }
                        .Concat(options.OllamaCores ?? [])
                        .Where(option => option is not null && !string.IsNullOrWhiteSpace(option.Uri))
                        .Any(option => identity.NormalizeEndpoint(option.Uri).Equals(requestedEndpoint, StringComparison.OrdinalIgnoreCase));
                }

                if (model.ProviderKind.Equals(ProviderModelKinds.OpenAICompatible, StringComparison.OrdinalIgnoreCase))
                {
                    var requestedEndpoint = identity.NormalizeOpenAiCompatibleEndpoint(model.Endpoint);
                    return new[] { options.ChatGPTLocalCore }
                        .Concat(options.ChatGPTLocalCores ?? [])
                        .Where(option => option is not null && !string.IsNullOrWhiteSpace(option.Endpoint))
                        .Any(option => identity.NormalizeOpenAiCompatibleEndpoint(option.Endpoint).Equals(requestedEndpoint, StringComparison.OrdinalIgnoreCase));
                }

                if (model.ProviderKind.Equals(ProviderModelKinds.OpenAI, StringComparison.OrdinalIgnoreCase))
                {
                    var configured = options.OpenAICore;
                    if (configured is null || string.IsNullOrWhiteSpace(configured.ModelName))
                        return false;
                    var configuredEndpoint = identity.NormalizeOpenAiCompatibleEndpoint(
                        string.IsNullOrWhiteSpace(configured.Endpoint) ? "https://api.openai.com/v1" : configured.Endpoint);
                    return configuredEndpoint.Equals(identity.NormalizeOpenAiCompatibleEndpoint(model.Endpoint), StringComparison.OrdinalIgnoreCase);
                }

                if (model.ProviderKind.Equals(ProviderModelKinds.AzureOpenAI, StringComparison.OrdinalIgnoreCase))
                {
                    var configured = options.OpenAIServiceCore;
                    return configured is not null
                        && !string.IsNullOrWhiteSpace(configured.Endpoint)
                        && identity.NormalizeEndpoint(configured.Endpoint).Equals(identity.NormalizeEndpoint(model.Endpoint), StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Validating a provider-qualified Council endpoint against configured hosts failed.");
                throw;
            }
        }

    }
}
