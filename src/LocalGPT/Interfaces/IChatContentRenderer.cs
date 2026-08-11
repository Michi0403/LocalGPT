namespace LocalGPT.Interfaces;

/// <summary>
/// Converts the progressively accumulated chat response into safe, renderable
/// HTML for the Blazor message template. The renderer is stateless; response
/// buffering remains owned by the per-response formatter.
/// </summary>
public interface IChatContentRenderer
{
    /// <summary>
    /// Runs the render operation.
    /// </summary>
    string Render(string? content);

    /// <summary>
    /// Normalizes for render.
    /// </summary>
    string NormalizeForRender(string? content);
}
