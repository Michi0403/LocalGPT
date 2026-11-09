using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Primitives.Autoplay;

/// <summary>
/// Autoplay <c>action-cycle</c>: plays the declared <c>actions</c> in turn order.
/// </summary>
/// <param name="parameters">Reads the autoplay parameters.</param>
/// <param name="logger">Receives autoplay diagnostics.</param>
public sealed class ActionCycleAutoplayPrimitive(
    IGameParameterReader parameters,
    ILogger<ActionCycleAutoplayPrimitive> logger) : IGameAutoplayPrimitive
{
    /// <inheritdoc />
    public string Key => "action-cycle";

    /// <inheritdoc />
    public string SelectAction(CouncilGameSessionSnapshot snapshot, JsonElement autoplayParameters)
    {
        try
        {
            var actions = parameters.ReadStrings(autoplayParameters, "actions");
            if (actions.Count == 0)
                return snapshot.LegalActions.FirstOrDefault() ?? string.Empty;
            return actions[(int)(snapshot.Turn % actions.Count)];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Selecting the cyclic autoplay action failed.");
            throw;
        }
    }
}
