using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Coordinates one focus-managed same-origin documentation modal per Blazor circuit.</summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[DocumentationUpdated("2.3.6")]
public sealed class DocumentationViewerService(ILogger<DocumentationViewerService> logger) : IDocumentationViewerService
{
    /// <summary>
    /// Stores the internal revision state used by <see cref="DocumentationViewerService"/> while executing its surrounding workflow.
    /// </summary>
    private long revision;

    /// <summary>
    /// Occurs when state changed changes or completes in <see cref="DocumentationViewerService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    /// <inheritdoc />
    public event Action? StateChanged;

    /// <summary>
    /// Gets or sets the state value that forms part of the documentation viewer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public LocalGptDocumentationViewerState State { get; private set; } = new();

    /// <summary>
    /// Performs open as part of the documentation viewer service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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

    /// <summary>
    /// Performs close as part of the documentation viewer service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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

    /// <summary>
    /// Normalizes URL as part of the documentation viewer service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="url">Url value supplied to the documentation viewer operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
