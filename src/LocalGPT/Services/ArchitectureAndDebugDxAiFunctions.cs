using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace LocalGPT.Services;

/// <summary>
/// Represents a get public architecture directory function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the get public architecture directory function workflow to provide the corresponding application capability.</param>
/// <param name="registry">Devexpress ai function registry dependency used by the get public architecture directory function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class GetPublicArchitectureDirectoryFunction(IDxAiFunctionJsonService json,
    IDxAiFunctionRegistry registry,
    ILogger<GetPublicArchitectureDirectoryFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the get public architecture directory function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="GetPublicArchitectureDirectoryFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.architecture.public_methods",
        "GET",
        "/__diag/architecture/public-methods",
        "Lists public LocalGPT controller and service methods and shows which DI-backed DXFunctions are directly invokable.",
        "No parameters.",
        "Read-only metadata. A listed public method is not permission and is not automatically invokable.",
        IsReadOnly: true,
        AvailableToAi: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "ReflectionDirectory");

    /// <summary>
    /// Performs invoke for <see cref="GetPublicArchitectureDirectoryFunction"/>, keeping the operation consistent with the state and invariants of the surrounding get public architecture directory function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var assembly = typeof(Program).Assembly;
            var methods = assembly.DefinedTypes
                .Where(type => type.IsPublic && !type.IsAbstract &&
                    (type.Namespace?.StartsWith("LocalGPT.Services", StringComparison.Ordinal) == true ||
                     typeof(ControllerBase).IsAssignableFrom(type.AsType())))
                .SelectMany(type => type.DeclaredMethods
                    .Where(method => method.IsPublic && !method.IsSpecialName)
                    .Select(method => new
                    {
                        Type = type.FullName,
                        Method = method.Name,
                        ReturnType = FriendlyName(method.ReturnType),
                        Parameters = method.GetParameters().Select(parameter => new { parameter.Name, Type = FriendlyName(parameter.ParameterType), parameter.HasDefaultValue }).ToList(),
                        ControllerRoute = ResolveRoute(type.AsType(), method),
                        IsController = typeof(ControllerBase).IsAssignableFrom(type.AsType())
                    }))
                .OrderBy(item => item.Type, StringComparer.Ordinal)
                .ThenBy(item => item.Method, StringComparer.Ordinal)
                .Take(4096)
                .ToList();
            var functions = registry.GetFunctions();
            logger.LogDebug("Published {MethodCount} public methods and {FunctionCount} DXFunctions.", methods.Count, functions.Count);
            return Task.FromResult(json.Success(new { Methods = methods, DxFunctions = functions }));
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GetPublicArchitectureDirectoryFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GetPublicArchitectureDirectoryFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs friendly name for <see cref="GetPublicArchitectureDirectoryFunction"/>, keeping the operation consistent with the state and invariants of the surrounding get public architecture directory function workflow.
    /// </summary>
    /// <param name="type">Type value supplied to the get public architecture directory function operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string FriendlyName(Type type) {
    try
    {
        return type.FullName ?? type.Name;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GetPublicArchitectureDirectoryFunction)}.{nameof(FriendlyName)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GetPublicArchitectureDirectoryFunction)}.{nameof(FriendlyName)} failed.");
        throw;
    }
}

    /// <summary>
    /// Resolves route for <see cref="GetPublicArchitectureDirectoryFunction"/>, keeping the operation consistent with the state and invariants of the surrounding get public architecture directory function workflow.
    /// </summary>
    /// <param name="type">Type value supplied to the get public architecture directory function operation and used when producing its result.</param>
    /// <param name="method">Method value supplied to the get public architecture directory function operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveRoute(Type type, MethodInfo method)
    {
    try
    {
            var controllerRoute = type.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
            var methodRoute = method.GetCustomAttributes()
                .OfType<HttpMethodAttribute>()
                .Select(attribute => attribute.Template)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
            return string.Join('/', new[] { controllerRoute, methodRoute }.Where(value => !string.IsNullOrWhiteSpace(value))).Replace("//", "/", StringComparison.Ordinal);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GetPublicArchitectureDirectoryFunction)}.{nameof(ResolveRoute)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GetPublicArchitectureDirectoryFunction)}.{nameof(ResolveRoute)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents an inspect debug artifact function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the inspect debug artifact function workflow to provide the corresponding application capability.</param>
/// <param name="inspector">Debug artifact inspection service dependency used by the inspect debug artifact function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class InspectDebugArtifactFunction(
    IDxAiFunctionJsonService json,
    IDebugArtifactInspectionService inspector,
    ILogger<InspectDebugArtifactFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the inspect debug artifact function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="InspectDebugArtifactFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.debug.inspect",
        "POST",
        "/__diag/debug/inspect",
        "Reads bounded portable-PDB document and debug metadata so a council can understand the matching build without loading or executing it.",
        "filePath: exact user-selected local debug artifact path.",
        "Local file read. Requires current human confirmation for the exact path. Does not load assemblies or execute symbol code.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "DebugArtifactInspectionService",
        ParameterSchemaJson: "{\"type\":\"object\",\"required\":[\"filePath\"],\"properties\":{\"filePath\":{\"type\":\"string\"}}}");

    /// <summary>
    /// Performs invoke for <see cref="InspectDebugArtifactFunction"/>, keeping the operation consistent with the state and invariants of the surrounding inspect debug artifact function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            logger.LogInformation("Debug-artifact inspection DXFunction started; file path content was omitted.");
            var binding = json.Bind<InspectDebugArtifactParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            var result = await inspector.InspectAsync(parameters.FilePath, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Debug-artifact inspection DXFunction completed.");
            return json.Success(result);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(InspectDebugArtifactFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(InspectDebugArtifactFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}
