namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Reads complete fenced ASCII frames from a Council presentation answer.
/// </summary>
public interface IGameAsciiFrameReader
{
    /// <summary>
    /// Reads the fenced frames.
    /// </summary>
    /// <param name="content">The answer content.</param>
    /// <param name="maximumFrames">The maximum number of frames.</param>
    /// <param name="maximumLength">The maximum length of one frame.</param>
    /// <returns>The frames.</returns>
    IReadOnlyList<string> Read(string? content, int maximumFrames, int maximumLength);
}
