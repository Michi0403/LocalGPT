using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace LocalGPT.Services;

public sealed partial class CouncilChatClient(
    IMultiModelCouncilService councilService,
    Func<MultiModelCouncilRequest> requestFactory,
    ILogger logger,
    CouncilRuntimeService councilRuntime,
    CouncilTextService councilText,
    Func<string, string>? downloadUrlResolver = null) : IChatClient
{


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

        try
        {
            var request = CreateRequest(messages);
            ArgumentNullException.ThrowIfNull(request);
            if (request.ModelNames.Count == 0)
            {
                yield return councilRuntime.CreateUpdate(
                    "No AI Council members are selected. Select at least one Ollama model in the DXAiChat council controls.",
                    logger);
                yield break;
            }

            yield return councilRuntime.CreateUpdate(
                $"_AI Council started with {request.ModelNames.Count} member(s): {string.Join(", ", request.ModelNames)}. Thinking and answer text are streamed to this panel as soon as each local model emits them._\n\n",
                logger);

            request.ProgressMessage = message =>
                updates.Writer.TryWrite($"_Council status: {message}_\n\n");
            request.StreamUpdate = text =>
            {
                if (!string.IsNullOrEmpty(text))
                    updates.Writer.TryWrite(text);
            };
            request.StepCompleted = step =>
                updates.Writer.TryWrite(councilRuntime.FormatStepProgress(step, logger));

            var startedAt = DateTimeOffset.UtcNow;
            var runTask = councilService.RunAsync(request, cancellationToken);

            while (!runTask.IsCompleted)
            {
                while (updates.Reader.TryRead(out var update))
                    yield return councilRuntime.CreateUpdate(update, logger);

                using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var dataAvailable = updates.Reader.WaitToReadAsync(waitCts.Token).AsTask();
                var heartbeat = Task.Delay(TimeSpan.FromSeconds(20), waitCts.Token);
                var completed = await Task.WhenAny(runTask, dataAvailable, heartbeat).ConfigureAwait(false);
                await waitCts.CancelAsync().ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    yield break;

                if (completed == heartbeat)
                {
                    yield return councilRuntime.CreateUpdate(
                        $"_Council still running after {(int)(DateTimeOffset.UtcNow - startedAt).TotalSeconds}s. Waiting for local model output..._\n\n",
                        logger);
                }
            }

            while (updates.Reader.TryRead(out var update))
                yield return councilRuntime.CreateUpdate(update, logger);

            MultiModelCouncilResult result;
            try
            {
                result = await runTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            if (result is null)
            {
                yield return councilRuntime.CreateUpdate(
                    "The AI Council ended without a result. Review the LocalGPT log for the failed council phase.",
                    logger);
                yield break;
            }

            yield return councilRuntime.CreateUpdate(FormatResult(result, includeProcess: false), logger);
        }
        finally
        {
            updates.Writer.TryComplete();
            logger.LogInformation("AI Council streaming response ended.");
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    private async Task<string> RunCouncilAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        try
        {
            var request = CreateRequest(messages);
            ArgumentNullException.ThrowIfNull(request);
            if (request.ModelNames.Count == 0)
                return "No AI Council members are selected. Select at least one Ollama model in the DXAiChat council controls.";

            var result = await councilService.RunAsync(request, cancellationToken).ConfigureAwait(false);
            return FormatResult(result, includeProcess: true);
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
                    .AppendLine(councilText.TrimForDisplay(result.Prompt, LocalGptCatalogService.MaxVisiblePromptCharacters, logger))
                    .AppendLine("```")
                    .AppendLine("</details>")
                    .AppendLine();
            }

            var warnings = result.Warnings
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var finalAnswer = result.FinalAnswer.Trim();
            if (councilText.LooksLikelyTruncated(finalAnswer, logger))
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
                            .AppendLine(step.Error.Trim())
                            .AppendLine();
                    }

                    if (!string.IsNullOrWhiteSpace(step.VisibleContent))
                    {
                        builder.AppendLine("**Visible answer:**")
                            .AppendLine()
                            .AppendLine(step.VisibleContent.Trim())
                            .AppendLine();
                    }

                    if (!string.IsNullOrWhiteSpace(step.Thinking))
                    {
                        builder
                            .AppendLine("<details class=\"model-thinking\">")
                            .AppendLine("<summary>Model thinking</summary>")
                            .AppendLine("<pre>")
                            .AppendLine(WebUtility.HtmlEncode(step.Thinking.Trim()))
                            .AppendLine("</pre>")
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

            if (councilText.LooksLikelyTruncated(finalAnswer, logger))
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

}
