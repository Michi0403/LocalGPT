using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Writes ruleset texts into the session: HUD messages, control ownership and outcomes.
/// </summary>
/// <param name="tokens">Builds session tokens.</param>
/// <param name="texts">Resolves and formats texts.</param>
/// <param name="logger">Receives message diagnostics.</param>
public sealed class GameMessageService(
    IGameTokenService tokens,
    IGameTextService texts,
    ILogger<GameMessageService> logger) : IGameMessageService
{
    /// <inheritdoc />
    public string Format(GameEngineContext context, string textKey, IReadOnlyDictionary<string, object?>? extra = null)
    {
        try
        {
            var values = tokens.Build(context.Session);
            if (extra is not null)
            {
                foreach (var pair in extra)
                    values[pair.Key] = pair.Value;
            }
            return texts.Format(context.Ruleset, textKey, values);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting game message {TextKey} failed.", textKey);
            throw;
        }
    }

    /// <inheritdoc />
    public void Say(GameEngineContext context, string textKey, IReadOnlyDictionary<string, object?>? extra = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(textKey))
                return;
            context.Session.Message = Format(context, textKey, extra);
            logger.LogDebug("Game session {SessionId} message {TextKey}: {Message}", context.Session.Id, textKey, context.Session.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Writing game message {TextKey} failed.", textKey);
            throw;
        }
    }

    /// <inheritdoc />
    public void Complete(GameEngineContext context, string ownerTextKey, string reasonTextKey, string messageTextKey, IReadOnlyDictionary<string, object?>? extra = null)
    {
        try
        {
            var session = context.Session;
            session.Status = "Completed";
            session.AutoplayEnabled = false;
            session.HumanInputRequired = false;
            session.CurrentTurnOwner = Format(context, ownerTextKey, extra);
            session.InputReason = Format(context, reasonTextKey, extra);
            session.Message = Format(context, messageTextKey, extra);
            logger.LogInformation("Game session {SessionId} ({GameKey}) completed: {Outcome}", session.Id, session.GameKey, session.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Completing the game session failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public void ApplyControlOwnership(GameEngineContext context, string reasonTextPrefix, IReadOnlyDictionary<string, object?>? extra = null)
    {
        try
        {
            var session = context.Session;
            var mode = session.ControlMode switch
            {
                CouncilGameControlMode.Ai => "ai",
                CouncilGameControlMode.Shared => "shared",
                _ => "human"
            };
            session.HumanInputRequired = session.ControlMode != CouncilGameControlMode.Ai;
            session.CurrentTurnOwner = Format(context, $"control.owner.{mode}", extra);
            session.InputReason = Format(context, $"{reasonTextPrefix}.{mode}", extra);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying game control ownership failed.");
            throw;
        }
    }
}
