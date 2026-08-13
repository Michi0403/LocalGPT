using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Reflection;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Invokes only a public method that the local user explicitly enabled in the database-backed catalog.
/// Parameter binding is typed from the method signature; the caller cannot choose an arbitrary CLR type or method name.
/// </summary>
/// <param name="vocabulary">Local gpt vocabulary service dependency used by the public service method invoker workflow to provide the corresponding application capability.</param>
/// <param name="serviceProvider">Service provider dependency used by the public service method invoker workflow to provide the corresponding application capability.</param>
/// <param name="catalog">Devexpress ai function catalog service dependency used by the public service method invoker workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PublicServiceMethodInvoker(ILocalGptVocabularyService vocabulary,
    
    IServiceProvider serviceProvider,
    IDxAiFunctionCatalogService catalog,
    ILogger<PublicServiceMethodInvoker> logger) : IPublicServiceMethodInvoker
{
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="PublicServiceMethodInvoker"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Performs invoke for <see cref="PublicServiceMethodInvoker"/>, keeping the operation consistent with the state and invariants of the surrounding public service method invoker workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The object produced by the operation.</returns>
    public async Task<object?> InvokeAsync(PublicServiceMethodInvocationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entry = await catalog.GetEntryAsync(request.CatalogKey, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Public service catalog entry '{request.CatalogKey}' was not found.");
        if (entry.Kind != vocabulary.Get().CatalogPublicServiceMethod || !entry.IsAvailable || !entry.IsEnabled)
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

    /// <summary>
    /// Performs bind arguments for <see cref="PublicServiceMethodInvoker"/>, keeping the operation consistent with the state and invariants of the surrounding public service method invoker workflow.
    /// </summary>
    /// <param name="method">Method value supplied to the public service method invoker operation and used when producing its result.</param>
    /// <param name="parameters">Parameters value supplied to the public service method invoker operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The object produced by the operation.</returns>
    private object?[] BindArguments(MethodInfo method, JsonElement parameters, CancellationToken cancellationToken)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicServiceMethodInvoker)}.{nameof(BindArguments)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicServiceMethodInvoker)}.{nameof(BindArguments)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs await result for <see cref="PublicServiceMethodInvoker"/>, keeping the operation consistent with the state and invariants of the surrounding public service method invoker workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the public service method invoker operation and used when producing its result.</param>
    /// <param name="returnType">Return type value supplied to the public service method invoker operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    private async Task<object?> AwaitResultAsync(object? value, Type returnType)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicServiceMethodInvoker)}.{nameof(AwaitResultAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicServiceMethodInvoker)}.{nameof(AwaitResultAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves type for <see cref="PublicServiceMethodInvoker"/>, keeping the operation consistent with the state and invariants of the surrounding public service method invoker workflow.
    /// </summary>
    /// <param name="typeName">Type name value supplied to the public service method invoker operation and used when producing its result.</param>
    /// <param name="assembly">Assembly value supplied to the public service method invoker operation and used when producing its result.</param>
    /// <returns>The type produced by the operation.</returns>
    private Type? ResolveType(string typeName, Assembly assembly) {
    try
    {
        return Type.GetType(typeName, throwOnError: false) ?? assembly.GetType(typeName.Split(',')[0].Trim(), throwOnError: false);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicServiceMethodInvoker)}.{nameof(ResolveType)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicServiceMethodInvoker)}.{nameof(ResolveType)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents an invoke configured public service method function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="invoker">Public service method invoker dependency used by the invoke configured public service method function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class InvokeConfiguredPublicServiceMethodFunction(
    IPublicServiceMethodInvoker invoker,
    ILogger<InvokeConfiguredPublicServiceMethodFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the invoke configured public service method function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="InvokeConfiguredPublicServiceMethodFunction"/>.</value>
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

    /// <summary>
    /// Performs invoke for <see cref="InvokeConfiguredPublicServiceMethodFunction"/>, keeping the operation consistent with the state and invariants of the surrounding invoke configured public service method function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            logger.LogInformation("Configured public-service DXFunction invocation started; parameter content was omitted.");
            var payload = request.Parameters.Deserialize<PublicServiceMethodInvocationParameters>(new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException("catalogKey is required.");
            var result = await invoker.InvokeAsync(new PublicServiceMethodInvocationRequest
            {
                CatalogKey = payload.CatalogKey,
                Parameters = payload.Parameters,
                RequestedBy = request.RequestedBy
            }, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Configured public-service DXFunction invocation completed for catalog entry {CatalogKey}.", payload.CatalogKey);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = result };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(InvokeConfiguredPublicServiceMethodFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(InvokeConfiguredPublicServiceMethodFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}
