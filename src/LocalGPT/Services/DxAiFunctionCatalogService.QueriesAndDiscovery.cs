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
    /// Retrieves exposed to peer as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Discovers entries as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Creates DevExpress entry as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="function">Function value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function catalog entry produced by the operation.</returns>
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

    /// <summary>
    /// Discovers public service methods as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Determines whether supported public method as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="method">Method value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

}
