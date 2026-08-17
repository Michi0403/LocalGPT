using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.CodeParser;
using DevExpress.CodeParser.Diagnostics;
using DevExpress.Xpo.Logger;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Security;
using LocalGPT.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalGPT.Controller
{
    /// <summary>
    /// Exposes the local GPT diagnostic application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
    /// </summary>
    public partial class LocalGptDiagnosticController
    {
        /// <summary>
        /// Retrieves memory smoke for the LocalGPT diagnostic API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="memory">Chat memory service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="chatClient">Chat client dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("/__diag/memory-smoke")]
        [HumanApprovalRequired("diagnostic.memory.smoke", "Write diagnostic memory", "Persist a bounded diagnostic conversation and call the configured model.", "High", "Memory reviewer")]
        public async Task<IResult> GetMemorySmoke(
            [FromServices] IChatMemoryService memory,
            [FromServices] IChatClient chatClient,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "write diagnostic memory and call a configured model") is { } denied)
                    return denied;

                await RunEnsureCreateAsyncOnce(memory, null, null).ConfigureAwait(false);

                var seedMessages = new List<BlazorChatMessage>
            {
                new(ChatRole.User, "Memory smoke test: the current user wants LocalGPT to support reviewed Java Minecraft mod/plugin work with Ollama gpt-oss:20b, persistent chat memory, AI helper files, and humane safety."),
                new(ChatRole.Assistant, "<details class=\"model-thinking\" open><summary>Model thinking</summary>Saved memory says LocalGPT should remember previous DXAiChat work, use AI guidance files, support Minecraft mod building, and protect people, including the current user.</details>\nMemory captured for debug testing.")
            };

                var conversationId = await memory.SaveConversationAsync("Diagnostic - gpt-oss:20b", seedMessages, cancellationToken: ct).ConfigureAwait(false);
                var response = await chatClient.GetResponseAsync(
                    [
                        new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "Using your LocalGPT bootstrap, saved memory, and AI guidance files, answer in exactly three bullets: project mission, one Minecraft Mod Builder feature you should support, and the humane safety rule for the current user. Mention gpt-oss:20b if you see it in memory.")
                    ],
                    new ChatOptions
                    {
                        MaxOutputTokens = 1024
                    },
                    ct).ConfigureAwait(false);

                return Results.Ok(new
                {
                    SavedConversationId = conversationId,
                    Conversations = await memory.GetConversationsAsync(5, ct).ConfigureAwait(false),
                    RecentThoughts = await memory.GetRecentThoughtsAsync(5, ct).ConfigureAwait(false),
                    Response = response.Text,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "GetMemorySmoke");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }    
        }

        /// <summary>
        /// Returns the post process review projection for the LocalGPT diagnostic API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="memory">Chat memory service dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="chatClient">Chat client dependency used by the LocalGPT diagnostic workflow to provide the corresponding application capability.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="ct">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpPost("/__diag/process-review")]
        [HumanApprovalRequired("diagnostic.process.review", "Run grounded process review", "Run the submitted grounded process review through the configured model and memory workflow.", "Medium", "Process reviewer")]
        public async Task<IResult> PostProcessReview(
            [FromBody] GroundedProcessReviewRequest request,
            [FromServices] IChatMemoryService memory,
            [FromServices] IChatClient chatClient,
            [FromQuery] bool userConfirmed,
            CancellationToken ct)
        {
            try
            {
                if (RequireHumanConfirmation(userConfirmed, "run a grounded model-based process review") is { } denied)
                    return denied;

                await RunEnsureCreateAsyncOnce(memory, null, null).ConfigureAwait(false);

                var facts = request.Facts
                    .Where(fact => !string.IsNullOrWhiteSpace(fact))
                    .Select(fact => fact.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(40)
                    .ToList();

                var evidence = new StringBuilder()
                    .AppendLine("Grounded process review evidence:")
                    .AppendLine("- LocalGPT is a Blazor/ASP.NET Core app hosted by a WinUI WebView2 shell.")
                    .AppendLine("- The preferred local debug model is Ollama gpt-oss:20b.")
                    .AppendLine("- Treat missing evidence as unknown, not as permission to invent details.");

                foreach (var fact in facts)
                    evidence.Append("- ").AppendLine(fact);

                var conversations = await memory.GetConversationsAsync(5, ct).ConfigureAwait(false);
                foreach (var conversation in conversations)
                {
                    evidence.Append("- Saved memory conversation: ")
                        .Append(conversation.DisplayName)
                        .Append(" (")
                        .Append(conversation.MessageCount)
                        .AppendLine(" messages).");
                }

                var prompt = $"""
                You are a grounded second reviewer for LocalGPT implementation work.

                Rules:
                - Use only the evidence below for factual claims.
                - If something is plausible but not in the evidence, put it under "Needs verification".
                - Do not invent file paths, commits, tests, UI results, or user decisions.
                - Be kind, concise, and useful.
                - Keep private reasoning brief enough to leave room for the visible review.
                - Return Markdown with exactly these sections: Verified facts, Risks, Next checks, Feature ideas, Needs verification.

                {evidence}

                Question:
                {(!string.IsNullOrWhiteSpace(request.Question) ? request.Question : "Review the current LocalGPT process and suggest grounded next steps.")}
                """;

                var response = await chatClient.GetResponseAsync(
                    [
                        new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, prompt)
                    ],
                    new ChatOptions
                    {
                        MaxOutputTokens = Math.Clamp(request.MaxOutputTokens, 256, 4096)
                    },
                    ct).ConfigureAwait(false);

                return Results.Ok(new
                {
                    Evidence = evidence.ToString(),
                    Response = response.Text,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Diagnostic operation {Operation} failed.", "PostProcessReview");
                return Results.InternalServerError("Diagnostic operation failed. Review the local server logs for details.");
            }           
        }

    }
}
