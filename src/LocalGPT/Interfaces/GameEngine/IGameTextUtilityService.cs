namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Text shaping helpers of terminal views and council games.
/// </summary>
public interface IGameTextUtilityService
{
    /// <summary>
    /// Collapses whitespace and cuts text to a maximum length.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <param name="maximumLength">The maximum length.</param>
    /// <returns>The compact text.</returns>
    string Compact(string? value, int maximumLength);

    /// <summary>
    /// Removes protocol tags and markup from a spoken line.
    /// </summary>
    /// <param name="value">The line.</param>
    /// <returns>The clean line.</returns>
    string CleanSpokenLine(string? value);

    /// <summary>
    /// Joins distinct non-empty values.
    /// </summary>
    /// <param name="separator">The separator.</param>
    /// <param name="values">The values.</param>
    /// <returns>The joined text.</returns>
    string JoinDistinct(string separator, IEnumerable<string?> values);

    /// <summary>
    /// Shortens a model selection name for frames (host and provider suffixes removed, 16 characters).
    /// </summary>
    /// <param name="value">The model name.</param>
    /// <returns>The short label.</returns>
    string ShortModel(string? value);

    /// <summary>
    /// Shortens a model selection name for transcript summaries (20 characters, no whitespace compaction).
    /// </summary>
    /// <param name="value">The model name.</param>
    /// <returns>The short label.</returns>
    string ShortSummaryModel(string? value);

    /// <summary>
    /// Pads or cuts text to an exact width.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <param name="width">The width.</param>
    /// <returns>The fitted text.</returns>
    string Fit(string? value, int width);

    /// <summary>
    /// Writes text into a character row, clipping at the row bounds.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="x">The start column.</param>
    /// <param name="text">The text.</param>
    void Put(char[] row, int x, string text);
}
