namespace LocalGPT.Interfaces;

/// <summary>
/// Exposes the current /chat ASCII presentation state to provider and Council prompt builders without coupling backend services to a browser component.
/// </summary>
public interface IChatAsciiExperienceState
{
    /// <summary>Gets a value indicating whether the shared ASCII terminal is currently open for the active chat.</summary>
    /// <value><c>true</c> while the ASCII presentation surface is open.</value>
    bool IsSurfaceOpen { get; }

    /// <summary>Gets a value indicating whether contextual/random ASCII fun is enabled for the active chat.</summary>
    /// <value><c>true</c> when models may optionally add bounded ASCII art or sequence content.</value>
    bool IsFunModeEnabled { get; }

    /// <summary>Gets the active conversation identifier associated with the current ASCII presentation state.</summary>
    /// <value>The active conversation identifier when one has already been persisted; otherwise <c>null</c>.</value>
    Guid? ConversationId { get; }

    /// <summary>Updates the ASCII presentation state owned by the current chat/circuit.</summary>
    /// <param name="conversationId">Current persisted conversation identifier, when available.</param>
    /// <param name="isSurfaceOpen">Whether the shared ASCII terminal is visible.</param>
    /// <param name="isFunModeEnabled">Whether contextual/random ASCII fun is enabled.</param>
    void Update(Guid? conversationId, bool isSurfaceOpen, bool isFunModeEnabled);

    /// <summary>Builds bounded model guidance describing whether the ASCII surface is open and which optional content conventions are available.</summary>
    /// <returns>A system-prompt fragment, or an empty string when ASCII presentation is completely inactive.</returns>
    string BuildModelGuidance();
}
