using LocalGPT.Services.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Writes ruleset texts into the session: HUD messages, control ownership and game outcomes.
/// </summary>
public interface IGameMessageService
{
    /// <summary>
    /// Formats a ruleset text with the session tokens and optional extra tokens.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="textKey">The text key.</param>
    /// <param name="extra">Extra tokens that override session tokens.</param>
    /// <returns>The formatted text.</returns>
    string Format(GameEngineContext context, string textKey, IReadOnlyDictionary<string, object?>? extra = null);

    /// <summary>
    /// Sets the HUD message from a ruleset text.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="textKey">The text key; empty keys leave the message unchanged.</param>
    /// <param name="extra">Extra tokens that override session tokens.</param>
    void Say(GameEngineContext context, string textKey, IReadOnlyDictionary<string, object?>? extra = null);

    /// <summary>
    /// Completes the session with an outcome owner, reason and message.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="ownerTextKey">The text key of the final turn owner.</param>
    /// <param name="reasonTextKey">The text key of the input reason.</param>
    /// <param name="messageTextKey">The text key of the HUD message.</param>
    /// <param name="extra">Extra tokens.</param>
    void Complete(GameEngineContext context, string ownerTextKey, string reasonTextKey, string messageTextKey, IReadOnlyDictionary<string, object?>? extra = null);

    /// <summary>
    /// Applies the turn owner and input requirement of the session's control mode, with a reason text family.
    /// </summary>
    /// <param name="context">The engine context.</param>
    /// <param name="reasonTextPrefix">The reason text prefix; the control mode suffix <c>.ai</c>, <c>.shared</c> or <c>.human</c> is appended.</param>
    /// <param name="extra">Extra tokens.</param>
    void ApplyControlOwnership(GameEngineContext context, string reasonTextPrefix, IReadOnlyDictionary<string, object?>? extra = null);
}
