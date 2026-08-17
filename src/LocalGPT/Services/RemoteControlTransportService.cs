using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Net;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Executes bounded HTTP/OData pulls and creates bounded webhook payloads for user-configured Remote Control connectors.</summary>
/// <param name="httpClientFactory">Factory used to create the reviewed no-auto-redirect HTTP client.</param>
/// <param name="templates">Remote Control interpolation and response-selection service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
public sealed class RemoteControlTransportService(
    IHttpClientFactory httpClientFactory,
    IRemoteControlTemplateService templates,
    ILogger<RemoteControlTransportService> logger) : IRemoteControlTransportService
{
    /// <summary>
    /// Defines the maximum redirects constant used by <see cref="RemoteControlTransportService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int MaximumRedirects = 5;

    /// <summary>
    /// Performs pull as part of the remote control transport service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<RemoteControlPayload> PullAsync(RemoteControlConnectorDefinition connector, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(connector);
            if (!connector.IsEnabled) throw new InvalidOperationException($"Remote Control connector '{connector.Key}' is disabled.");
            if (!connector.NetworkEnabled) throw new InvalidOperationException($"Remote Control connector '{connector.Key}' has outbound network access disabled.");
            if (connector.Transport == RemoteControlTransportKind.Webhook) throw new InvalidOperationException("Webhook connectors accept pushed payloads and cannot be pulled.");

            var connectorContext = new RemoteControlPayload { ConnectorKey = connector.Key, Trigger = RemoteControlTriggerKind.Manual };
            var url = await templates.ResolveAsync(connector.UrlTemplate, connectorContext, null, cancellationToken).ConfigureAwait(false);
            if (!Uri.TryCreate(url, UriKind.Absolute, out var initialUri))
                throw new InvalidDataException("The configured Remote Control URL template did not resolve to an absolute URI.");
            var allowedHosts = ParseAllowedHosts(connector.AllowedHostsJson);
            ValidateUri(connector, initialUri, allowedHosts);

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(connector.TimeoutSeconds, 1, RemoteControlLimits.MaximumTimeoutSeconds)));
            var currentUri = initialUri;
            for (var redirect = 0; redirect <= MaximumRedirects; redirect++)
            {
                using var request = await CreateRequestAsync(connector, currentUri, connectorContext, timeoutSource.Token).ConfigureAwait(false);
                using var response = await httpClientFactory.CreateClient("LocalGPTRemoteControl")
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token).ConfigureAwait(false);

                if (IsRedirect(response.StatusCode))
                {
                    if (redirect == MaximumRedirects) throw new HttpRequestException("Remote Control redirect limit was exceeded.");
                    var location = response.Headers.Location ?? throw new HttpRequestException("Remote Control redirect response did not include a Location header.");
                    currentUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
                    ValidateUri(connector, currentUri, allowedHosts);
                    continue;
                }

                var content = await ReadBoundedContentAsync(response, connector.MaxPayloadBytes, timeoutSource.Token).ConfigureAwait(false);
                var contentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty;
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Remote Control endpoint returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");
                var selectedJson = templates.ParseSelectedJson(content, contentType, connector.ResponseFormat, connector.ResponseSelector);
                logger.LogInformation("Remote Control pull completed for connector {ConnectorKey} from host {Host} with HTTP {StatusCode}; payload content was omitted.", connector.Key, currentUri.Host, (int)response.StatusCode);
                return new RemoteControlPayload
                {
                    ConnectorKey = connector.Key,
                    Trigger = RemoteControlTriggerKind.Pull,
                    ContentType = contentType,
                    RawText = content,
                    Json = selectedJson,
                    PayloadBytes = Encoding.UTF8.GetByteCount(content),
                    HttpStatusCode = (int)response.StatusCode
                };
            }
            throw new InvalidOperationException("Remote Control pull ended without a terminal HTTP response.");
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Remote Control pull was cancelled for connector {ConnectorKey}.", connector?.Key);
            else
                logger.LogError(exception, "Remote Control pull failed for connector {ConnectorKey}; URL, headers, body and payload were omitted.", connector?.Key);
            throw;
        }
    }

    /// <summary>
    /// Performs accept webhook as part of the remote control transport service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public RemoteControlPayload AcceptWebhook(RemoteControlConnectorDefinition connector, string content, string contentType)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(connector);
            if (!connector.IsEnabled) throw new InvalidOperationException($"Remote Control connector '{connector.Key}' is disabled.");
            if (connector.Transport != RemoteControlTransportKind.Webhook) throw new InvalidOperationException($"Remote Control connector '{connector.Key}' is not a webhook connector.");
            content ??= string.Empty;
            var bytes = Encoding.UTF8.GetByteCount(content);
            var maximum = Math.Clamp(connector.MaxPayloadBytes, 1, RemoteControlLimits.AbsoluteMaximumPayloadBytes);
            if (bytes > maximum) throw new InvalidDataException($"Webhook payload exceeds the configured {maximum} byte limit.");
            var selectedJson = templates.ParseSelectedJson(content, contentType ?? string.Empty, connector.ResponseFormat, connector.ResponseSelector);
            return new RemoteControlPayload
            {
                ConnectorKey = connector.Key,
                Trigger = RemoteControlTriggerKind.Webhook,
                ContentType = contentType ?? string.Empty,
                RawText = content,
                Json = selectedJson,
                PayloadBytes = bytes
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Remote Control webhook payload validation failed for connector {ConnectorKey}; token and payload were omitted.", connector?.Key);
            throw;
        }
    }

    /// <summary>
    /// Creates request as part of the remote control transport service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connector">Connector value supplied to the remote control transport operation and used when producing its result.</param>
    /// <param name="uri">Uri value supplied to the remote control transport operation and used when producing its result.</param>
    /// <param name="connectorContext">Connector context value supplied to the remote control transport operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP request message produced by the operation.</returns>
    private async Task<HttpRequestMessage> CreateRequestAsync(RemoteControlConnectorDefinition connector, Uri uri, RemoteControlPayload connectorContext, CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(ToHttpMethod(connector.Method), uri);
            var body = await templates.ResolveAsync(connector.RequestBodyTemplate, connectorContext, null, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(body) && connector.Method is not RemoteControlHttpMethod.Get)
                request.Content = new StringContent(body, Encoding.UTF8, string.IsNullOrWhiteSpace(connector.RequestContentType) ? "application/json" : connector.RequestContentType.Trim());

            if (!string.IsNullOrWhiteSpace(connector.HeadersJson))
            {
                using var document = JsonDocument.Parse(connector.HeadersJson);
                if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Remote Control headers must be a JSON object.");
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.String) throw new InvalidDataException($"Remote Control header '{property.Name}' must contain a string template.");
                    var value = await templates.ResolveAsync(property.Value.GetString() ?? string.Empty, connectorContext, null, cancellationToken).ConfigureAwait(false);
                    if (!request.Headers.TryAddWithoutValidation(property.Name, value))
                    {
                        if (request.Content is null) request.Content = new StringContent(string.Empty, Encoding.UTF8, connector.RequestContentType);
                        request.Content.Headers.TryAddWithoutValidation(property.Name, value);
                    }
                }
            }
            return request;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Creating a Remote Control HTTP request was cancelled for connector {ConnectorKey}.", connector.Key);
            else
                logger.LogError(exception, "Creating a Remote Control HTTP request failed for connector {ConnectorKey}; request content was omitted.", connector.Key);
            throw;
        }
    }

    /// <summary>
    /// Reads bounded content as part of the remote control transport service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="response">Response value supplied to the remote control transport operation and used when producing its result.</param>
    /// <param name="configuredMaximumBytes">Configured maximum bytes value supplied to the remote control transport operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> ReadBoundedContentAsync(HttpResponseMessage response, int configuredMaximumBytes, CancellationToken cancellationToken)
    {
        try
        {
            var maximum = Math.Clamp(configuredMaximumBytes, 1, RemoteControlLimits.AbsoluteMaximumPayloadBytes);
            if (response.Content.Headers.ContentLength is long contentLength && contentLength > maximum)
                throw new InvalidDataException($"Remote Control response exceeds the configured {maximum} byte limit.");
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var memory = new MemoryStream(Math.Min(maximum, 64 * 1024));
            var buffer = new byte[16 * 1024];
            var total = 0;
            while (true)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (read == 0) break;
                total += read;
                if (total > maximum) throw new InvalidDataException($"Remote Control response exceeds the configured {maximum} byte limit.");
                await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
            return Encoding.UTF8.GetString(memory.ToArray());
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Reading a Remote Control HTTP response was cancelled.");
            else
                logger.LogError(exception, "Reading a Remote Control HTTP response failed; response content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Parses allowed hosts as part of the remote control transport service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="allowedHostsJson">Allowed hosts json value supplied to the remote control transport operation and used when producing its result.</param>
    /// <returns>The i read only set string produced by the operation.</returns>
    private IReadOnlySet<string> ParseAllowedHosts(string allowedHostsJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(allowedHostsJson)) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var document = JsonDocument.Parse(allowedHostsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Allowed hosts must be a JSON array.");
            return document.RootElement.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString()?.Trim() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing Remote Control allowed-host policy failed; host values were omitted.");
            throw;
        }
    }

    /// <summary>
    /// Validates URI as part of the remote control transport service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="connector">Connector value supplied to the remote control transport operation and used when producing its result.</param>
    /// <param name="uri">Uri value supplied to the remote control transport operation and used when producing its result.</param>
    /// <param name="allowedHosts">String dependency used by the remote control transport workflow to provide the corresponding application capability.</param>
    private void ValidateUri(RemoteControlConnectorDefinition connector, Uri uri, IReadOnlySet<string> allowedHosts)
    {
        try
        {
            if (uri.Scheme != Uri.UriSchemeHttps && !(connector.AllowInsecureHttp && uri.Scheme == Uri.UriSchemeHttp))
                throw new InvalidDataException("Remote Control outbound connections require HTTPS unless this connector explicitly allows plain HTTP.");
            if (!string.IsNullOrEmpty(uri.UserInfo)) throw new InvalidDataException("Remote Control URLs may not embed credentials in the URI.");
            if (!string.IsNullOrEmpty(uri.Fragment)) throw new InvalidDataException("Remote Control URLs may not contain URI fragments.");
            if (!allowedHosts.Contains(uri.IdnHost) && !allowedHosts.Contains(uri.Host))
                throw new InvalidDataException($"Remote Control host '{uri.Host}' is not present in this connector's explicit allowed-host list.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Remote Control network-policy validation failed for connector {ConnectorKey}; full URI was omitted.", connector.Key);
            throw;
        }
    }

    /// <summary>
    /// Determines whether redirect as part of the remote control transport service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="statusCode">Status code value supplied to the remote control transport operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsRedirect(HttpStatusCode statusCode)
    {
        try
        {
            return statusCode is HttpStatusCode.MovedPermanently or HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Evaluating a Remote Control redirect status failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs to HTTP method as part of the remote control transport service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="method">Method value supplied to the remote control transport operation and used when producing its result.</param>
    /// <returns>The HTTP method produced by the operation.</returns>
    private HttpMethod ToHttpMethod(RemoteControlHttpMethod method)
    {
        try
        {
            return method switch
            {
                RemoteControlHttpMethod.Get => HttpMethod.Get,
                RemoteControlHttpMethod.Post => HttpMethod.Post,
                RemoteControlHttpMethod.Put => HttpMethod.Put,
                RemoteControlHttpMethod.Patch => HttpMethod.Patch,
                RemoteControlHttpMethod.Delete => HttpMethod.Delete,
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported Remote Control HTTP method.")
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Mapping a Remote Control HTTP method failed.");
            throw;
        }
    }
}
