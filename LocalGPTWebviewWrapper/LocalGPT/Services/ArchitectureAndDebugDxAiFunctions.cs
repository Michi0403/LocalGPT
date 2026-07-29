using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace LocalGPT.Services;

public sealed class GetPublicArchitectureDirectoryFunction(IDxAiFunctionJsonService json, 
    IDxAiFunctionRegistry registry,
    ILogger<GetPublicArchitectureDirectoryFunction> logger) : IDxAiFunctionHandler
{
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

    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
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

    private string FriendlyName(Type type) => type.FullName ?? type.Name;

    private string ResolveRoute(Type type, MethodInfo method)
    {
        var controllerRoute = type.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
        var methodRoute = method.GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .Select(attribute => attribute.Template)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        return string.Join('/', new[] { controllerRoute, methodRoute }.Where(value => !string.IsNullOrWhiteSpace(value))).Replace("//", "/", StringComparison.Ordinal);
    }
}

public sealed class InspectDebugArtifactFunction(IDxAiFunctionJsonService json, IDebugArtifactInspectionService inspector) : IDxAiFunctionHandler
{
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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = json.Deserialize<Parameters>(request.Parameters);
        var result = await inspector.InspectAsync(parameters.FilePath, cancellationToken).ConfigureAwait(false);
        return json.Success(result);
    }

    private sealed class Parameters
    {
        public string FilePath { get; set; } = string.Empty;
    }
}
