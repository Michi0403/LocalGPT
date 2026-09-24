using System.Collections;
using System.Security.Cryptography;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>Owns database-backed LocalGPT environment overrides and exposes redacted process/user/machine views for the Toolchains workbench.</summary>
public sealed class ToolchainEnvironmentService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    IPlatformRuntimeService platform,
    ILogger<ToolchainEnvironmentService> logger) : IToolchainEnvironmentService
{
    private const string DataType = "toolchain.environment";
    private const string StoragePrefix = "toolchain:environment:";
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim mutationGate = new(1, 1);

    /// <inheritdoc />
    public async Task ApplyApplicationOverridesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var records = await GetStoredRecordsAsync(cancellationToken).ConfigureAwait(false);
            var overrides = records.Where(item => item.ApplyOnStartup && item.Scope == ToolchainEnvironmentScope.Application && item.ToolchainInstallationId is null).ToList();
            foreach (var item in overrides)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Environment.SetEnvironmentVariable(item.Name, item.Value, EnvironmentVariableTarget.Process);
            }
            logger.LogInformation("Applied {Count} database-backed LocalGPT application environment override(s) to the current process; names and values were omitted from logs.", overrides.Count);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Applying LocalGPT application environment overrides was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Applying LocalGPT application environment overrides failed; names and values were omitted from logs.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ToolchainEnvironmentSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ApplyApplicationOverridesAsync(cancellationToken).ConfigureAwait(false);
            var records = await GetStoredRecordsAsync(cancellationToken).ConfigureAwait(false);
            var rows = new List<ToolchainEnvironmentEntry>();
            var effective = ReadScope(EnvironmentVariableTarget.Process);
            AddRows(rows, effective, ToolchainEnvironmentScope.Effective, "Effective LocalGPT process", writable: false, effective, null);
            AddRows(rows, effective, ToolchainEnvironmentScope.Process, "Process", writable: true, effective, null);

            foreach (var group in records.Where(item => item.Scope == ToolchainEnvironmentScope.Application)
                .GroupBy(item => item.ToolchainInstallationId))
            {
                AddRows(rows,
                    group.ToDictionary(item => item.Name, item => item.Value, StringComparer.OrdinalIgnoreCase),
                    ToolchainEnvironmentScope.Application,
                    group.Key is null ? "LocalGPT application override · database" : "Toolchain application override · database",
                    writable: true,
                    effective,
                    group.Key);
            }

            var supportsPersistent = platform.ToolchainPlatform == ToolchainPlatformKind.Windows;
            if (supportsPersistent)
            {
                AddRows(rows, ReadScope(EnvironmentVariableTarget.User), ToolchainEnvironmentScope.User, "Operating-system user", writable: true, effective, null);
                AddRows(rows, ReadScope(EnvironmentVariableTarget.Machine), ToolchainEnvironmentScope.Machine, "Operating-system machine", writable: true, effective, null);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return new ToolchainEnvironmentSnapshot
            {
                Platform = platform.ProviderBootstrapToken,
                SupportsPersistentOperatingSystemScopes = supportsPersistent,
                Entries = rows.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ThenBy(item => item.Scope).ThenBy(item => item.ToolchainInstallationId).ToList()
            };
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Building the toolchain environment snapshot was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the toolchain environment snapshot failed; environment names and values were omitted from logs.");
            throw;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ToolchainEnvironmentEntry> FilterEntries(IReadOnlyCollection<ToolchainEnvironmentEntry> entries, string? searchText, int maximumResults = 300)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(entries);
            var boundedMaximum = Math.Clamp(maximumResults, 1, 5000);
            if (string.IsNullOrWhiteSpace(searchText))
                return entries.Take(boundedMaximum).ToList();

            var normalizedSearch = searchText.Trim();
            return entries
                .Where(item => item.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                    || item.Source.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                .Take(boundedMaximum)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Filtering the toolchain environment workbench failed; search text and environment names were omitted from logs.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ToolchainEnvironmentSnapshot> ChangeAsync(ToolchainEnvironmentChangeRequest request, CancellationToken cancellationToken = default)
    {
        var lockTaken = false;
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Explicit human confirmation is required before changing an environment variable.");
            var name = ValidateName(request.Name);
            if (request.Scope == ToolchainEnvironmentScope.Effective)
                throw new InvalidOperationException("The effective environment is a read-only projection. Choose Process, Application, User, or Machine.");
            if ((request.Scope == ToolchainEnvironmentScope.User || request.Scope == ToolchainEnvironmentScope.Machine) && platform.ToolchainPlatform != ToolchainPlatformKind.Windows)
                throw new PlatformNotSupportedException("This runtime exposes persistent User/Machine environment mutation only on Windows. Use the LocalGPT Application scope for a portable override.");
            if (!request.Remove && request.Value.Length > 32768)
                throw new ArgumentException("Environment-variable values are limited to 32768 characters in this workbench.", nameof(request));
            if (request.ToolchainInstallationId is not null && request.Scope != ToolchainEnvironmentScope.Application)
                throw new InvalidOperationException("Toolchain-specific environment overrides use Application scope and are applied only by that toolchain execution profile.");

            await mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockTaken = true;
            var value = request.Remove ? null : request.Value;
            if (request.ToolchainInstallationId is null)
            {
                switch (request.Scope)
                {
                    case ToolchainEnvironmentScope.Process:
                    case ToolchainEnvironmentScope.Application:
                        Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);
                        break;
                    case ToolchainEnvironmentScope.User:
                        Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
                        break;
                    case ToolchainEnvironmentScope.Machine:
                        Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Machine);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(request.Scope));
                }
            }

            await PersistRecordAsync(request, name, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Changed one database-tracked toolchain environment variable in scope {Scope}; name and value were omitted from logs.", request.Scope);
            return await GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Changing a toolchain environment variable was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Changing a toolchain environment variable failed; name and value were omitted from logs.");
            throw;
        }
        finally
        {
            if (lockTaken)
                mutationGate.Release();
        }
    }

    private async Task PersistRecordAsync(ToolchainEnvironmentChangeRequest request, string name, CancellationToken cancellationToken)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var rows = await db.SystemVariables.Where(item => item.DataType == DataType && item.Name.StartsWith(StoragePrefix)).ToListAsync(cancellationToken).ConfigureAwait(false);
            var match = rows.Select(item => (Row: item, Record: DeserializeRecord(item.ValueString)))
                .FirstOrDefault(item => item.Record is not null && item.Record.Scope == request.Scope && item.Record.ToolchainInstallationId == request.ToolchainInstallationId && item.Record.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (request.Remove)
            {
                if (match.Row is not null)
                    db.SystemVariables.Remove(match.Row);
            }
            else
            {
                var record = match.Record ?? new ToolchainEnvironmentOverrideRecord();
                record.Name = name;
                record.Scope = request.Scope;
                record.ToolchainInstallationId = request.ToolchainInstallationId;
                record.Value = request.Value;
                record.ApplyOnStartup = request.Scope == ToolchainEnvironmentScope.Application && request.ToolchainInstallationId is null;
                record.UpdatedAtUtc = DateTime.UtcNow;
                record.UpdatedBy = "CurrentUser";
                var row = match.Row ?? new SystemVariable { Name = BuildStorageName(record.Id), DataType = DataType, ValueString = "{}" };
                row.DataType = DataType;
                row.ValueString = JsonSerializer.Serialize(record, JsonOptions);
                row.LastUpdated = DateTime.UtcNow;
                if (match.Row is null)
                    db.SystemVariables.Add(row);
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Persisting a toolchain environment override was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Persisting a toolchain environment override failed; names and values were omitted from logs.");
            throw;
        }
    }

    private async Task<IReadOnlyList<ToolchainEnvironmentOverrideRecord>> GetStoredRecordsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var rows = await db.SystemVariables.AsNoTracking()
                .Where(item => item.DataType == DataType && item.Name.StartsWith(StoragePrefix))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            return rows.Select(item => DeserializeRecord(item.ValueString)).Where(item => item is not null).Select(item => item!).ToList();
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Loading persisted toolchain environment overrides was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading persisted toolchain environment overrides failed; names and values were omitted from logs.");
            throw;
        }
    }

    private ToolchainEnvironmentOverrideRecord? DeserializeRecord(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<ToolchainEnvironmentOverrideRecord>(value, JsonOptions);
        }
        catch (JsonException exception)
        {
            logger.LogDebug(exception, "Ignoring one malformed persisted toolchain environment record; serialized content was omitted from logs.");
            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading one persisted toolchain environment record failed; serialized content was omitted from logs.");
            throw;
        }
    }

    private string BuildStorageName(Guid id)
    {
        try
        {
            return StoragePrefix + id.ToString("N");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building a toolchain environment storage key failed.");
            throw;
        }
    }

    private Dictionary<string, string> ReadScope(EnvironmentVariableTarget target)
    {
        try
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry item in Environment.GetEnvironmentVariables(target))
            {
                if (item.Key is not string name || string.IsNullOrWhiteSpace(name))
                    continue;
                result[name] = Convert.ToString(item.Value) ?? string.Empty;
            }
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading an operating-system environment scope failed; environment names and values were omitted from logs.");
            throw;
        }
    }

    private void AddRows(List<ToolchainEnvironmentEntry> rows, IReadOnlyDictionary<string, string> values, ToolchainEnvironmentScope scope, string source, bool writable, IReadOnlyDictionary<string, string> effective, Guid? toolchainInstallationId)
    {
        try
        {
            foreach (var item in values)
            {
                var sensitive = IsSensitiveName(item.Key);
                effective.TryGetValue(item.Key, out var effectiveValue);
                rows.Add(new ToolchainEnvironmentEntry
                {
                    Name = item.Key,
                    Value = sensitive ? "••••••••" : item.Value,
                    Scope = scope,
                    ToolchainInstallationId = toolchainInstallationId,
                    Source = source,
                    IsSensitive = sensitive,
                    IsWritable = writable,
                    IsOverridden = scope != ToolchainEnvironmentScope.Effective && !string.Equals(item.Value, effectiveValue, StringComparison.Ordinal)
                });
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Projecting toolchain environment entries failed; names and values were omitted from logs.");
            throw;
        }
    }

    private string ValidateName(string value)
    {
        try
        {
            var name = value?.Trim() ?? string.Empty;
            if (name.Length is < 1 or > 256 || name.Contains('=') || name.Contains('\0'))
                throw new ArgumentException("Environment-variable names must contain 1-256 characters and cannot contain '=' or a NUL character.", nameof(value));
            return name;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Validating a toolchain environment-variable name failed; the name was omitted from logs.");
            throw;
        }
    }

    private bool IsSensitiveName(string name)
    {
        try
        {
            var markers = new[] { "TOKEN", "SECRET", "PASSWORD", "PASSWD", "API_KEY", "APIKEY", "CREDENTIAL", "AUTH", "COOKIE" };
            return markers.Any(marker => name.Contains(marker, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Classifying a toolchain environment-variable name failed; the name was omitted from logs.");
            throw;
        }
    }
}

/// <summary>Downloads only pre-seeded toolchain artifacts selected by stable catalog key and never executes them automatically.</summary>
public sealed class ToolchainAcquisitionService(
    IHttpClientFactory httpClientFactory,
    IPlatformRuntimeService platform,
    ILogger<ToolchainAcquisitionService> logger) : IToolchainAcquisitionService
{
    private const long MaximumDownloadBytes = 1_073_741_824;

    /// <inheritdoc />
    public Task<IReadOnlyList<ToolchainAcquisitionSource>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var platform = CurrentPlatform();
            IReadOnlyList<ToolchainAcquisitionSource> result = BuildCatalog()
                .Select(item =>
                {
                    item.SupportedOnCurrentPlatform = item.Platform.Equals("any", StringComparison.OrdinalIgnoreCase) || item.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase);
                    return item;
                })
                .OrderByDescending(item => item.SupportedOnCurrentPlatform)
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return Task.FromResult(result);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading the reviewed toolchain acquisition catalog failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ToolchainAcquisitionDownloadResult> DownloadAsync(ToolchainAcquisitionDownloadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Explicit human confirmation is required before downloading a toolchain artifact.");
            var source = BuildCatalog().FirstOrDefault(item => item.Key.Equals(request.SourceKey?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new KeyNotFoundException("The requested toolchain acquisition source is not in LocalGPT's reviewed catalog.");
            var currentPlatform = CurrentPlatform();
            if (!source.Platform.Equals("any", StringComparison.OrdinalIgnoreCase) && !source.Platform.Equals(currentPlatform, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"This catalog artifact targets {source.Platform}, while LocalGPT is running on {currentPlatform}.");
            if (!Uri.TryCreate(source.DownloadUri, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("The reviewed toolchain source does not use HTTPS.");

            var downloadRoot = LocalGptApplicationDataPaths.ResolveUserPath("Downloads", "Toolchains", source.Key);
            Directory.CreateDirectory(downloadRoot);
            var finalPath = Path.Combine(downloadRoot, source.FileName);
            var temporaryPath = finalPath + ".part-" + Guid.NewGuid().ToString("N");
            try
            {
                using var client = httpClientFactory.CreateClient("LocalGPTToolchainAcquisition");
                using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentLength is > MaximumDownloadBytes)
                    throw new InvalidOperationException("The toolchain artifact exceeds LocalGPT's 1 GiB direct-download limit.");

                var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using var configuredSourceStreamAsyncDisposal = sourceStream.ConfigureAwait(false);
                var destination = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await using var configuredDestinationAsyncDisposal = destination.ConfigureAwait(false);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = new byte[128 * 1024];
                long total = 0;
                while (true)
                {
                    var read = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                        break;
                    total += read;
                    if (total > MaximumDownloadBytes)
                        throw new InvalidOperationException("The toolchain artifact exceeded LocalGPT's 1 GiB direct-download limit while streaming.");
                    hash.AppendData(buffer, 0, read);
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }
                await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
                File.Move(temporaryPath, finalPath, overwrite: true);
                var digest = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
                logger.LogInformation("Downloaded reviewed toolchain source {SourceKey} with {Bytes} bytes; the local path was omitted from logs.", source.Key, total);
                return new ToolchainAcquisitionDownloadResult
                {
                    SourceKey = source.Key,
                    LocalPath = finalPath,
                    Bytes = total,
                    Sha256 = digest,
                    DownloadedAtUtc = DateTime.UtcNow,
                    InstallHint = source.InstallHint
                };
            }
            finally
            {
                try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); } catch { }
            }
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Downloading a reviewed toolchain artifact was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Downloading a reviewed toolchain artifact failed; URL and local path were omitted from logs.");
            throw;
        }
    }

    private List<ToolchainAcquisitionSource> BuildCatalog()
    {
        try
        {
            return
            [
                Source("python-3.12.10-windows-x64", "python", "Python 3.12", "Python Software Foundation", "3.12.10", "Manufacturer", "https://www.python.org/ftp/python/3.12.10/python-3.12.10-amd64.exe", "python-3.12.10-amd64.exe", "windows", "Run the reviewed Python installer manually, then use Toolchains → Discover and the embedded-AI Python linker."),
                Source("python-3.12.10-macos", "python", "Python 3.12", "Python Software Foundation", "3.12.10", "Manufacturer", "https://www.python.org/ftp/python/3.12.10/python-3.12.10-macos11.pkg", "python-3.12.10-macos11.pkg", "macos", "Open the package manually, then rediscover Python in Toolchains."),
                Source("python-3.12.10-source", "python", "Python 3.12 source", "Python Software Foundation", "3.12.10", "Manufacturer", "https://www.python.org/ftp/python/3.12.10/Python-3.12.10.tgz", "Python-3.12.10.tgz", "linux", "Source archive only. Prefer your distribution packages when appropriate, then rediscover Python in Toolchains."),
                Source("dotnet-install-windows", "dotnet-sdk", ".NET SDK install script", "Microsoft", "channel-selected", "Manufacturer", "https://dot.net/v1/dotnet-install.ps1", "dotnet-install.ps1", "windows", "Review and run the script manually with your chosen channel/version. LocalGPT downloads but never executes it automatically."),
                Source("dotnet-install-unix", "dotnet-sdk", ".NET SDK install script", "Microsoft", "channel-selected", "Manufacturer", "https://dot.net/v1/dotnet-install.sh", "dotnet-install.sh", "linux", "Review and run the script manually with your chosen channel/version. LocalGPT downloads but never executes it automatically."),
                Source("dotnet-install-macos", "dotnet-sdk", ".NET SDK install script", "Microsoft", "channel-selected", "Manufacturer", "https://dot.net/v1/dotnet-install.sh", "dotnet-install.sh", "macos", "Review and run the script manually with your chosen channel/version. LocalGPT downloads but never executes it automatically."),
                Source("node-22.14.0-windows-x64", "node", "Node.js 22", "OpenJS Foundation", "22.14.0", "Manufacturer", "https://nodejs.org/dist/v22.14.0/node-v22.14.0-x64.msi", "node-v22.14.0-x64.msi", "windows", "Run the MSI manually, then rediscover Node.js in Toolchains."),
                Source("node-22.14.0-linux-x64", "node", "Node.js 22", "OpenJS Foundation", "22.14.0", "Manufacturer", "https://nodejs.org/dist/v22.14.0/node-v22.14.0-linux-x64.tar.xz", "node-v22.14.0-linux-x64.tar.xz", "linux", "Extract/link manually or use your distribution package manager, then rediscover Node.js in Toolchains."),
                Source("node-22.14.0-macos", "node", "Node.js 22", "OpenJS Foundation", "22.14.0", "Manufacturer", "https://nodejs.org/dist/v22.14.0/node-v22.14.0.pkg", "node-v22.14.0.pkg", "macos", "Open the package manually, then rediscover Node.js in Toolchains."),
                Source("arduino-cli-source", "arduino-cli", "Arduino CLI source", "Arduino", "main", "GitHub", "https://github.com/arduino/arduino-cli/archive/refs/heads/master.zip", "arduino-cli-master.zip", "any", "Source archive only. Build or install it separately, then let LocalGPT discover the resulting executable."),
                Source("platformio-core-source", "platformio", "PlatformIO Core source", "PlatformIO", "develop", "GitHub", "https://github.com/platformio/platformio-core/archive/refs/heads/develop.zip", "platformio-core-develop.zip", "any", "Source archive only. Install the reviewed project with Python tooling if desired, then rediscover PlatformIO."),
                Source("cmake-source", "cmake", "CMake source", "Kitware", "master", "GitHub", "https://github.com/Kitware/CMake/archive/refs/heads/master.zip", "cmake-master.zip", "any", "Source archive only. Build/install separately; LocalGPT does not execute build scripts from downloads."),
                Source("ffmpeg-source", "ffmpeg", "FFmpeg source", "FFmpeg", "master", "GitHub", "https://github.com/FFmpeg/FFmpeg/archive/refs/heads/master.zip", "ffmpeg-master.zip", "any", "Source archive only. Whisper requires an ffmpeg executable at runtime; build/install it separately or use your operating-system package source.")
            ];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Constructing the reviewed toolchain acquisition catalog failed.");
            throw;
        }
    }

    private ToolchainAcquisitionSource Source(string key, string profileKey, string name, string publisher, string version, string kind, string uri, string fileName, string platform, string hint)
    {
        try
        {
            return new ToolchainAcquisitionSource
            {
                Key = key,
                ProfileKey = profileKey,
                DisplayName = name,
                Publisher = publisher,
                Version = version,
                SourceKind = kind,
                DownloadUri = uri,
                FileName = fileName,
                Platform = platform,
                InstallHint = hint
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating one reviewed toolchain source entry failed.");
            throw;
        }
    }

    private string CurrentPlatform()
    {
        try
        {
            return platform.ProviderBootstrapToken;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Detecting the current platform for toolchain acquisition failed.");
            throw;
        }
    }
}

/// <summary>Applies persisted LocalGPT application environment overrides as the host starts, before interactive toolchain work begins.</summary>
public sealed class ToolchainEnvironmentInitializationHostedService(
    IToolchainEnvironmentService environment,
    ILogger<ToolchainEnvironmentInitializationHostedService> logger) : IHostedService
{
    /// <summary>Applies persisted application overrides to the current process.</summary>
    /// <param name="cancellationToken">Cancellation token supplied by the host.</param>
    /// <returns>A task that completes after initialization.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await environment.ApplyApplicationOverridesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Toolchain environment startup initialization was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            // A malformed manually edited override must not prevent LocalGPT from starting.
            // The Toolchains page will surface the failure again when the operator reviews the environment.
            logger.LogError(exception, "Toolchain environment startup initialization failed; names and values were omitted from logs. LocalGPT will continue without applying the failing override set.");
        }
    }

    /// <summary>Performs no environment rollback because the process is stopping and persisted configuration remains authoritative.</summary>
    /// <param name="cancellationToken">Cancellation token supplied by the host.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Toolchain environment hosted-service stop was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Toolchain environment hosted-service stop failed.");
            throw;
        }
    }
}
