using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;

namespace LocalGPT.Services;

public sealed class CouncilChatClient(
    IMultiModelCouncilService councilService,
    Func<MultiModelCouncilRequest> requestFactory) : IChatClient
{
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

        yield return CreateUpdate($"_AI Council started with {request.ModelNames.Count} member(s): {string.Join(", ", request.ModelNames)}. Local models may take a while; partial progress is shown here so DXAiChat does not look frozen._\n\n");

        MultiModelCouncilResult result;
        try
        {
            result = await councilService.RunAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            yield break;
        }

        yield return CreateUpdate(FormatResult(result));
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
        return FormatResult(result);
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
        return prompt.Length <= 12000 ? prompt : prompt[^12000..];
    }

    private static string FormatResult(MultiModelCouncilResult result)
    {
        var builder = new StringBuilder()
            .AppendLine("# AI Council Result")
            .AppendLine()
            .Append("Members: ")
            .AppendLine(result.ModelNames.Count == 0 ? "none" : string.Join(", ", result.ModelNames))
            .AppendLine();

        if (result.Warnings.Count > 0)
        {
            builder.AppendLine("## Warnings");
            foreach (var warning in result.Warnings)
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

        if (result.Artifacts.Count > 0)
        {
            builder.AppendLine("## Downloadable Artifacts");
            foreach (var artifact in result.Artifacts)
                builder.Append("- [").Append(artifact.Name).Append("](").Append(artifact.DownloadUrl).Append(") - ").AppendLine(artifact.Kind);
            builder.AppendLine();
        }

        if (result.Steps.Count > 0)
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
                    builder.AppendLine("**Visible answer excerpt:**")
                        .AppendLine()
                        .AppendLine(TrimForMessage(step.VisibleContent, 1800))
                        .AppendLine();
                }

                if (!string.IsNullOrWhiteSpace(step.Thinking))
                {
                    builder
                        .AppendLine("<details class=\"model-thinking\" open>")
                        .AppendLine("<summary>Model thinking</summary>")
                        .AppendLine("<pre>")
                        .AppendLine(WebUtility.HtmlEncode(TrimForMessage(step.Thinking, 1800)))
                        .AppendLine("</pre>")
                        .AppendLine("</details>")
                        .AppendLine();
                }

                builder.AppendLine("</details>").AppendLine();
            }
        }

        builder
            .AppendLine("## Consensus")
            .AppendLine(result.FinalAnswer);

        return builder.ToString();
    }

    private static ChatResponseUpdate CreateUpdate(string text) =>
        new(ChatRole.Assistant, [new TextContent(text)]);

    private static string TrimForMessage(string text, int maxLength)
    {
        var normalized = text.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized[..maxLength].TrimEnd()}\n\n_Trimmed in DXAiChat; see the council log/memory for full text._";
    }
}
