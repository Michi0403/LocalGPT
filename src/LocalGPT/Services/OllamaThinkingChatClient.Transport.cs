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
    /// <returns>The HTTP response message produced by the operation.</returns>
    private async Task<HttpResponseMessage> SendRequestWithToolFallbackAsync(
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

            var response = await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode ||
                response.StatusCode is not (System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.NotImplemented))
            {
                return response;
            }

            // Newer Ollama runtimes expose provider reasoning only when the request explicitly opts into
            // thinking. Older runtimes/models may reject that field. Probe once per provider-qualified
            // model, remember the result, and preserve the normal request instead of breaking chat.
            if (request.Think == true)
            {
                response.Dispose();
                request.Think = null;
                var withoutThinking = await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
                if (withoutThinking.IsSuccessStatusCode)
                {
                    councilRuntime.OllamaThinkingChatClientRememberExplicitThinkingRejected(http.BaseAddress, model, logger);
                    return withoutThinking;
                }
                if (withoutThinking.StatusCode is not (System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.NotImplemented))
                    return withoutThinking;

                // If tools are present, the rejection can still be tool metadata rather than thinking.
                // Restore the reasoning request while probing the established textual-tool fallback.
                if (request.Tools is { Count: > 0 })
                {
                    withoutThinking.Dispose();
                    request.Think = true;
                    EnsureTextualDxFunctionFallbackPrompt(request);
                    request.Tools = null;
                    var withoutTools = await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
                    if (withoutTools.IsSuccessStatusCode)
                    {
                        councilRuntime.OllamaThinkingChatClientRememberNativeToolsRejected(http.BaseAddress, model, logger);
                        return withoutTools;
                    }
                    if (withoutTools.StatusCode is not (System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.NotImplemented))
                        return withoutTools;

                    // Both optional request features are now independently implicated: tools still failed
                    // without thinking and thinking still failed without tools. Try the minimal request and
                    // only cache both incompatibilities when that control request succeeds.
                    withoutTools.Dispose();
                    request.Think = null;
                    var minimalResponse = await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
                    if (minimalResponse.IsSuccessStatusCode)
                    {
                        councilRuntime.OllamaThinkingChatClientRememberExplicitThinkingRejected(http.BaseAddress, model, logger);
                        councilRuntime.OllamaThinkingChatClientRememberNativeToolsRejected(http.BaseAddress, model, logger);
                    }
                    return minimalResponse;
                }

                // The same 400/501 occurred with and without the thinking flag and no tools are present,
                // so there is no evidence that thinking caused the rejection. Do not poison the cache.
                return withoutThinking;
            }

            if (request.Tools is { Count: > 0 })
            {
                councilRuntime.OllamaThinkingChatClientRememberNativeToolsRejected(http.BaseAddress, model, logger);
                logger.LogInformation(
                    "Ollama model {Model} rejected native tool metadata with HTTP {StatusCode}; retrying the same chat request without automatic tools.",
                    model,
                    (int)response.StatusCode);
                response.Dispose();
                EnsureTextualDxFunctionFallbackPrompt(request);
                request.Tools = null;
                return await SendRequestOnceAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
            }

            return response;
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

            var functions = GetAutomaticFunctions();
            ValidateUniqueAutomaticToolNames(functions);
            if (functions.Count == 0)
                return;

            var functionDirectory = string.Join(
                Environment.NewLine,
                functions.Select(function => $"- {function.Name}: {function.Parameters}"));
            request.Messages.Insert(0, new OllamaChatMessage
            {
                Role = "system",
                Content = $$$"""
                {{{marker}}}
                This exact provider-qualified Ollama model does not accept native tool metadata. LocalGPT still supports policy-checked DXFunctions through textual call recovery.
                If a function is genuinely needed, emit one standalone JSON object with exactly this shape and no invented function name:
                {"functionName":"exact.registry.name","arguments":{}}
                LocalGPT will validate the exact registry name, apply normal automatic/deferred approval policy, execute it when permitted, display the tool activity, return the tool result, and then continue your response.
                Do not guess a function name. If the capability you want is not in the directory below, report it as a requested capability instead of pretending it exists.
                Exact automatic/deferred function directory for this request:
                {{{functionDirectory}}}
                """
            });
            logger.LogInformation(
                "Attached textual DXFunction fallback instructions with {FunctionCount} exact registry name(s) for Ollama model {Model} because native tool metadata is unavailable.",
                functions.Count,
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
    /// <returns>The HTTP response message produced by the operation.</returns>
    private async Task<HttpResponseMessage> SendRequestOnceAsync(
        OllamaChatRequest request,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken)
    {
    try
    {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
            {
                Content = JsonContent.Create(request, options: jsonOptions)
            };
            return await http.SendAsync(httpRequest, completionOption, cancellationToken).ConfigureAwait(false);
    
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
