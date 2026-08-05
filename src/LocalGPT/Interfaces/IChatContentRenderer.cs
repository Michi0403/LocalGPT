namespace LocalGPT.Interfaces;

/// <summary>
/// Converts the progressively accumulated chat response into safe, renderable
/// HTML for the Blazor message template. The renderer is stateless; response
/// buffering remains owned by the per-response formatter.
/// </summary>
public interface IChatContentRenderer
{
    string Render(string? content);

    string NormalizeForRender(string? content);
}
