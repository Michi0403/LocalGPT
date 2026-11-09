using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.GameEngine;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// A style primitive: adds semantic colour runs for one frame row from a style rule row.
/// </summary>
public interface IGameStylePrimitive
{
    /// <summary>
    /// Gets the primitive key referenced by style rule rows.
    /// </summary>
    /// <value>The stable primitive key.</value>
    string Key { get; }

    /// <summary>
    /// Adds the runs of one row.
    /// </summary>
    /// <param name="rule">The style rule row.</param>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="line">The row text.</param>
    /// <param name="runs">The runs to extend.</param>
    void AddRuns(GameProjectRule rule, int row, string line, List<CouncilAsciiStyleRun> runs);
}
