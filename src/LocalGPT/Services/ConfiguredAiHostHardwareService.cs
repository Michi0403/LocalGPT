using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using LocalGPT.WireProtocol;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>
/// Owns durable hardware definitions for configured physical AI hosts. Automatic probes are evidence only;
/// user-confirmed/imported facts are never silently replaced by weaker legacy discovery.
/// </summary>
/// <param name="dbContextFactory">Creates LocalGPT database contexts.</param>
/// <param name="databaseInitializer">Ensures schema migrations are applied.</param>
/// <param name="hardwareInventory">Provides best-effort local read-only discovery.</param>
/// <param name="logger">Writes bounded host-hardware diagnostics.</param>
public sealed class ConfiguredAiHostHardwareService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    IHardwareInventoryService hardwareInventory,
    ILogger<ConfiguredAiHostHardwareService> logger) : IConfiguredAiHostHardwareService
{
    /// <summary>Stores the JSON options used for portable GPU and endpoint arrays persisted with configured-host profiles.</summary>
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Loads the durable hardware profile for the physical host that owns a configured provider endpoint.</summary>
    /// <inheritdoc />
    public async Task<ConfiguredAiHostHardwareProfile?> GetForEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var hostKey = GetHostKey(endpoint);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var profile = await db.ConfiguredAiHostHardwareProfiles.AsNoTracking()
                .SingleOrDefaultAsync(item => item.HostKey == hostKey, cancellationToken)
                .ConfigureAwait(false);
            Hydrate(profile);
            return profile;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Reading configured AI host hardware was cancelled.");
            else
                logger.LogError(exception, "Reading configured AI host hardware failed; endpoint details were omitted.");
            throw;
        }
    }

    /// <summary>Loads all durable configured-host hardware profiles in stable display order.</summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfiguredAiHostHardwareProfile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var profiles = await db.ConfiguredAiHostHardwareProfiles.AsNoTracking()
                .OrderBy(item => item.HostName).ThenBy(item => item.HostKey)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var profile in profiles)
                Hydrate(profile);
            return profiles;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Loading configured AI host hardware profiles was cancelled.");
            else
                logger.LogError(exception, "Loading configured AI host hardware profiles failed.");
            throw;
        }
    }

    /// <summary>Persists explicit user-confirmed hardware facts for one configured physical AI host.</summary>
    /// <inheritdoc />
    public async Task<ConfiguredAiHostHardwareProfile> SaveAsync(ConfiguredAiHostHardwareDraft draft, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(draft);
            ArgumentException.ThrowIfNullOrWhiteSpace(draft.Endpoint);
            var hostKey = GetHostKey(draft.Endpoint);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var entity = await db.ConfiguredAiHostHardwareProfiles
                .SingleOrDefaultAsync(item => item.HostKey == hostKey, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                entity = new ConfiguredAiHostHardwareProfile { Id = Guid.NewGuid(), HostKey = hostKey };
                db.ConfiguredAiHostHardwareProfiles.Add(entity);
            }
            ApplyDraft(entity, draft, userConfirmed: true);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            Hydrate(entity);
            logger.LogInformation("Saved user-confirmed hardware for configured AI host {HostKey}.", hostKey);
            return entity;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Saving configured AI host hardware was cancelled.");
            else
                logger.LogError(exception, "Saving configured AI host hardware failed; user-entered values were omitted.");
            throw;
        }
    }

    /// <summary>Parses a local HWiNFO text export deterministically and saves the extracted host hardware as confirmed evidence.</summary>
    /// <inheritdoc />
    public async Task<ConfiguredAiHostHardwareProfile> ImportHwInfoAsync(string endpoint, string reportText, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
            ArgumentException.ThrowIfNullOrWhiteSpace(reportText);
            var draft = CreateDraft(endpoint, await GetForEndpointAsync(endpoint, cancellationToken).ConfigureAwait(false));
            var gpuMatch = Regex.Match(reportText, @"(?im)^\s*(?:Grafikspeicher|Video\s+Memory)\s*:\s*([0-9]+(?:[\.,][0-9]+)?)\s*(MByte|MB|GByte|GB|GiB)\b.*$");
            if (!gpuMatch.Success)
                throw new InvalidDataException("The HWiNFO report did not contain a supported GPU-memory line (Grafikspeicher / Video Memory).");

            var gpuHeading = Regex.Matches(reportText, @"(?im)^\s*(.+?(?:Radeon|GeForce|Arc).+?)\s*-{3,}\s*$")
                .Cast<Match>().LastOrDefault(match => match.Index < gpuMatch.Index);
            if (gpuHeading is not null)
                draft.GpuName = Regex.Replace(gpuHeading.Groups[1].Value.Trim(), @"^(?:ATI/AMD\s+)", string.Empty, RegexOptions.IgnoreCase);
            if (draft.GpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase)) draft.GpuVendor = "AMD";
            else if (draft.GpuName.Contains("GeForce", StringComparison.OrdinalIgnoreCase)) draft.GpuVendor = "NVIDIA";
            else if (draft.GpuName.Contains("Arc", StringComparison.OrdinalIgnoreCase)) draft.GpuVendor = "Intel";

            var amount = double.Parse(gpuMatch.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
            draft.DedicatedVramGiB = gpuMatch.Groups[2].Value.StartsWith("M", StringComparison.OrdinalIgnoreCase)
                ? amount / 1024d
                : amount;

            var memoryMatch = Regex.Match(reportText, @"(?im)^\s*(?:Gesamtspeichergröße|Total\s+Memory\s+Size)\s*:\s*([0-9]+(?:[\.,][0-9]+)?)\s*(GByte|GB|GiB|MByte|MB)\b");
            if (memoryMatch.Success)
            {
                var systemAmount = double.Parse(memoryMatch.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                draft.SystemMemoryGiB = memoryMatch.Groups[2].Value.StartsWith("M", StringComparison.OrdinalIgnoreCase)
                    ? systemAmount / 1024d
                    : systemAmount;
            }
            else
            {
                // Some HWiNFO text exports encode the unit in the label, e.g. "Total Memory Size [MB]: 65536".
                var labeledMemoryMatch = Regex.Match(reportText, @"(?im)^\s*Total\s+Memory\s+Size\s*\[(MB|GB|GiB)\]\s*:\s*([0-9]+(?:[\.,][0-9]+)?)\s*$");
                if (labeledMemoryMatch.Success)
                {
                    var systemAmount = double.Parse(labeledMemoryMatch.Groups[2].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
                    draft.SystemMemoryGiB = labeledMemoryMatch.Groups[1].Value.StartsWith("M", StringComparison.OrdinalIgnoreCase)
                        ? systemAmount / 1024d
                        : systemAmount;
                }
            }
            var cpuMatch = Regex.Match(reportText, @"(?im)^\s*(?:Prozessorname|Processor\s+Name)\s*:\s*(.+?)\s*$");
            if (cpuMatch.Success) draft.CpuName = cpuMatch.Groups[1].Value.Trim();
            var hostMatch = Regex.Match(reportText, @"(?im)^\s*(?:Computername|Computer\s+Name)\s*:\s*(.+?)\s*$");
            if (hostMatch.Success) draft.HostName = hostMatch.Groups[1].Value.Trim();
            var osMatch = Regex.Match(reportText, @"(?im)^\s*(?:Betriebssystem|Operating\s+System)\s*:\s*(.+?)\s*$");
            if (osMatch.Success) draft.OperatingSystem = osMatch.Groups[1].Value.Trim();
            draft.SourceKind = "HWiNFO";
            draft.Confidence = "ImportedReport";
            var saved = await SaveAsync(draft, cancellationToken).ConfigureAwait(false);
            saved.SourceKind = "HWiNFO";
            saved.Confidence = "ImportedReport";
            await UpdateProvenanceAsync(saved, cancellationToken).ConfigureAwait(false);
            return saved;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Importing HWiNFO host hardware was cancelled.");
            else if (exception is InvalidDataException)
                logger.LogWarning(exception, "HWiNFO host-hardware import did not contain the required deterministic fields.");
            else
                logger.LogError(exception, "Importing HWiNFO host hardware failed; report content was omitted.");
            throw;
        }
    }

    /// <summary>Runs best-effort read-only local hardware probes for a loopback configured host without overwriting confirmed values.</summary>
    /// <inheritdoc />
    public async Task<ConfiguredAiHostHardwareProfile> DetectLocalAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || !uri.IsLoopback)
                throw new InvalidOperationException("Automatic hardware probing is local-only. Configure or import hardware for remote hosts on /install.");
            var existing = await GetForEndpointAsync(endpoint, cancellationToken).ConfigureAwait(false);
            if (existing?.IsUserConfirmed == true)
                return existing;
            var hardware = await hardwareInventory.GetHardwareAsync(cancellationToken).ConfigureAwait(false);
            var draft = CreateDraft(endpoint, existing);
            draft.HostName = Environment.MachineName;
            draft.OperatingSystem = RuntimeInformation.OSDescription;
            draft.Architecture = RuntimeInformation.OSArchitecture.ToString();
            var cpu = hardware.FirstOrDefault(item => item.Kind == OneWireHardwareKind.Cpu);
            if (cpu is not null) draft.CpuName = cpu.Name;
            var gpu = hardware.FirstOrDefault(item => item.Kind == OneWireHardwareKind.Gpu);
            if (gpu is not null)
            {
                draft.GpuName = gpu.Name;
                draft.GpuVendor = gpu.Vendor;
                draft.DedicatedVramGiB = gpu.DedicatedMemoryBytes is > 0 ? gpu.DedicatedMemoryBytes.Value / 1024d / 1024d / 1024d : null;
            }
            draft.SourceKind = "LocalProbe";
            draft.Confidence = gpu?.DedicatedMemoryBytes is > 0 ? "VendorReported" : "DetectedIdentityOnly";
            var saved = await SaveDetectedAsync(draft, cancellationToken).ConfigureAwait(false);
            return saved;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Detecting local configured-host hardware was cancelled.");
            else
                logger.LogError(exception, "Detecting local configured-host hardware failed; endpoint details were omitted.");
            throw;
        }
    }

    /// <summary>Normalizes one configured endpoint into the physical-host key used by durable hardware ownership.</summary>
    /// <inheritdoc />
    public string GetHostKey(string endpoint)
    {
        try
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
                return endpoint.Trim().ToLowerInvariant();
            return uri.IsLoopback ? "local-machine" : uri.Host.Trim().ToLowerInvariant();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing configured AI host key failed; endpoint details were omitted.");
            throw;
        }
    }

    /// <summary>Creates the editable Install-page representation of one configured host hardware profile.</summary>
    /// <inheritdoc />
    public ConfiguredAiHostHardwareDraft CreateDraft(string endpoint, ConfiguredAiHostHardwareProfile? profile = null)
    {
        try
        {
            var gpu = profile?.Gpus.FirstOrDefault();
            return new ConfiguredAiHostHardwareDraft
            {
                Endpoint = endpoint,
                HostKey = GetHostKey(endpoint),
                HostName = profile?.HostName ?? (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? (uri.IsLoopback ? Environment.MachineName : uri.Host) : endpoint),
                OperatingSystem = profile?.OperatingSystem ?? string.Empty,
                Architecture = profile?.Architecture ?? string.Empty,
                CpuName = profile?.CpuName ?? string.Empty,
                SystemMemoryGiB = profile?.SystemMemoryBytes is > 0 ? profile.SystemMemoryBytes.Value / 1024d / 1024d / 1024d : null,
                GpuName = gpu?.Name ?? string.Empty,
                GpuVendor = gpu?.Vendor ?? string.Empty,
                DedicatedVramGiB = gpu?.DedicatedMemoryBytes is > 0 ? gpu.DedicatedMemoryBytes.Value / 1024d / 1024d / 1024d : null,
                SourceKind = profile?.SourceKind ?? "Manual",
                Confidence = profile?.Confidence ?? "UserConfirmed"
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating configured-host hardware draft failed; endpoint details were omitted.");
            throw;
        }
    }

    /// <summary>Persists automatically detected hardware while preserving the distinction from user-confirmed values.</summary>
    /// <param name="draft">Detected host-hardware values.</param>
    /// <param name="cancellationToken">Cancellation token for persistence.</param>
    /// <returns>The persisted detected profile.</returns>
    private async Task<ConfiguredAiHostHardwareProfile> SaveDetectedAsync(ConfiguredAiHostHardwareDraft draft, CancellationToken cancellationToken)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var entity = await db.ConfiguredAiHostHardwareProfiles.SingleOrDefaultAsync(item => item.HostKey == draft.HostKey, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                entity = new ConfiguredAiHostHardwareProfile { Id = Guid.NewGuid(), HostKey = draft.HostKey };
                db.ConfiguredAiHostHardwareProfiles.Add(entity);
            }
            ApplyDraft(entity, draft, userConfirmed: false);
            entity.LastDetectedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            Hydrate(entity);
            return entity;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Saving detected configured-host hardware was cancelled.");
            else
                logger.LogError(exception, "Saving detected configured-host hardware failed; values were omitted.");
            throw;
        }
    }

    /// <summary>Persists the HWiNFO/source confidence labels after the imported facts have been saved as user-confirmed host hardware.</summary>
    /// <param name="profile">Profile whose source/confidence labels should be persisted.</param>
    /// <param name="cancellationToken">Cancellation token for persistence.</param>
    /// <returns>A task that completes when provenance has been stored.</returns>
    private async Task UpdateProvenanceAsync(ConfiguredAiHostHardwareProfile profile, CancellationToken cancellationToken)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var entity = await db.ConfiguredAiHostHardwareProfiles.SingleAsync(item => item.Id == profile.Id, cancellationToken).ConfigureAwait(false);
            entity.SourceKind = profile.SourceKind;
            entity.Confidence = profile.Confidence;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Updating configured-host hardware provenance was cancelled.");
            else
                logger.LogError(exception, "Updating configured-host hardware provenance failed.");
            throw;
        }
    }

    /// <summary>Copies normalized editable hardware values into the durable configured-host entity.</summary>
    /// <param name="entity">Durable host-hardware entity being updated.</param>
    /// <param name="draft">Validated editable values.</param>
    /// <param name="userConfirmed">Whether the source is explicit user-confirmed evidence.</param>
    private void ApplyDraft(ConfiguredAiHostHardwareProfile entity, ConfiguredAiHostHardwareDraft draft, bool userConfirmed)
    {
        try
        {
            entity.HostKey = GetHostKey(draft.Endpoint);
            entity.HostName = string.IsNullOrWhiteSpace(draft.HostName) ? entity.HostKey : draft.HostName.Trim();
            entity.OperatingSystem = (draft.OperatingSystem ?? string.Empty).Trim();
            entity.Architecture = (draft.Architecture ?? string.Empty).Trim();
            entity.CpuName = (draft.CpuName ?? string.Empty).Trim();
            entity.SystemMemoryBytes = ToBytes(draft.SystemMemoryGiB);
            var gpus = string.IsNullOrWhiteSpace(draft.GpuName) && draft.DedicatedVramGiB is null
                ? new List<ConfiguredAiHostGpu>()
                : [new ConfiguredAiHostGpu { Index = 0, Name = (draft.GpuName ?? string.Empty).Trim(), Vendor = (draft.GpuVendor ?? string.Empty).Trim(), DedicatedMemoryBytes = ToBytes(draft.DedicatedVramGiB) }];
            entity.GpusJson = JsonSerializer.Serialize(gpus, jsonOptions);
            var endpoints = DeserializeEndpoints(entity.ProviderEndpointsJson);
            if (!endpoints.Contains(draft.Endpoint, StringComparer.OrdinalIgnoreCase)) endpoints.Add(draft.Endpoint.Trim());
            entity.ProviderEndpointsJson = JsonSerializer.Serialize(endpoints.OrderBy(item => item, StringComparer.OrdinalIgnoreCase), jsonOptions);
            entity.SourceKind = string.IsNullOrWhiteSpace(draft.SourceKind) ? (userConfirmed ? "Manual" : "LocalProbe") : draft.SourceKind.Trim();
            entity.Confidence = string.IsNullOrWhiteSpace(draft.Confidence) ? (userConfirmed ? "UserConfirmed" : "Detected") : draft.Confidence.Trim();
            entity.IsUserConfirmed = userConfirmed || entity.IsUserConfirmed;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying configured-host hardware draft failed; draft values were omitted.");
            throw;
        }
    }

    /// <summary>Hydrates the non-mapped GPU collection from its portable persisted JSON representation.</summary>
    /// <param name="profile">Profile to hydrate when non-null.</param>
    private void Hydrate(ConfiguredAiHostHardwareProfile? profile)
    {
        try
        {
            if (profile is null) return;
            try
            {
                profile.Gpus = JsonSerializer.Deserialize<List<ConfiguredAiHostGpu>>(profile.GpusJson, jsonOptions) ?? [];
            }
            catch (JsonException exception)
            {
                logger.LogWarning(exception, "Stored configured-host GPU JSON was invalid; the host profile remains available without GPU details.");
                profile.Gpus = [];
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Hydrating configured-host hardware failed.");
            throw;
        }
    }

    /// <summary>Deserializes a persisted endpoint array and tolerates damaged optional JSON by rebuilding it on the next save.</summary>
    /// <param name="value">Persisted endpoint JSON.</param>
    /// <returns>The normalized mutable endpoint list.</returns>
    private List<string> DeserializeEndpoints(string value)
    {
        try
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(string.IsNullOrWhiteSpace(value) ? "[]" : value, jsonOptions) ?? [];
            }
            catch (JsonException exception)
            {
                logger.LogWarning(exception, "Stored configured-host endpoint JSON was invalid and will be rebuilt on save.");
                return [];
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Deserializing configured-host endpoints failed.");
            throw;
        }
    }

    /// <summary>Converts a positive GiB form value to a checked 64-bit byte count.</summary>
    /// <param name="gib">Memory size in GiB.</param>
    /// <returns>The byte count, or <see langword="null"/> when the value is unknown/non-positive.</returns>
    private long? ToBytes(double? gib)
    {
        try
        {
            if (gib is null || gib <= 0d) return null;
            return checked((long)Math.Round(gib.Value * 1024d * 1024d * 1024d, MidpointRounding.AwayFromZero));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Converting configured-host memory GiB to bytes failed.");
            throw;
        }
    }
}
