using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>
/// Owns ASCII-chat transcript formatting, animation extraction, compact speaker naming, and replay signatures so UI components remain presentation-only.
/// </summary>
public sealed class AsciiChatTextService
{
    /// <summary>Capability marker used by persisted Council teams whose workflow requires the shared ASCII surface to be visible before provider work begins.</summary>
    public const string RequiredSurfaceCapability = "localgpt.ascii.surface.required";

    /// <summary>Stores the logger used to record bounded ASCII text-processing diagnostics without recording chat content.</summary>
    private readonly ILogger<AsciiChatTextService> logger;

    /// <summary>Matches one bounded fenced ASCII animation block inside canonical chat content.</summary>
    private readonly Regex AsciiSequenceBlockPattern = new(
        "```ascii-sequence\\s*(?<body>.*?)```",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    /// <summary>Matches explicit frame separators inside an ASCII animation block.</summary>
    private readonly Regex AsciiSequenceFrameSeparatorPattern = new(
        "^\\s*---\\s*frame(?:\\s+\\d+)?\\s*---\\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    /// <summary>Matches provider trace summary elements so thinking and function labels remain visible in terminal text.</summary>
    private readonly Regex SummaryPattern = new(
        "<summary>(?<summary>.*?)</summary>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    /// <summary>Matches inert HTML tags that should be removed after trace summaries are projected into terminal text.</summary>
    private readonly Regex HtmlTagPattern = new(
        "<[^>]+>",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    /// <summary>Matches Markdown heading markers while preserving the human-visible heading content.</summary>
    private readonly Regex MarkdownHeadingPattern = new(
        "^\\s{0,3}#{1,6}\\s+(?<heading>.+?)\\s*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    /// <summary>Matches fenced ASCII/text delimiters that should not appear as Markdown syntax in the terminal.</summary>
    private readonly Regex AsciiFencePattern = new(
        "^\\s*```(?:ascii|text)?\\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    /// <summary>
    /// Initializes the ASCII chat text service.
    /// </summary>
    /// <param name="logger">Logger used for bounded formatting diagnostics without recording chat content.</param>
    public AsciiChatTextService(ILogger<AsciiChatTextService> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Builds the replayable Matrix-style transcript from canonical DXAiChat messages and server-owned Council participant lanes.
    /// </summary>
    /// <param name="messages">Canonical conversation messages also used by the regular chat presentation.</param>
    /// <param name="participantActivities">Council participant activities associated with the current chat.</param>
    /// <param name="assistantDisplayName">Provider/model display name used to derive the compact assistant nickname.</param>
    /// <returns>A plain-text transcript suitable for a monospace terminal surface.</returns>
    public string BuildConversationTranscript(
        IReadOnlyList<BlazorChatMessage> messages,
        IReadOnlyList<CouncilLiveParticipantActivitySnapshot> participantActivities,
        string assistantDisplayName)
    {
        try
        {
            if (messages.Count == 0)
            {
                return "┌─ LOCALGPT ASCII CHAT ─────────────────────────────────────────────────────────┐" + Environment.NewLine
                    + "│ This terminal mirrors the current /chat conversation.                         │" + Environment.NewLine
                    + "│ Messages, thinking traces and function activity will replay here.              │" + Environment.NewLine
                    + "└───────────────────────────────────────────────────────────────────────────────┘";
            }

            var assistantNickname = BuildNickname(assistantDisplayName);
            var builder = new StringBuilder();
            foreach (var message in messages)
            {
                if (message is null || string.IsNullOrWhiteSpace(message.Content))
                    continue;

                var nickname = ResolveRoleNickname(message.Role, assistantNickname);
                var body = NormalizeMessageBody(message.Content);
                if (string.IsNullOrWhiteSpace(body))
                    continue;

                if (builder.Length > 0)
                    builder.AppendLine().AppendLine();
                AppendChatMessage(builder, nickname, body);
            }

            AppendCouncilParticipantActivities(builder, participantActivities);

            return builder.Length == 0
                ? "LOCALGPT: The current conversation has no text content to render yet."
                : builder.ToString().TrimEnd();
        }
        catch (RegexMatchTimeoutException exception)
        {
            logger.LogWarning(exception, "ASCII chat transcript formatting exceeded its bounded regex budget; raw message text will be shown instead.");
            return JoinRawMessages(messages);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "ASCII chat transcript formatting failed; raw message text will be shown instead.");
            return JoinRawMessages(messages);
        }
    }

    /// <summary>
    /// Extracts the newest bounded ASCII animation sequence from canonical chat history.
    /// </summary>
    /// <param name="messages">Canonical conversation messages shared with the normal chat view.</param>
    /// <returns>Two to twelve frames when the newest message contains a valid sequence; otherwise an empty collection.</returns>
    public IReadOnlyList<string> ExtractSequenceFrames(IReadOnlyList<BlazorChatMessage> messages)
    {
        try
        {
            for (var index = messages.Count - 1; index >= 0; index--)
            {
                var content = messages[index]?.Content;
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                var matches = AsciiSequenceBlockPattern.Matches(content);
                if (matches.Count == 0)
                    continue;

                var body = matches[matches.Count - 1].Groups["body"].Value;
                var frames = AsciiSequenceFrameSeparatorPattern
                    .Split(body)
                    .Select(frame => WebUtility.HtmlDecode(frame).Trim('\r', '\n'))
                    .Where(frame => !string.IsNullOrWhiteSpace(frame))
                    .Take(12)
                    .Select(frame => frame.Length <= 4096 ? frame : frame[..4096])
                    .ToList();
                return frames.Count >= 2 ? frames : [];
            }

            return [];
        }
        catch (RegexMatchTimeoutException exception)
        {
            logger.LogDebug(exception, "ASCII sequence extraction exceeded its bounded regex budget.");
            return [];
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "ASCII sequence extraction failed; the normal transcript remains available.");
            return [];
        }
    }

    /// <summary>
    /// Builds a stable change signature for the current ASCII animation frames without exposing their content to logs.
    /// </summary>
    /// <param name="frames">Current bounded animation frames.</param>
    /// <returns>A separator-delimited signature used only for renderer change detection.</returns>
    public string BuildSequenceSignature(IReadOnlyList<string> frames)
    {
        try
        {
            return frames.Count > 1 ? string.Join("\u001f", frames) : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "ASCII sequence signature generation failed; the renderer will refresh the sequence defensively.");
            return Guid.NewGuid().ToString("N");
        }
    }

    /// <summary>
    /// Builds a bounded signature for Council participant activity used to detect when the mirrored ASCII transcript needs refreshing.
    /// </summary>
    /// <param name="participantActivities">Server-owned Council participant snapshots associated with the current chat.</param>
    /// <returns>A content-free signature derived from activity keys, timestamps, running state, and visible lengths.</returns>
    public string BuildParticipantSignature(IReadOnlyList<CouncilLiveParticipantActivitySnapshot> participantActivities)
    {
        try
        {
            return string.Join(
                "\u001e",
                participantActivities.Select(activity =>
                    $"{activity.ActivityKey}\u001f{activity.UpdatedAtUtc.Ticks}\u001f{activity.IsRunning}\u001f{activity.Content?.Length ?? 0}\u001f{activity.FinalContent?.Length ?? 0}"));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "ASCII Council participant signature generation failed; the mirror will refresh defensively.");
            return Guid.NewGuid().ToString("N");
        }
    }

    /// <summary>Returns whether one configured Council team requires the shared ASCII presentation surface to be open while its workflow runs.</summary>
    /// <param name="team">Persisted service-backed team definition to classify.</param>
    /// <returns><c>true</c> when the team carries the maintained ASCII-surface requirement marker.</returns>
    public bool RequiresAsciiSurface(OrganicCouncilTeamDefinition? team)
    {
        try
        {
            return team?.PreferredCapabilities?.Any(capability =>
                string.Equals(capability, RequiredSurfaceCapability, StringComparison.OrdinalIgnoreCase)) == true;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Classifying a Council team for the required ASCII presentation surface failed.");
            return false;
        }
    }

    /// <summary>Returns whether one Human Collaboration request is a configured Council role response that can be projected into an active ASCII game surface.</summary>
    /// <param name="request">Human Collaboration request to classify without mutating it.</param>
    /// <returns><c>true</c> when the request uses the configured Council role-response operation contract.</returns>
    public bool IsCouncilRoleResponseRequest(HumanCollaborationRequest request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            return request.OperationKey.StartsWith("council.role.response.", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Classifying a Human Collaboration request for the ASCII game surface failed.");
            return false;
        }
    }

    /// <summary>Normalizes Markdown and provider trace markup into readable terminal text while preserving ASCII geometry.</summary>
    /// <param name="content">Canonical user-visible chat content to project into the ASCII surface.</param>
    /// <returns>Plain terminal text with thinking and function-trace labels retained.</returns>
    private string NormalizeMessageBody(string content)
    {
        try
        {
            var normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal);
            normalized = AsciiSequenceBlockPattern.Replace(normalized, match =>
            {
                var frames = AsciiSequenceFrameSeparatorPattern.Split(match.Groups["body"].Value)
                    .Count(frame => !string.IsNullOrWhiteSpace(frame));
                return $"\n[ASCII SEQUENCE · {Math.Clamp(frames, 0, 12)} frame(s) · playing above transcript]\n";
            });
            normalized = SummaryPattern.Replace(normalized, match =>
            {
                var summary = WebUtility.HtmlDecode(HtmlTagPattern.Replace(match.Groups["summary"].Value, string.Empty)).Trim();
                if (summary.StartsWith("Model thinking", StringComparison.OrdinalIgnoreCase))
                    return "\n[THINK]\n";
                if (summary.StartsWith("Function call", StringComparison.OrdinalIgnoreCase))
                    return $"\n[CALL {summary["Function call".Length..].Trim(' ', '·')}]\n";
                if (summary.StartsWith("Function result", StringComparison.OrdinalIgnoreCase))
                    return $"\n[RESULT {summary["Function result".Length..].Trim(' ', '·')}]\n";
                return $"\n[{summary}]\n";
            });
            normalized = normalized
                .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
                .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
                .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase)
                .Replace("</p>", "\n", StringComparison.OrdinalIgnoreCase)
                .Replace("</div>", "\n", StringComparison.OrdinalIgnoreCase)
                .Replace("</details>", "\n", StringComparison.OrdinalIgnoreCase);
            normalized = HtmlTagPattern.Replace(normalized, string.Empty);
            normalized = WebUtility.HtmlDecode(normalized);
            normalized = MarkdownHeadingPattern.Replace(normalized, match => match.Groups["heading"].Value);
            normalized = AsciiFencePattern.Replace(normalized, string.Empty);
            normalized = normalized.Replace("```ascii-game", "[ASCII GAME]", StringComparison.OrdinalIgnoreCase);
            normalized = Regex.Replace(normalized, "\\n{4,}", "\n\n\n", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
            return normalized.Trim();
        }
        catch (RegexMatchTimeoutException exception)
        {
            logger.LogDebug(exception, "One ASCII message normalization regex exceeded its bounded budget.");
            return WebUtility.HtmlDecode(content).Trim();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not normalize one message for the ASCII transcript.");
            return WebUtility.HtmlDecode(content).Trim();
        }
    }

    /// <summary>Appends current Council participant lanes without duplicating or mutating authoritative Council history.</summary>
    /// <param name="builder">Destination transcript builder containing canonical chat messages.</param>
    /// <param name="participantActivities">Server-owned Council participant snapshots associated with the current chat.</param>
    private void AppendCouncilParticipantActivities(
        StringBuilder builder,
        IReadOnlyList<CouncilLiveParticipantActivitySnapshot> participantActivities)
    {
        try
        {
            if (participantActivities.Count == 0)
                return;

            foreach (var activity in participantActivities
                .OrderBy(item => item.StartedAtUtc)
                .ThenBy(item => item.ActivityKey, StringComparer.Ordinal))
            {
                var body = BuildCouncilParticipantBody(activity);
                if (string.IsNullOrWhiteSpace(body))
                    continue;

                if (builder.Length > 0)
                    builder.AppendLine().AppendLine();
                AppendChatMessage(builder, BuildNickname(activity.ModelName), body);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not append Council participant lanes to the ASCII transcript; the canonical conversation remains available.");
        }
    }

    /// <summary>Builds one readable Council participant entry separating trace activity from final speech.</summary>
    /// <param name="activity">Server-owned participant activity snapshot to project into terminal text.</param>
    /// <returns>A bounded participant body containing phase, role, trace and final answer information when available.</returns>
    private string BuildCouncilParticipantBody(CouncilLiveParticipantActivitySnapshot activity)
    {
        try
        {
            var streamed = NormalizeMessageBody(activity.Content ?? string.Empty);
            var final = NormalizeMessageBody(activity.FinalContent ?? string.Empty);
            var body = new StringBuilder();

            var phase = string.IsNullOrWhiteSpace(activity.Phase) ? "Council" : activity.Phase.Trim();
            var role = string.IsNullOrWhiteSpace(activity.Role) ? string.Empty : activity.Role.Trim();
            body.Append('[').Append(phase);
            if (!string.IsNullOrWhiteSpace(role) && !string.Equals(role, phase, StringComparison.OrdinalIgnoreCase))
                body.Append(" · ").Append(role);
            body.Append(']');

            if (!string.IsNullOrWhiteSpace(final))
            {
                var trace = streamed;
                var finalIndex = streamed.LastIndexOf(final, StringComparison.Ordinal);
                if (finalIndex >= 0 && finalIndex + final.Length >= streamed.Length - 8)
                    trace = streamed[..finalIndex].TrimEnd();

                if (!string.IsNullOrWhiteSpace(trace))
                    body.AppendLine().Append("[THINK / FUNCTION TRACE]").AppendLine().Append(trace);
                body.AppendLine().Append("[SAY]").AppendLine().Append(final);
            }
            else if (!string.IsNullOrWhiteSpace(streamed))
            {
                body.AppendLine().Append(activity.IsRunning ? "[LIVE]" : "[TRACE]").AppendLine().Append(streamed);
            }
            else if (!string.IsNullOrWhiteSpace(activity.StatusMessage))
            {
                body.AppendLine().Append("[STATUS] ").Append(activity.StatusMessage.Trim());
            }

            return body.ToString().Trim();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not project Council participant {ActivityKey} into the ASCII transcript.", activity.ActivityKey);
            return string.Empty;
        }
    }

    /// <summary>Appends one nickname-prefixed chat message while aligning continuation lines without distorting ASCII art.</summary>
    /// <param name="builder">Destination transcript builder.</param>
    /// <param name="nickname">Compact terminal speaker label.</param>
    /// <param name="body">Normalized message body.</param>
    private void AppendChatMessage(StringBuilder builder, string nickname, string body)
    {
        try
        {
            var lines = body.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            var prefix = $"{nickname}: ";
            var continuation = new string(' ', prefix.Length);
            for (var index = 0; index < lines.Length; index++)
            {
                builder.Append(index == 0 ? prefix : continuation).Append(lines[index]);
                if (index < lines.Length - 1)
                    builder.AppendLine();
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not append one service-owned ASCII transcript message.");
            builder.Append(nickname).Append(": ").Append(body);
        }
    }

    /// <summary>Creates a short stable terminal nickname from a provider-qualified model display name.</summary>
    /// <param name="displayName">Provider or model display name from the active chat session.</param>
    /// <returns>A compact uppercase terminal nickname.</returns>
    private string BuildNickname(string displayName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return "AI";

            var value = displayName.Trim();
            if (value.Contains("Council", StringComparison.OrdinalIgnoreCase))
                return "COUNCIL";
            var emDashIndex = value.LastIndexOf('—');
            if (emDashIndex >= 0 && emDashIndex + 1 < value.Length)
                value = value[(emDashIndex + 1)..].Trim();
            var endpointIndex = value.IndexOf('@');
            if (endpointIndex > 0)
                value = value[..endpointIndex].Trim();
            value = value.Replace(':', '-');
            value = Regex.Replace(value, "[^A-Za-z0-9._-]+", string.Empty, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
            const int maxNicknameLength = 16;
            if (value.Length > maxNicknameLength)
            {
                var suffixIndex = value.LastIndexOf('-');
                var suffix = suffixIndex > 0 && value.Length - suffixIndex <= 6 ? value[suffixIndex..] : string.Empty;
                value = string.IsNullOrEmpty(suffix)
                    ? value[..maxNicknameLength]
                    : value[..Math.Max(1, maxNicknameLength - suffix.Length)] + suffix;
            }
            return string.IsNullOrWhiteSpace(value) ? "AI" : value.ToUpperInvariant();
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Could not derive an ASCII nickname from an assistant display name.");
            return "AI";
        }
    }

    /// <summary>Maps one DevExpress DXAiChat role to the compact nickname shown in the ASCII transcript.</summary>
    /// <param name="role">DevExpress chat role attached to the canonical message.</param>
    /// <param name="assistantNickname">Nickname already derived for the selected assistant.</param>
    /// <returns>The terminal speaker label for the message.</returns>
    private string ResolveRoleNickname(ChatMessageRole role, string assistantNickname)
    {
        try
        {
            if (role == ChatMessageRole.User)
                return "YOU";
            if (role == ChatMessageRole.Assistant)
                return assistantNickname;
            if (role == ChatMessageRole.System)
                return "SYS";
            if (role == ChatMessageRole.Error)
                return "ERROR";
            return "CHAT";
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Could not map one DevExpress chat role to an ASCII nickname.");
            return "CHAT";
        }
    }

    /// <summary>Builds the fallback plain transcript used only when richer ASCII formatting cannot be completed safely.</summary>
    /// <param name="messages">Canonical chat messages to join without additional parsing.</param>
    /// <returns>Plain message content separated by blank lines.</returns>
    private string JoinRawMessages(IReadOnlyList<BlazorChatMessage> messages)
    {
        try
        {
            return string.Join(Environment.NewLine + Environment.NewLine, messages.Select(message => message.Content ?? string.Empty));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not build the fallback ASCII transcript from canonical chat messages.");
            return string.Empty;
        }
    }
}
