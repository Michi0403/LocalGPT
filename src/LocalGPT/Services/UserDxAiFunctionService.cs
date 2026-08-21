using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Persists user-owned DXFunctions and exposes a synchronized runtime descriptor cache implemented by Remote Control pipelines.</summary>
/// <param name="dbContextFactory">Local gpt memory database context dependency used by the user DevExpress AI function workflow to provide the corresponding application capability.</param>
/// <param name="databaseInitializer">Database initialization service dependency used by the user DevExpress AI function workflow to provide the corresponding application capability.</param>
/// <param name="scopeFactory">Service scope factory dependency used by the user DevExpress AI function workflow to provide the corresponding application capability.</param>
/// <param name="regexCompilation">Regex compilation service dependency used by the user DevExpress AI function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class UserDxAiFunctionService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    IServiceScopeFactory scopeFactory,
    IRegexCompilationService regexCompilation,
    ILogger<UserDxAiFunctionService> logger) : IUserDxAiFunctionService
{
    /// <summary>
    /// Stores the internal cache gate state used by <see cref="UserDxAiFunctionService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object cacheGate = new();
    /// <summary>
    /// Stores the in-memory cached definitions collection maintained internally by <see cref="UserDxAiFunctionService"/> for its current workflow state.
    /// </summary>
    private IReadOnlyDictionary<string, UserDxAiFunctionDefinition> cachedDefinitions = new Dictionary<string, UserDxAiFunctionDefinition>(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Stores the internal name pattern state used by <see cref="UserDxAiFunctionService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly System.Text.RegularExpressions.Regex namePattern = regexCompilation.Compile("^user\\.[a-z0-9][a-z0-9._-]{0,118}$", "c", TimeSpan.FromSeconds(2), nameof(UserDxAiFunctionService));

    /// <summary>
    /// Performs refresh as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var rows = await db.UserDxAiFunctionDefinitions.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
            lock (cacheGate)
                cachedDefinitions = rows.ToDictionary(item => item.FunctionName, Clone, StringComparer.OrdinalIgnoreCase);
            logger.LogInformation("Refreshed {FunctionCount} user-owned DXFunction definition(s).", rows.Count);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Refreshing user DXFunctions was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Refreshing user DXFunctions failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs list as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserDxAiFunctionDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            return await db.UserDxAiFunctionDefinitions.AsNoTracking()
                .OrderBy(item => item.DisplayName)
                .ThenBy(item => item.FunctionName)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Listing user DXFunctions was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing user DXFunctions failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs get as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<UserDxAiFunctionDefinition?> GetAsync(string functionName, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalized = NormalizeName(functionName);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            return await db.UserDxAiFunctionDefinitions.AsNoTracking().SingleOrDefaultAsync(item => item.FunctionName == normalized, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Loading user DXFunction {FunctionName} was cancelled.", functionName);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Loading user DXFunction {FunctionName} failed.", functionName);
            throw;
        }
    }

    /// <summary>
    /// Performs save as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<UserDxAiFunctionDefinition> SaveAsync(SaveUserDxAiFunctionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Saving a user-owned DXFunction requires explicit local confirmation.");
            var functionName = NormalizeName(request.FunctionName);
            var pipelineKey = NormalizePipelineKey(request.PipelineKey);
            ValidateParameterSchema(request.ParameterSchemaJson);

            using (var validationScope = scopeFactory.CreateScope())
            {
                var pipelines = validationScope.ServiceProvider.GetRequiredService<IRemoteControlPipelineService>();
                var pipeline = await pipelines.GetAsync(pipelineKey, cancellationToken).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Remote Control pipeline '{pipelineKey}' was not found.");
                if (!pipeline.IsEnabled)
                    throw new InvalidOperationException($"Remote Control pipeline '{pipelineKey}' must be enabled before a user DXFunction can reference it.");
            }

            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            UserDxAiFunctionDefinition? row = null;
            if (request.Id is Guid id)
                row = await db.UserDxAiFunctionDefinitions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken).ConfigureAwait(false);
            row ??= await db.UserDxAiFunctionDefinitions.SingleOrDefaultAsync(item => item.FunctionName == functionName, cancellationToken).ConfigureAwait(false);
            var now = DateTime.UtcNow;
            if (row is null)
            {
                row = new UserDxAiFunctionDefinition { Id = Guid.NewGuid(), CreatedAtUtc = now };
                db.UserDxAiFunctionDefinitions.Add(row);
            }
            row.FunctionName = functionName;
            row.DisplayName = Bound(request.DisplayName, 160, functionName);
            row.Purpose = Bound(request.Purpose, 2000, "User-owned composed LocalGPT capability.");
            row.SafetyNotes = Bound(request.SafetyNotes, 2000, "This user-defined function executes the referenced Remote Control pipeline through LocalGPT's existing DXFunction registry and approval policy.");
            row.ParameterSchemaJson = Bound(request.ParameterSchemaJson, 32000, "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":true}");
            row.PipelineKey = pipelineKey;
            row.IsEnabled = request.IsEnabled;
            row.AvailableToAi = request.AvailableToAi;
            row.IsReadOnly = request.IsReadOnly;
            row.RequiresHumanConfirmation = request.RequiresHumanConfirmation;
            row.SupportsAutomaticInvocation = request.SupportsAutomaticInvocation;
            row.UpdatedAtUtc = now;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await RefreshAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved user-owned DXFunction {FunctionName} backed by Remote Control pipeline {PipelineKey}.", row.FunctionName, row.PipelineKey);
            return Clone(row);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Saving a user DXFunction was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving a user DXFunction failed; parameter schema and pipeline payload were omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs delete as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string functionName, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Deleting a user-owned DXFunction requires explicit local confirmation.");
            var normalized = NormalizeName(functionName);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var row = await db.UserDxAiFunctionDefinitions.SingleOrDefaultAsync(item => item.FunctionName == normalized, cancellationToken).ConfigureAwait(false);
            if (row is null)
                return false;
            db.UserDxAiFunctionDefinitions.Remove(row);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await RefreshAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Deleted user-owned DXFunction {FunctionName}.", normalized);
            return true;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Deleting user DXFunction {FunctionName} was cancelled.", functionName);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Deleting user DXFunction {FunctionName} failed.", functionName);
            throw;
        }
    }

    /// <summary>
    /// Retrieves descriptors as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public IReadOnlyList<DxaichatFunctionInfo> GetDescriptors()
    {
        try
        {
            IReadOnlyList<UserDxAiFunctionDefinition> rows;
            lock (cacheGate)
                rows = cachedDefinitions.Values.Where(item => item.IsEnabled).Select(Clone).ToList();
            return rows.Select(ToDescriptor).OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading cached user DXFunction descriptors failed.");
            throw;
        }
    }

    /// <summary>
    /// Attempts to retrieve descriptor as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public bool TryGetDescriptor(string functionName, out DxaichatFunctionInfo descriptor)
    {
        try
        {
            descriptor = default!;
            UserDxAiFunctionDefinition? row;
            lock (cacheGate)
                cachedDefinitions.TryGetValue(functionName, out row);
            if (row is null || !row.IsEnabled)
                return false;
            descriptor = ToDescriptor(row);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving cached user DXFunction descriptor {FunctionName} failed.", functionName);
            throw;
        }
    }

    /// <summary>
    /// Performs invoke as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(string functionName, DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            UserDxAiFunctionDefinition? row;
            lock (cacheGate)
                cachedDefinitions.TryGetValue(functionName, out row);
            if (row is null || !row.IsEnabled)
                return new DxAiFunctionInvocationResult { FunctionName = functionName, Succeeded = false, Status = "NotFound", Error = "The user-owned DXFunction is no longer enabled." };

            using var scope = scopeFactory.CreateScope();
            var pipelines = scope.ServiceProvider.GetRequiredService<IRemoteControlPipelineService>();
            var pipeline = await pipelines.GetAsync(row.PipelineKey, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Remote Control pipeline '{row.PipelineKey}' was not found.");
            if (pipeline.ConnectorKey.StartsWith("user-source.", StringComparison.OrdinalIgnoreCase) &&
                pipelines.ParseSteps(pipeline.StepsJson).Count == 0)
            {
                var connectors = scope.ServiceProvider.GetRequiredService<IRemoteControlConnectorService>();
                var sourcePayload = await connectors.PullAsync(pipeline.ConnectorKey, runPipelines: false, automaticInvocation: request.AutomaticInvocation, cancellationToken: cancellationToken).ConfigureAwait(false);
                return new DxAiFunctionInvocationResult
                {
                    FunctionName = functionName,
                    Succeeded = true,
                    Status = "Completed",
                    Value = sourcePayload.Json is JsonElement selectedJson ? selectedJson.Clone() : sourcePayload.RawText
                };
            }

            var raw = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? "{}" : request.Parameters.GetRawText();
            var json = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? JsonSerializer.SerializeToElement(new Dictionary<string, object?>())
                : request.Parameters.Clone();
            var payload = new RemoteControlPayload
            {
                ConnectorKey = string.Empty,
                Trigger = RemoteControlTriggerKind.Manual,
                ContentType = "application/json",
                RawText = raw,
                Json = json,
                PayloadBytes = Encoding.UTF8.GetByteCount(raw)
            };
            var execution = await pipelines.ExecuteAsync(row.PipelineKey, payload, request.AutomaticInvocation, cancellationToken).ConfigureAwait(false);
            return new DxAiFunctionInvocationResult
            {
                FunctionName = functionName,
                Succeeded = execution.Succeeded,
                Status = execution.Status,
                Error = execution.Error,
                Value = execution
            };
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Invoking user DXFunction {FunctionName} was cancelled.", functionName);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Invoking user DXFunction {FunctionName} failed; invocation payload was omitted from logs.", functionName);
            return new DxAiFunctionInvocationResult { FunctionName = functionName, Succeeded = false, Status = "Failed", Error = "The user-owned DXFunction failed. Review LocalGPT logs." };
        }
    }

    /// <summary>
    /// Normalizes name as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the user DevExpress AI function operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeName(string value)
    {
        try
        {
            var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!namePattern.IsMatch(normalized))
                throw new ArgumentException("User DXFunction names must start with 'user.' and contain only lowercase letters, digits, dots, underscores, or hyphens.", nameof(value));
            return normalized;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing a user DXFunction name failed; source value omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes pipeline key as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the user DevExpress AI function operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizePipelineKey(string value)
    {
        try
        {
            var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ArgumentException("A Remote Control pipeline key is required.", nameof(value));
            return normalized;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Normalizing a user DXFunction pipeline key failed; source value omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Validates parameter schema as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="schema">Schema value supplied to the user DevExpress AI function operation and used when producing its result.</param>
    private void ValidateParameterSchema(string schema)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(schema) ? "{}" : schema);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new JsonException("DXFunction parameter schema must be a JSON object.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Validating a user DXFunction parameter schema failed; schema content omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Performs to descriptor as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="row">Row value supplied to the user DevExpress AI function operation and used when producing its result.</param>
    /// <returns>The dxaichat function info produced by the operation.</returns>
    private DxaichatFunctionInfo ToDescriptor(UserDxAiFunctionDefinition row)
    {
        try
        {
            return new DxaichatFunctionInfo(
                row.FunctionName,
                "POST",
                $"/api/dxai/functions/{row.FunctionName}/invoke",
                row.Purpose,
                "Parameters follow the user-authored JSON schema registered with this function.",
                row.SafetyNotes,
                IsReadOnly: row.IsReadOnly,
                AvailableToAi: row.AvailableToAi,
                RequiresHumanConfirmation: row.RequiresHumanConfirmation,
                SupportsDirectInvocation: true,
                SupportsAutomaticInvocation: row.SupportsAutomaticInvocation,
                Source: "UserDxFunction",
                ParameterSchemaJson: row.ParameterSchemaJson,
                SupportsDeferredApprovalRequest: row.RequiresHumanConfirmation);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating a user DXFunction runtime descriptor failed for {FunctionName}.", row.FunctionName);
            throw;
        }
    }

    /// <summary>
    /// Performs clone as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="row">Row value supplied to the user DevExpress AI function operation and used when producing its result.</param>
    /// <returns>The user DevExpress AI function definition produced by the operation.</returns>
    private UserDxAiFunctionDefinition Clone(UserDxAiFunctionDefinition row)
    {
        try
        {
            return new UserDxAiFunctionDefinition
            {
                Id = row.Id,
                FunctionName = row.FunctionName,
                DisplayName = row.DisplayName,
                Purpose = row.Purpose,
                SafetyNotes = row.SafetyNotes,
                ParameterSchemaJson = row.ParameterSchemaJson,
                PipelineKey = row.PipelineKey,
                IsEnabled = row.IsEnabled,
                AvailableToAi = row.AvailableToAi,
                IsReadOnly = row.IsReadOnly,
                RequiresHumanConfirmation = row.RequiresHumanConfirmation,
                SupportsAutomaticInvocation = row.SupportsAutomaticInvocation,
                CreatedAtUtc = row.CreatedAtUtc,
                UpdatedAtUtc = row.UpdatedAtUtc
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cloning a user DXFunction definition failed for {FunctionName}.", row.FunctionName);
            throw;
        }
    }

    /// <summary>
    /// Performs bound as part of the user DevExpress AI function service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the user DevExpress AI function operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the user DevExpress AI function operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the user DevExpress AI function operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Bound(string? value, int maximum, string fallback)
    {
        try
        {
            var text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            return text.Length <= maximum ? text : text[..maximum];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bounding user DXFunction text failed; content omitted from logs.");
            throw;
        }
    }

}
