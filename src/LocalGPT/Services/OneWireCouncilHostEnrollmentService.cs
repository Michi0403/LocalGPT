using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Stores Council-host enrollment only after normal 1-Wire trust has already been established.</summary>
/// <param name="security">One wire runtime security service dependency used by the one wire council host enrollment workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Local gpt catalog service dependency used by the one wire council host enrollment workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OneWireCouncilHostEnrollmentService(
    IOneWireRuntimeSecurityService security,
    LocalGptCatalogService catalog,
    ILogger<OneWireCouncilHostEnrollmentService> logger) : IOneWireCouncilHostEnrollmentService
{
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to gate state owned by <see cref="OneWireCouncilHostEnrollmentService"/>.
    /// </summary>
    private readonly SemaphoreSlim Gate = new(1, 1);
    /// <summary>
    /// Stores the internal file path state used by <see cref="OneWireCouncilHostEnrollmentService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string FilePath = Path.Combine(LocalGptApplicationDataPaths.ResolveUserPath("OneWire"), "council-hosts.json");

    /// <summary>
    /// Performs list as part of the one wire council host enrollment service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<OneWireCouncilHostEnrollment>> ListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try { return (await LoadUnsafeAsync(cancellationToken).ConfigureAwait(false)).Where(item => item.Enabled).OrderBy(item => item.DisplayName).ToList(); }
            finally { Gate.Release(); }
        }
        catch (Exception ex) { logger.LogError(ex, "Listing enrolled 1-Wire Council hosts failed."); throw; }
    }

    /// <summary>
    /// Performs enroll as part of the one wire council host enrollment service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The one wire council host enrollment produced by the operation.</returns>
    public async Task<OneWireCouncilHostEnrollment> EnrollAsync(string peerId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(peerId);
            if (!userConfirmed) throw new InvalidOperationException("Council-host enrollment requires explicit user confirmation.");
            var trusted = (await security.GetTrustedPeersAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => string.Equals(item.PeerId, peerId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (trusted is null) throw new InvalidOperationException("The selected LocalGPT must complete normal 1-Wire pairing/trust before Council-host enrollment.");
            if (trusted.ValidUntilUtc <= DateTimeOffset.UtcNow) throw new InvalidOperationException("The selected LocalGPT trust relationship has expired.");

            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var items = await LoadUnsafeAsync(cancellationToken).ConfigureAwait(false);
                var item = items.FirstOrDefault(candidate => string.Equals(candidate.PeerId, trusted.PeerId, StringComparison.OrdinalIgnoreCase));
                if (item is null)
                {
                    item = new OneWireCouncilHostEnrollment { PeerId = trusted.PeerId, EnrolledAtUtc = DateTimeOffset.UtcNow };
                    items.Add(item);
                }
                item.DisplayName = string.IsNullOrWhiteSpace(trusted.DisplayName) ? trusted.PeerId : trusted.DisplayName;
                item.Enabled = true;
                await SaveUnsafeAsync(items, cancellationToken).ConfigureAwait(false);
                return item;
            }
            finally { Gate.Release(); }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Enrolling trusted 1-Wire peer {PeerId} as Council host failed.", peerId);
            throw;
        }
    }

    /// <summary>
    /// Performs remove as part of the one wire council host enrollment service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public async Task<bool> RemoveAsync(string peerId, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed) throw new InvalidOperationException("Removing a Council work host requires explicit user confirmation.");
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var items = await LoadUnsafeAsync(cancellationToken).ConfigureAwait(false);
                var removed = items.RemoveAll(item => string.Equals(item.PeerId, peerId, StringComparison.OrdinalIgnoreCase)) > 0;
                if (removed) await SaveUnsafeAsync(items, cancellationToken).ConfigureAwait(false);
                return removed;
            }
            finally { Gate.Release(); }
        }
        catch (Exception ex) { logger.LogError(ex, "Removing 1-Wire Council host {PeerId} failed.", peerId); throw; }
    }

    /// <summary>
    /// Loads unsafe as part of the one wire council host enrollment service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private async Task<List<OneWireCouncilHostEnrollment>> LoadUnsafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(FilePath)) return [];
            var result = JsonSerializer.Deserialize<List<OneWireCouncilHostEnrollment>>(await File.ReadAllTextAsync(FilePath, cancellationToken).ConfigureAwait(false), catalog.JsonOptions) ?? [];
            logger.LogDebug("Loaded {HostCount} 1-Wire Council host enrollment(s).", result.Count);
            return result;
        }
        catch (Exception ex) { logger.LogError(ex, "Loading 1-Wire Council host enrollment failed."); throw; }
    }

    /// <summary>
    /// Persists unsafe as part of the one wire council host enrollment service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="items">Items value supplied to the one wire council host enrollment operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SaveUnsafeAsync(List<OneWireCouncilHostEnrollment> items, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var temp = FilePath + ".tmp";
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(items, catalog.JsonOptions), cancellationToken).ConfigureAwait(false);
            File.Move(temp, FilePath, true);
            logger.LogDebug("Saved {HostCount} 1-Wire Council host enrollment(s).", items.Count);
        }
        catch (Exception ex) { logger.LogError(ex, "Saving 1-Wire Council host enrollment failed."); throw; }
    }
}
