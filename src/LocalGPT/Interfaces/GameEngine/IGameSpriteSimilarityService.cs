namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Compares sprites so the asset check can reject copies of another entrant's sprite.
/// </summary>
public interface IGameSpriteSimilarityService
{
    /// <summary>
    /// Computes the cell overlap of two sprites over their non-empty cells.
    /// </summary>
    /// <param name="first">The first sprite rows.</param>
    /// <param name="second">The second sprite rows.</param>
    /// <returns>A similarity in [0, 1].</returns>
    double Similarity(IReadOnlyList<string> first, IReadOnlyList<string> second);
}
