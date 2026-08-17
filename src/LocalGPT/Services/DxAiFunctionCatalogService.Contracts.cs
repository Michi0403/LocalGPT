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
    /// Resolves contract as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="implementation">Implementation value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <param name="method">Method value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>The type produced by the operation.</returns>
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

    /// <summary>
    /// Performs same signature as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="left">Left value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <param name="right">Right value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Builds parameter schema as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="method">Method value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Performs JSON type as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="type">Type value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Performs infer read only as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="method">Method value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Performs infer editor as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="name">Name value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <param name="schema">Schema value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <param name="requiresConfirmation">Value indicating whether requires confirmation should apply to this operation.</param>
    /// <returns>The one wire interaction editor produced by the operation.</returns>
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

    /// <summary>
    /// Performs preserve policy and refresh descriptor as part of the DevExpress AI function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="stored">Stored value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
    /// <param name="current">Current value supplied to the DevExpress AI function catalog operation and used when producing its result.</param>
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

}
