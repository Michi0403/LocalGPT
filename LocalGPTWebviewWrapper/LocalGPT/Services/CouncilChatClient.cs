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
        var text = await RunCouncilAsync(messages, cancellationToken);
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(text)]);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    private async Task<string> RunCouncilAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var request = requestFactory();
        request.Prompt = BuildPrompt(messages);

        if (request.ModelNames.Count == 0)
            return "No AI Council members are selected. Select at least one Ollama model in the DXAiChat council controls.";

        var result = await councilService.RunAsync(request, cancellationToken);
        return FormatResult(result);
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

        builder
            .AppendLine("## Consensus")
            .AppendLine(result.FinalAnswer);

        return builder.ToString();
    }
}
