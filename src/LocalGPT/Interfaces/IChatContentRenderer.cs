namespace LocalGPT.Interfaces;

/// <summary>
/// Converts the progressively accumulated chat response into safe, renderable
/// HTML for the Blazor message template. The renderer is stateless; response
/// buffering remains owned by the per-response formatter.
/// </summary>
public interface IChatContentRenderer
{
    /// <summary>
    /// Performs render for <see cref="IChatContentRenderer"/>, keeping the operation consistent with the state and invariants of the surrounding chat content workflow.
    /// </summary>
    /// <param name="content">Content value supplied to the chat content operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string Render(string? content);

    /// <summary>
    /// Normalizes for render.
    /// </summary>
    /// <param name="content">Content value supplied to the chat content operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string NormalizeForRender(string? content);
}
