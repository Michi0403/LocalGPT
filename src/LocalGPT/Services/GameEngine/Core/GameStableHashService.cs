using System.Security.Cryptography;
using System.Text;
using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Core;

/// <summary>
/// Deterministic SHA-256 based hashing for seeds, rigs and engine-owned choices.
/// </summary>
/// <param name="logger">Receives hashing diagnostics.</param>
public sealed class GameStableHashService(ILogger<GameStableHashService> logger) : IGameStableHashService
{
    /// <inheritdoc />
    public byte[] Hash(string? text)
    {
        try
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Hashing game text failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public int SeedFromText(string? text)
    {
        try
        {
            return BitConverter.ToInt32(Hash(text), 0) & int.MaxValue;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Deriving a game seed failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public int StableRange(string seed, int minimum, int maximum)
    {
        try
        {
            var value = BitConverter.ToUInt32(Hash(seed), 0);
            return minimum + (int)(value % (uint)Math.Max(1, maximum - minimum + 1));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Picking a stable game value failed.");
            throw;
        }
    }
}
