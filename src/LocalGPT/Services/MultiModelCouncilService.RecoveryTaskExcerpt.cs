using Microsoft.Extensions.AI;

namespace LocalGPT.Services
{
    /// <summary>
    /// Builds the bounded task restatement used by the final-answer recovery turn, so a participant that produced only
    /// thinking (or lost the start of a long prompt to the provider context window) finishes the original task.
    /// </summary>
    public sealed partial class MultiModelCouncilService
    {
        /// <summary>Runtime value bounding the restated task excerpt in final-answer recovery.</summary>
        private const string FinalRecoveryTaskExcerptCharactersKey = "CouncilFinalRecoveryTaskExcerptCharacters";

        /// <summary>
        /// Returns the head and tail of the latest user message of the participant conversation, bounded by the
        /// configured excerpt size. Head and tail are kept because workflow templates place the instruction first and
        /// the user request last.
        /// </summary>
        /// <param name="originalMessages">Messages of the participant turn that produced no visible answer.</param>
        /// <param name="logger">Logger used for bounded diagnostics.</param>
        /// <returns>The task excerpt, or an empty string when no user message exists.</returns>
        private string MultiModelCouncilServiceBuildRecoveryTaskExcerpt(IReadOnlyList<ChatMessage> originalMessages, ILogger logger)
        {
            try
            {
                var task = originalMessages.LastOrDefault(message => message.Role == ChatRole.User && !string.IsNullOrWhiteSpace(message.Text))?.Text?.Trim()
                    ?? string.Empty;
                var limit = ReadFinalRecoveryExcerptCharacters(logger);
                if (task.Length <= limit)
                    return task;
                var head = limit / 3;
                var tail = limit - head;
                return task[..head] + Environment.NewLine + "[… middle of the request omitted for the recovery turn …]" + Environment.NewLine + task[^tail..];
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not build the task excerpt for Council final-answer recovery; the recovery turn continues without it.");
                return string.Empty;
            }
        }

        /// <summary>Reads the configured excerpt size for final-answer recovery.</summary>
        /// <param name="logger">Logger used for bounded diagnostics.</param>
        /// <returns>Excerpt characters, at least 1000.</returns>
        private int ReadFinalRecoveryExcerptCharacters(ILogger logger)
        {
            try
            {
                return Math.Max(1000, runtimePolicy.GetInt(FinalRecoveryTaskExcerptCharactersKey));
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Runtime value {Key} is unavailable; final-answer recovery uses a 6000 character task excerpt.", FinalRecoveryTaskExcerptCharactersKey);
                return 6000;
            }
        }
    }
}
