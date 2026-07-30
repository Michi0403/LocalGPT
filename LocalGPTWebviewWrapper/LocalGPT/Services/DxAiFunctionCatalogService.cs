using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Keeps the user-facing DX function catalog in the existing SystemVariables table. This deliberately avoids
/// destructive schema replacement: old databases receive missing records, while user-edited exposure and
/// confirmation settings survive descriptor refreshes and application upgrades.
/// </summary>
public sealed class DxAiFunctionCatalogService(ILocalGptVocabularyService vocabulary,
    
    IDatabaseInitializationService databaseInitialization,
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDxAiFunctionRegistry registry,
    IOrganicAddonManifestService addonManifests,
    ILogger<DxAiFunctionCatalogService> logger) : IDxAiFunctionCatalogService
{
    private const string DataType = "DxAiFunctionCatalogEntry";
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    // The catalog service is scoped, but boot synchronization, Council preflight, and UI policy edits
    // can run in different scopes against the same SQLite rows. One process-wide gate prevents stale
    // tracked SystemVariable instances from racing each other.
    private readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<IReadOnlyList<DxAiFunctionCatalogEntry>> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        await databaseInitialization.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var variables = await db.SystemVariables
                .Where(item => item.DataType == DataType)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var existing = variables
                .Select(variable => (Variable: variable, Entry: Deserialize(variable.ValueString)))
                .Where(item => item.Entry is not null && !string.IsNullOrWhiteSpace(item.Entry.CatalogKey))
                .ToDictionary(item => item.Entry!.CatalogKey, item => item, StringComparer.OrdinalIgnoreCase);

            var discovered = DiscoverEntries();
            var discoveredKeys = discovered.Select(item => item.CatalogKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var descriptor in discovered)
            {
                if (existing.TryGetValue(descriptor.CatalogKey, out var stored))
                {
                    PreservePolicyAndRefreshDescriptor(stored.Entry!, descriptor);
                    stored.Variable.ValueString = JsonSerializer.Serialize(stored.Entry, JsonOptions);
                    stored.Variable.LastUpdated = DateTime.UtcNow;
                }
                else
                {
                    db.SystemVariables.Add(new SystemVariable
                    {
                        Name = BuildStorageName(descriptor.CatalogKey),
                        DataType = DataType,
                        ValueString = JsonSerializer.Serialize(descriptor, JsonOptions),
                        LastUpdated = DateTime.UtcNow
                    });
                    existing[descriptor.CatalogKey] = (null!, descriptor);
                }
            }

            foreach (var stored in existing.Values.Where(item => item.Variable is not null && !discoveredKeys.Contains(item.Entry!.CatalogKey)))
            {
                stored.Entry!.IsAvailable = false;
                stored.Entry.UpdatedAtUtc = DateTime.UtcNow;
                stored.Entry.UpdatedBy = "LocalGPT runtime catalog";
                stored.Variable.ValueString = JsonSerializer.Serialize(stored.Entry, JsonOptions);
                stored.Variable.LastUpdated = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var result = await ReadEntriesAsync(db, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Synchronized {CatalogCount} DX function/public service catalog entries without replacing user policy.",
                result.Count);
            return result;
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<IReadOnlyList<DxAiFunctionCatalogEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        await databaseInitialization.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var entries = await ReadEntriesAsync(db, cancellationToken).ConfigureAwait(false);
        return entries.Count == 0 ? await SynchronizeAsync(cancellationToken).ConfigureAwait(false) : entries;
    }

    public async Task<DxAiFunctionCatalogEntry?> GetEntryAsync(string catalogKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogKey);
        return (await GetEntriesAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(item => string.Equals(item.CatalogKey, catalogKey, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<DxAiFunctionCatalogEntry?> GetByFunctionNameAsync(string functionName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        return (await GetEntriesAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(item => string.Equals(item.FunctionName, functionName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<DxAiFunctionCatalogEntry> SavePolicyAsync(DxAiFunctionCatalogSaveRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CatalogKey);
        _ = JsonSerializer.Deserialize<List<string>>(request.AllowedPeerIdsJson) ?? [];

        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var variables = await db.SystemVariables.Where(item => item.DataType == DataType).ToListAsync(cancellationToken).ConfigureAwait(false);
            var match = variables
                .Select(variable => (Variable: variable, Entry: Deserialize(variable.ValueString)))
                .FirstOrDefault(item => item.Entry is not null && string.Equals(item.Entry.CatalogKey, request.CatalogKey, StringComparison.OrdinalIgnoreCase));
            if (match.Entry is null || match.Variable is null)
                throw new KeyNotFoundException($"DX function catalog entry '{request.CatalogKey}' was not found. Synchronize the catalog first.");

            match.Entry.IsEnabled = request.IsEnabled;
            match.Entry.ExposeToAiChat = request.ExposeToAiChat;
            match.Entry.ExposeToOneWire = request.ExposeToOneWire;
            match.Entry.AllowRemoteInvocation = request.AllowRemoteInvocation;
            match.Entry.RequiresFrontendConfirmation = request.RequiresFrontendConfirmation;
            match.Entry.InteractionEditor = request.InteractionEditor;
            match.Entry.AllowedPeerIdsJson = NormalizePeersJson(request.AllowedPeerIdsJson);
            match.Entry.IsSystemSeed = false;
            match.Entry.UpdatedAtUtc = DateTime.UtcNow;
            match.Entry.UpdatedBy = string.IsNullOrWhiteSpace(request.UpdatedBy) ? "CurrentUser" : request.UpdatedBy.Trim();
            match.Variable.ValueString = JsonSerializer.Serialize(match.Entry, JsonOptions);
            match.Variable.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return match.Entry;
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<IReadOnlyList<DxAiFunctionCatalogEntry>> GetExposedToPeerAsync(string peerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(peerId);
        return (await GetEntriesAsync(cancellationToken).ConfigureAwait(false))
            .Where(item => item.IsAvailable && item.IsEnabled && item.ExposeToOneWire && item.AllowsPeer(peerId))
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IReadOnlyList<DxAiFunctionCatalogEntry> DiscoverEntries()
    {
        var entries = registry.GetFunctions().Select(CreateDxEntry).ToList();
        entries.AddRange(DiscoverPublicServiceMethods());
        entries.AddRange(addonManifests.GetCatalogEntries());
        return entries.OrderBy(item => item.Kind).ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private DxAiFunctionCatalogEntry CreateDxEntry(DxaichatFunctionInfo function)
    {
        var entry = new DxAiFunctionCatalogEntry
        {
            CatalogKey = $"dx:{function.Name}",
            Kind = vocabulary.Get().CatalogDxFunction,
            FunctionName = function.Name,
            DisplayName = function.Name,
            Purpose = function.Purpose,
            Method = function.Method,
            Route = function.Route,
            ParameterSchemaJson = function.ParameterSchemaJson,
            Source = function.Source,
            IsReadOnly = function.IsReadOnly,
            IsAvailable = true,
            IsEnabled = true,
            ExposeToAiChat = function.AvailableToAi,
            ExposeToOneWire = function.SupportsDirectInvocation,
            AllowRemoteInvocation = function.SupportsDirectInvocation,
            RequiresFrontendConfirmation = function.RequiresHumanConfirmation,
            InteractionEditor = InferEditor(function.Name, function.ParameterSchemaJson, function.RequiresHumanConfirmation),
            IsSystemSeed = true
        };
        entry.DescriptorHash = ComputeDescriptorHash(entry);
        return entry;
    }

    private IEnumerable<DxAiFunctionCatalogEntry> DiscoverPublicServiceMethods()
    {
        var assembly = typeof(Program).Assembly;
        foreach (var implementation in assembly.DefinedTypes
            .Where(type => type is { IsClass: true, IsAbstract: false, IsPublic: true } &&
                type.Namespace?.StartsWith("LocalGPT.Services", StringComparison.Ordinal) == true))
        {
            foreach (var method in implementation.DeclaredMethods.Where(IsSupportedPublicMethod))
            {
                var contract = ResolveContract(implementation.AsType(), method);
                var parameterTypeNames = method.GetParameters().Select(parameter => parameter.ParameterType.AssemblyQualifiedName ?? parameter.ParameterType.FullName ?? parameter.ParameterType.Name).ToList();
                var signature = $"{implementation.FullName}|{method.Name}|{string.Join('|', parameterTypeNames)}";
                var shortHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signature))).ToLowerInvariant()[..12];
                var display = $"{implementation.Name}.{method.Name}";
                var entry = new DxAiFunctionCatalogEntry
                {
                    CatalogKey = $"service:{shortHash}",
                    Kind = vocabulary.Get().CatalogPublicServiceMethod,
                    FunctionName = $"service.{implementation.Name}.{method.Name}.{shortHash}",
                    DisplayName = display,
                    Purpose = $"Invoke the configured public service method {display} through dependency injection and the LocalGPT frontend confirmation path.",
                    Method = "POST",
                    Route = "/api/dxai/public-service/invoke",
                    ParameterSchemaJson = BuildParameterSchema(method),
                    Source = "PublicServiceMethodCatalog",
                    ServiceContractTypeName = contract.AssemblyQualifiedName ?? contract.FullName ?? contract.Name,
                    ImplementationTypeName = implementation.AssemblyQualifiedName ?? implementation.FullName ?? implementation.Name,
                    ServiceMethodName = method.Name,
                    ParameterTypeNamesJson = JsonSerializer.Serialize(parameterTypeNames),
                    IsReadOnly = InferReadOnly(method),
                    IsAvailable = true,
                    IsEnabled = true,
                    ExposeToAiChat = false,
                    ExposeToOneWire = false,
                    AllowRemoteInvocation = false,
                    RequiresFrontendConfirmation = true,
                    InteractionEditor = OneWireInteractionEditor.Json,
                    IsSystemSeed = true
                };
                entry.DescriptorHash = ComputeDescriptorHash(entry);
                yield return entry;
            }
        }
    }

    private bool IsSupportedPublicMethod(MethodInfo method) =>
        method.IsPublic && !method.IsStatic && !method.IsSpecialName && !method.IsGenericMethodDefinition &&
        method.GetParameters().Length <= 16 &&
        method.GetParameters().All(parameter => !parameter.ParameterType.IsByRef && !parameter.ParameterType.IsPointer);

    private Type ResolveContract(Type implementation, MethodInfo method)
    {
        foreach (var contract in implementation.GetInterfaces())
        {
            if (contract.GetMethods().Any(candidate => SameSignature(candidate, method)))
                return contract;
        }
        return implementation;
    }

    private bool SameSignature(MethodInfo left, MethodInfo right)
    {
        if (!string.Equals(left.Name, right.Name, StringComparison.Ordinal)) return false;
        var leftParameters = left.GetParameters();
        var rightParameters = right.GetParameters();
        return leftParameters.Length == rightParameters.Length && leftParameters.Select(item => item.ParameterType).SequenceEqual(rightParameters.Select(item => item.ParameterType));
    }

    private string BuildParameterSchema(MethodInfo method)
    {
        var properties = new Dictionary<string, object?>();
        var required = new List<string>();
        foreach (var parameter in method.GetParameters())
        {
            if (parameter.ParameterType == typeof(CancellationToken)) continue;
            properties[parameter.Name ?? $"arg{parameter.Position}"] = new
            {
                type = JsonType(parameter.ParameterType),
                clrType = parameter.ParameterType.FullName ?? parameter.ParameterType.Name,
                hasDefault = parameter.HasDefaultValue
            };
            if (!parameter.HasDefaultValue && Nullable.GetUnderlyingType(parameter.ParameterType) is null && parameter.ParameterType.IsValueType)
                required.Add(parameter.Name ?? $"arg{parameter.Position}");
        }
        return JsonSerializer.Serialize(new { type = "object", properties, required, additionalProperties = false }, JsonOptions);
    }

    private string JsonType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(bool)) return "boolean";
        if (type.IsPrimitive || type == typeof(decimal)) return "number";
        if (type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type)) return "array";
        return type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset) ? "string" : "object";
    }

    private bool InferReadOnly(MethodInfo method)
    {
        var name = method.Name;
        return name.StartsWith("Get", StringComparison.Ordinal) || name.StartsWith("List", StringComparison.Ordinal) ||
            name.StartsWith("Read", StringComparison.Ordinal) || name.StartsWith("Find", StringComparison.Ordinal) ||
            name.StartsWith("Inspect", StringComparison.Ordinal) || name.StartsWith("Preview", StringComparison.Ordinal) ||
            name.StartsWith("Validate", StringComparison.Ordinal) || name.StartsWith("Can", StringComparison.Ordinal) ||
            name.StartsWith("Has", StringComparison.Ordinal) || name.StartsWith("Is", StringComparison.Ordinal);
    }

    private OneWireInteractionEditor InferEditor(string name, string schema, bool requiresConfirmation)
    {
        if (name.Contains("text", StringComparison.OrdinalIgnoreCase) || schema.Contains("content", StringComparison.OrdinalIgnoreCase))
            return OneWireInteractionEditor.RichText;
        return requiresConfirmation ? OneWireInteractionEditor.Json : OneWireInteractionEditor.None;
    }

    private void PreservePolicyAndRefreshDescriptor(DxAiFunctionCatalogEntry stored, DxAiFunctionCatalogEntry current)
    {
        var policy = new
        {
            stored.IsEnabled,
            stored.ExposeToAiChat,
            stored.ExposeToOneWire,
            stored.AllowRemoteInvocation,
            stored.RequiresFrontendConfirmation,
            stored.InteractionEditor,
            stored.AllowedPeerIdsJson,
            stored.IsSystemSeed,
            stored.CreatedAtUtc,
            stored.UpdatedBy
        };
        stored.Kind = current.Kind;
        stored.FunctionName = current.FunctionName;
        stored.DisplayName = current.DisplayName;
        stored.Purpose = current.Purpose;
        stored.Method = current.Method;
        stored.Route = current.Route;
        stored.ParameterSchemaJson = current.ParameterSchemaJson;
        stored.Source = current.Source;
        stored.ServiceContractTypeName = current.ServiceContractTypeName;
        stored.ImplementationTypeName = current.ImplementationTypeName;
        stored.ServiceMethodName = current.ServiceMethodName;
        stored.ParameterTypeNamesJson = current.ParameterTypeNamesJson;
        stored.IsReadOnly = current.IsReadOnly;
        stored.IsAvailable = current.IsAvailable;
        stored.DescriptorHash = current.DescriptorHash;
        stored.IsEnabled = policy.IsEnabled;
        stored.ExposeToAiChat = policy.ExposeToAiChat;
        stored.ExposeToOneWire = policy.ExposeToOneWire;
        stored.AllowRemoteInvocation = policy.AllowRemoteInvocation;
        stored.RequiresFrontendConfirmation = policy.RequiresFrontendConfirmation;
        stored.InteractionEditor = policy.InteractionEditor;
        stored.AllowedPeerIdsJson = policy.AllowedPeerIdsJson;
        stored.IsSystemSeed = policy.IsSystemSeed;
        stored.CreatedAtUtc = policy.CreatedAtUtc == default ? DateTime.UtcNow : policy.CreatedAtUtc;
        stored.UpdatedAtUtc = DateTime.UtcNow;
        stored.UpdatedBy = policy.UpdatedBy;
    }

    private async Task<List<DxAiFunctionCatalogEntry>> ReadEntriesAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken)
    {
        var values = await db.SystemVariables.AsNoTracking()
            .Where(item => item.DataType == DataType)
            .OrderBy(item => item.Name)
            .Select(item => item.ValueString)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return values.Select(Deserialize).Where(item => item is not null).Cast<DxAiFunctionCatalogEntry>()
            .OrderBy(item => item.Kind).ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private DxAiFunctionCatalogEntry? Deserialize(string value)
    {
        try { return JsonSerializer.Deserialize<DxAiFunctionCatalogEntry>(value, JsonOptions); }
        catch (JsonException) { return null; }
    }

    private string NormalizePeersJson(string json)
    {
        var peers = (JsonSerializer.Deserialize<List<string>>(json) ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return JsonSerializer.Serialize(peers);
    }

    private string BuildStorageName(string catalogKey)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(catalogKey))).ToLowerInvariant();
        return $"DxFunctionCatalog.{hash[..32]}";
    }

    private string ComputeDescriptorHash(DxAiFunctionCatalogEntry entry)
    {
        var source = string.Join('|', entry.Kind, entry.FunctionName, entry.Method, entry.Route, entry.ParameterSchemaJson,
            entry.ServiceContractTypeName, entry.ImplementationTypeName, entry.ServiceMethodName, entry.ParameterTypeNamesJson);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
    }
}

public sealed class DxAiFunctionCatalogHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DxAiFunctionCatalogHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IDxAiFunctionCatalogService>()
                .SynchronizeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "DX function catalog synchronization failed. Existing database content was left untouched and startup continues.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
