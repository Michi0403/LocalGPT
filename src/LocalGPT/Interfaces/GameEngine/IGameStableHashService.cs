namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Deterministic hashing used for seeds, rigs and authoritative engine choices.
/// </summary>
public interface IGameStableHashService
{
    /// <summary>
    /// Hashes UTF-8 text with SHA-256.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The 32 hash bytes.</returns>
    byte[] Hash(string? text);

    /// <summary>
    /// Derives a non-negative seed from text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The first four hash bytes as a non-negative integer.</returns>
    int SeedFromText(string? text);

    /// <summary>
    /// Picks a stable value in an inclusive range from text.
    /// </summary>
    /// <param name="seed">The seed text.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <returns>The stable value.</returns>
    int StableRange(string seed, int minimum, int maximum);
}
