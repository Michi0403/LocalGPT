using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates remote knowledge import behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class RemoteKnowledgeImportService
    {
    /// <summary>
    /// Performs send public as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="initialUri">Initial uri value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP response message produced by the operation.</returns>
    private async Task<HttpResponseMessage> SendPublicAsync(Uri initialUri, CancellationToken cancellationToken)
    {
    try
    {
            var current = initialUri;
            for (var redirect = 0; redirect <= 8; redirect++)
            {
                await EnsurePublicHostAsync(current, cancellationToken).ConfigureAwait(false);
                var response = await http.GetAsync(current, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if ((int)response.StatusCode is < 300 or >= 400)
                    return response;

                var location = response.Headers.Location;
                response.Dispose();
                if (location is null)
                    throw new HttpRequestException("Remote source returned a redirect without a Location header.");
                current = location.IsAbsoluteUri ? location : new Uri(current, location);
                if (current.Scheme is not ("http" or "https"))
                    throw new InvalidOperationException("Remote import redirects may only use http or https.");
            }

            throw new HttpRequestException("Remote source exceeded the maximum redirect count.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(SendPublicAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(SendPublicAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Ensures public host as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="uri">Uri value supplied to the remote knowledge import operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task EnsurePublicHostAsync(Uri uri, CancellationToken cancellationToken)
    {
    try
    {
            if (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Remote import does not access loopback/private hosts. Use the local learn-base path importer for local content.");
            var addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken).ConfigureAwait(false);
            if (addresses.Length == 0 || addresses.Any(IsPrivateAddress))
                throw new InvalidOperationException("Remote import resolved to a private, local or link-local network address.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(EnsurePublicHostAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(EnsurePublicHostAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether private address as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="address">P address dependency used by the remote knowledge import workflow to provide the corresponding application capability.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsPrivateAddress(IPAddress address)
    {
    try
    {
            if (IPAddress.IsLoopback(address)) return true;
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes = address.GetAddressBytes();
                return bytes[0] == 10 || bytes[0] == 127 ||
                       bytes[0] == 169 && bytes[1] == 254 ||
                       bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
                       bytes[0] == 192 && bytes[1] == 168;
            }
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.Equals(IPAddress.IPv6Loopback);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(IsPrivateAddress)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(IsPrivateAddress)} failed.");
        throw;
    }
}


    /// <summary>
    /// Performs throw if disposed as part of the remote knowledge import service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void ThrowIfDisposed()
    {
    try
    {
            if (Volatile.Read(ref disposeState) != 0)
                throw new ObjectDisposedException(nameof(RemoteKnowledgeImportService));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ThrowIfDisposed)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RemoteKnowledgeImportService)}.{nameof(ThrowIfDisposed)} failed.");
        throw;
    }
}

    /// <summary>
    /// Releases resources owned by <see cref="RemoteKnowledgeImportService"/> and leaves the remote knowledge import workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Interlocked.Exchange(ref disposeState, 1) != 0)
                return;
            http.Dispose();
            logger.LogDebug("Disposed the remote-knowledge HTTP client.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Disposing the remote-knowledge import service failed.");
            throw;
        }
    }


    }
}
