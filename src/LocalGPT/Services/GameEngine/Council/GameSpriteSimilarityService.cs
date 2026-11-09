using LocalGPT.Interfaces.GameEngine;

namespace LocalGPT.Services.GameEngine.Council;

/// <summary>
/// Compares sprites cell by cell so the asset check can reject copies of another entrant's sprite.
/// </summary>
/// <param name="logger">Receives comparison diagnostics.</param>
public sealed class GameSpriteSimilarityService(ILogger<GameSpriteSimilarityService> logger) : IGameSpriteSimilarityService
{
    /// <inheritdoc />
    public double Similarity(IReadOnlyList<string> first, IReadOnlyList<string> second)
    {
        try
        {
            if (first.Count == 0 || second.Count == 0)
                return 0d;
            var rowCount = Math.Max(first.Count, second.Count);
            var width = Math.Max(first.Max(row => row.Length), second.Max(row => row.Length));
            var shared = 0;
            var union = 0;
            for (var row = 0; row < rowCount; row++)
            {
                for (var column = 0; column < width; column++)
                {
                    var firstCell = row < first.Count && column < first[row].Length ? first[row][column] : '.';
                    var secondCell = row < second.Count && column < second[row].Length ? second[row][column] : '.';
                    var firstEmpty = firstCell is '.' or ' ';
                    var secondEmpty = secondCell is '.' or ' ';
                    if (firstEmpty && secondEmpty)
                        continue;
                    union++;
                    if (firstCell == secondCell)
                        shared++;
                }
            }
            return union == 0 ? 0d : (double)shared / union;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Comparing sprites failed.");
            throw;
        }
    }
}
