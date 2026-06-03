using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;

namespace LocalGPT.Services;

public sealed partial class CouncilChatClient(
    IMultiModelCouncilService councilService,
    Func<MultiModelCouncilRequest> requestFactory,
    Func<string, string>? downloadUrlResolver = null) : IChatClient
{
    private const int MaxDxAiChatPromptCharacters = 60000;
    private const int MaxVisiblePromptCharacters = 12000;

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var text = await RunCouncilAsync(messages, cancellationToken);
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, [new TextContent(text)]));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(messages);
        if (request.ModelNames.Count == 0)
        {
            yield return CreateUpdate("No AI Council members are selected. Select at least one Ollama model in the DXAiChat council controls.");
            yield break;
        }

        yield return CreateUpdate($"_AI Council started with {request.ModelNames.Count} member(s): {string.Join(", ", request.ModelNames)}. Local models may take a while; progress/status stays visible, detailed model output is inspectable, and the final result appears in a clean Council result block._\n\n");

        var updates = new ConcurrentQueue<string>();
        request.ProgressMessage = message => updates.Enqueue($"_Council status: {message}_\n\n");
        request.StreamUpdate = text =>
        {
            if (!string.IsNullOrEmpty(text))
                updates.Enqueue(text);
        };
        request.StepCompleted = step => updates.Enqueue(FormatStepProgress(step));

        var startedAt = DateTimeOffset.UtcNow;
        var lastHeartbeat = DateTimeOffset.UtcNow;
        var runTask = councilService.RunAsync(request, cancellationToken);
        while (!runTask.IsCompleted)
        {
            while (updates.TryDequeue(out var update))
                yield return CreateUpdate(update);

            if (DateTimeOffset.UtcNow - lastHeartbeat > TimeSpan.FromSeconds(20))
            {
                lastHeartbeat = DateTimeOffset.UtcNow;
                yield return CreateUpdate($"_Council still running after {(int)(DateTimeOffset.UtcNow - startedAt).TotalSeconds}s. Waiting for local Ollama model output..._\n\n");
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
            yield return CreateUpdate(update);

        MultiModelCouncilResult result;
        try
        {
            result = await runTask;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            yield break;
        }

        yield return CreateUpdate(FormatResult(result, includeProcess: false));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    private async Task<string> RunCouncilAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var request = CreateRequest(messages);

        if (request.ModelNames.Count == 0)
            return "No AI Council members are selected. Select at least one Ollama model in the DXAiChat council controls.";

        var result = await councilService.RunAsync(request, cancellationToken);
        return FormatResult(result, includeProcess: true);
    }

    private MultiModelCouncilRequest CreateRequest(IEnumerable<ChatMessage> messages)
    {
        var request = requestFactory();
        request.Prompt = BuildPrompt(messages);
        return request;
    }

    private static string BuildPrompt(IEnumerable<ChatMessage> messages)
    {
        var builder = new StringBuilder()
            .AppendLine("Answer this DXAiChat conversation as the LocalGPT AI Council.")
            .AppendLine("Use the selected members, preserve user intent, and include a concise consensus.")
            .AppendLine();

        foreach (var message in messages.Where(message => message.Role != ChatRole.System))
        {
            var text = message.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            builder
                .Append(message.Role == ChatRole.Assistant ? "Assistant" : "User")
                .AppendLine(":")
                .AppendLine(text)
                .AppendLine();
        }

        var prompt = builder.ToString().Trim();
        return prompt.Length <= MaxDxAiChatPromptCharacters
            ? prompt
            : prompt[^MaxDxAiChatPromptCharacters..];
    }

    private string FormatResult(MultiModelCouncilResult result, bool includeProcess)
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
                .AppendLine(TrimForDisplay(result.Prompt, MaxVisiblePromptCharacters))
                .AppendLine("```")
                .AppendLine("</details>")
                .AppendLine();
        }

        var warnings = result.Warnings
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var finalAnswer = result.FinalAnswer.Trim();
        if (LooksLikelyTruncated(finalAnswer))
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

        if (LooksLikelyTruncated(finalAnswer))
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
                .AppendLine("## Authoritative Download Links")
                .AppendLine("These links were generated by LocalGPT after the council run and supersede any model-guessed artifact path inside the consensus text.")
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
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private string ResolveDownloadUrl(string downloadUrl)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl))
            return downloadUrl;

        if (Uri.TryCreate(downloadUrl, UriKind.Absolute, out _))
            return downloadUrl;

        return downloadUrlResolver?.Invoke(downloadUrl) ?? downloadUrl;
    }

    private static string FormatStepProgress(MultiModelCouncilStep step)
    {
        var builder = new StringBuilder()
            .AppendLine()
            .AppendLine("<p class=\"localgpt-stream-status\"><em>")
            .Append(WebUtility.HtmlEncode($"{step.ModelName} finished {step.Phase} / {step.Role} in {step.DurationSeconds:n1}s. Step details were streamed above; final consensus appears below."))
            .AppendLine("</em></p>")
            .AppendLine();

        if (!string.IsNullOrWhiteSpace(step.Error))
        {
            builder
                .AppendLine($"<details class=\"council-step\" open>")
                .Append("<summary>")
                .Append(WebUtility.HtmlEncode($"{step.ModelName} error during {step.Phase}"))
                .AppendLine("</summary>")
                .AppendLine()
                .AppendLine("**Error:**")
                .AppendLine()
                .AppendLine(step.Error.Trim())
                .AppendLine()
                .AppendLine("</details>")
                .AppendLine();
        }

        return builder.ToString();
    }

    private static string TrimForDisplay(string text, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var trimmed = text.Trim();
        return trimmed.Length <= maxCharacters
            ? trimmed
            : $"{trimmed[..maxCharacters].TrimEnd()}{Environment.NewLine}... prompt truncated for display; full prompt is stored in the CouncilLogs markdown file and SQLite user message ...";
    }

    private static bool LooksLikelyTruncated(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.TrimEnd();
        if (trimmed.Length < 1000)
            return false;

        if (trimmed.EndsWith("...", StringComparison.Ordinal) ||
            trimmed.EndsWith("…", StringComparison.Ordinal) ||
            trimmed.EndsWith(".", StringComparison.Ordinal) ||
            trimmed.EndsWith("!", StringComparison.Ordinal) ||
            trimmed.EndsWith("?", StringComparison.Ordinal) ||
            trimmed.EndsWith("]", StringComparison.Ordinal) ||
            trimmed.EndsWith(")", StringComparison.Ordinal) ||
            trimmed.EndsWith("}", StringComparison.Ordinal) ||
            trimmed.EndsWith("```", StringComparison.Ordinal))
        {
            return false;
        }

        return TruncatedTailPattern().IsMatch(trimmed) ||
            !char.IsPunctuation(trimmed[^1]);
    }

    [GeneratedRegex(@"\b(?:with|and|or|the|a|an|for|to|in|of|by|as|if|when|once|then|because|from|into|that|this|which|th)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TruncatedTailPattern();

    private static ChatResponseUpdate CreateUpdate(string text) =>
        new(ChatRole.Assistant, [new TextContent(text)]);
}
