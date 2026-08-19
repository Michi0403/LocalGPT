using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Services;

/// <summary>
/// Durable local benchmark evidence persistence. The benchmark UI keeps bounded render projections in memory while
/// full task streams are written beneath the LocalGPT user-data directory and loaded only when a developer asks for them.
/// </summary>
public sealed partial class ProviderModelBenchmarkService
{
    /// <summary>
    /// Defines the benchmark evidence schema version constant used by <see cref="ProviderModelBenchmarkService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const int BenchmarkEvidenceSchemaVersion = 1;
    /// <summary>
    /// Defines the benchmark evidence report file name constant used by <see cref="ProviderModelBenchmarkService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string BenchmarkEvidenceReportFileName = "report.json";

    /// <summary>
    /// Gets the benchmark evidence JSON options value that forms part of the provider model benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The benchmark evidence JSON options value exposed by <see cref="ProviderModelBenchmarkService"/>.</value>
    private JsonSerializerOptions BenchmarkEvidenceJsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Gets the benchmark evidence root value that forms part of the provider model benchmark state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The benchmark evidence root value exposed by <see cref="ProviderModelBenchmarkService"/>.</value>
    private string BenchmarkEvidenceRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LocalGPT",
        "BenchmarkEvidence");

    /// <summary>
    /// Retrieves stored evidence as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public Task<IReadOnlyList<ProviderModelBenchmarkStoredEvidence>> GetStoredEvidenceAsync(
        int maxCount = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var root = BenchmarkEvidenceRoot;
            if (!Directory.Exists(root))
                return Task.FromResult<IReadOnlyList<ProviderModelBenchmarkStoredEvidence>>([]);

            var boundedCount = Math.Clamp(maxCount, 1, 100);
            var results = new List<ProviderModelBenchmarkStoredEvidence>();
            foreach (var directory in new DirectoryInfo(root)
                .EnumerateDirectories()
                .OrderByDescending(item => item.LastWriteTimeUtc))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Guid.TryParseExact(directory.Name, "N", out var runId))
                    continue;

                var reportFile = new FileInfo(Path.Combine(directory.FullName, BenchmarkEvidenceReportFileName));
                if (!reportFile.Exists)
                    continue;

                results.Add(new ProviderModelBenchmarkStoredEvidence
                {
                    RunId = runId,
                    StoredAtUtc = new DateTimeOffset(reportFile.LastWriteTimeUtc, TimeSpan.Zero),
                    ByteLength = reportFile.Length
                });
                if (results.Count >= boundedCount)
                    break;
            }

            return Task.FromResult<IReadOnlyList<ProviderModelBenchmarkStoredEvidence>>(results);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Enumerating stored provider benchmark evidence was cancelled.");
            else
                logger.LogWarning(exception, "Stored provider benchmark evidence could not be enumerated.");
            throw;
        }
    }

    /// <summary>
    /// Loads stored evidence as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<ProviderModelBenchmarkReport?> LoadStoredEvidenceAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
            return null;

        try
        {
            var path = GetReportArchivePath(runId);
            if (!File.Exists(path))
                return null;

            var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
            var archive = await JsonSerializer.DeserializeAsync<ProviderModelBenchmarkEvidenceArchive>(
                stream,
                BenchmarkEvidenceJsonOptions,
                cancellationToken).ConfigureAwait(false);
            if (archive is null || archive.SchemaVersion <= 0 || archive.Report.RunId != runId)
                return null;
            return archive.Report;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Stored provider benchmark evidence {BenchmarkRunId} is not valid JSON.", runId);
            return null;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Loading stored provider benchmark evidence {BenchmarkRunId} was cancelled.", runId);
            else
                logger.LogWarning(exception, "Stored provider benchmark evidence {BenchmarkRunId} could not be loaded.", runId);
            throw;
        }
    }

    /// <summary>
    /// Loads task evidence as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<ProviderModelBenchmarkTaskEvidenceArchive?> LoadTaskEvidenceAsync(
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveTaskEvidencePath(artifactId, out var path))
            return null;

        try
        {
            if (!File.Exists(path))
                return null;
            var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<ProviderModelBenchmarkTaskEvidenceArchive>(
                stream,
                BenchmarkEvidenceJsonOptions,
                cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Stored provider benchmark task evidence {ArtifactId} is not valid JSON.", artifactId);
            return null;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Loading stored provider benchmark task evidence {ArtifactId} was cancelled.", artifactId);
            else
                logger.LogWarning(exception, "Stored provider benchmark task evidence {ArtifactId} could not be loaded.", artifactId);
            throw;
        }
    }

    /// <summary>
    /// Attempts to persist full task evidence as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="model">Model value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="profileName">Profile name value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="taskOrdinal">Task ordinal value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="taskPrompt">Task prompt value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="providerTrace">Provider trace value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="responseText">Response text value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="taskResult">Task result value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private async Task<string> TryPersistFullTaskEvidenceAsync(
        Guid runId,
        ProviderModelReference model,
        string profileName,
        int taskOrdinal,
        string taskPrompt,
        string providerTrace,
        string responseText,
        ProviderModelBenchmarkTaskResult taskResult)
    {
        try
        {
            var runDirectory = GetRunEvidenceDirectory(runId);
            Directory.CreateDirectory(runDirectory);
            var profileHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(profileName ?? string.Empty)))[..12].ToLowerInvariant();
            var fileName = $"task-{model.StableId}-{profileHash}-{Math.Max(1, taskOrdinal):00}.json";
            var finalPath = Path.Combine(runDirectory, fileName);
            var tempPath = finalPath + $".{Guid.NewGuid():N}.tmp";
            var archive = new ProviderModelBenchmarkTaskEvidenceArchive
            {
                SchemaVersion = BenchmarkEvidenceSchemaVersion,
                RunId = runId,
                CapturedAtUtc = DateTimeOffset.UtcNow,
                TargetStableId = model.StableId,
                TargetSelectionKey = model.SelectionKey,
                ProfileName = profileName ?? string.Empty,
                TaskOrdinal = Math.Max(1, taskOrdinal),
                TaskName = taskResult.TaskName,
                TaskPrompt = taskPrompt ?? string.Empty,
                ProviderTrace = providerTrace ?? string.Empty,
                ResponseText = responseText ?? string.Empty,
                Succeeded = taskResult.Succeeded,
                QualityScore = taskResult.QualityScore,
                TokensPerSecond = taskResult.TokensPerSecond,
                TotalMilliseconds = taskResult.TotalMilliseconds,
                AttemptCount = taskResult.AttemptCount,
                Error = taskResult.Error
            };

            await WriteJsonAtomicallyAsync(tempPath, finalPath, archive).ConfigureAwait(false);
            return $"{runId:N}/{fileName}";
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Full provider benchmark task evidence could not be stored for run {BenchmarkRunId}, model {ModelIdentity}, profile {ProfileName}, task {TaskOrdinal}.",
                runId,
                model.StableId,
                profileName,
                taskOrdinal);
            return string.Empty;
        }
    }

    /// <summary>
    /// Attempts to persist benchmark evidence as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="report">Report value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task TryPersistBenchmarkEvidenceAsync(ProviderModelBenchmarkReport report)
    {
        try
        {
            if (report.RunId == Guid.Empty)
                return;
            var runDirectory = GetRunEvidenceDirectory(report.RunId);
            Directory.CreateDirectory(runDirectory);
            var finalPath = Path.Combine(runDirectory, BenchmarkEvidenceReportFileName);
            var tempPath = finalPath + $".{Guid.NewGuid():N}.tmp";
            var archive = new ProviderModelBenchmarkEvidenceArchive
            {
                SchemaVersion = BenchmarkEvidenceSchemaVersion,
                StoredAtUtc = DateTimeOffset.UtcNow,
                Report = report
            };
            await WriteJsonAtomicallyAsync(tempPath, finalPath, archive).ConfigureAwait(false);
            logger.LogInformation(
                "Stored provider benchmark audit evidence {BenchmarkRunId} under LocalGPT user data with {TargetCount} target(s).",
                report.RunId,
                report.Targets.Count);
        }
        catch (Exception exception)
        {
            // Evidence persistence must never convert a completed benchmark into a failed benchmark. It is an audit side effect,
            // not a configuration write or provider measurement.
            logger.LogWarning(exception, "Provider benchmark audit evidence {BenchmarkRunId} could not be stored.", report.RunId);
        }
    }

    /// <summary>
    /// Writes JSON atomically as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="ProviderModelBenchmarkService"/>.</typeparam>
    /// <param name="tempPath">Temp path value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="finalPath">Final path value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="payload">Payload value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task WriteJsonAtomicallyAsync<T>(string tempPath, string finalPath, T payload)
    {
        try
        {
            {
                var stream = new FileStream(
                    tempPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 64 * 1024,
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan);
                await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
                await JsonSerializer.SerializeAsync(
                    stream,
                    payload,
                    BenchmarkEvidenceJsonOptions,
                    CancellationToken.None).ConfigureAwait(false);
                await stream.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            }
            File.Move(tempPath, finalPath, overwrite: true);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Atomic provider benchmark evidence write failed for {EvidencePath}.", finalPath);
            throw;
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (Exception cleanupException)
            {
                logger.LogDebug(cleanupException, "Temporary provider benchmark evidence file cleanup failed for {TemporaryEvidencePath}.", tempPath);
            }
        }
    }

    /// <summary>
    /// Retrieves run evidence directory as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string GetRunEvidenceDirectory(Guid runId)
    {
        try
        {
            return Path.Combine(BenchmarkEvidenceRoot, runId.ToString("N"));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving benchmark evidence directory for run {BenchmarkRunId} failed.", runId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves report archive path as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string GetReportArchivePath(Guid runId)
    {
        try
        {
            return Path.Combine(GetRunEvidenceDirectory(runId), BenchmarkEvidenceReportFileName);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving benchmark report archive path for run {BenchmarkRunId} failed.", runId);
            throw;
        }
    }

    /// <summary>
    /// Attempts to resolve task evidence path as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="artifactId">Identifier of the artifact to use for this operation.</param>
    /// <param name="path">Path value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TryResolveTaskEvidencePath(string? artifactId, out string path)
    {
        try
        {
            path = string.Empty;
            if (string.IsNullOrWhiteSpace(artifactId))
                return false;
            var normalized = artifactId.Replace('\\', '/').Trim('/');
            var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !Guid.TryParseExact(parts[0], "N", out var runId))
                return false;
            var fileName = parts[1];
            if (!fileName.StartsWith("task-", StringComparison.Ordinal) ||
                !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
                return false;
            path = Path.Combine(GetRunEvidenceDirectory(runId), fileName);
            return true;
        }
        catch (Exception exception)
        {
            path = string.Empty;
            logger.LogWarning(exception, "Resolving provider benchmark task evidence path failed for artifact {ArtifactId}.", artifactId);
            return false;
        }
    }
}
