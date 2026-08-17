using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Persists and executes user-authored Remote Control pipelines by routing every action through LocalGPT's existing DXFunction registry.</summary>
/// <param name="dbContextFactory">Database context factory.</param>
/// <param name="databaseInitializer">Database initialization dependency.</param>
/// <param name="catalog">Database-backed DXFunction and public-service catalog.</param>
/// <param name="registry">DXFunction invocation registry.</param>
/// <param name="templates">Remote Control interpolation service.</param>
/// <param name="executionStore">Remote Control execution audit store.</param>
/// <param name="regex">Shared regular-expression policy service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
public sealed class RemoteControlPipelineService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    IDxAiFunctionCatalogService catalog,
    IDxAiFunctionRegistry registry,
    IRemoteControlTemplateService templates,
    IRemoteControlExecutionStoreService executionStore,
    IRegexCompilationService regex,
    ILogger<RemoteControlPipelineService> logger) : IRemoteControlPipelineService
{
    /// <summary>
    /// Stores the internal key pattern state used by <see cref="RemoteControlPipelineService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly System.Text.RegularExpressions.Regex _keyPattern = regex.Compile("^[a-z0-9][a-z0-9._-]{0,95}$", "c", TimeSpan.FromSeconds(2), nameof(RemoteControlPipelineService));
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="RemoteControlPipelineService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true, WriteIndented = true };

    /// <summary>
    /// Performs list as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<RemoteControlPipelineDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            return await db.RemoteControlPipelineDefinitions.AsNoTracking()
                .OrderBy(item => item.DisplayName)
                .ThenBy(item => item.Key)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Listing Remote Control pipelines was cancelled.");
            else logger.LogError(exception, "Listing Remote Control pipelines failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs get as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<RemoteControlPipelineDefinition?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedKey = NormalizeKey(key);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            return await db.RemoteControlPipelineDefinitions.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Key == normalizedKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Loading Remote Control pipeline {PipelineKey} was cancelled.", key);
            else logger.LogError(exception, "Loading Remote Control pipeline {PipelineKey} failed.", key);
            throw;
        }
    }

    /// <summary>
    /// Performs save as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<RemoteControlPipelineDefinition> SaveAsync(RemoteControlPipelineDefinition definition, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(definition);
            definition.Key = NormalizeKey(definition.Key);
            definition.DisplayName = Bound(definition.DisplayName, 160, definition.Key);
            definition.Description = Bound(definition.Description, 2_000);
            definition.ConnectorKey = string.IsNullOrWhiteSpace(definition.ConnectorKey) ? string.Empty : NormalizeKey(definition.ConnectorKey);
            var steps = ParseSteps(NormalizeStepsJson(definition.StepsJson)).ToList();
            await NormalizeAndValidateStepsAsync(steps, cancellationToken).ConfigureAwait(false);
            definition.StepsJson = JsonSerializer.Serialize(steps, JsonOptions);

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var existing = await db.RemoteControlPipelineDefinitions.SingleOrDefaultAsync(item => item.Key == definition.Key, cancellationToken).ConfigureAwait(false);
            var now = DateTime.UtcNow;
            if (existing is null)
            {
                definition.Id = definition.Id == Guid.Empty ? Guid.NewGuid() : definition.Id;
                definition.CreatedAtUtc = now;
                definition.UpdatedAtUtc = now;
                db.RemoteControlPipelineDefinitions.Add(definition);
                existing = definition;
            }
            else
            {
                existing.DisplayName = definition.DisplayName;
                existing.Description = definition.Description;
                existing.ConnectorKey = definition.ConnectorKey;
                existing.Triggers = definition.Triggers;
                existing.StepsJson = definition.StepsJson;
                existing.IsEnabled = definition.IsEnabled;
                existing.UpdatedAtUtc = now;
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved Remote Control pipeline {PipelineKey} with {StepCount} action step(s); argument templates were omitted from logs.", existing.Key, steps.Count);
            return Clone(existing);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Saving a Remote Control pipeline was cancelled.");
            else logger.LogError(exception, "Saving a Remote Control pipeline failed; action templates were omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs delete as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
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
            var existing = await db.RemoteControlPipelineDefinitions.SingleOrDefaultAsync(item => item.Key == normalizedKey, cancellationToken).ConfigureAwait(false);
            if (existing is null) return false;
            var wrapperReference = await db.UserDxAiFunctionDefinitions.AsNoTracking()
                .AnyAsync(item => item.PipelineKey == normalizedKey, cancellationToken).ConfigureAwait(false);
            if (wrapperReference)
                throw new InvalidOperationException($"Remote Control pipeline '{normalizedKey}' is referenced by a user-owned DXFunction. Delete or retarget that user function first.");
            db.RemoteControlPipelineDefinitions.Remove(existing);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Deleted Remote Control pipeline {PipelineKey}.", normalizedKey);
            return true;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Deleting Remote Control pipeline {PipelineKey} was cancelled.", key);
            else logger.LogError(exception, "Deleting Remote Control pipeline {PipelineKey} failed.", key);
            throw;
        }
    }

    /// <summary>
    /// Performs execute as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<RemoteControlPipelineExecutionResult> ExecuteAsync(
        string key,
        RemoteControlPayload payload,
        bool automaticInvocation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(payload);
            var pipeline = await GetAsync(key, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Remote Control pipeline '{key}' was not found.");
            if (!pipeline.IsEnabled) throw new InvalidOperationException($"Remote Control pipeline '{pipeline.Key}' is disabled.");
            var steps = ParseSteps(pipeline.StepsJson);
            var audit = await executionStore.StartAsync(payload.ConnectorKey, pipeline.Key, payload.Trigger, payload.PayloadBytes, payload.HttpStatusCode, cancellationToken).ConfigureAwait(false);
            var result = new RemoteControlPipelineExecutionResult
            {
                ExecutionId = audit.Id,
                PipelineKey = pipeline.Key,
                ConnectorKey = payload.ConnectorKey,
                Trigger = payload.Trigger,
                Status = "Running"
            };
            var completedSteps = new Dictionary<string, RemoteControlPipelineStepResult>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var step in steps)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var stepResult = await ExecuteStepAsync(step, payload, completedSteps, automaticInvocation, cancellationToken).ConfigureAwait(false);
                    result.Steps.Add(stepResult);
                    completedSteps[step.Key] = stepResult;
                    if (!stepResult.Succeeded && !step.ContinueOnFailure) break;
                }
                result.Succeeded = result.Steps.Count == steps.Count && result.Steps.All(item => item.Succeeded || steps.First(step => step.Key == item.StepKey).ContinueOnFailure);
                result.Status = result.Succeeded ? "Completed" : "Failed";
                result.Error = result.Succeeded ? string.Empty : result.Steps.LastOrDefault(item => !item.Succeeded)?.Error ?? "One or more Remote Control steps failed.";
                await UpdatePipelineStatusAsync(pipeline.Key, result.Succeeded, result.Status, result.Error, cancellationToken).ConfigureAwait(false);
                await executionStore.CompleteAsync(audit.Id, result.Succeeded, result.Steps.Count, result.Status, result.Error, cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Remote Control pipeline {PipelineKey} completed with status {Status} after {StepCount} step(s).", pipeline.Key, result.Status, result.Steps.Count);
                return result;
            }
            catch (Exception exception)
            {
                await UpdatePipelineStatusAsync(pipeline.Key, false, "Failed", exception.Message, cancellationToken).ConfigureAwait(false);
                await executionStore.CompleteAsync(audit.Id, false, result.Steps.Count, "Failed", exception.Message, cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Executing Remote Control pipeline {PipelineKey} was cancelled.", key);
            else logger.LogError(exception, "Executing Remote Control pipeline {PipelineKey} failed; payload and action arguments were omitted.", key);
            throw;
        }
    }

    /// <summary>
    /// Executes matching as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<RemoteControlPipelineExecutionResult>> ExecuteMatchingAsync(RemoteControlPayload payload, bool automaticInvocation, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(payload);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var connectorKey = NormalizeKey(payload.ConnectorKey);
            var candidates = await db.RemoteControlPipelineDefinitions.AsNoTracking()
                .Where(item => item.IsEnabled && item.ConnectorKey == connectorKey)
                .OrderBy(item => item.DisplayName)
                .ThenBy(item => item.Key)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var matching = candidates.Where(item => (item.Triggers & payload.Trigger) != 0).ToList();
            var results = new List<RemoteControlPipelineExecutionResult>(matching.Count);
            foreach (var pipeline in matching)
                results.Add(await ExecuteAsync(pipeline.Key, payload, automaticInvocation, cancellationToken).ConfigureAwait(false));
            return results;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Executing matching Remote Control pipelines was cancelled for connector {ConnectorKey}.", payload?.ConnectorKey);
            else logger.LogError(exception, "Executing matching Remote Control pipelines failed for connector {ConnectorKey}; payload content was omitted.", payload?.ConnectorKey);
            throw;
        }
    }

    /// <summary>
    /// Lists targets as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<DxAiFunctionCatalogEntry>> ListTargetsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await catalog.GetEntriesAsync(cancellationToken).ConfigureAwait(false);
            return entries
                .Where(item => item.IsAvailable
                    && item.IsEnabled
                    && !item.FunctionName.StartsWith("localgpt.remote_control.", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(item.Source, "UserDxFunction", StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.DisplayName)
                .ThenBy(item => item.CatalogKey)
                .ToList();
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Listing selectable Remote Control pipeline targets was cancelled.");
            else logger.LogError(exception, "Listing selectable Remote Control pipeline targets failed.");
            throw;
        }
    }

    /// <summary>
    /// Parses steps as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public IReadOnlyList<RemoteControlPipelineStepDefinition> ParseSteps(string stepsJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(stepsJson)) return [];
            var steps = JsonSerializer.Deserialize<List<RemoteControlPipelineStepDefinition>>(stepsJson, JsonOptions) ?? [];
            if (steps.Count > 32) throw new InvalidDataException("A Remote Control pipeline may contain at most 32 action steps.");
            return steps;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing Remote Control pipeline steps failed; step definitions were omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Executes step as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="step">Step value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="payload">Payload value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="completedSteps">Remote control pipeline step result dependency used by the remote control pipeline workflow to provide the corresponding application capability.</param>
    /// <param name="automaticInvocation">Value indicating whether automatic invocation should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The remote control pipeline step result produced by the operation.</returns>
    private async Task<RemoteControlPipelineStepResult> ExecuteStepAsync(
        RemoteControlPipelineStepDefinition step,
        RemoteControlPayload payload,
        IReadOnlyDictionary<string, RemoteControlPipelineStepResult> completedSteps,
        bool automaticInvocation,
        CancellationToken cancellationToken)
    {
        try
        {
            var target = await ResolveTargetAsync(step, cancellationToken).ConfigureAwait(false);
            if (!target.Entry.IsAvailable || !target.Entry.IsEnabled)
                return new RemoteControlPipelineStepResult { StepKey = step.Key, FunctionName = target.FunctionName, Status = "Disabled", Error = "The target DXFunction Catalog entry is unavailable or disabled." };
            if (target.FunctionName.StartsWith("localgpt.remote_control.", StringComparison.OrdinalIgnoreCase))
                return new RemoteControlPipelineStepResult { StepKey = step.Key, FunctionName = target.FunctionName, Status = "ControlPlaneRecursionDenied", Error = "Remote Control pipelines may not invoke Remote Control control-plane DXFunctions." };
            if (string.Equals(target.Entry.Source, "UserDxFunction", StringComparison.OrdinalIgnoreCase))
                return new RemoteControlPipelineStepResult { StepKey = step.Key, FunctionName = target.FunctionName, Status = "WrapperRecursionDenied", Error = "Remote Control pipelines may not invoke user-owned wrapper DXFunctions. Compose their underlying actions directly instead." };

            var resolvedTemplate = await templates.ResolveAsync(step.ArgumentsTemplateJson, payload, completedSteps, cancellationToken).ConfigureAwait(false);
            using var argumentsDocument = JsonDocument.Parse(string.IsNullOrWhiteSpace(resolvedTemplate) ? "{}" : resolvedTemplate);
            var arguments = argumentsDocument.RootElement.Clone();
            if (arguments.ValueKind != JsonValueKind.Object) throw new InvalidDataException($"Remote Control step '{step.Key}' arguments must resolve to a JSON object.");

            JsonElement invocationParameters;
            if (target.IsPublicServiceMethod)
            {
                invocationParameters = JsonSerializer.SerializeToElement(new PublicServiceInvocationEnvelope
                {
                    CatalogKey = target.Entry.CatalogKey,
                    Parameters = arguments
                }, JsonOptions);
            }
            else
            {
                invocationParameters = arguments;
            }

            var invocation = await registry.InvokeAsync(target.FunctionName, new DxAiFunctionInvocationRequest
            {
                Parameters = invocationParameters,
                AutomaticInvocation = automaticInvocation,
                UserConfirmed = false,
                RequestedBy = $"RemoteControlPipeline:{step.Key}"
            }, cancellationToken).ConfigureAwait(false);
            return new RemoteControlPipelineStepResult
            {
                StepKey = step.Key,
                FunctionName = target.FunctionName,
                Succeeded = invocation.Succeeded,
                Status = invocation.Status,
                Value = invocation.Value,
                Error = Bound(invocation.Error, 1_024)
            };
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Remote Control pipeline step {StepKey} was cancelled.", step.Key);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Remote Control pipeline step {StepKey} failed; action arguments were omitted.", step.Key);
            return new RemoteControlPipelineStepResult
            {
                StepKey = step.Key,
                FunctionName = step.FunctionName,
                Succeeded = false,
                Status = "Failed",
                Error = Bound(exception.Message, 1_024)
            };
        }
    }

    /// <summary>
    /// Normalizes and validate steps as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="steps">Remote control pipeline step definition dependency used by the remote control pipeline workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task NormalizeAndValidateStepsAsync(IReadOnlyList<RemoteControlPipelineStepDefinition> steps, CancellationToken cancellationToken)
    {
        try
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var step in steps)
            {
                step.Key = NormalizeKey(step.Key);
                step.DisplayName = Bound(step.DisplayName, 160, step.Key);
                step.TargetCatalogKey = Bound(step.TargetCatalogKey, 256);
                step.FunctionName = Bound(step.FunctionName, 256);
                if (!keys.Add(step.Key)) throw new InvalidDataException($"Remote Control pipeline step key '{step.Key}' is duplicated.");
                if (string.IsNullOrWhiteSpace(step.TargetCatalogKey) && string.IsNullOrWhiteSpace(step.FunctionName))
                    throw new InvalidDataException($"Remote Control pipeline step '{step.Key}' must reference a DXFunction catalog key or function name.");
                if ((step.ArgumentsTemplateJson?.Length ?? 0) > 65_536) throw new InvalidDataException($"Remote Control pipeline step '{step.Key}' argument template is too large.");
                var target = await ResolveTargetAsync(step, cancellationToken).ConfigureAwait(false);
                if (target.FunctionName.StartsWith("localgpt.remote_control.", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Remote Control step '{step.Key}' cannot target a Remote Control control-plane DXFunction.");
                if (string.Equals(target.Entry.Source, "UserDxFunction", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Remote Control step '{step.Key}' cannot target a user-owned wrapper DXFunction. Add the underlying action steps to this pipeline instead to avoid recursive wrapper graphs.");
                step.ArgumentsTemplateJson = string.IsNullOrWhiteSpace(step.ArgumentsTemplateJson) ? "{}" : step.ArgumentsTemplateJson.Trim();
                try
                {
                    var probe = await templates.ResolveAsync(step.ArgumentsTemplateJson, new RemoteControlPayload
                    {
                        ConnectorKey = "validation",
                        Trigger = RemoteControlTriggerKind.Manual,
                        RawText = "{}",
                        Json = JsonSerializer.SerializeToElement(new Dictionary<string, object?>())
                    }, new Dictionary<string, RemoteControlPipelineStepResult>(), cancellationToken).ConfigureAwait(false);
                    using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(probe) ? "{}" : probe);
                    if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException($"Remote Control pipeline step '{step.Key}' argument template must resolve to a JSON object.");
                }
                catch (KeyNotFoundException)
                {
                    // Runtime payload/step tokens can legitimately be unresolved at save time.
                    // Target existence, normalized step identity, and template size are still validated for every step;
                    // the fully resolved JSON object is validated again at execution time.
                }
            }
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Validating Remote Control pipeline steps was cancelled.");
            else logger.LogError(exception, "Validating Remote Control pipeline steps failed; action templates were omitted.");
            throw;
        }
    }

    /// <summary>
    /// Resolves target as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="step">Step value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The resolved target produced by the operation.</returns>
    private async Task<ResolvedTarget> ResolveTargetAsync(RemoteControlPipelineStepDefinition step, CancellationToken cancellationToken)
    {
        try
        {
            DxAiFunctionCatalogEntry? entry;
            if (!string.IsNullOrWhiteSpace(step.TargetCatalogKey))
                entry = await catalog.GetEntryAsync(step.TargetCatalogKey.Trim(), cancellationToken).ConfigureAwait(false);
            else
                entry = await catalog.GetByFunctionNameAsync(step.FunctionName.Trim(), cancellationToken).ConfigureAwait(false);
            if (entry is null) throw new InvalidDataException($"Remote Control step '{step.Key}' references a DXFunction Catalog entry that does not exist.");
            var isServiceMethod = !string.IsNullOrWhiteSpace(entry.ServiceMethodName) || string.Equals(entry.Source, "PublicServiceMethodCatalog", StringComparison.OrdinalIgnoreCase);
            var functionName = isServiceMethod ? "localgpt.public_service.invoke" : entry.FunctionName;
            if (string.IsNullOrWhiteSpace(functionName)) throw new InvalidDataException($"Remote Control step '{step.Key}' does not resolve to an invokable DXFunction.");
            return new ResolvedTarget(entry, functionName, isServiceMethod);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Resolving Remote Control pipeline target {StepKey} was cancelled.", step.Key);
            else logger.LogError(exception, "Resolving Remote Control pipeline target {StepKey} failed.", step.Key);
            throw;
        }
    }

    /// <summary>
    /// Updates pipeline status as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="succeeded">Value indicating whether succeeded should apply to this operation.</param>
    /// <param name="status">Status value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="error">Error value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task UpdatePipelineStatusAsync(string key, bool succeeded, string status, string error, CancellationToken cancellationToken)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var row = await db.RemoteControlPipelineDefinitions.SingleOrDefaultAsync(item => item.Key == key, cancellationToken).ConfigureAwait(false);
            if (row is null) return;
            row.LastAttemptUtc = DateTime.UtcNow;
            if (succeeded) row.LastSuccessUtc = row.LastAttemptUtc;
            row.LastStatus = Bound(status, 256);
            row.LastError = Bound(error, 1_024);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException) logger.LogDebug(exception, "Updating Remote Control pipeline status {PipelineKey} was cancelled.", key);
            else logger.LogError(exception, "Updating Remote Control pipeline status {PipelineKey} failed.", key);
            throw;
        }
    }

    /// <summary>
    /// Normalizes steps JSON as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="stepsJson">Steps json value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeStepsJson(string stepsJson)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(stepsJson)) return "[]";
            if (stepsJson.Length > 262_144) throw new InvalidDataException("Remote Control pipeline step JSON is too large.");
            var steps = JsonSerializer.Deserialize<List<RemoteControlPipelineStepDefinition>>(stepsJson, JsonOptions) ?? [];
            return JsonSerializer.Serialize(steps, JsonOptions);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing Remote Control pipeline JSON failed; step definitions were omitted.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes key as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the remote control pipeline operation and used when producing its result.</param>
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
            logger.LogError(exception, "Normalizing a Remote Control pipeline key failed; key content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs bound as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="maximumLength">Maximum length value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the remote control pipeline operation and used when producing its result.</param>
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
            logger.LogError(exception, "Bounding Remote Control pipeline text failed; content was omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs clone as part of the remote control pipeline service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <returns>The remote control pipeline definition produced by the operation.</returns>
    private RemoteControlPipelineDefinition Clone(RemoteControlPipelineDefinition source)
    {
        try
        {
            return new RemoteControlPipelineDefinition
            {
                Id = source.Id,
                Key = source.Key,
                DisplayName = source.DisplayName,
                Description = source.Description,
                ConnectorKey = source.ConnectorKey,
                Triggers = source.Triggers,
                StepsJson = source.StepsJson,
                IsEnabled = source.IsEnabled,
                CreatedAtUtc = source.CreatedAtUtc,
                UpdatedAtUtc = source.UpdatedAtUtc,
                LastAttemptUtc = source.LastAttemptUtc,
                LastSuccessUtc = source.LastSuccessUtc,
                LastStatus = source.LastStatus,
                LastError = source.LastError
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cloning a Remote Control pipeline failed.");
            throw;
        }
    }

    /// <summary>
    /// Represents a resolved target helper type nested within <see cref="RemoteControlPipelineService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="Entry">Entry value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="FunctionName">Function name value supplied to the remote control pipeline operation and used when producing its result.</param>
    /// <param name="IsPublicServiceMethod">Value indicating whether public service method should apply to this operation.</param>
    private sealed record ResolvedTarget(DxAiFunctionCatalogEntry Entry, string FunctionName, bool IsPublicServiceMethod);

    /// <summary>
    /// Represents a public service invocation envelope helper type nested within <see cref="RemoteControlPipelineService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    private sealed class PublicServiceInvocationEnvelope
    {
        /// <summary>
        /// Gets or sets the stable catalog key used to identify or correlate this public service invocation envelope instance with related application state.
        /// </summary>
        /// <value>The catalog key value exposed by <see cref="PublicServiceInvocationEnvelope"/>.</value>
        public string CatalogKey { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the parameters value that forms part of the public service invocation envelope state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The parameters value exposed by <see cref="PublicServiceInvocationEnvelope"/>.</value>
        public JsonElement Parameters { get; set; }
    }
}
