using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Persists user-owned Remote Control connectors and coordinates pulls, webhooks, status updates, and matching action pipelines.</summary>
/// <param name="dbContextFactory">Database context factory.</param>
/// <param name="databaseInitializer">Database initialization dependency.</param>
/// <param name="transport">Bounded network and webhook transport service.</param>
/// <param name="pipelines">Remote Control action-pipeline service.</param>
/// <param name="executionStore">Bounded execution audit store.</param>
/// <param name="regex">Shared regular-expression policy service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
public sealed class RemoteControlConnectorService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    IRemoteControlTransportService transport,
    IRemoteControlPipelineService pipelines,
    IRemoteControlExecutionStoreService executionStore,
    IRegexCompilationService regex,
    ILogger<RemoteControlConnectorService> logger) : IRemoteControlConnectorService
{
    /// <summary>
    /// Stores the internal key pattern state used by <see cref="RemoteControlConnectorService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly System.Text.RegularExpressions.Regex _keyPattern = regex.Compile("^[a-z0-9][a-z0-9._-]{0,95}$", "c", TimeSpan.FromSeconds(2), nameof(RemoteControlConnectorService));
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="RemoteControlConnectorService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true, WriteIndented = true };

    /// <summary>
    /// Performs list as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<RemoteControlConnectorDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            return await db.RemoteControlConnectorDefinitions.AsNoTracking()
                .OrderBy(item => item.DisplayName)
                .ThenBy(item => item.Key)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Listing Remote Control connectors was cancelled.");
            else logger.LogError(exception, "Listing Remote Control connectors failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs get as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<RemoteControlConnectorDefinition?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedKey = NormalizeKey(key);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            return await db.RemoteControlConnectorDefinitions.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Key == normalizedKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Loading Remote Control connector {ConnectorKey} was cancelled.", key);
            else logger.LogError(exception, "Loading Remote Control connector {ConnectorKey} failed.", key);
            throw;
        }
    }

    /// <summary>
    /// Performs save as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<RemoteControlConnectorDefinition> SaveAsync(RemoteControlConnectorDefinition definition, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(definition);
            NormalizeAndValidate(definition);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var existing = await db.RemoteControlConnectorDefinitions
                .SingleOrDefaultAsync(item => item.Key == definition.Key, cancellationToken).ConfigureAwait(false);
            var now = DateTime.UtcNow;
            if (existing is null)
            {
                definition.Id = definition.Id == Guid.Empty ? Guid.NewGuid() : definition.Id;
                definition.CreatedAtUtc = now;
                definition.UpdatedAtUtc = now;
                db.RemoteControlConnectorDefinitions.Add(definition);
                existing = definition;
            }
            else
            {
                existing.DisplayName = definition.DisplayName;
                existing.Description = definition.Description;
                existing.Transport = definition.Transport;
                existing.Method = definition.Method;
                existing.UrlTemplate = definition.UrlTemplate;
                existing.HeadersJson = definition.HeadersJson;
                existing.RequestBodyTemplate = definition.RequestBodyTemplate;
                existing.RequestContentType = definition.RequestContentType;
                existing.ResponseFormat = definition.ResponseFormat;
                existing.ResponseSelector = definition.ResponseSelector;
                existing.PollIntervalSeconds = definition.PollIntervalSeconds;
                existing.TimeoutSeconds = definition.TimeoutSeconds;
                existing.MaxPayloadBytes = definition.MaxPayloadBytes;
                existing.IsEnabled = definition.IsEnabled;
                existing.NetworkEnabled = definition.NetworkEnabled;
                existing.AllowInsecureHttp = definition.AllowInsecureHttp;
                existing.AllowedHostsJson = definition.AllowedHostsJson;
                if (definition.Transport == RemoteControlTransportKind.Webhook && !string.IsNullOrWhiteSpace(definition.WebhookToken))
                    existing.WebhookToken = definition.WebhookToken;
                else if (definition.Transport != RemoteControlTransportKind.Webhook)
                    existing.WebhookToken = string.Empty;
                existing.UpdatedAtUtc = now;
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved Remote Control connector {ConnectorKey}; transport {Transport}, enabled {Enabled}, network enabled {NetworkEnabled}.", existing.Key, existing.Transport, existing.IsEnabled, existing.NetworkEnabled);
            return Clone(existing);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Saving a Remote Control connector was cancelled.");
            else logger.LogError(exception, "Saving a Remote Control connector failed; URL, headers, request body, allowlist, and webhook token were omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs delete as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedKey = NormalizeKey(key);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var existing = await db.RemoteControlConnectorDefinitions.SingleOrDefaultAsync(item => item.Key == normalizedKey, cancellationToken).ConfigureAwait(false);
            if (existing is null) return false;
            var dependentPipelines = await db.RemoteControlPipelineDefinitions.Where(item => item.ConnectorKey == normalizedKey).ToListAsync(cancellationToken).ConfigureAwait(false);
            if (dependentPipelines.Count > 0)
            {
                var dependentKeys = dependentPipelines.Select(item => item.Key).ToList();
                var wrapperReference = await db.UserDxAiFunctionDefinitions.AsNoTracking()
                    .AnyAsync(item => dependentKeys.Contains(item.PipelineKey), cancellationToken).ConfigureAwait(false);
                if (wrapperReference)
                    throw new InvalidOperationException($"Remote Control connector '{normalizedKey}' owns a pipeline referenced by a user-owned DXFunction. Delete or retarget that user function first.");
                db.RemoteControlPipelineDefinitions.RemoveRange(dependentPipelines);
            }
            db.RemoteControlConnectorDefinitions.Remove(existing);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Deleted Remote Control connector {ConnectorKey} and {PipelineCount} dependent pipeline(s).", normalizedKey, dependentPipelines.Count);
            return true;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Deleting Remote Control connector {ConnectorKey} was cancelled.", key);
            else logger.LogError(exception, "Deleting Remote Control connector {ConnectorKey} failed.", key);
            throw;
        }
    }

    /// <summary>
    /// Performs rotate webhook token as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<string> RotateWebhookTokenAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedKey = NormalizeKey(key);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var connector = await db.RemoteControlConnectorDefinitions.SingleOrDefaultAsync(item => item.Key == normalizedKey, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Remote Control connector '{normalizedKey}' was not found.");
            if (connector.Transport != RemoteControlTransportKind.Webhook)
                throw new InvalidOperationException("Webhook tokens can only be rotated for webhook connectors.");
            connector.WebhookToken = CreateWebhookToken();
            connector.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Rotated webhook token for Remote Control connector {ConnectorKey}; token value was omitted from logs.", normalizedKey);
            return connector.WebhookToken;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Rotating Remote Control webhook token {ConnectorKey} was cancelled.", key);
            else logger.LogError(exception, "Rotating Remote Control webhook token {ConnectorKey} failed; token value was omitted.", key);
            throw;
        }
    }

    /// <summary>
    /// Performs pull as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<RemoteControlPayload> PullAsync(string key, bool runPipelines, bool automaticInvocation, CancellationToken cancellationToken = default)
    {
        try
        {
            var connector = await GetRequiredAsync(key, cancellationToken).ConfigureAwait(false);
            var audit = await executionStore.StartAsync(connector.Key, string.Empty, RemoteControlTriggerKind.Pull, 0, null, cancellationToken).ConfigureAwait(false);
            try
            {
                var payload = await transport.PullAsync(connector, cancellationToken).ConfigureAwait(false);
                await UpdateStatusAsync(connector.Key, true, "PullCompleted", payload.ContentType, payload.RawText, string.Empty, cancellationToken).ConfigureAwait(false);
                if (runPipelines)
                    await pipelines.ExecuteMatchingAsync(payload, automaticInvocation, cancellationToken).ConfigureAwait(false);
                await executionStore.CompleteAsync(audit.Id, true, 0, "PullCompleted", string.Empty, cancellationToken).ConfigureAwait(false);
                return payload;
            }
            catch (Exception exception)
            {
                await UpdateStatusAsync(connector.Key, false, "PullFailed", string.Empty, string.Empty, exception.Message, cancellationToken).ConfigureAwait(false);
                await executionStore.CompleteAsync(audit.Id, false, 0, "PullFailed", exception.Message, cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Pulling Remote Control connector {ConnectorKey} was cancelled.", key);
            else logger.LogError(exception, "Pulling Remote Control connector {ConnectorKey} failed; remote payload and credentials were omitted.", key);
            throw;
        }
    }

    /// <summary>
    /// Performs receive webhook as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<RemoteControlPayload> ReceiveWebhookAsync(string key, string token, string content, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            var connector = await GetRequiredAsync(key, cancellationToken).ConfigureAwait(false);
            if (!connector.IsEnabled || connector.Transport != RemoteControlTransportKind.Webhook)
                throw new InvalidOperationException("The requested webhook connector is not enabled.");
            if (!TokenMatches(connector.WebhookToken, token))
                throw new UnauthorizedAccessException("The Remote Control webhook token is invalid.");
            var audit = await executionStore.StartAsync(connector.Key, string.Empty, RemoteControlTriggerKind.Webhook, Encoding.UTF8.GetByteCount(content ?? string.Empty), null, cancellationToken).ConfigureAwait(false);
            try
            {
                var payload = transport.AcceptWebhook(connector, content ?? string.Empty, contentType ?? string.Empty);
                await UpdateStatusAsync(connector.Key, true, "WebhookAccepted", payload.ContentType, payload.RawText, string.Empty, cancellationToken).ConfigureAwait(false);
                await pipelines.ExecuteMatchingAsync(payload, automaticInvocation: true, cancellationToken).ConfigureAwait(false);
                await executionStore.CompleteAsync(audit.Id, true, 0, "WebhookAccepted", string.Empty, cancellationToken).ConfigureAwait(false);
                return payload;
            }
            catch (Exception exception)
            {
                await UpdateStatusAsync(connector.Key, false, "WebhookFailed", contentType, string.Empty, exception.Message, cancellationToken).ConfigureAwait(false);
                await executionStore.CompleteAsync(audit.Id, false, 0, "WebhookFailed", exception.Message, cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Receiving Remote Control webhook {ConnectorKey} was cancelled.", key);
            else if (exception is UnauthorizedAccessException) logger.LogWarning("Rejected Remote Control webhook for connector {ConnectorKey} because token authentication failed.", key);
            else logger.LogError(exception, "Receiving Remote Control webhook {ConnectorKey} failed; payload and token were omitted.", key);
            throw;
        }
    }

    /// <summary>
    /// Lists due for polling as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<RemoteControlConnectorDefinition>> ListDueForPollingAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var candidates = await db.RemoteControlConnectorDefinitions.AsNoTracking()
                .Where(item => item.IsEnabled && item.NetworkEnabled && item.Transport != RemoteControlTransportKind.Webhook && item.PollIntervalSeconds >= RemoteControlLimits.MinimumPollIntervalSeconds)
                .OrderBy(item => item.Key)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            return candidates.Where(item => !item.LastAttemptUtc.HasValue || item.LastAttemptUtc.Value.AddSeconds(item.PollIntervalSeconds) <= utcNow).ToList();
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Listing due Remote Control polling connectors was cancelled.");
            else logger.LogError(exception, "Listing due Remote Control polling connectors failed.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves history as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<RemoteControlExecutionRecord>> GetHistoryAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            return await executionStore.ListAsync(take, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Loading Remote Control history was cancelled.");
            else logger.LogError(exception, "Loading Remote Control history failed.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves required as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The remote control connector definition produced by the operation.</returns>
    private async Task<RemoteControlConnectorDefinition> GetRequiredAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            return await GetAsync(key, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Remote Control connector '{key}' was not found.");
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Loading required Remote Control connector was cancelled.");
            else logger.LogError(exception, "Loading required Remote Control connector failed.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes and validate as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="definition">Definition value supplied to the remote control connector operation and used when producing its result.</param>
    private void NormalizeAndValidate(RemoteControlConnectorDefinition definition)
    {
        try
        {
            definition.Key = NormalizeKey(definition.Key);
            definition.DisplayName = Bound(definition.DisplayName, 160, definition.Key);
            definition.Description = Bound(definition.Description, 2_000);
            definition.UrlTemplate = Bound(definition.UrlTemplate, 4_096);
            definition.RequestBodyTemplate = Bound(definition.RequestBodyTemplate, 65_536);
            definition.RequestContentType = Bound(definition.RequestContentType, 160, "application/json");
            definition.ResponseSelector = Bound(definition.ResponseSelector, 1_024);
            definition.TimeoutSeconds = Math.Clamp(definition.TimeoutSeconds <= 0 ? 30 : definition.TimeoutSeconds, 1, RemoteControlLimits.MaximumTimeoutSeconds);
            definition.MaxPayloadBytes = Math.Clamp(definition.MaxPayloadBytes <= 0 ? RemoteControlLimits.DefaultMaximumPayloadBytes : definition.MaxPayloadBytes, 1_024, RemoteControlLimits.AbsoluteMaximumPayloadBytes);
            definition.PollIntervalSeconds = definition.PollIntervalSeconds <= 0 ? 0 : Math.Max(RemoteControlLimits.MinimumPollIntervalSeconds, definition.PollIntervalSeconds);
            definition.HeadersJson = NormalizeHeaders(definition.HeadersJson);
            definition.AllowedHostsJson = NormalizeAllowedHosts(definition.AllowedHostsJson);

            if (definition.Transport == RemoteControlTransportKind.Webhook)
            {
                definition.NetworkEnabled = false;
                definition.PollIntervalSeconds = 0;
                definition.UrlTemplate = string.Empty;
                definition.AllowedHostsJson = "[]";
                if (string.IsNullOrWhiteSpace(definition.WebhookToken)) definition.WebhookToken = CreateWebhookToken();
                else definition.WebhookToken = Bound(definition.WebhookToken, 256);
            }
            else
            {
                definition.WebhookToken = string.Empty;
                if (definition.NetworkEnabled && string.IsNullOrWhiteSpace(definition.UrlTemplate))
                    throw new InvalidDataException("An outbound Remote Control connector cannot enable network access without a URL template.");
                if (definition.NetworkEnabled && ParseAllowedHosts(definition.AllowedHostsJson).Count == 0)
                    throw new InvalidDataException("An outbound Remote Control connector cannot enable network access without at least one explicitly allowed host.");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing Remote Control connector policy failed; endpoint, credentials, and tokens were omitted.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes headers as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the remote control connector operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeHeaders(string value)
    {
        try
        {
            var input = string.IsNullOrWhiteSpace(value) ? "{}" : value;
            if (input.Length > 32_768) throw new InvalidDataException("Remote Control connector headers JSON is too large.");
            using var document = JsonDocument.Parse(input);
            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Remote Control connector headers must be a JSON object.");
            var headers = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                var name = Bound(property.Name, 256);
                if (string.IsNullOrWhiteSpace(name) || name.Contains('\r') || name.Contains('\n')) throw new InvalidDataException("Remote Control header names cannot be empty or contain line breaks.");
                if (name.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Connection", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Remote Control header '{name}' is transport-owned and cannot be overridden by a connector.");
                if (property.Value.ValueKind != JsonValueKind.String) throw new InvalidDataException($"Remote Control header '{name}' must have a string template value.");
                var headerValue = Bound(property.Value.GetString(), 8_192);
                if (headerValue.Contains('\r') || headerValue.Contains('\n')) throw new InvalidDataException($"Remote Control header '{name}' cannot contain line breaks.");
                headers[name] = headerValue;
            }
            return JsonSerializer.Serialize(headers, JsonOptions);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing Remote Control connector headers failed; header content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes allowed hosts as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the remote control connector operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeAllowedHosts(string value)
    {
        try
        {
            var hosts = ParseAllowedHosts(value)
                .Select(item => item.Trim().TrimEnd('.').ToLowerInvariant())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var host in hosts)
            {
                if (host.Length > 253 || host.Contains('/') || host.Contains(':') || host.Contains('@') || host.Contains(' '))
                    throw new InvalidDataException("Remote Control allowed hosts must be DNS host names without scheme, port, path, or credentials.");
            }
            return JsonSerializer.Serialize(hosts, JsonOptions);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing Remote Control connector host allowlist failed; host content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Parses allowed hosts as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the remote control connector operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<string> ParseAllowedHosts(string value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value)) return [];
            if (value.Length > 16_384) throw new InvalidDataException("Remote Control allowed-host JSON is too large.");
            return JsonSerializer.Deserialize<List<string>>(value, JsonOptions) ?? [];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing Remote Control allowed-host JSON failed; host content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Updates status as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="succeeded">Value indicating whether succeeded should apply to this operation.</param>
    /// <param name="status">Status value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="contentType">Content type value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="payload">Payload value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="error">Error value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task UpdateStatusAsync(string key, bool succeeded, string status, string contentType, string payload, string error, CancellationToken cancellationToken)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var connector = await db.RemoteControlConnectorDefinitions.SingleOrDefaultAsync(item => item.Key == key, cancellationToken).ConfigureAwait(false);
            if (connector is null) return;
            connector.LastAttemptUtc = DateTime.UtcNow;
            if (succeeded) connector.LastSuccessUtc = connector.LastAttemptUtc;
            connector.LastStatus = Bound(status, 256);
            connector.LastContentType = Bound(contentType, 256);
            connector.LastPayloadPreview = succeeded ? Bound(payload, 2_000) : string.Empty;
            connector.LastError = Bound(error, 1_024);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Updating Remote Control connector status {ConnectorKey} was cancelled.", key);
            else logger.LogError(exception, "Updating Remote Control connector status {ConnectorKey} failed; payload was omitted.", key);
            throw;
        }
    }

    /// <summary>
    /// Normalizes key as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control connector operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeKey(string key)
    {
        try
        {
            var normalized = key?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!_keyPattern.IsMatch(normalized)) throw new InvalidDataException("Remote Control keys must start with a lowercase letter or digit and contain only lowercase letters, digits, '.', '_' or '-'.");
            return normalized;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing a Remote Control connector key failed; key content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs bound as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="maximumLength">Maximum length value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the remote control connector operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Bound(string? value, int maximumLength, string fallback = "")
    {
        try
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bounding Remote Control connector text failed; content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Creates webhook token as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The string produced by the operation.</returns>
    private string CreateWebhookToken()
    {
        try
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Generating a Remote Control webhook token failed; no token value was logged.");
            throw;
        }
    }

    /// <summary>
    /// Performs token matches as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="expected">Expected value supplied to the remote control connector operation and used when producing its result.</param>
    /// <param name="supplied">Supplied value supplied to the remote control connector operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TokenMatches(string expected, string supplied)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(supplied)) return false;
            var left = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
            var right = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
            return CryptographicOperations.FixedTimeEquals(left, right);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Comparing a Remote Control webhook token failed; token values were omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs clone as part of the remote control connector service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the remote control connector operation and used when producing its result.</param>
    /// <returns>The remote control connector definition produced by the operation.</returns>
    private RemoteControlConnectorDefinition Clone(RemoteControlConnectorDefinition source)
    {
        try
        {
            return new RemoteControlConnectorDefinition
            {
                Id = source.Id,
                Key = source.Key,
                DisplayName = source.DisplayName,
                Description = source.Description,
                Transport = source.Transport,
                Method = source.Method,
                UrlTemplate = source.UrlTemplate,
                HeadersJson = source.HeadersJson,
                RequestBodyTemplate = source.RequestBodyTemplate,
                RequestContentType = source.RequestContentType,
                ResponseFormat = source.ResponseFormat,
                ResponseSelector = source.ResponseSelector,
                PollIntervalSeconds = source.PollIntervalSeconds,
                TimeoutSeconds = source.TimeoutSeconds,
                MaxPayloadBytes = source.MaxPayloadBytes,
                IsEnabled = source.IsEnabled,
                NetworkEnabled = source.NetworkEnabled,
                AllowInsecureHttp = source.AllowInsecureHttp,
                AllowedHostsJson = source.AllowedHostsJson,
                WebhookToken = source.WebhookToken,
                CreatedAtUtc = source.CreatedAtUtc,
                UpdatedAtUtc = source.UpdatedAtUtc,
                LastAttemptUtc = source.LastAttemptUtc,
                LastSuccessUtc = source.LastSuccessUtc,
                LastStatus = source.LastStatus,
                LastContentType = source.LastContentType,
                LastPayloadPreview = source.LastPayloadPreview,
                LastError = source.LastError
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cloning a Remote Control connector failed.");
            throw;
        }
    }
}
