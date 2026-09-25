using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using LocalGPT.Runtime.Plugins;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>Owns persisted runtime extensions and exposes them through the shared LocalGPT DXFunction policy surface.</summary>
/// <param name="dbContextFactory">Database context factory used to persist runtime extensions.</param>
/// <param name="databaseInitializer">Database initialization dependency.</param>
/// <param name="executionHost">Runtime host that materializes and invokes explicitly loaded executable payloads.</param>
/// <param name="definitionSupport">Validation and persistence-copy helpers for runtime extension definitions.</param>
/// <param name="logger">Logger used for bounded operational diagnostics.</param>
public sealed class RuntimePluginService(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDatabaseInitializationService databaseInitializer,
    RuntimePluginExecutionHost executionHost,
    RuntimePluginDefinitionSupport definitionSupport,
    ILogger<RuntimePluginService> logger) : IRuntimePluginService
{
    private readonly object cacheGate = new();
    private IReadOnlyDictionary<string, RuntimePluginDefinition> cachedDefinitions = new Dictionary<string, RuntimePluginDefinition>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var rows = await db.RuntimePluginDefinitions.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
            lock (cacheGate)
                cachedDefinitions = rows.ToDictionary(item => item.FunctionName, definitionSupport.Clone, StringComparer.OrdinalIgnoreCase);
            foreach (var disabled in rows.Where(item => !item.IsEnabled).Select(item => item.Id))
                executionHost.Unload(disabled);
            logger.LogInformation("Refreshed {PluginCount} persisted runtime extension definition(s).", rows.Count);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Refreshing runtime extensions was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Refreshing runtime extensions failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuntimePluginDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            return await db.RuntimePluginDefinitions.AsNoTracking().OrderBy(item => item.Name).ThenBy(item => item.FunctionName).ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Listing runtime extensions was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Listing runtime extensions failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<RuntimePluginDefinition> SaveAsync(SaveRuntimePluginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.UserConfirmed)
                throw new InvalidOperationException("Saving executable runtime extension content requires explicit local confirmation.");
            var source = request.Plugin ?? throw new ArgumentException("Plugin definition is required.", nameof(request));
            definitionSupport.NormalizeAndValidate(source);
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            if (await db.RuntimePluginDefinitions.AnyAsync(item => item.Id != source.Id && item.FunctionName == source.FunctionName, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException($"Runtime extension function '{source.FunctionName}' already exists.");
            var row = await db.RuntimePluginDefinitions.SingleOrDefaultAsync(item => item.Id == source.Id, cancellationToken).ConfigureAwait(false);
            if (row is null)
            {
                row = new RuntimePluginDefinition { Id = source.Id == Guid.Empty ? Guid.NewGuid() : source.Id, CreatedAtUtc = DateTime.UtcNow };
                db.RuntimePluginDefinitions.Add(row);
            }
            definitionSupport.CopyEditable(source, row);
            row.UpdatedAtUtc = DateTime.UtcNow;
            row.ContentHash = definitionSupport.HashContent(row);
            row.LastBuildStatus = "NotBuilt";
            row.LastBuildMessage = string.Empty;
            row.LastLoadedAtUtc = null;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            executionHost.Unload(row.Id);
            await RefreshAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Saved runtime extension {PluginId} as function {FunctionName}; executable source and package content omitted from logs.", row.Id, row.FunctionName);
            return definitionSupport.Clone(row);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Saving a runtime extension was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Saving a runtime extension failed; executable source and package content omitted from logs.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                throw new InvalidOperationException("Deleting a runtime extension requires explicit local confirmation.");
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var row = await db.RuntimePluginDefinitions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken).ConfigureAwait(false);
            if (row is null)
                return false;
            db.RuntimePluginDefinitions.Remove(row);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            executionHost.Unload(id);
            await RefreshAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Deleted runtime extension {PluginId}.", id);
            return true;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Deleting runtime extension {PluginId} was cancelled.", id);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Deleting runtime extension {PluginId} failed.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<RuntimePluginBuildResult> BuildAndLoadAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!userConfirmed)
                return new RuntimePluginBuildResult { PluginId = id, Status = "HumanConfirmationRequired", Message = "Building or loading executable extension content requires explicit local confirmation." };
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var row = await db.RuntimePluginDefinitions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Runtime extension '{id}' was not found.");
            definitionSupport.NormalizeAndValidate(row);
            var result = row.Kind switch
            {
                RuntimePluginKind.CSharpScript => await executionHost.BuildCSharpAsync(row, await ResolveCompilerAsync(db, row, "DotNet", "dotnet-sdk", cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false),
                RuntimePluginKind.JavaScript => await executionHost.ValidateJavaScriptRuntimeAsync(row, await ResolveCompilerAsync(db, row, "JavaScript", "node", cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false),
                RuntimePluginKind.CompiledAssembly => await executionHost.LoadCompiledPackageAsync(row, cancellationToken).ConfigureAwait(false),
                _ => throw new InvalidOperationException($"Unsupported runtime extension kind '{row.Kind}'.")
            };
            row.LastBuildStatus = result.Status;
            row.LastBuildMessage = definitionSupport.Bound(result.Message, 4000);
            row.LastLoadedAtUtc = result.Succeeded ? DateTime.UtcNow : null;
            row.ContentHash = definitionSupport.HashContent(row);
            row.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await RefreshAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Runtime extension {PluginId} build/load finished with status {Status}.", id, result.Status);
            return result;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Building/loading runtime extension {PluginId} was cancelled.", id);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building/loading runtime extension {PluginId} failed.", id);
            return new RuntimePluginBuildResult { PluginId = id, Status = "BuildFailed", Message = definitionSupport.Bound(exception.Message, 4000) };
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<DxaichatFunctionInfo> GetDescriptors()
    {
        try
        {
            lock (cacheGate)
                return cachedDefinitions.Values.Where(item => item.IsEnabled).Select(ToDescriptor).OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading runtime extension DXFunction descriptors failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public bool TryGetDescriptor(string functionName, out DxaichatFunctionInfo descriptor)
    {
        try
        {
            lock (cacheGate)
            {
                if (cachedDefinitions.TryGetValue(functionName, out var definition) && definition.IsEnabled)
                {
                    descriptor = ToDescriptor(definition);
                    return true;
                }
            }
            descriptor = null!;
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving runtime extension descriptor for {FunctionName} failed.", functionName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(string functionName, DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
            ArgumentNullException.ThrowIfNull(request);
            RuntimePluginDefinition definition;
            lock (cacheGate)
            {
                if (!cachedDefinitions.TryGetValue(functionName, out definition!) || !definition.IsEnabled)
                    return Failure(functionName, request, "NotFound", "No enabled runtime extension exists with this function name.");
                definition = definitionSupport.Clone(definition);
            }
            var parameters = ParseParameters(request.Parameters);
            string value;
            if (definition.Kind == RuntimePluginKind.JavaScript)
            {
                await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
                await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var compiler = await ResolveCompilerAsync(db, definition, "JavaScript", "node", cancellationToken).ConfigureAwait(false);
                value = await executionHost.InvokeJavaScriptAsync(definition, compiler, parameters, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                value = await executionHost.InvokeLoadedAsync(definition, parameters, cancellationToken).ConfigureAwait(false);
            }
            logger.LogInformation("Runtime extension function {FunctionName} completed for operation {OperationId}; executable content and parameters omitted from logs.", functionName, request.OperationId);
            return Success(functionName, request, ParseValue(value));
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Runtime extension function {FunctionName} was cancelled.", functionName);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Runtime extension function {FunctionName} failed; executable content and parameters omitted from logs.", functionName);
            return Failure(functionName, request, "ExecutionFailed", definitionSupport.Bound(exception.Message, 4000));
        }
    }

    /// <inheritdoc />
    public string GetTemplate(RuntimePluginKind kind)
    {
        try
        {
            var template = kind switch
            {
                RuntimePluginKind.CSharpScript => "var name = parameters.TryGetProperty(\"name\", out var value) ? value.GetString() : \"world\";\nreturn $\"Hello {name} from a LocalGPT runtime extension.\";",
                RuntimePluginKind.JavaScript => "let input = '';\nprocess.stdin.setEncoding('utf8');\nprocess.stdin.on('data', chunk => input += chunk);\nprocess.stdin.on('end', () => {\n  const parameters = input ? JSON.parse(input) : {};\n  process.stdout.write(JSON.stringify({ ok: true, parameters }));\n});",
                RuntimePluginKind.CompiledAssembly => string.Empty,
                _ => string.Empty
            };
            logger.LogDebug("Returned runtime extension starter template for kind {PluginKind}.", kind);
            return template;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating runtime extension starter template for kind {PluginKind} failed.", kind);
            throw;
        }
    }

    private async Task<ProjectCompilerInstallation> ResolveCompilerAsync(LocalGptMemoryDbContext db, RuntimePluginDefinition definition, string language, string profileKey, CancellationToken cancellationToken)
    {
        try
        {
            ProjectCompilerInstallation? compiler = null;
            if (definition.CompilerInstallationId.HasValue)
            {
                compiler = await db.ProjectCompilerInstallations.AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == definition.CompilerInstallationId && item.IsEnabled, cancellationToken)
                    .ConfigureAwait(false);
                if (compiler is not null
                    && !compiler.KnowledgeProfileKey.Equals(profileKey, StringComparison.OrdinalIgnoreCase)
                    && !compiler.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"The selected toolchain is not compatible with this {language} runtime extension. Choose a matching validated compiler/runtime in Setup → Toolchains.");
                }
            }

            compiler ??= await db.ProjectCompilerInstallations.AsNoTracking()
                .Where(item => item.IsEnabled && item.KnowledgeProfileKey == profileKey)
                .OrderByDescending(item => item.LastValidationSucceeded)
                .ThenByDescending(item => item.IsDefaultForLanguage)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            compiler ??= await db.ProjectCompilerInstallations.AsNoTracking()
                .Where(item => item.IsEnabled && item.IsDefaultForLanguage && item.Language == language)
                .OrderByDescending(item => item.LastValidationSucceeded)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (compiler is null)
                throw new InvalidOperationException($"No enabled {language} toolchain is configured. Use Setup → Toolchains to discover or add one, then select it for this runtime extension.");
            if (!compiler.LastValidationSucceeded)
                throw new InvalidOperationException($"The selected {language} toolchain has not passed validation. Rediscover/validate it in Setup → Toolchains before building this runtime extension.");
            logger.LogDebug("Resolved configured {Language} toolchain {CompilerId} for runtime extension {PluginId}; executable path omitted from logs.", language, compiler.Id, definition.Id);
            return compiler;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Resolving a configured {Language} toolchain for runtime extension {PluginId} was cancelled.", language, definition.Id);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving a configured {Language} toolchain for runtime extension {PluginId} failed; executable path omitted from logs.", language, definition.Id);
            throw;
        }
    }

    private DxaichatFunctionInfo ToDescriptor(RuntimePluginDefinition definition)
    {
        try
        {
            var descriptor = new DxaichatFunctionInfo(
                definition.FunctionName,
                "RUNTIME",
                $"runtime-plugin:{definition.Id:N}",
                definition.Purpose,
                "Parameters follow the persisted JSON schema for this user runtime extension.",
                definition.SafetyNotes,
                definition.IsReadOnly,
                definition.AvailableToAi,
                definition.RequiresHumanConfirmation,
                true,
                definition.SupportsAutomaticInvocation,
                "RuntimePlugin",
                definition.ParameterSchemaJson,
                false,
                definition.RequiresHumanConfirmation,
                definition.RequiresHumanConfirmation);
            logger.LogDebug("Created runtime extension descriptor for {FunctionName}.", definition.FunctionName);
            return descriptor;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating runtime extension descriptor for {FunctionName} failed.", definition.FunctionName);
            throw;
        }
    }

    private JsonElement ParseParameters(JsonElement parameters)
    {
        try
        {
            if (parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                using var empty = JsonDocument.Parse("{}");
                logger.LogDebug("Normalized empty runtime extension parameters.");
                return empty.RootElement.Clone();
            }
            logger.LogDebug("Cloned runtime extension parameters without logging their content.");
            return parameters.Clone();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Preparing runtime extension parameters failed; parameter content omitted from logs.");
            throw;
        }
    }

    private object? ParseValue(string value)
    {
        try
        {
            var trimmed = value?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(trimmed))
                return string.Empty;
            try { return JsonSerializer.Deserialize<JsonElement>(trimmed); }
            catch (JsonException) { return trimmed; }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing runtime extension output failed; output content omitted from logs.");
            throw;
        }
    }

    private DxAiFunctionInvocationResult Success(string functionName, DxAiFunctionInvocationRequest request, object? value)
    {
        try
        {
            logger.LogDebug("Creating completed runtime extension result for {FunctionName}.", functionName);
            return new DxAiFunctionInvocationResult { FunctionName = functionName, OperationId = request.OperationId ?? Guid.NewGuid(), Succeeded = true, Status = "Completed", Value = value };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating completed runtime extension result for {FunctionName} failed.", functionName);
            throw;
        }
    }

    private DxAiFunctionInvocationResult Failure(string functionName, DxAiFunctionInvocationRequest request, string status, string error)
    {
        try
        {
            logger.LogDebug("Creating runtime extension failure result for {FunctionName} with status {Status}.", functionName, status);
            return new DxAiFunctionInvocationResult { FunctionName = functionName, OperationId = request.OperationId ?? Guid.NewGuid(), Status = status, Error = error };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating runtime extension failure result for {FunctionName} failed.", functionName);
            throw;
        }
    }
}
