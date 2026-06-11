using DevExpress.Charts.Native;
using DevExpress.CodeParser;
using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using static DevExpress.Xpo.Helpers.AssociatedCollectionCriteriaHelper;

namespace LocalGPT.Services;

public sealed partial class CouncilChatClient(
    IMultiModelCouncilService councilService,
    Func<MultiModelCouncilRequest> requestFactory,
    ILogger logger,
    Func<string, string>? downloadUrlResolver = null) : IChatClient
{


    public async Task<ChatResponse?> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var text = await RunCouncilAsync(messages, cancellationToken);
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, [new TextContent(text)]));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,$"Error in GetResponseAsync messages {messages.ToString()} options {options?.ToString()}");
            return null;
        }
        
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(messages);
            ArgumentNullException.ThrowIfNull(request);
            if (request.ModelNames.Count == 0)
            {
                yield return CouncilChatStaticsGeneral.CreateUpdate("No AI Council members are selected. Select at least one Ollama model in the DXAiChat council controls.", logger);
                yield break;
            }

            yield return CouncilChatStaticsGeneral.CreateUpdate($"_AI Council started with {request.ModelNames.Count} member(s): {string.Join(", ", request.ModelNames)}. Local models may take a while; progress/status stays visible, detailed model output is inspectable, and the final result appears in a clean Council result block._\n\n", logger);

            var updates = new ConcurrentQueue<string>();
            request.ProgressMessage = message => updates.Enqueue($"_Council status: {message}_\n\n");
            request.StreamUpdate = text =>
            {
                if (!string.IsNullOrEmpty(text))
                    updates.Enqueue(text);
            };
            request.StepCompleted = step => updates.Enqueue(CouncilChatStaticsGeneral.FormatStepProgress(step, logger));

            var startedAt = DateTimeOffset.UtcNow;
            var lastHeartbeat = DateTimeOffset.UtcNow;
            var runTask = councilService.RunAsync(request, cancellationToken);
            while (!runTask.IsCompleted)
            {
                while (updates.TryDequeue(out var update))
                    yield return CouncilChatStaticsGeneral.CreateUpdate(update, logger);

                if (DateTimeOffset.UtcNow - lastHeartbeat > TimeSpan.FromSeconds(20))
                {
                    lastHeartbeat = DateTimeOffset.UtcNow;
                    yield return CouncilChatStaticsGeneral.CreateUpdate($"_Council still running after {(int)(DateTimeOffset.UtcNow - startedAt).TotalSeconds}s. Waiting for local Ollama model output..._\n\n", logger);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
            }

            while (updates.TryDequeue(out var update))
                yield return CouncilChatStaticsGeneral.CreateUpdate(update, logger);

            MultiModelCouncilResult result;
            try
            {
                result = await runTask;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return CouncilChatStaticsGeneral.CreateUpdate(FormatResult(result, includeProcess: false), logger);
        }
        finally
        {
            logger.LogInformation($"Ending GetStreamingResponseAsync messages {messages.ToString()} options {options?.ToString()}");
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

            var result = await councilService.RunAsync(request, cancellationToken);
            return FormatResult(result, includeProcess: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in RunCouncilAsync messages {messages.ToString()}");
            return string.Empty;
        }
       
    }

    private MultiModelCouncilRequest? CreateRequest(IEnumerable<ChatMessage> messages)
    {
        try
        {
            var request = requestFactory();
            request.Prompt = CouncilChatStaticsGeneral.BuildPrompt(messages, logger);
            return request;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error in CreateRequest messages {messages.ToString()}");
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
            logger.LogError(ex, $"Error in ResolveDownloadUrl downloadUrl {downloadUrl.ToString()}");
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
                    .AppendLine(CouncilChatStringFunctions.TrimForDisplay(result.Prompt, GlobalVariableSlopCollectionToRemove.MaxVisiblePromptCharacters, logger))
                    .AppendLine("```")
                    .AppendLine("</details>")
                    .AppendLine();
            }

            var warnings = result.Warnings
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var finalAnswer = result.FinalAnswer.Trim();
            if (CouncilChatStringFunctions.LooksLikelyTruncated(finalAnswer, logger))
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

            if (CouncilChatStringFunctions.LooksLikelyTruncated(finalAnswer, logger))
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
            logger.LogError(ex, $"Error in FormatResult result {result.ToString()} includeProcess {includeProcess.ToString()}");
            return string.Empty;
        }
    }

}
