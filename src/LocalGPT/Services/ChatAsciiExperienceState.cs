using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Stores circuit-scoped ASCII presentation capability state so normal chat and Council providers can react to the UI without creating a second conversation channel.
/// </summary>
/// <param name="logger">Logger used for bounded diagnostics when presentation-state operations fail.</param>
public sealed class ChatAsciiExperienceState(ILogger<ChatAsciiExperienceState> logger) : IChatAsciiExperienceState
{
    /// <summary>Synchronizes foreground component updates with background Council/provider reads.</summary>
    private readonly object sync = new();
    /// <summary>Stores whether the shared ASCII terminal is currently open.</summary>
    private bool isSurfaceOpen;
    /// <summary>Tracks the user's opt-in permission for contextual ASCII embellishments so background provider and Council work can read one circuit-consistent capability state.</summary>
    private bool isFunModeEnabled;
    /// <summary>Stores the current persisted conversation identifier, when one exists.</summary>
    private Guid? conversationId;

    /// <summary>Gets a value indicating whether the shared ASCII terminal is currently open for the active chat.</summary>
    /// <value><c>true</c> while the ASCII presentation surface is open.</value>
    public bool IsSurfaceOpen
    {
        get
        {
            lock (sync)
                return isSurfaceOpen;
        }
    }

    /// <summary>Gets a value indicating whether contextual/random ASCII fun is enabled for the active chat.</summary>
    /// <value><c>true</c> when models may optionally add bounded ASCII art or sequence content.</value>
    public bool IsFunModeEnabled
    {
        get
        {
            lock (sync)
                return isFunModeEnabled;
        }
    }

    /// <summary>Gets the active conversation identifier associated with the current ASCII presentation state.</summary>
    /// <value>The active persisted conversation identifier, when available.</value>
    public Guid? ConversationId
    {
        get
        {
            lock (sync)
                return conversationId;
        }
    }

    /// <summary>Updates the ASCII presentation state owned by the current chat/circuit.</summary>
    /// <param name="conversationIdValue">Current persisted conversation identifier, when available.</param>
    /// <param name="isSurfaceOpenValue">Whether the shared ASCII terminal is visible.</param>
    /// <param name="isFunModeEnabledValue">Whether contextual/random ASCII fun is enabled.</param>
    public void Update(Guid? conversationIdValue, bool isSurfaceOpenValue, bool isFunModeEnabledValue)
    {
        try
        {
            lock (sync)
            {
                conversationId = conversationIdValue;
                isSurfaceOpen = isSurfaceOpenValue;
                isFunModeEnabled = isFunModeEnabledValue;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Updating the circuit-scoped ASCII chat presentation state failed.");
            throw;
        }
    }

    /// <summary>Builds bounded model guidance describing whether the ASCII surface is open and which optional content conventions are available.</summary>
    /// <returns>A system-prompt fragment, or an empty string when ASCII presentation is completely inactive.</returns>
    public string BuildModelGuidance()
    {
        try
        {
            bool open;
            bool fun;
            lock (sync)
            {
                open = isSurfaceOpen;
                fun = isFunModeEnabled;
            }

            if (!open && !fun)
                return string.Empty;

            var state = $"LocalGPT ASCII presentation state: terminal={(open ? "OPEN" : "CLOSED")}; contextual ASCII fun={(fun ? "ENABLED" : "DISABLED")}.";
            if (!open)
                return state + " The user is currently using the regular chat presentation. Do not add ASCII-only decoration solely for the closed terminal.";
            if (!fun)
                return state + " The terminal mirrors this same canonical conversation, including reasoning/function traces. Respond normally; do not add decorative ASCII solely because the terminal is open.";

            return state + " " +
                "The ASCII terminal mirrors the same canonical conversation and remains an operator-capable LocalGPT terminal; it is not a separate chat channel. " +
                "While contextual ASCII fun is enabled, EVERY assistant/Council-facing turn should include at least one context-appropriate visual reaction: a tiny smiley/text ornament, compact ```ascii art, or a short ```ascii-sequence animation. " +
                "Vary the decoration and keep most turns lightweight; use 2-12 pregenerated frames separated by a line exactly like '--- frame ---' only when motion adds value. " +
                "Decorations are additive and must never replace the meaningful answer, overwrite prior transcript text, or emit ANSI/control escape sequences. " +
                "Function calls, results, and thinking traces are mirrored automatically, so do not restate them merely for the terminal. " +
                "For interactive Council/game display work, inspect localgpt.ascii.surface.get and localgpt.game.display.get, use the smallest cell/text/fill/blit/full-frame operation that fits, and submit pregenerated animations once so browser playback does not interrupt the conversation flow. " +
                "Use database-backed regex/knowledge DXFunctions when they can retrieve established display or parsing conventions instead of recreating them in prose.";
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Building ASCII chat model guidance failed; the provider request will continue without optional ASCII guidance.");
            return string.Empty;
        }
    }
}
