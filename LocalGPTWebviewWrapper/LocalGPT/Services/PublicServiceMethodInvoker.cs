using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Reflection;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Invokes only a public method that the local user explicitly enabled in the database-backed catalog.
/// Parameter binding is typed from the method signature; the caller cannot choose an arbitrary CLR type or method name.
/// </summary>
public sealed class PublicServiceMethodInvoker(
    IServiceProvider serviceProvider,
    IDxAiFunctionCatalogService catalog,
    ILogger<PublicServiceMethodInvoker> logger) : IPublicServiceMethodInvoker
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public async Task<object?> InvokeAsync(PublicServiceMethodInvocationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entry = await catalog.GetEntryAsync(request.CatalogKey, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Public service catalog entry '{request.CatalogKey}' was not found.");
        if (entry.Kind != DxAiFunctionCatalogKinds.PublicServiceMethod || !entry.IsAvailable || !entry.IsEnabled)
            throw new InvalidOperationException("The selected public service method is unavailable or disabled.");
        if (!entry.AllowRemoteInvocation && !entry.ExposeToAiChat)
            throw new InvalidOperationException("The selected public service method is discovery-only until the local user enables invocation in the DX Function Catalog.");

        var assembly = typeof(Program).Assembly;
        var contractType = ResolveType(entry.ServiceContractTypeName, assembly)
            ?? throw new TypeLoadException($"Service contract '{entry.ServiceContractTypeName}' is unavailable.");
        var implementationType = ResolveType(entry.ImplementationTypeName, assembly)
            ?? throw new TypeLoadException($"Service implementation '{entry.ImplementationTypeName}' is unavailable.");
        var instance = serviceProvider.GetService(contractType) ?? serviceProvider.GetService(implementationType)
            ?? throw new InvalidOperationException($"The configured service '{contractType.FullName}' is not registered in the current dependency-injection scope.");

        var parameterTypeNames = JsonSerializer.Deserialize<List<string>>(entry.ParameterTypeNamesJson, JsonOptions) ?? [];
        var method = implementationType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(candidate => string.Equals(candidate.Name, entry.ServiceMethodName, StringComparison.Ordinal) &&
                candidate.GetParameters().Select(item => item.ParameterType.AssemblyQualifiedName ?? item.ParameterType.FullName ?? item.ParameterType.Name)
                    .SequenceEqual(parameterTypeNames, StringComparer.Ordinal))
            ?? throw new MissingMethodException(implementationType.FullName, entry.ServiceMethodName);

        var arguments = BindArguments(method, request.Parameters, cancellationToken);
        try
        {
            var value = method.Invoke(instance, arguments);
            var result = await AwaitResultAsync(value, method.ReturnType).ConfigureAwait(false);
            logger.LogInformation("Invoked user-enabled service catalog entry {CatalogKey} ({DisplayName}).", entry.CatalogKey, entry.DisplayName);
            return result;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static object?[] BindArguments(MethodInfo method, JsonElement parameters, CancellationToken cancellationToken)
    {
        var properties = parameters.ValueKind == JsonValueKind.Object
            ? parameters.EnumerateObject().ToDictionary(item => item.Name, item => item.Value, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        return method.GetParameters().Select(parameter =>
        {
            if (parameter.ParameterType == typeof(CancellationToken)) return (object?)cancellationToken;
            if (properties.TryGetValue(parameter.Name ?? string.Empty, out var value))
                return value.Deserialize(parameter.ParameterType, JsonOptions);
            if (parameter.HasDefaultValue) return parameter.DefaultValue;
            if (!parameter.ParameterType.IsValueType || Nullable.GetUnderlyingType(parameter.ParameterType) is not null) return null;
            throw new JsonException($"Required parameter '{parameter.Name}' is missing.");
        }).ToArray();
    }

    private static async Task<object?> AwaitResultAsync(object? value, Type returnType)
    {
        if (value is Task task)
        {
            await task.ConfigureAwait(false);
            return returnType.IsGenericType ? returnType.GetProperty("Result")?.GetValue(task) : null;
        }
        if (returnType == typeof(ValueTask) && value is ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);
            return null;
        }
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var asTask = returnType.GetMethod("AsTask")?.Invoke(value, null) as Task
                ?? throw new InvalidOperationException("Could not await the configured ValueTask result.");
            await asTask.ConfigureAwait(false);
            return asTask.GetType().GetProperty("Result")?.GetValue(asTask);
        }
        return value;
    }

    private static Type? ResolveType(string typeName, Assembly assembly) =>
        Type.GetType(typeName, throwOnError: false) ?? assembly.GetType(typeName.Split(',')[0].Trim(), throwOnError: false);
}

public sealed class InvokeConfiguredPublicServiceMethodFunction(
    IPublicServiceMethodInvoker invoker) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.public_service.invoke",
        "POST",
        "/api/dxai/public-service/invoke",
        "Invokes one public service method that the local user explicitly enabled in the database-backed DX Function Catalog.",
        "catalogKey is required; parameters is an object matching the selected method signature.",
        "The catalog entry controls exposure. Frontend confirmation is required before this generic bridge executes a configured service method.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "PublicServiceMethodInvoker",
        ParameterSchemaJson: "{\"type\":\"object\",\"required\":[\"catalogKey\"],\"properties\":{\"catalogKey\":{\"type\":\"string\"},\"parameters\":{\"type\":\"object\"}}}");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var payload = request.Parameters.Deserialize<InvocationParameters>(new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true })
            ?? throw new JsonException("catalogKey is required.");
        var result = await invoker.InvokeAsync(new PublicServiceMethodInvocationRequest
        {
            CatalogKey = payload.CatalogKey,
            Parameters = payload.Parameters,
            RequestedBy = request.RequestedBy
        }, cancellationToken).ConfigureAwait(false);
        return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = result };
    }

    private sealed class InvocationParameters
    {
        public string CatalogKey { get; set; } = string.Empty;
        public JsonElement Parameters { get; set; }
    }
}
