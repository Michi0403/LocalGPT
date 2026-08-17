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
/// Coordinates DevExpress AI function catalog behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed partial class DxAiFunctionCatalogService
{
    /// <summary>
    /// Reads entries as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="db">Database value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs select canonical catalog row as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="rows">Entry) dependency used by the DevExpress AI function catalog workflow to provide the corresponding application capability.</param>
    /// <returns>The system variable variable DevExpress AI function catalog entry entry produced by the operation.</returns>
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


    /// <summary>
    /// Retrieves semantic identity as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="entry">Entry value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Retrieves stored type name as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="assemblyQualifiedTypeName">Assembly qualified type name value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Retrieves stable type identity as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="type">Type value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Performs deserialize as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function catalog entry produced by the operation.</returns>
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

    /// <summary>
    /// Normalizes peers JSON as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="json">Json value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Builds storage name as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="catalogKey">Catalog key value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Computes descriptor hash as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="entry">Entry value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
