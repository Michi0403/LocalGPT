using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class ListCouncilRuntimeClassesFunction(
    ICouncilRuntimeClassService runtimeClasses,
    ILogger<ListCouncilRuntimeClassesFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.runtime-class.list",
        "POST",
        "/api/dxai/functions/localgpt.runtime-class.list/invoke",
        "Lists database-backed Council runtime classes, their namespaces, kinds and field/input counts.",
        "JSON parameters: namespace optional; kind optional; includeDisabled optional boolean.",
        "Read-only. Definitions are configuration and source-study metadata; listing them does not execute a game, repository or input binding.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"namespace":{"type":"string"},"kind":{"type":"string"},"includeDisabled":{"type":"boolean"}},"additionalProperties":false}
        """);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var includeDisabled = request.Parameters.ValueKind == JsonValueKind.Object &&
                request.Parameters.TryGetProperty("includeDisabled", out var includeElement) &&
                includeElement.ValueKind == JsonValueKind.True;
            var namespaceFilter = GetString(request.Parameters, "namespace");
            var kindFilter = GetString(request.Parameters, "kind");
            var definitions = await runtimeClasses.GetDefinitionsAsync(includeDisabled, cancellationToken).ConfigureAwait(false);
            var filtered = definitions
                .Where(item => string.IsNullOrWhiteSpace(namespaceFilter) ||
                    item.Namespace.Contains(namespaceFilter, StringComparison.OrdinalIgnoreCase) ||
                    item.Key.Contains(namespaceFilter, StringComparison.OrdinalIgnoreCase) ||
                    item.DisplayName.Contains(namespaceFilter, StringComparison.OrdinalIgnoreCase) ||
                    item.Aliases.Any(alias => alias.Contains(namespaceFilter, StringComparison.OrdinalIgnoreCase)))
                .Where(item => string.IsNullOrWhiteSpace(kindFilter) || item.Kind.ToString().Equals(kindFilter, StringComparison.OrdinalIgnoreCase))
                .Select(item => new
                {
                    item.Key,
                    item.Namespace,
                    item.DisplayName,
                    Kind = item.Kind.ToString(),
                    FieldCount = item.Fields.Count,
                    InputBindingCount = item.InputBindings.Count,
                    item.Aliases,
                    LookupHint = $"Use localgpt.runtime-class.get with key '{item.Key}'; lookup is case/punctuation tolerant.",
                    item.IsEnabled,
                    item.SourceReferences
                })
                .ToList();
            logger.LogInformation("Listed {RuntimeClassCount} Council runtime class definition(s).", filtered.Count);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = filtered };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not list Council runtime classes.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Runtime class listing failed. Review LocalGPT logs." };
        }
    }

    private string GetString(JsonElement parameters, string name) {
    try
    {
        return parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? string.Empty
            : string.Empty;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ListCouncilRuntimeClassesFunction)}.{nameof(GetString)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ListCouncilRuntimeClassesFunction)}.{nameof(GetString)} failed.");
        throw;
    }
}
}

public sealed class ResolveCouncilRuntimeClassFunction(
    ICouncilRuntimeClassService runtimeClasses,
    ILogger<ResolveCouncilRuntimeClassFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.runtime-class.resolve",
        "POST",
        "/api/dxai/functions/localgpt.runtime-class.resolve/invoke",
        "Resolves a runtime-class key, namespace, display name or common alias without case or punctuation sensitivity.",
        "JSON parameters: query required. Examples: games.ascii.doom.map, LocalGPT.Games.AsciiDoom.Map, doom map.",
        "Read-only. Returns the canonical database-backed definition and stable key so models do not need discovery loops.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["query"],"properties":{"query":{"type":"string","maxLength":240}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = request.Parameters.ValueKind == JsonValueKind.Object && request.Parameters.TryGetProperty("query", out var element)
                ? element.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(query))
                return new DxAiFunctionInvocationResult { Succeeded = false, Status = "InvalidParameters", Error = "Parameter 'query' is required." };
            var definition = await runtimeClasses.FindAsync(query, cancellationToken).ConfigureAwait(false);
            if (definition is null)
                return new DxAiFunctionInvocationResult { Succeeded = false, Status = "NotFound", Error = $"No runtime class matched '{query}'. Call localgpt.runtime-class.list without a namespace filter to inspect canonical keys." };
            logger.LogInformation("Resolved runtime class alias {RuntimeClassAlias} to {RuntimeClassKey}.", query, definition.Key);
            return new DxAiFunctionInvocationResult
            {
                Succeeded = true,
                Status = "Completed",
                Value = new
                {
                    CanonicalKey = definition.Key,
                    definition.Namespace,
                    definition.DisplayName,
                    definition.Kind,
                    definition.Aliases,
                    Definition = definition
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not resolve a Council runtime class alias.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Runtime class resolution failed. Review LocalGPT logs." };
        }
    }
}

public sealed class GetCouncilRuntimeClassFunction(
    ICouncilRuntimeClassService runtimeClasses,
    ILogger<GetCouncilRuntimeClassFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.runtime-class.get",
        "POST",
        "/api/dxai/functions/localgpt.runtime-class.get/invoke",
        "Reads one database-backed Council runtime class including field ownership, human blocking gates, keyboard/gamepad bindings and source references.",
        "JSON parameters: key required.",
        "Read-only. A binding describes permitted configuration; it does not synthesize or send keyboard/gamepad input.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["key"],"properties":{"key":{"type":"string","maxLength":240}},"additionalProperties":false}
        """);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(
        DxAiFunctionInvocationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = request.Parameters.ValueKind == JsonValueKind.Object && request.Parameters.TryGetProperty("key", out var element)
                ? element.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(key))
                return new DxAiFunctionInvocationResult { Succeeded = false, Status = "InvalidParameters", Error = "Parameter 'key' is required." };
            var definition = await runtimeClasses.FindAsync(key, cancellationToken).ConfigureAwait(false);
            if (definition is null)
                return new DxAiFunctionInvocationResult { Succeeded = false, Status = "NotFound", Error = $"Runtime class '{key}' was not found." };
            logger.LogInformation("Read Council runtime class {RuntimeClassKey}.", definition.Key);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = definition };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read one Council runtime class.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Runtime class read failed. Review LocalGPT logs." };
        }
    }
}
