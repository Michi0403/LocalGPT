using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Stores Council-host enrollment only after normal 1-Wire trust has already been established.</summary>
public sealed class OneWireCouncilHostEnrollmentService(
    IOneWireRuntimeSecurityService security,
    LocalGptCatalogService catalog,
    ILogger<OneWireCouncilHostEnrollmentService> logger) : IOneWireCouncilHostEnrollmentService
{
    private readonly SemaphoreSlim Gate = new(1, 1);
    private readonly string FilePath = Path.Combine(LocalGptApplicationDataPaths.ResolveUserPath("OneWire"), "council-hosts.json");

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
