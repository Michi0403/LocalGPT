using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Council;

/// <summary>
/// Reads complete fenced ASCII frames from a Council presentation answer; frames made only of echoed placeholders are skipped.
/// </summary>
/// <param name="reader">Decodes answers and detects placeholders.</param>
/// <param name="logger">Receives reader diagnostics.</param>
public sealed class GameAsciiFrameReader(IGameCouncilTextReader reader, ILogger<GameAsciiFrameReader> logger) : IGameAsciiFrameReader
{
    /// <inheritdoc />
    public IReadOnlyList<string> Read(string? content, int maximumFrames, int maximumLength)
    {
        try
        {
            var frames = new List<string>();
            var lines = reader.PlainModelText(content).Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
            List<string>? frameLines = null;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (frameLines is null)
                {
                    if (trimmed.StartsWith("```", StringComparison.Ordinal))
                        frameLines = [];
                    continue;
                }

                if (trimmed.StartsWith("```", StringComparison.Ordinal))
                {
                    var frame = string.Join('\n', frameLines).TrimEnd();
                    var echoedPlaceholderOnly = frameLines
                        .Where(frameLine => !string.IsNullOrWhiteSpace(frameLine))
                        .All(reader.IsTemplatePlaceholder);
                    if (!string.IsNullOrWhiteSpace(frame) && !echoedPlaceholderOnly)
                        frames.Add(frame.Length > maximumLength ? frame[..maximumLength] : frame);
                    frameLines = null;
                    if (frames.Count >= maximumFrames)
                        break;
                    continue;
                }

                frameLines.Add(line);
            }
            return frames;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading Council ASCII frames failed.");
            throw;
        }
    }
}
