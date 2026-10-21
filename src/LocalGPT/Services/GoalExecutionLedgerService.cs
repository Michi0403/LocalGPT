using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Goal-step write-ahead journal in the same per-user storage convention as the Council spooler.
/// A completed subgoal is committed atomically before the next begins. A process crash during a
/// Council call cannot prove whether external effects happened: recovery marks that step interrupted
/// and never silently invokes it a second time.
/// </summary>
/// <param name="logger">Reports I/O errors without logging user goal contents.</param>
public sealed class GoalExecutionLedgerService(ILogger<GoalExecutionLedgerService> logger) : IGoalExecutionLedgerService
{
    /// <summary>Per-goal locking; the ledger is a singleton shared by interactive and 1-Wire scopes.</summary>
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> goalGates = new();
    /// <summary>Only checkpoints written by this process can belong to a live in-process goal.</summary>
    private readonly ConcurrentDictionary<Guid, byte> liveProcessGoals = new();
    /// <summary>JSON format for persisted business objects.</summary>
    private readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <inheritdoc/>
    public async Task<IDisposable> AcquireAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        if (goalId == Guid.Empty) throw new ArgumentException("Goal identity must be stable.", nameof(goalId));
        var gate = goalGates.GetOrAdd(goalId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new GoalLease(gate);
    }

    /// <inheritdoc/>
    public async Task SaveAsync(GoalExecutionCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (checkpoint.GoalId == Guid.Empty) throw new ArgumentException("Missing goal ID.", nameof(checkpoint));
        var path = ResolvePath(checkpoint.GoalId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        checkpoint.UpdatedAtUtc = DateTime.UtcNow;
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            // Atomic replace within the same directory prevents a partially serialized recovery state.
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 16384, FileOptions.WriteThrough | FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, checkpoint, JsonOptions, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, path, overwrite: true);
            // A newly acquired semaphore after restart is NOT proof the old run is alive.
            // Record ownership only after this process has durably written its running checkpoint.
            if (checkpoint.State == GoalExecutionState.Running)
                liveProcessGoals.TryAdd(checkpoint.GoalId, 0);
            else
                liveProcessGoals.TryRemove(checkpoint.GoalId, out _);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Goal checkpoint persistence failed for {GoalId}.", checkpoint.GoalId);
            throw;
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    /// <inheritdoc/>
    public async Task<GoalExecutionCheckpoint?> LoadAsync(Guid goalId, CancellationToken cancellationToken = default)
    {
        if (goalId == Guid.Empty) throw new ArgumentException("Missing goal ID.", nameof(goalId));
        var path = ResolvePath(goalId);
        if (!File.Exists(path)) return null;
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                16384, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var state = await JsonSerializer.DeserializeAsync<GoalExecutionCheckpoint>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (state is null || state.GoalId != goalId) throw new InvalidDataException("Checkpoint identity mismatch.");
            // Do not persist this projection as the normal in-process caller may still own the active lease.
            if (state.State == GoalExecutionState.Running &&
                (!liveProcessGoals.ContainsKey(goalId) ||
                 !goalGates.TryGetValue(goalId, out var active) || active.CurrentCount > 0))
            {
                state.State = GoalExecutionState.Interrupted;
                foreach (var receipt in state.Receipts.Where(receipt => receipt.State == GoalExecutionState.Running))
                    receipt.State = GoalExecutionState.Interrupted;
            }
            return state;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Goal checkpoint could not be loaded for {GoalId}.", goalId);
            throw;
        }
    }

    /// <summary>Builds a safe, GUID-only path under configured user-data storage.</summary>
    /// <param name="goalId">Validated goal identity.</param>
    /// <returns>Private checkpoint path.</returns>
    private string ResolvePath(Guid goalId) =>
        LocalGptApplicationDataPaths.ResolveUserPath("GoalExecution", goalId.ToString("N") + ".json");

    /// <summary>Owns the semaphore acquired for the current goal.</summary>
    /// <param name="gate">Acquired lock to release once.</param>
    private sealed class GoalLease(SemaphoreSlim gate) : IDisposable
    {
        /// <summary>Prevents double release.</summary>
        private int released;
        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref released, 1) == 0) gate.Release();
        }
    }
}
