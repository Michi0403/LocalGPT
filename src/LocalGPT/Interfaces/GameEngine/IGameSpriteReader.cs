using System.Text.Json;
using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Reads pixel-legend or ASCII sprites from Council answers using the game's sprite parameters.
/// </summary>
public interface IGameSpriteReader
{
    /// <summary>
    /// Reads a sprite.
    /// </summary>
    /// <param name="content">The answer content.</param>
    /// <param name="spriteParameters">The sprite parameters of the game.</param>
    /// <returns>The sprite, empty when unusable.</returns>
    GameSpriteReading Read(string? content, JsonElement spriteParameters);
}
