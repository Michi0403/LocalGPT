using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace LocalGPT.Services;

/// <summary>
/// Adapts the multi-model Council runtime to the Microsoft.Extensions.AI chat-client contract.
/// </summary>
/// <param name="serviceScopeFactory">Creates scoped Council execution dependencies.</param>
/// <param name="requestFactory">Creates the current user-configured Council request.</param>
/// <param name="logger">Writes bounded chat and Council diagnostics.</param>
/// <param name="councilRuntime">Builds prompts, updates and bounded runtime text.</param>
/// <param name="councilText">Provides maintained Council text parsing and validation.</param>
/// <param name="catalog">Provides maintained limits and display defaults.</param>
/// <param name="liveSessions">Owns detachable live Council-session state.</param>
/// <param name="downloadUrlResolver">Optionally maps generated artifact routes for the current host.</param>
[DocumentationUpdated("2.1.20")]
public sealed partial class CouncilChatClient(
    IServiceScopeFactory serviceScopeFactory,
    Func<MultiModelCouncilRequest> requestFactory,
    ILogger logger,
    CouncilRuntimeService councilRuntime,
    CouncilTextService councilText,
    LocalGptCatalogService catalog,
    ICouncilLiveSessionService liveSessions,
    Func<string, string>? downloadUrlResolver = null) : IChatClient
{


    /// <summary>Runs one non-streaming Council request and returns its formatted assistant response.</summary>
    /// <param name="messages">Current chat history.</param>
    /// <param name="options">Optional Microsoft.Extensions.AI chat options.</param>
    /// <param name="cancellationToken">Cancels the Council operation.</param>
    /// <returns>A task that completes with the formatted Council chat response.</returns>
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var text = await RunCouncilAsync(messages, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(text))
                text = "The AI Council ended without a final answer. Review LocalGPT application logs and the streamed council status for the failed phase.";
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, [new TextContent(text)]));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI Council non-streaming response failed.");
            return new ChatResponse(new ChatMessage(
                ChatRole.Assistant,
                [new TextContent("The AI Council could not complete this response. Review LocalGPT application logs, verify the selected local models, and try again.")]));
        }
        
    }

    /// <summary>Starts or attaches to a live Council run and streams bounded response updates.</summary>
    /// <param name="messages">Current chat history.</param>
    /// <param name="options">Optional Microsoft.Extensions.AI chat options.</param>
    /// <param name="cancellationToken">Detaches this stream when canceled; explicit session controls own run cancellation.</param>
    /// <returns>An asynchronous sequence of Council response updates.</returns>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var updates = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        var attached = 1;

        var requestMessages = messages.ToList();
        var request = CreateRequest(requestMessages);
        ArgumentNullException.ThrowIfNull(request);
        if (request.ModelNames.Count == 0)
        {
            yield return councilRuntime.CreateUpdate(
                "No AI Council members are selected. Select at least one provider model in the DXAiChat council controls.",
                logger);
            yield break;
        }
        if (request.UnavailableModelSelections.Count > 0)
        {
            yield return councilRuntime.CreateUpdate(
                councilText.ProviderUnavailableRunNotice(request.UnavailableModelSelections, logger),
                logger);
            yield break;
        }

        var introduction = $"_AI Council started with {request.ModelNames.Count} member(s): {string.Join(", ", request.ModelNames)}. Provider thinking, tool activity and answer text remain visible; model execution is host-aware while each member stream is presented as an intact readable block._\n\n";
        var liveMessageMarker = $"<!-- localgpt-live-council:{request.RunId:N} -->\n";
        var initiatingUserMessage = requestMessages
            .LastOrDefault(message => message.Role == ChatRole.User)
            ?.Text
            ?.Trim() ?? string.Empty;
        var liveCancellation = liveSessions.Begin(
            request.RunId,
            request.ModelNames,
            initiatingUserMessage,
            introduction);
        liveSessions.SetStatus(request.RunId, "Preparing Council preflight and host-aware execution lanes.");

        void Publish(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            liveSessions.Append(request.RunId, text);
            if (Volatile.Read(ref attached) == 1)
                updates.Writer.TryWrite(text);
        }

        request.ProgressMessage = message => liveSessions.SetStatus(request.RunId, message);
        request.StreamUpdate = Publish;
        request.StepCompleted = step => Publish(councilRuntime.FormatStepProgress(step, logger));

        yield return councilRuntime.CreateUpdate(liveMessageMarker + introduction, logger);
        var startedAt = DateTimeOffset.UtcNow;
        var nextHeartbeatAt = startedAt.AddSeconds(35);
        var runTask = RunCouncilInBackgroundAsync(request, liveCancellation, updates.Writer, Publish);

        try
        {
            while (!runTask.IsCompleted)
            {
                while (updates.Reader.TryRead(out var update))
                    yield return councilRuntime.CreateUpdate(update, logger);

                using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var dataAvailable = updates.Reader.WaitToReadAsync(waitCts.Token).AsTask();
                var heartbeat = Task.Delay(TimeSpan.FromSeconds(10), waitCts.Token);
                var completed = await Task.WhenAny(runTask, dataAvailable, heartbeat).ConfigureAwait(false);
                await waitCts.CancelAsync().ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    yield break;

                if (completed == heartbeat && DateTimeOffset.UtcNow >= nextHeartbeatAt)
                {
                    // Heartbeats are live-session state, not model transcript content. Touching the
                    // session refreshes the visual running indicator without injecting status text
                    // into a member's thinking, answer, saved transcript, or missing-feature report.
                    liveSessions.Touch(request.RunId);
                    nextHeartbeatAt = DateTimeOffset.UtcNow.AddSeconds(10);
                }
            }

            while (updates.Reader.TryRead(out var update))
                yield return councilRuntime.CreateUpdate(update, logger);
        }
        finally
        {
            Interlocked.Exchange(ref attached, 0);
            logger.LogInformation(
                "AI Council DXChat stream detached for run {RunId}; the Council runtime remains owned by the live-session service until completion or explicit stop.",
                request.RunId);
        }
    }

    /// <summary>Executes the Council in an isolated scope while publishing updates to the detachable live session.</summary>
    /// <param name="request">Prepared Council request.</param>
    /// <param name="cancellationToken">Cancels the owned live run.</param>
    /// <param name="writer">Completes the attached update channel.</param>
    /// <param name="publish">Publishes one bounded visible update.</param>
    /// <returns>A task that completes with the Council result, or null when canceled/failed.</returns>
    private async Task<MultiModelCouncilResult?> RunCouncilInBackgroundAsync(
        MultiModelCouncilRequest request,
        CancellationToken cancellationToken,
        ChannelWriter<string> writer,
        Action<string> publish)
    {
        try
        {
            var scope = serviceScopeFactory.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                var councilService = scope.ServiceProvider.GetRequiredService<IMultiModelCouncilService>();
                var result = await councilService.RunAsync(request, cancellationToken).ConfigureAwait(false);
                if (result is null)
                {
                    publish("The AI Council ended without a result. Review the LocalGPT log for the failed council phase.");
                    return null;
                }

                publish(FormatResult(result, includeProcess: false));
                return result;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            publish("_The AI Council run was stopped by an explicit user action._\n\n");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI Council background execution failed for run {RunId}.", request.RunId);
            publish("The AI Council could not complete this response. Review LocalGPT application logs, verify the selected local models, and try again.");
            return null;
        }
        finally
        {
            liveSessions.Complete(request.RunId);
            writer.TryComplete();
        }
    }

    /// <summary>Returns no additional keyed service from this adapter.</summary>
    /// <param name="serviceType">Requested service type.</param>
    /// <param name="serviceKey">Optional service key.</param>
    /// <returns>Always null because dependencies are provided through constructor injection.</returns>
    public object? GetService(Type serviceType, object? serviceKey = null) {
    try
    {
        return null;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilChatClient)}.{nameof(GetService)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilChatClient)}.{nameof(GetService)} failed.");
        throw;
    }
}

    /// <summary>Completes adapter disposal; owned services are managed by dependency injection.</summary>
    public void Dispose()
    {
    try
    {
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilChatClient)}.{nameof(Dispose)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilChatClient)}.{nameof(Dispose)} failed.");
        throw;
    }
}

    /// <summary>Runs a non-streaming Council request in an isolated service scope.</summary>
    /// <param name="messages">Current chat history.</param>
    /// <param name="cancellationToken">Cancels the Council run.</param>
    /// <returns>A task that completes with formatted result Markdown.</returns>
    private async Task<string> RunCouncilAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        try
        {
            var request = CreateRequest(messages);
            ArgumentNullException.ThrowIfNull(request);
            if (request.ModelNames.Count == 0)
                return "No AI Council members are selected. Select at least one provider model in the DXAiChat council controls.";
            if (request.UnavailableModelSelections.Count > 0)
                return councilText.ProviderUnavailableRunNotice(request.UnavailableModelSelections, logger);

            var scope = serviceScopeFactory.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                var councilService = scope.ServiceProvider.GetRequiredService<IMultiModelCouncilService>();
                var result = await councilService.RunAsync(request, cancellationToken).ConfigureAwait(false);
                return FormatResult(result, includeProcess: true);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI Council execution failed.");
            return string.Empty;
        }
       
    }

    /// <summary>Creates the configured Council request and replaces its prompt with bounded normalized chat history.</summary>
    /// <param name="messages">Current chat history.</param>
    /// <returns>The prepared request, or null when request creation fails.</returns>
    private MultiModelCouncilRequest? CreateRequest(IEnumerable<ChatMessage> messages)
    {
        try
        {
            var request = requestFactory();
            request.Prompt = councilRuntime.BuildPrompt(messages, logger);
            return request;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI Council request creation failed.");
            return null;
        }

    }

 
    /// <summary>Resolves one generated artifact route against the current host when necessary.</summary>
    /// <param name="downloadUrl">Absolute or application-relative download URL.</param>
    /// <returns>The resolved URL, or an empty string when resolution fails.</returns>
    public string ResolveDownloadUrl(string downloadUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
                return downloadUrl;

            if (Uri.TryCreate(downloadUrl, UriKind.Absolute, out _))
                return downloadUrl;

            return downloadUrlResolver?.Invoke(downloadUrl) ?? downloadUrl;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not resolve an artifact download URL.");
            return string.Empty;
        }
    }

    /// <summary>Formats one Council result as canonical safe Markdown for DXAiChat.</summary>
    /// <param name="result">Completed Council result.</param>
    /// <param name="includeProcess">Whether to include per-step process disclosures.</param>
    /// <returns>Formatted Markdown with warnings, consensus and artifact links.</returns>
    public string FormatResult(MultiModelCouncilResult result, bool includeProcess)
    {
        try
        {
            var builder = new StringBuilder()
            .AppendLine("# AI Council Result")
            .AppendLine()
            .Append("Members: ")
            .AppendLine(result.ModelNames.Count == 0 ? "none" : string.Join(", ", result.ModelNames))
            .AppendLine();

            if (result.ProjectId is Guid projectId)
            {
                builder.Append("Database project: ").Append(projectId);
                if (result.ProjectRevisionId is Guid revisionId)
                    builder.Append("; revision: ").Append(revisionId);
                builder.AppendLine().AppendLine("Open the Projects page to select or refine this council-run project.").AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(result.Prompt))
            {
                builder
                    .AppendLine("## Original request")
                    .AppendLine()
                    .AppendLine("This is the council prompt reconstructed from the DXAiChat conversation so saved chats and logs remain auditable.")
                    .AppendLine()
                    .AppendLine("<details class=\"council-prompt\">")
                    .AppendLine("<summary>Prompt sent to the AI Council</summary>")
                    .AppendLine()
                    .AppendLine("```text")
                    .AppendLine(councilText.TrimForDisplay(result.Prompt, catalog.MaxVisiblePromptCharacters, logger))
                    .AppendLine("```")
                    .AppendLine("</details>")
                    .AppendLine();
            }

            var warnings = result.Warnings
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var rawFinalAnswer = result.FinalAnswer.Trim();
            var finalAnswer = EncodeModelMarkdown(rawFinalAnswer);
            if (councilText.LooksLikelyTruncated(rawFinalAnswer, logger))
            {
                warnings.Add("The final answer looks like it may have stopped mid-generation. Use the continuation prompt below to resume from the last section instead of starting over.");
            }

            if (warnings.Count > 0)
            {
                builder.AppendLine("## Warnings");
                foreach (var warning in warnings)
                    builder.Append("- ").AppendLine(warning);
                builder.AppendLine();
            }

            if (result.UserPoll is not null)
            {
                builder
                    .AppendLine("## User Poll")
                    .AppendLine(result.UserPoll.Question);
                foreach (var option in result.UserPoll.Options)
                    builder.Append("- ").Append(option.Label).Append(": ").AppendLine(option.FollowUpPrompt);
                builder.AppendLine();
            }

            if (includeProcess && result.Steps.Count > 0)
            {
                builder.AppendLine("## Council process");
                foreach (var step in result.Steps.OrderBy(step => step.SortOrder))
                {
                    builder
                        .AppendLine($"<details class=\"council-step\">")
                        .Append("<summary>")
                        .Append(WebUtility.HtmlEncode($"{step.ModelName} — {step.Phase} / {step.Role} ({step.DurationSeconds:n1}s)"))
                        .AppendLine("</summary>")
                        .AppendLine();

                    if (!string.IsNullOrWhiteSpace(step.Error))
                    {
                        builder.AppendLine("**Error:**")
                            .AppendLine()
                            .AppendLine(EncodeModelMarkdown(step.Error.Trim()))
                            .AppendLine();
                    }

                    if (!string.IsNullOrWhiteSpace(step.VisibleContent))
                    {
                        builder.AppendLine("**Visible answer:**")
                            .AppendLine()
                            .AppendLine(EncodeModelMarkdown(step.VisibleContent.Trim()))
                            .AppendLine();
                    }

                    if (!string.IsNullOrWhiteSpace(step.Thinking))
                    {
                        builder
                            .AppendLine("<details class=\"model-thinking open\" open>")
                            .AppendLine("<summary>Model thinking</summary>")
                            .AppendLine()
                            .AppendLine(EncodeModelMarkdown(step.Thinking.Trim()))
                            .AppendLine()
                            .AppendLine("</details>")
                            .AppendLine();
                    }

                    builder.AppendLine("</details>").AppendLine();
                }
            }

            builder
                .AppendLine("## Consensus")
                .AppendLine(finalAnswer)
                .AppendLine();

            if (councilText.LooksLikelyTruncated(rawFinalAnswer, logger))
            {
                builder
                    .AppendLine("## Continue Action")
                    .AppendLine("The response appears incomplete. Send this follow-up through DXAiChat:")
                    .AppendLine()
                    .AppendLine("> Continue the previous AI Council answer from the exact cutoff. Do not repeat earlier sections. Finish the artifact/debugging plan and include final download or workspace links if generated.")
                    .AppendLine();
            }

            if (result.Artifacts.Count > 0)
            {
                builder
                    .AppendLine("## Generated Artifact Links")
                    .AppendLine("These links were generated by LocalGPT after the council run. Treat the status labels as binding; generated-only artifacts are not build- or runtime-proven.")
                    .AppendLine();

                foreach (var artifact in result.Artifacts)
                {
                    var downloadUrl = ResolveDownloadUrl(artifact.DownloadUrl);
                    builder
                        .Append("- [")
                        .Append(artifact.Name)
                        .Append("](")
                        .Append(downloadUrl)
                        .Append(") - ")
                        .Append(artifact.Kind)
                        .Append(": ")
                        .AppendLine(artifact.Summary);

                    builder
                        .Append("  - Status: ")
                        .Append(artifact.QualityStatus)
                        .Append("; contract: ")
                        .AppendLine(artifact.ContractStatus);

                    if (artifact.ContractChecks.Count > 0)
                        builder.Append("  - Checks: ").AppendLine(string.Join("; ", artifact.ContractChecks));

                    if (artifact.MissingRequirements.Count > 0)
                        builder.Append("  - Missing: ").AppendLine(string.Join("; ", artifact.MissingRequirements));
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "FormatResult");
            return string.Empty;
        }
    }

    /// <summary>
    /// Canonicalizes model-owned text as HTML-safe Markdown so copied LocalGPT panel tags cannot affect the chat DOM.
    /// </summary>
    /// <param name="value">Model-generated Markdown or previously encoded model text.</param>
    /// <returns>Single-encoded Markdown that preserves headings, lists, tables and physical line breaks.</returns>
    private string EncodeModelMarkdown(string value) {
    try
    {
        return WebUtility.HtmlEncode(WebUtility.HtmlDecode(value));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(CouncilChatClient)}.{nameof(EncodeModelMarkdown)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(CouncilChatClient)}.{nameof(EncodeModelMarkdown)} failed.");
        throw;
    }
}

}
