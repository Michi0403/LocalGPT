using System.Text.Json;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Reads terminal text styles declared in ruleset parameters.
/// </summary>
public interface IGameStyleReader
{
    /// <summary>
    /// Gets the maximum number of semantic style runs collected for one frame.
    /// </summary>
    /// <value>The run budget.</value>
    int MaximumRuns { get; }

    /// <summary>
    /// Reads a style object with <c>colorMode</c>, <c>foreground</c>, <c>background</c>, <c>bold</c>, <c>dim</c> and <c>invert</c>.
    /// </summary>
    /// <param name="style">The style object.</param>
    /// <returns>The text style.</returns>
    CouncilAsciiTextStyle Read(JsonElement style);
}
