using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Coordinates one focus-managed same-origin documentation modal per Blazor circuit.</summary>
[DocumentationUpdated("2.3.6")]
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
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationViewerService)}.{nameof(Open)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationViewerService)}.{nameof(Open)} failed.");
        throw;
    }
}

    /// <inheritdoc />
    public void Close()
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationViewerService)}.{nameof(Close)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationViewerService)}.{nameof(Close)} failed.");
        throw;
    }
}

    private string NormalizeUrl(string url)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(url);
            var normalized = url.Trim();
            if (!normalized.StartsWith('/') || normalized.StartsWith("//", StringComparison.Ordinal) || normalized.Contains('\\'))
                throw new ArgumentException("Documentation viewer URLs must be same-origin application-relative paths.", nameof(url));
            if (normalized.Any(char.IsControl))
                throw new ArgumentException("Documentation viewer URLs may not contain control characters.", nameof(url));
            return normalized;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DocumentationViewerService)}.{nameof(NormalizeUrl)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DocumentationViewerService)}.{nameof(NormalizeUrl)} failed.");
        throw;
    }
}
}
