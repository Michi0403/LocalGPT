using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Coordinates one focus-managed same-origin documentation modal per Blazor circuit.</summary>
[DocumentationUpdated("2.3.5")]
public sealed class DocumentationViewerService(ILogger<DocumentationViewerService> logger) : IDocumentationViewerService
{
    private long revision;

    /// <inheritdoc />
    public event Action? StateChanged;

    /// <inheritdoc />
    public LocalGptDocumentationViewerState State { get; private set; } = new();

    /// <inheritdoc />
    public void Open(LocalGptDocumentationViewerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var url = NormalizeUrl(request.Url);
        var title = string.IsNullOrWhiteSpace(request.Title) ? "LocalGPT documentation" : request.Title.Trim();
        State = new LocalGptDocumentationViewerState
        {
            IsOpen = true,
            Url = url,
            Title = title,
            Revision = Interlocked.Increment(ref revision)
        };
        logger.LogInformation("Opened the LocalGPT documentation viewer for {DocumentationUrl}.", url);
        StateChanged?.Invoke();
    }

    /// <inheritdoc />
    public void Close()
    {
        if (!State.IsOpen) return;
        State = new LocalGptDocumentationViewerState
        {
            IsOpen = false,
            Revision = Interlocked.Increment(ref revision)
        };
        logger.LogDebug("Closed the LocalGPT documentation viewer.");
        StateChanged?.Invoke();
    }

    private string NormalizeUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        var normalized = url.Trim();
        if (!normalized.StartsWith("/", StringComparison.Ordinal) || normalized.StartsWith("//", StringComparison.Ordinal) || normalized.Contains('\\'))
            throw new ArgumentException("Documentation viewer URLs must be same-origin application-relative paths.", nameof(url));
        if (normalized.Any(char.IsControl))
            throw new ArgumentException("Documentation viewer URLs may not contain control characters.", nameof(url));
        return normalized;
    }
}
