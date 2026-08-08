using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services;

/// <summary>
/// Keeps the user-facing DX function catalog in the existing SystemVariables table. This deliberately avoids
/// destructive schema replacement: old databases receive missing records, while user-edited exposure and
/// confirmation settings survive descriptor refreshes and application upgrades.
/// </summary>
public sealed class DxAiFunctionCatalogService(ILocalGptVocabularyService vocabulary,
    DxAiFunctionCatalogSynchronizationGate synchronizationGate,
    IDatabaseInitializationService databaseInitialization,
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDxAiFunctionRegistry registry,
    IOrganicAddonManifestService addonManifests,
    ILogger<DxAiFunctionCatalogService> logger) : IDxAiFunctionCatalogService
{
    private const string DataType = "DxAiFunctionCatalogEntry";
    private const string StorageNamePrefix = "DxFunctionCatalog.";
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<IReadOnlyList<DxAiFunctionCatalogEntry>> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        await databaseInitialization.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await synchronizationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await SynchronizeCoreAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (DbUpdateException exception) when (
                    attempt < 3 && IsSystemVariableNameConflict(exception))
                {
                    logger.LogWarning(
                        exception,
                        "DX function catalog storage changed concurrently. Retrying synchronization attempt {Attempt} of 3 without replacing user policy.",
                        attempt + 1);
                    await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            synchronizationGate.Release();
        }
    }

    private async Task<IReadOnlyList<DxAiFunctionCatalogEntry>> SynchronizeCoreAsync(CancellationToken cancellationToken)
    {
    try
    {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            // Catalog rows from earlier versions may carry a legacy DataType or a partially written payload.
            // Load both the owned storage-name range and the current DataType so a unique Name is always reused
            // instead of queued as a second INSERT.
            var variables = await db.SystemVariables
                .Where(item => item.DataType == DataType || item.Name.StartsWith(StorageNamePrefix))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var storedRows = variables
                .Select(variable => (Variable: variable, Entry: Deserialize(variable.ValueString)))
                .Where(item => item.Entry is not null && !string.IsNullOrWhiteSpace(item.Entry.CatalogKey))
                .Select(item => (item.Variable, Entry: item.Entry!))
                .ToList();
            var existing = new Dictionary<string, (SystemVariable Variable, DxAiFunctionCatalogEntry Entry)>(StringComparer.OrdinalIgnoreCase);
            var duplicateCount = 0;
            var removedVariables = new HashSet<SystemVariable>();
            foreach (var group in storedRows.GroupBy(item => GetSemanticIdentity(item.Entry), StringComparer.OrdinalIgnoreCase))
            {
                var canonical = SelectCanonicalCatalogRow(group);
                canonical.Variable.DataType = DataType;
                existing[group.Key] = canonical;
                foreach (var duplicate in group.Where(item => item.Variable.Id != canonical.Variable.Id))
                {
                    db.SystemVariables.Remove(duplicate.Variable);
                    removedVariables.Add(duplicate.Variable);
                    duplicateCount++;
                }
            }

            if (duplicateCount > 0)
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var variablesByName = variables
                .Where(variable => !removedVariables.Contains(variable))
                .GroupBy(variable => variable.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(variable => variable.LastUpdated).ThenBy(variable => variable.Id).First(),
                    StringComparer.OrdinalIgnoreCase);
            var claimedVariables = new HashSet<SystemVariable>();
            var discovered = DiscoverEntries();
            var discoveredKeys = discovered.Select(GetSemanticIdentity).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var descriptor in discovered)
            {
                var semanticIdentity = GetSemanticIdentity(descriptor);
                var storageName = BuildStorageName(descriptor.CatalogKey);
                var hasStored = existing.TryGetValue(semanticIdentity, out var stored);
                variablesByName.TryGetValue(storageName, out var namedVariable);

                if (namedVariable is not null && (!hasStored || namedVariable.Id != stored.Variable.Id))
                {
                    // The unique storage name already exists, possibly with a legacy DataType or stale payload.
                    // Reuse that row and carry forward the policy from the semantic match when one exists.
                    var refreshedEntry = hasStored ? stored.Entry : Deserialize(namedVariable.ValueString) ?? descriptor;
                    PreservePolicyAndRefreshDescriptor(refreshedEntry, descriptor);
                    namedVariable.Name = storageName;
                    namedVariable.DataType = DataType;
                    namedVariable.ValueString = JsonSerializer.Serialize(refreshedEntry, JsonOptions);
                    namedVariable.LastUpdated = DateTime.UtcNow;

                    if (hasStored && stored.Variable.Id != namedVariable.Id)
                    {
                        db.SystemVariables.Remove(stored.Variable);
                        removedVariables.Add(stored.Variable);
                        duplicateCount++;
                    }

                    foreach (var priorKey in existing
                        .Where(item => ReferenceEquals(item.Value.Variable, namedVariable) &&
                            !string.Equals(item.Key, semanticIdentity, StringComparison.OrdinalIgnoreCase))
                        .Select(item => item.Key)
                        .ToList())
                    {
                        existing.Remove(priorKey);
                    }

                    existing[semanticIdentity] = (namedVariable, refreshedEntry);
                    claimedVariables.Add(namedVariable);
                    continue;
                }

                if (hasStored)
                {
                    PreservePolicyAndRefreshDescriptor(stored.Entry, descriptor);
                    stored.Variable.Name = storageName;
                    stored.Variable.DataType = DataType;
                    stored.Variable.ValueString = JsonSerializer.Serialize(stored.Entry, JsonOptions);
                    stored.Variable.LastUpdated = DateTime.UtcNow;
                    variablesByName[storageName] = stored.Variable;
                    claimedVariables.Add(stored.Variable);
                    continue;
                }

                var variable = new SystemVariable
                {
                    Name = storageName,
                    DataType = DataType,
                    ValueString = JsonSerializer.Serialize(descriptor, JsonOptions),
                    LastUpdated = DateTime.UtcNow
                };
                db.SystemVariables.Add(variable);
                variablesByName[storageName] = variable;
                existing[semanticIdentity] = (variable, descriptor);
                claimedVariables.Add(variable);
            }

            foreach (var stored in existing.Values.Where(item =>
                !claimedVariables.Contains(item.Variable) &&
                !removedVariables.Contains(item.Variable) &&
                !discoveredKeys.Contains(GetSemanticIdentity(item.Entry))))
            {
                stored.Entry.IsAvailable = false;
                stored.Entry.UpdatedAtUtc = DateTime.UtcNow;
                stored.Entry.UpdatedBy = "LocalGPT runtime catalog";
                stored.Variable.DataType = DataType;
                stored.Variable.ValueString = JsonSerializer.Serialize(stored.Entry, JsonOptions);
                stored.Variable.LastUpdated = DateTime.UtcNow;
            }

            foreach (var invalidLegacyRow in variables.Where(variable =>
                !claimedVariables.Contains(variable) &&
                !removedVariables.Contains(variable) &&
                variable.Name.StartsWith(StorageNamePrefix, StringComparison.OrdinalIgnoreCase) &&
                Deserialize(variable.ValueString) is null))
            {
                db.SystemVariables.Remove(invalidLegacyRow);
                removedVariables.Add(invalidLegacyRow);
                duplicateCount++;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var result = await ReadEntriesAsync(db, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Synchronized {CatalogCount} unique DX function/public service catalog entries and removed {DuplicateCount} duplicate rows without replacing user policy.",
                result.Count,
                duplicateCount);
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(SynchronizeCoreAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(SynchronizeCoreAsync)} failed.");
        throw;
    }
}

    private bool IsSystemVariableNameConflict(DbUpdateException exception)
    {
    try
    {
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                if (current is SqliteException sqliteException &&
                    sqliteException.SqliteErrorCode == 19 &&
                    sqliteException.Message.Contains(
                        "UNIQUE constraint failed: SystemVariables.Name",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(IsSystemVariableNameConflict)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(IsSystemVariableNameConflict)} failed.");
        throw;
    }
}

    public async Task<IReadOnlyList<DxAiFunctionCatalogEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            await databaseInitialization.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var entries = await ReadEntriesAsync(db, cancellationToken).ConfigureAwait(false);
            return entries.Count == 0 ? await SynchronizeAsync(cancellationToken).ConfigureAwait(false) : entries;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetEntriesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetEntriesAsync)} failed.");
        throw;
    }
}

    public async Task<DxAiFunctionCatalogEntry?> GetEntryAsync(string catalogKey, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(catalogKey);
            return (await GetEntriesAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => string.Equals(item.CatalogKey, catalogKey, StringComparison.OrdinalIgnoreCase));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetEntryAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetEntryAsync)} failed.");
        throw;
    }
}

    public async Task<DxAiFunctionCatalogEntry?> GetByFunctionNameAsync(string functionName, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
            return (await GetEntriesAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => string.Equals(item.FunctionName, functionName, StringComparison.OrdinalIgnoreCase));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetByFunctionNameAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetByFunctionNameAsync)} failed.");
        throw;
    }
}

    public async Task<DxAiFunctionCatalogEntry> SavePolicyAsync(DxAiFunctionCatalogSaveRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.CatalogKey);
            _ = JsonSerializer.Deserialize<List<string>>(request.AllowedPeerIdsJson) ?? [];

            await synchronizationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var variables = await db.SystemVariables
                    .Where(item => item.DataType == DataType)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                var matches = variables
                    .Select(variable => (Variable: variable, Entry: Deserialize(variable.ValueString)))
                    .Where(item => item.Entry is not null && string.Equals(item.Entry.CatalogKey, request.CatalogKey, StringComparison.OrdinalIgnoreCase))
                    .Select(item => (item.Variable, Entry: item.Entry!))
                    .ToList();
                if (matches.Count == 0)
                    throw new KeyNotFoundException($"DX function catalog entry '{request.CatalogKey}' was not found. Synchronize the catalog first.");

                var match = SelectCanonicalCatalogRow(matches);
                foreach (var duplicate in matches.Where(item => item.Variable.Id != match.Variable.Id))
                    db.SystemVariables.Remove(duplicate.Variable);

                match.Variable.Name = BuildStorageName(match.Entry.CatalogKey);
                match.Variable.DataType = DataType;
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
                synchronizationGate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(SavePolicyAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(SavePolicyAsync)} failed.");
        throw;
    }
}

    public async Task<IReadOnlyList<DxAiFunctionCatalogEntry>> GetExposedToPeerAsync(string peerId, CancellationToken cancellationToken = default)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(peerId);
            return (await GetEntriesAsync(cancellationToken).ConfigureAwait(false))
                .Where(item => item.IsAvailable && item.IsEnabled && item.ExposeToOneWire && item.AllowsPeer(peerId))
                .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetExposedToPeerAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetExposedToPeerAsync)} failed.");
        throw;
    }
}

    private IReadOnlyList<DxAiFunctionCatalogEntry> DiscoverEntries()
    {
    try
    {
            var entries = registry.GetFunctions().Select(CreateDxEntry).ToList();
            entries.AddRange(DiscoverPublicServiceMethods());
            entries.AddRange(addonManifests.GetCatalogEntries());
            return entries
                .Where(item => !string.IsNullOrWhiteSpace(item.CatalogKey))
                .GroupBy(GetSemanticIdentity, StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderBy(item => item.Source, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .First())
                .OrderBy(item => item.Kind)
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(DiscoverEntries)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(DiscoverEntries)} failed.");
        throw;
    }
}

    private DxAiFunctionCatalogEntry CreateDxEntry(DxaichatFunctionInfo function)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(CreateDxEntry)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(CreateDxEntry)} failed.");
        throw;
    }
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
                var parameterTypeNames = method.GetParameters()
                    .Select(parameter => parameter.ParameterType.AssemblyQualifiedName ?? parameter.ParameterType.FullName ?? parameter.ParameterType.Name)
                    .ToList();
                var stableParameterTypeNames = method.GetParameters()
                    .Select(parameter => GetStableTypeIdentity(parameter.ParameterType));
                var signature = $"{implementation.FullName}|{method.Name}|{string.Join('|', stableParameterTypeNames)}";
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

    private bool IsSupportedPublicMethod(MethodInfo method) {
    try
    {
        return method.IsPublic && !method.IsStatic && !method.IsSpecialName && !method.IsGenericMethodDefinition &&
        method.GetParameters().Length <= 16 &&
        method.GetParameters().All(parameter => !parameter.ParameterType.IsByRef && !parameter.ParameterType.IsPointer);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(IsSupportedPublicMethod)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(IsSupportedPublicMethod)} failed.");
        throw;
    }
}

    private Type ResolveContract(Type implementation, MethodInfo method)
    {
    try
    {
            foreach (var contract in implementation.GetInterfaces())
            {
                if (contract.GetMethods().Any(candidate => SameSignature(candidate, method)))
                    return contract;
            }
            return implementation;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(ResolveContract)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(ResolveContract)} failed.");
        throw;
    }
}

    private bool SameSignature(MethodInfo left, MethodInfo right)
    {
    try
    {
            if (!string.Equals(left.Name, right.Name, StringComparison.Ordinal)) return false;
            var leftParameters = left.GetParameters();
            var rightParameters = right.GetParameters();
            return leftParameters.Length == rightParameters.Length && leftParameters.Select(item => item.ParameterType).SequenceEqual(rightParameters.Select(item => item.ParameterType));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(SameSignature)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(SameSignature)} failed.");
        throw;
    }
}

    private string BuildParameterSchema(MethodInfo method)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(BuildParameterSchema)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(BuildParameterSchema)} failed.");
        throw;
    }
}

    private string JsonType(Type type)
    {
    try
    {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(bool)) return "boolean";
            if (type.IsPrimitive || type == typeof(decimal)) return "number";
            if (type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type)) return "array";
            return type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset) ? "string" : "object";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(JsonType)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(JsonType)} failed.");
        throw;
    }
}

    private bool InferReadOnly(MethodInfo method)
    {
    try
    {
            var name = method.Name;
            return name.StartsWith("Get", StringComparison.Ordinal) || name.StartsWith("List", StringComparison.Ordinal) ||
                name.StartsWith("Read", StringComparison.Ordinal) || name.StartsWith("Find", StringComparison.Ordinal) ||
                name.StartsWith("Inspect", StringComparison.Ordinal) || name.StartsWith("Preview", StringComparison.Ordinal) ||
                name.StartsWith("Validate", StringComparison.Ordinal) || name.StartsWith("Can", StringComparison.Ordinal) ||
                name.StartsWith("Has", StringComparison.Ordinal) || name.StartsWith("Is", StringComparison.Ordinal);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(InferReadOnly)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(InferReadOnly)} failed.");
        throw;
    }
}

    private OneWireInteractionEditor InferEditor(string name, string schema, bool requiresConfirmation)
    {
    try
    {
            if (name.Contains("text", StringComparison.OrdinalIgnoreCase) || schema.Contains("content", StringComparison.OrdinalIgnoreCase))
                return OneWireInteractionEditor.RichText;
            return requiresConfirmation ? OneWireInteractionEditor.Json : OneWireInteractionEditor.None;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(InferEditor)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(InferEditor)} failed.");
        throw;
    }
}

    private void PreservePolicyAndRefreshDescriptor(DxAiFunctionCatalogEntry stored, DxAiFunctionCatalogEntry current)
    {
    try
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
            stored.CatalogKey = current.CatalogKey;
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(PreservePolicyAndRefreshDescriptor)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(PreservePolicyAndRefreshDescriptor)} failed.");
        throw;
    }
}

    private async Task<List<DxAiFunctionCatalogEntry>> ReadEntriesAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken)
    {
    try
    {
            var variables = await db.SystemVariables.AsNoTracking()
                .Where(item => item.DataType == DataType)
                .OrderBy(item => item.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return variables
                .Select(variable => (Variable: variable, Entry: Deserialize(variable.ValueString)))
                .Where(item => item.Entry is not null && !string.IsNullOrWhiteSpace(item.Entry.CatalogKey))
                .Select(item => (item.Variable, Entry: item.Entry!))
                .GroupBy(item => GetSemanticIdentity(item.Entry), StringComparer.OrdinalIgnoreCase)
                .Select(group => SelectCanonicalCatalogRow(group).Entry)
                .OrderBy(item => item.Kind)
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(ReadEntriesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(ReadEntriesAsync)} failed.");
        throw;
    }
}

    private (SystemVariable Variable, DxAiFunctionCatalogEntry Entry) SelectCanonicalCatalogRow(
        IEnumerable<(SystemVariable Variable, DxAiFunctionCatalogEntry Entry)> rows)
    {
        return rows
            .OrderBy(item => item.Entry.IsSystemSeed ? 1 : 0)
            .ThenBy(item => string.Equals(
                item.Variable.Name,
                BuildStorageName(item.Entry.CatalogKey),
                StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenByDescending(item => item.Variable.LastUpdated)
            .ThenBy(item => item.Variable.Id)
            .First();
    }


    private string GetSemanticIdentity(DxAiFunctionCatalogEntry entry)
    {
    try
    {
            if (entry.CatalogKey.StartsWith("service:", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entry.Kind, vocabulary.Get().CatalogPublicServiceMethod, StringComparison.OrdinalIgnoreCase))
            {
                var implementation = GetStoredTypeName(entry.ImplementationTypeName);
                var schema = Regex.Replace(entry.ParameterSchemaJson ?? string.Empty, @"\s+", string.Empty);
                return $"service|{implementation}|{entry.ServiceMethodName}|{schema}";
            }

            if (entry.CatalogKey.StartsWith("dx:", StringComparison.OrdinalIgnoreCase))
                return $"dx|{entry.FunctionName}";

            return $"catalog|{entry.CatalogKey}";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetSemanticIdentity)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetSemanticIdentity)} failed.");
        throw;
    }
}

    private string GetStoredTypeName(string? assemblyQualifiedTypeName)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(assemblyQualifiedTypeName))
                return string.Empty;
            var separator = assemblyQualifiedTypeName.IndexOf(',');
            return (separator < 0 ? assemblyQualifiedTypeName : assemblyQualifiedTypeName[..separator]).Trim();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetStoredTypeName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetStoredTypeName)} failed.");
        throw;
    }
}

    private string GetStableTypeIdentity(Type type)
    {
    try
    {
            if (type.IsByRef)
                return $"{GetStableTypeIdentity(type.GetElementType()!)}&";
            if (type.IsPointer)
                return $"{GetStableTypeIdentity(type.GetElementType()!)}*";
            if (type.IsArray)
                return $"{GetStableTypeIdentity(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
            if (!type.IsGenericType)
                return type.FullName ?? type.Name;

            var genericDefinitionName = type.GetGenericTypeDefinition().FullName ?? type.Name;
            var tick = genericDefinitionName.IndexOf('`');
            if (tick >= 0)
                genericDefinitionName = genericDefinitionName[..tick];
            return $"{genericDefinitionName}<{string.Join(',', type.GetGenericArguments().Select(GetStableTypeIdentity))}>";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetStableTypeIdentity)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(GetStableTypeIdentity)} failed.");
        throw;
    }
}

    private DxAiFunctionCatalogEntry? Deserialize(string value)
    {
    try
    {
            try { return JsonSerializer.Deserialize<DxAiFunctionCatalogEntry>(value, JsonOptions); }
            catch (JsonException) { return null; }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(Deserialize)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(Deserialize)} failed.");
        throw;
    }
}

    private string NormalizePeersJson(string json)
    {
    try
    {
            var peers = (JsonSerializer.Deserialize<List<string>>(json) ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return JsonSerializer.Serialize(peers);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(NormalizePeersJson)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(NormalizePeersJson)} failed.");
        throw;
    }
}

    private string BuildStorageName(string catalogKey)
    {
    try
    {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(catalogKey))).ToLowerInvariant();
            return $"{StorageNamePrefix}{hash[..32]}";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(BuildStorageName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(BuildStorageName)} failed.");
        throw;
    }
}

    private string ComputeDescriptorHash(DxAiFunctionCatalogEntry entry)
    {
    try
    {
            var source = string.Join('|', entry.Kind, entry.FunctionName, entry.Method, entry.Route, entry.ParameterSchemaJson,
                entry.ServiceContractTypeName, entry.ImplementationTypeName, entry.ServiceMethodName, entry.ParameterTypeNamesJson);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(ComputeDescriptorHash)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogService)}.{nameof(ComputeDescriptorHash)} failed.");
        throw;
    }
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

    public Task StopAsync(CancellationToken cancellationToken) {
    try
    {
        return Task.CompletedTask;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogHostedService)}.{nameof(StopAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DxAiFunctionCatalogHostedService)}.{nameof(StopAsync)} failed.");
        throw;
    }
}
}
