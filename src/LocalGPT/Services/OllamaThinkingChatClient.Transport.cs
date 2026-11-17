using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services.Formatting;
using Microsoft.Extensions.AI;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalGPT.Services;

/// <summary>
/// Represents an Ollama thinking chat application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed partial class OllamaThinkingChatClient
{
    /// <summary>
    /// Performs send request with tool fallback for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="completionOption">Completion option value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The provider HTTP response or a local context preflight failure before sending.</returns>
    private async Task<(HttpResponseMessage? Response, string? PreflightFailure)> SendRequestWithToolFallbackAsync(
        OllamaChatRequest request,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.Think == true &&
                councilRuntime.OllamaThinkingChatClientShouldSkipExplicitThinking(http.BaseAddress, model, logger))
            {
                request.Think = null;
            }

            if (request.Tools is { Count: > 0 } &&
                councilRuntime.OllamaThinkingChatClientShouldSkipNativeTools(http.BaseAddress, model, logger))
            {
                logger.LogDebug(
                    "Skipping native tool metadata for Ollama model {Model} because this provider-qualified model rejected it earlier in the current LocalGPT process.",
                    model);
                EnsureTextualDxFunctionFallbackPrompt(request);
                request.Tools = null;
            }

            var (response, responseFailure) = await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
            if (response is null)
                return (null, responseFailure);
            if (response.IsSuccessStatusCode ||
                response.StatusCode is not (System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.NotImplemented))
            {
                return (response, null);
            }

            // A normal chat can have hundreds of eligible DXFunctions. Some Ollama models
            // reject that much native JSON schema metadata even though they support tools.
            // Retry once with the shared discovery broker and upload-workspace readers
            // before concluding that this provider cannot execute native functions.
            // An explicit strict tool policy is never broadened or silently rewritten.
            if (request.Tools is { Count: > 48 } && GetDiscoveryBrokerTools(request.Tools) is { } compactTools)
            {
                var originalToolCount = request.Tools.Count;
                response.Dispose();
                request.Tools = compactTools;
                var (compactResponse, compactFailure) = await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
                if (compactResponse is null)
                    return (null, compactFailure);
                logger.LogInformation(
                    "Ollama model {Model} native-tool compatibility retry: {OriginalToolCount} schemas reduced to {CompactToolCount}; HTTP {StatusCode}. Shared discovery/invocation and workspace tools remain available.",
                    model, originalToolCount, compactTools.Count, (int)compactResponse.StatusCode);
                if (compactResponse.IsSuccessStatusCode ||
                    compactResponse.StatusCode is not (System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.NotImplemented))
                    return (compactResponse, null);
                response = compactResponse;
            }

            // Newer Ollama runtimes expose provider reasoning only when the request explicitly opts into
            // thinking. Older runtimes/models may reject that field. Probe once per provider-qualified
            // model, remember the result, and preserve the normal request instead of breaking chat.
            if (request.Think == true)
            {
                response.Dispose();
                request.Think = null;
                var (withoutThinking, withoutThinkingFailure) = await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
                if (withoutThinking is null)
                    return (null, withoutThinkingFailure);
                if (withoutThinking.IsSuccessStatusCode)
                {
                    councilRuntime.OllamaThinkingChatClientRememberExplicitThinkingRejected(http.BaseAddress, model, logger);
                    return (withoutThinking, null);
                }
                if (withoutThinking.StatusCode is not (System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.NotImplemented))
                    return (withoutThinking, null);

                // If tools are present, the rejection can still be tool metadata rather than thinking.
                // Restore the reasoning request while probing the established textual-tool fallback.
                if (request.Tools is { Count: > 0 })
                {
                    withoutThinking.Dispose();
                    request.Think = true;
                    EnsureTextualDxFunctionFallbackPrompt(request);
                    request.Tools = null;
                    var (withoutTools, withoutToolsFailure) = await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
                    if (withoutTools is null)
                        return (null, withoutToolsFailure);
                    if (withoutTools.IsSuccessStatusCode)
                    {
                        councilRuntime.OllamaThinkingChatClientRememberNativeToolsRejected(http.BaseAddress, model, logger);
                        return (withoutTools, null);
                    }
                    if (withoutTools.StatusCode is not (System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.NotImplemented))
                        return (withoutTools, null);

                    // Both optional request features are now independently implicated: tools still failed
                    // without thinking and thinking still failed without tools. Try the minimal request and
                    // only cache both incompatibilities when that control request succeeds.
                    withoutTools.Dispose();
                    request.Think = null;
                    var (minimalResponse, minimalResponseFailure) = await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
                    if (minimalResponse is null)
                        return (null, minimalResponseFailure);
                    if (minimalResponse.IsSuccessStatusCode)
                    {
                        councilRuntime.OllamaThinkingChatClientRememberExplicitThinkingRejected(http.BaseAddress, model, logger);
                        councilRuntime.OllamaThinkingChatClientRememberNativeToolsRejected(http.BaseAddress, model, logger);
                    }
                    return (minimalResponse, null);
                }

                // The same 400/501 occurred with and without the thinking flag and no tools are present,
                // so there is no evidence that thinking caused the rejection. Do not poison the cache.
                return (withoutThinking, null);
            }

            if (request.Tools is { Count: > 0 })
            {
                logger.LogInformation(
                    "Ollama model {Model} rejected native tool metadata with HTTP {StatusCode}; retrying the same chat request without automatic tools.",
                    model,
                    (int)response.StatusCode);
                response.Dispose();
                EnsureTextualDxFunctionFallbackPrompt(request);
                request.Tools = null;
                var fallback = await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
                if (fallback.Response?.IsSuccessStatusCode == true)
                    councilRuntime.OllamaThinkingChatClientRememberNativeToolsRejected(http.BaseAddress, model, logger);
                return fallback;
            }

            return (response, null);
        }
        catch (Exception __serviceMethodException)
        {
            if (__serviceMethodException is OperationCanceledException)
                logger.LogDebug(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(SendRequestWithToolFallbackAsync)} was canceled.");
            else
                logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(SendRequestWithToolFallbackAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Ensures textual DevExpress function fallback prompt for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    private void EnsureTextualDxFunctionFallbackPrompt(OllamaChatRequest request)
    {
        try
        {
            const string marker = "[LOCALGPT_TEXTUAL_DXFUNCTION_FALLBACK]";
            if (request.Messages.Any(message =>
                    message.Role.Equals("system", StringComparison.OrdinalIgnoreCase) &&
                    message.Content.Contains(marker, StringComparison.Ordinal)))
            {
                return;
            }

            var nativeNames = request.Tools?.Select(tool => tool.Function.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var functions = GetAutomaticFunctions()
                .Where(function => nativeNames is null || nativeNames.Contains(ToOllamaToolName(function.Name))).ToList();
            ValidateUniqueAutomaticToolNames(functions);
            if (functions.Count == 0)
                return;

            // A provider that cannot receive native tools must not receive hundreds
            // of tool descriptions as unbounded system-prompt text either. The
            // registry remains fully accessible through dxai.tools.discover/invoke.
            // Keep explicit strict/preferred tool lists intact.
            var functionNames = functions.Select(function => function.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var advertisedFunctions = automaticFunctionAllowList is null && functions.Count > 48 &&
                functionNames.Contains("dxai.tools.discover") && functionNames.Contains("dxai.tools.preview") &&
                functionNames.Contains("dxai.tools.invoke")
                ? functions.Where(function =>
                    function.Name.StartsWith("dxai.tools.", StringComparison.OrdinalIgnoreCase) ||
                    function.Name.StartsWith("chat.upload_workspace_", StringComparison.OrdinalIgnoreCase)).ToList()
                : functions;
            var functionDirectory = string.Join(
                Environment.NewLine,
                advertisedFunctions.Select(function => $"- {function.Name}: {function.Parameters}"));
            request.Messages.Insert(0, new OllamaChatMessage
            {
                Role = "system",
                Content = $$$"""
                {{{marker}}}
                This exact provider-qualified Ollama model does not accept native tool metadata. LocalGPT still supports policy-checked DXFunctions through textual call recovery.
                If a function is genuinely needed, emit one standalone JSON object with exactly this shape and no invented function name:
                {"functionName":"exact.registry.name","arguments":{}}
                LocalGPT will validate the exact registry name, apply normal automatic/deferred approval policy, execute it when permitted, display the tool activity, return the tool result, and then continue your response.
                Do not guess a function name. If another registered capability is needed, first call dxai.tools.discover with a narrow search query; use dxai.tools.preview with the exact name and parameters to get a previewFingerprint, then call dxai.tools.invoke with that fingerprint. Preserve approval requirements. Only report a capability gap if discovery confirms it is unavailable.
                Exact automatic/deferred function directory for this request:
                {{{functionDirectory}}}
                """
            });
            logger.LogInformation(
                "Attached textual DXFunction fallback instructions with {FunctionCount} exact registry name(s) for Ollama model {Model} because native tool metadata is unavailable.",
                advertisedFunctions.Count,
                model);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not attach textual DXFunction fallback instructions for Ollama model {Model}.", model);
            throw;
        }
    }

    /// <summary>
    /// Performs send request once for <see cref="OllamaThinkingChatClient"/>, keeping the operation consistent with the state and invariants of the surrounding Ollama thinking chat workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="completionOption">Completion option value supplied to the Ollama thinking chat operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The provider HTTP response or a local context preflight failure before sending.</returns>
    private async Task<(HttpResponseMessage? Response, string? PreflightFailure)> SendRequestOnceAsync(
        OllamaChatRequest request,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken)
    {
    try
    {
            cancellationToken.ThrowIfCancellationRequested();
            // Compatibility probes can attach a textual tool directory after the original
            // fit check. Recheck the exact payload against its already allocated context.
            var requestOptions = request.Options!;
            var budget = councilRuntime.OllamaThinkingChatClientResolveRequestBudget(
                request.ContextCeiling ?? requestOptions.NumCtx, requestOptions.NumPredict, false,
                MeasureRequestCharacters(request.Messages, request.Tools), model, logger, request.ContextCeiling ?? requestOptions.NumCtx);
            if (budget.PromptExceedsMaximum)
            {
                var diagnostic = $"OllamaContextBudgetExceeded;required={budget.RequiredContextTokens};" +
                    $"allowed={budget.NumCtx.GetValueOrDefault()};output={budget.NumPredict}; " +
                    $"The request for {model} no longer fits after transport preparation.";
                logger.LogWarning("Ollama transport preflight failed for {Model}: estimated {Required}, allowed {Allowed}.",
                    model, budget.RequiredContextTokens, budget.NumCtx);
                return (null, diagnostic);
            }
            // NumPredict already includes the thinking reserve from the initial request.
            requestOptions.NumPredict = budget.NumPredict;
            if (fitContextToRequest)
                requestOptions.NumCtx = councilRuntime.FitOllamaRequestContext(budget, model, logger);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
            {
                Content = JsonContent.Create(request, options: jsonOptions)
            };
            return (await http.SendAsync(httpRequest, completionOption, cancellationToken).ConfigureAwait(false), null);
    
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        logger.LogDebug(
            "Ollama HTTP request was cancelled by its caller for {ClientMethod}; aborting the underlying transport is expected during Council stop/round cancellation.",
            nameof(SendRequestOnceAsync));
        throw;
    }
    catch (Exception __serviceMethodException)
    {
        logger.LogError(__serviceMethodException, $"Service method {nameof(OllamaThinkingChatClient)}.{nameof(SendRequestOnceAsync)} failed.");
        throw;
    }
}

}
