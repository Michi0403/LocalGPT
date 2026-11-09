using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Reads terminal text styles declared in ruleset parameters.
/// </summary>
/// <param name="parameters">Reads style properties.</param>
/// <param name="logger">Receives style diagnostics.</param>
public sealed class GameStyleReader(IGameParameterReader parameters, ILogger<GameStyleReader> logger) : IGameStyleReader
{
    /// <summary>
    /// The semantic style run budget of one frame.
    /// </summary>
    private const int StyleRunBudget = 4096;

    /// <inheritdoc />
    public int MaximumRuns => StyleRunBudget;

    /// <inheritdoc />
    public CouncilAsciiTextStyle Read(JsonElement style)
    {
        try
        {
            var mode = parameters.ReadString(style, "colorMode", nameof(CouncilAsciiColorMode.Indexed256));
            return new CouncilAsciiTextStyle
            {
                ColorMode = Enum.TryParse<CouncilAsciiColorMode>(mode, true, out var parsed) ? parsed : CouncilAsciiColorMode.Indexed256,
                ForegroundColor = parameters.TryRead(style, "foreground", out _) ? parameters.ReadInt(style, "foreground", 0) : null,
                BackgroundColor = parameters.TryRead(style, "background", out _) ? parameters.ReadInt(style, "background", 0) : null,
                Bold = parameters.ReadBool(style, "bold", false),
                Dim = parameters.ReadBool(style, "dim", false),
                Invert = parameters.ReadBool(style, "invert", false)
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a game text style failed.");
            throw;
        }
    }
}
