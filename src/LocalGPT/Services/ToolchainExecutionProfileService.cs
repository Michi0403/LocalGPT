using System.Diagnostics;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services;

/// <summary>Owns database-backed reusable toolchain execution profiles and bounded shell-free process execution.</summary>
public sealed class ToolchainExecutionProfileService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    ILogger<ToolchainExecutionProfileService> logger) : IToolchainExecutionProfileService
{
    private const string DataType = "toolchain.profile";
    private const string StoragePrefix = "toolchain:profile:";
    private const int MaximumCapturedCharacters = 1_000_000;
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim gate = new(1, 1);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ToolchainExecutionProfile>> GetProfilesAsync(string? capabilityKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var rows = await db.SystemVariables.AsNoTracking()
                .Where(item => item.DataType == DataType && item.Name.StartsWith(StoragePrefix))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            return rows.Select(item => Deserialize(item.ValueString))
                .Where(item => item is not null && item.IsEnabled &&
                    (string.IsNullOrWhiteSpace(capabilityKey) || item.CapabilityKey.Equals(capabilityKey.Trim(), StringComparison.OrdinalIgnoreCase)))
                .Select(item => item!)
                .OrderByDescending(item => item.IsDefaultForCapability)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Loading toolchain execution profiles was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading toolchain execution profiles failed; command paths, arguments and environment values were omitted from logs.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ToolchainExecutionProfile?> GetProfileAsync(string profileKey, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(profileKey);
            var normalized = NormalizeKey(profileKey);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var row = await db.SystemVariables.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Name == BuildStorageName(normalized) && item.DataType == DataType, cancellationToken)
                .ConfigureAwait(false);
            return row is null ? null : Deserialize(row.ValueString);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Loading one toolchain execution profile was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading one toolchain execution profile failed; the profile key and command details were omitted from logs.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ToolchainExecutionProfile> SaveProfileAsync(SaveToolchainExecutionProfileRequest request, CancellationToken cancellationToken = default)
    {
        var lockTaken = false;
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Profile);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Explicit human confirmation is required before changing a toolchain execution profile.");

            var profile = NormalizeProfile(request.Profile);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockTaken = true;

            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var storageName = BuildStorageName(profile.ProfileKey);
            var row = await db.SystemVariables.SingleOrDefaultAsync(item => item.Name == storageName, cancellationToken).ConfigureAwait(false);
            if (row is null)
            {
                row = new SystemVariable { Name = storageName, DataType = DataType, ValueString = "{}" };
                db.SystemVariables.Add(row);
            }
            else
            {
                var previous = Deserialize(row.ValueString);
                if (previous is not null)
                {
                    profile.Id = previous.Id;
                    profile.CreatedAtUtc = previous.CreatedAtUtc;
                }
            }

            if (profile.IsDefaultForCapability && !string.IsNullOrWhiteSpace(profile.CapabilityKey))
            {
                var siblings = await db.SystemVariables
                    .Where(item => item.DataType == DataType && item.Name.StartsWith(StoragePrefix) && item.Name != storageName)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
                foreach (var sibling in siblings)
                {
                    var siblingProfile = Deserialize(sibling.ValueString);
                    if (siblingProfile is null || !siblingProfile.IsDefaultForCapability ||
                        !siblingProfile.CapabilityKey.Equals(profile.CapabilityKey, StringComparison.OrdinalIgnoreCase))
                        continue;
                    siblingProfile.IsDefaultForCapability = false;
                    siblingProfile.UpdatedAtUtc = DateTime.UtcNow;
                    sibling.ValueString = JsonSerializer.Serialize(siblingProfile, JsonOptions);
                    sibling.LastUpdated = DateTime.UtcNow;
                }
            }

            row.DataType = DataType;
            row.ValueString = JsonSerializer.Serialize(profile, JsonOptions);
            row.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved toolchain execution profile {ProfileKey} for capability {CapabilityKey}; command paths, arguments and environment values were omitted from logs.", profile.ProfileKey, profile.CapabilityKey);
            return profile;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Saving a toolchain execution profile was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving a toolchain execution profile failed; command paths, arguments and environment values were omitted from logs.");
            throw;
        }
        finally
        {
            if (lockTaken)
                gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ToolchainProcessExecutionResult> ExecuteAsync(ToolchainProcessExecutionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var profile = await GetProfileAsync(request.ProfileKey, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Toolchain execution profile '{request.ProfileKey}' was not found.");
            if (!profile.IsEnabled)
                throw new InvalidOperationException("The selected toolchain execution profile is disabled.");
            if (profile.RequiresApproval && !request.UserConfirmed)
                throw new InvalidOperationException("Explicit human confirmation is required before running this toolchain process profile.");
            if (request.Arguments.Count > 256 || request.Arguments.Any(item => item.Length > 8192))
                throw new ArgumentException("Toolchain process arguments exceed the bounded execution contract.", nameof(request));

            ProjectCompilerInstallation? installation = null;
            if (profile.ToolchainInstallationId is Guid installationId)
            {
                await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
                var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
                installation = await db.ProjectCompilerInstallations.AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == installationId && item.IsEnabled, cancellationToken).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException("The toolchain installation assigned to this process profile was not found or is disabled.");
            }

            var executable = ResolveExecutable(profile, installation);
            if (!File.Exists(executable))
                throw new FileNotFoundException("The configured toolchain executable does not exist.", executable);
            var arguments = BuildArguments(profile, request.Arguments);
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = ResolveWorkingDirectory(profile, installation, executable),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);
            foreach (var item in ReadEnvironment(installation?.EnvironmentVariablesJson))
                startInfo.Environment[item.Key] = item.Value;
            foreach (var item in await ReadDatabaseEnvironmentAsync(profile.ToolchainInstallationId, cancellationToken).ConfigureAwait(false))
                startInfo.Environment[item.Key] = item.Value;
            foreach (var item in ReadEnvironment(profile.EnvironmentVariablesJson))
                startInfo.Environment[item.Key] = item.Value;

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            if (!process.Start())
                throw new InvalidOperationException("The configured toolchain process could not be started.");
            var stdoutTask = ReadBoundedAsync(process.StandardOutput, cancellationToken);
            var stderrTask = ReadBoundedAsync(process.StandardError, cancellationToken);
            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
                throw;
            }
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);
            logger.LogInformation("Toolchain process profile {ProfileKey} completed with exit code {ExitCode}; arguments and output were omitted from logs.", profile.ProfileKey, process.ExitCode);
            return new ToolchainProcessExecutionResult
            {
                ProfileKey = profile.ProfileKey,
                ExitCode = process.ExitCode,
                StandardOutput = stdout,
                StandardError = stderr,
                CompletedAtUtc = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Toolchain process execution was cancelled; command details were omitted from logs.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Toolchain process execution failed; command details and output were omitted from logs.");
            throw;
        }
    }

    private ToolchainExecutionProfile NormalizeProfile(ToolchainExecutionProfile source)
    {
        try
        {
            var profile = new ToolchainExecutionProfile
            {
                Id = source.Id == Guid.Empty ? Guid.NewGuid() : source.Id,
                ProfileKey = NormalizeKey(source.ProfileKey),
                Name = RequireText(source.Name, 200, nameof(source.Name)),
                ToolchainInstallationId = source.ToolchainInstallationId,
                CapabilityKey = Trim(source.CapabilityKey, 160),
                ExecutionKind = source.ExecutionKind,
                EntryPoint = Trim(source.EntryPoint, 2048),
                ArgumentsJson = NormalizeArrayJson(source.ArgumentsJson),
                WorkingDirectory = Trim(source.WorkingDirectory, 2048),
                EnvironmentVariablesJson = NormalizeObjectJson(source.EnvironmentVariablesJson),
                ConfigurationJson = NormalizeObjectJson(source.ConfigurationJson),
                IsEnabled = source.IsEnabled,
                IsDefaultForCapability = source.IsDefaultForCapability,
                RequiresApproval = source.RequiresApproval,
                CreatedAtUtc = source.CreatedAtUtc == default ? DateTime.UtcNow : source.CreatedAtUtc,
                UpdatedAtUtc = DateTime.UtcNow,
                UpdatedBy = string.IsNullOrWhiteSpace(source.UpdatedBy) ? "CurrentUser" : Trim(source.UpdatedBy, 160)
            };
            if (profile.ExecutionKind is ToolchainExecutionKind.Module or ToolchainExecutionKind.Script && string.IsNullOrWhiteSpace(profile.EntryPoint))
                throw new ArgumentException("Module and script profiles require an entry point.", nameof(source));
            return profile;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing a toolchain execution profile failed; profile content was omitted from logs.");
            throw;
        }
    }

    private string ResolveExecutable(ToolchainExecutionProfile profile, ProjectCompilerInstallation? installation)
    {
        try
        {
            if (profile.ExecutionKind == ToolchainExecutionKind.Executable && !string.IsNullOrWhiteSpace(profile.EntryPoint))
                return Path.GetFullPath(profile.EntryPoint);
            return installation is not null
                ? Path.GetFullPath(installation.ExecutablePath)
                : throw new InvalidOperationException("This profile requires a registered toolchain installation.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving a toolchain profile executable failed; paths were omitted from logs.");
            throw;
        }
    }

    private IReadOnlyList<string> BuildArguments(ToolchainExecutionProfile profile, IReadOnlyCollection<string> extra)
    {
        try
        {
            var result = JsonSerializer.Deserialize<List<string>>(profile.ArgumentsJson, JsonOptions) ?? [];
            switch (profile.ExecutionKind)
            {
                case ToolchainExecutionKind.Module:
                    result.Insert(0, profile.EntryPoint);
                    result.Insert(0, "-m");
                    break;
                case ToolchainExecutionKind.Script:
                    result.Insert(0, Path.GetFullPath(profile.EntryPoint));
                    break;
            }
            result.AddRange(extra);
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building toolchain process arguments failed; arguments were omitted from logs.");
            throw;
        }
    }

    private string ResolveWorkingDirectory(ToolchainExecutionProfile profile, ProjectCompilerInstallation? installation, string executable)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(profile.WorkingDirectory))
                return Path.GetFullPath(profile.WorkingDirectory);
            if (!string.IsNullOrWhiteSpace(installation?.CompilerHomePath) && Directory.Exists(installation.CompilerHomePath))
                return Path.GetFullPath(installation.CompilerHomePath);
            return Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving a toolchain working directory failed; paths were omitted from logs.");
            throw;
        }
    }

    private async Task<Dictionary<string, string>> ReadDatabaseEnvironmentAsync(Guid? toolchainInstallationId, CancellationToken cancellationToken)
    {
        try
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var rows = await db.SystemVariables.AsNoTracking()
                .Where(item => item.DataType == "toolchain.environment" && item.Name.StartsWith("toolchain:environment:"))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var records = rows.Select(item => DeserializeEnvironmentOverride(item.ValueString))
                .Where(item => item is not null && item.Scope == ToolchainEnvironmentScope.Application)
                .Select(item => item!)
                .ToList();
            foreach (var item in records.Where(item => item.ToolchainInstallationId is null))
                result[item.Name] = item.Value;
            if (toolchainInstallationId is Guid id)
            {
                foreach (var item in records.Where(item => item.ToolchainInstallationId == id))
                    result[item.Name] = item.Value;
            }
            return result;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Loading database-backed toolchain environment overrides was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading database-backed toolchain environment overrides failed; names and values were omitted from logs.");
            throw;
        }
    }

    private ToolchainEnvironmentOverrideRecord? DeserializeEnvironmentOverride(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<ToolchainEnvironmentOverrideRecord>(value, JsonOptions);
        }
        catch (JsonException exception)
        {
            logger.LogDebug(exception, "Ignoring one malformed database-backed toolchain environment override; serialized content was omitted from logs.");
            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading one database-backed toolchain environment override failed; serialized content was omitted from logs.");
            throw;
        }
    }

    private Dictionary<string, string> ReadEnvironment(string? json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading toolchain environment JSON failed; names and values were omitted from logs.");
            throw;
        }
    }

    private async Task<string> ReadBoundedAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        try
        {
            var buffer = new char[8192];
            var builder = new StringBuilder();
            while (builder.Length < MaximumCapturedCharacters)
            {
                var read = await reader.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, MaximumCapturedCharacters - builder.Length)), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;
                builder.Append(buffer, 0, read);
            }
            return builder.ToString();
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Reading bounded toolchain process output was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading bounded toolchain process output failed; output was omitted from logs.");
            throw;
        }
    }

    private ToolchainExecutionProfile? Deserialize(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<ToolchainExecutionProfile>(value, JsonOptions);
        }
        catch (JsonException exception)
        {
            logger.LogDebug(exception, "Ignoring one malformed persisted toolchain execution profile; serialized content was omitted from logs.");
            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading one persisted toolchain execution profile failed; serialized content was omitted from logs.");
            throw;
        }
    }

    private string BuildStorageName(string key)
    {
        try
        {
            return StoragePrefix + key;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building a toolchain profile storage key failed; the key was omitted from logs.");
            throw;
        }
    }

    private string NormalizeKey(string value)
    {
        try
        {
            var key = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (key.Length is < 1 or > 96 || key.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_')))
                throw new ArgumentException("Profile keys must contain 1-96 letters, digits, '.', '-' or '_' characters.", nameof(value));
            return key;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing a toolchain profile key failed; the key was omitted from logs.");
            throw;
        }
    }

    private string RequireText(string value, int maximum, string name)
    {
        try
        {
            var text = (value ?? string.Empty).Trim();
            if (text.Length is < 1 || text.Length > maximum)
                throw new ArgumentException($"{name} must contain 1-{maximum} characters.", name);
            return text;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Validating required toolchain profile text failed; text content was omitted from logs.");
            throw;
        }
    }

    private string Trim(string? value, int maximum)
    {
        try
        {
            var text = (value ?? string.Empty).Trim();
            return text.Length <= maximum ? text : text[..maximum];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing bounded toolchain profile text failed; text content was omitted from logs.");
            throw;
        }
    }

    private string NormalizeArrayJson(string? value)
    {
        try
        {
            var json = string.IsNullOrWhiteSpace(value) ? "[]" : value.Trim();
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("ArgumentsJson must be a JSON array.");
            return json;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Validating toolchain argument JSON failed; JSON content was omitted from logs.");
            throw;
        }
    }

    private string NormalizeObjectJson(string? value)
    {
        try
        {
            var json = string.IsNullOrWhiteSpace(value) ? "{}" : value.Trim();
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("The configured JSON value must be an object.");
            return json;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Validating toolchain object JSON failed; JSON content was omitted from logs.");
            throw;
        }
    }
}
