using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Parses and executes the reusable meta-command vocabulary used by LocalGPT's Matrix-style ASCII operator surfaces.</summary>
public interface IConsoleOperatorService
{
    /// <summary>Returns shell backends the current host can expose behind LocalGPT's own ASCII wall.</summary>
    /// <returns>The available shell descriptor collection.</returns>
    IReadOnlyList<LocalConsoleShellDescriptor> GetAvailableShells();

    /// <summary>Submits one user-entered line, treating colon-prefixed lines as meta commands and ordinary lines as shell commands.</summary>
    /// <param name="request">Operator line and current shell selection.</param>
    /// <param name="cancellationToken">Cancellation token for parsing/control work.</param>
    /// <returns>The immediate operator-layer result.</returns>
    Task<LocalConsoleOperatorResult> SubmitAsync(LocalConsoleOperatorRequest request, CancellationToken cancellationToken = default);

    /// <summary>Formats a bounded list of saved chat sessions for the ASCII operator transcript.</summary>
    /// <param name="conversations">Saved conversation summaries visible to the current chat page.</param>
    /// <param name="take">Maximum number of entries to include.</param>
    /// <returns>A compact human-readable session list.</returns>
    string FormatSavedConversations(IReadOnlyList<ChatMemoryConversationSummary> conversations, int take = 12);

    /// <summary>Resolves one saved chat by full identifier, identifier prefix, or case-insensitive title fragment.</summary>
    /// <param name="conversations">Saved conversation summaries visible to the current chat page.</param>
    /// <param name="selector">Human-entered identifier or title selector.</param>
    /// <returns>The matching saved conversation, or <c>null</c> when no unambiguous match exists.</returns>
    ChatMemoryConversationSummary? ResolveSavedConversation(IReadOnlyList<ChatMemoryConversationSummary> conversations, string selector);
}
