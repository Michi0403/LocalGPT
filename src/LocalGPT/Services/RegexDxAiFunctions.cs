using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Database-backed regular-expression functions used by LocalGPT itself and advertised through the same
/// DI/DX-function/1-Wire discovery path as every other council capability.
/// </summary>
/// <param name="regexPatterns">Regex pattern service dependency used by the list regex patterns function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ListRegexPatternsFunction(IRegexPatternService regexPatterns, ILogger<ListRegexPatternsFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the list regex patterns function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ListRegexPatternsFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.regex.list",
        "POST",
        "/api/dxai/functions/localgpt.regex.list/invoke",
        "Lists the database-maintained regular-expression catalog used for project, architecture, protocol and response analysis.",
        "JSON parameters: take optional integer from 1 to 5000; prefix optional name prefix.",
        "Read-only. Patterns are data, not authorization or executable code.",
        /// <summary>
        /// Stores the internal parameter schema JSON state used by <see cref="ListRegexPatternsFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type":"object",
          "properties":{
            "take":{"type":"integer","minimum":1,"maximum":5000},
            "prefix":{"type":"string","maxLength":128}
          },
          "additionalProperties":false
        }
        """);

    /// <summary>
    /// Performs invoke for <see cref="ListRegexPatternsFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list regex patterns function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            logger.LogInformation("Regex catalog list DXFunction started.");
            var parameters = Deserialize<RegexPatternListParameters>(request.Parameters);
            var rows = await regexPatterns.ListAllAsync(Math.Clamp(parameters.Take, 1, 5000)).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(parameters.Prefix))
                rows = rows.Where(item => item.Name.StartsWith(parameters.Prefix.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            logger.LogInformation("Regex catalog list DXFunction completed with {PatternCount} pattern(s).", rows.Count);
            return Completed(rows.Select(item => new
            {
                item.Name,
                item.Pattern,
                item.Flags,
                item.CreatedOn,
                item.UpdatedOn
            }).ToList());
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ListRegexPatternsFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ListRegexPatternsFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs deserialize for <see cref="ListRegexPatternsFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list regex patterns function workflow.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="ListRegexPatternsFunction"/>.</typeparam>
    /// <returns>The t produced by the operation.</returns>
    private T Deserialize<T>(JsonElement element) where T : new() => element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
        ? new T()
        : element.Deserialize<T>(JsonOptions) ?? new T();
    /// <summary>
    /// Stores the internal JSON options state used by <see cref="ListRegexPatternsFunction"/> while executing its surrounding workflow.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    /// <summary>
    /// Completes d for <see cref="ListRegexPatternsFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list regex patterns function workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the list regex patterns function operation and used when producing its result.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    private DxAiFunctionInvocationResult Completed(object value) {
    try
    {
        return new() { Succeeded = true, Status = "Completed", Value = value };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ListRegexPatternsFunction)}.{nameof(Completed)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ListRegexPatternsFunction)}.{nameof(Completed)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents a get regex pattern function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="regexPatterns">Regex pattern service dependency used by the get regex pattern function workflow to provide the corresponding application capability.</param>
/// <param name="parameters">Regex function parameter service dependency used by the get regex pattern function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class GetRegexPatternFunction(IRegexPatternService regexPatterns, IRegexFunctionParameterService parameters, ILogger<GetRegexPatternFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the get regex pattern function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="GetRegexPatternFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.regex.get",
        "POST",
        "/api/dxai/functions/localgpt.regex.get/invoke",
        "Reads one exact database-backed regular-expression definition by stable name.",
        "JSON parameters: name required.",
        "Read-only. The returned pattern is untrusted matching data and grants no file or command access.",
        /// <summary>
        /// Stores the internal parameter schema JSON state used by <see cref="GetRegexPatternFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["name"],"properties":{"name":{"type":"string","minLength":1,"maxLength":128}},"additionalProperties":false}
        """);

    /// <summary>
    /// Performs invoke for <see cref="GetRegexPatternFunction"/>, keeping the operation consistent with the state and invariants of the surrounding get regex pattern function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Regex catalog get DXFunction started.");
            var name = parameters.GetRequiredString(request.Parameters, "name");
            var row = (await regexPatterns.ListAllAsync().ConfigureAwait(false))
                .SingleOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            if (row is null)
            {
                logger.LogWarning("Regex catalog get DXFunction did not find the requested database-backed pattern.");
                return new DxAiFunctionInvocationResult { Succeeded = false, Status = "NotFound", Error = $"Regex '{name}' was not found." };
            }

            logger.LogInformation("Regex catalog get DXFunction completed.");
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = row };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Regex catalog get DXFunction failed; parameters and returned pattern content were omitted from logs.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = ex.Message };
        }
    }


}

/// <summary>
/// Represents an upsert regex pattern function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="regexPatterns">Regex pattern service dependency used by the upsert regex pattern function workflow to provide the corresponding application capability.</param>
/// <param name="parameters">Regex function parameter service dependency used by the upsert regex pattern function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class UpsertRegexPatternFunction(IRegexPatternService regexPatterns, IRegexFunctionParameterService parameters, ILogger<UpsertRegexPatternFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the upsert regex pattern function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="UpsertRegexPatternFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.regex.upsert",
        "POST",
        "/api/dxai/functions/localgpt.regex.upsert/invoke",
        "Creates or updates a named regex in LocalGPT's SQLite knowledge-maintenance catalog.",
        "JSON parameters: name and pattern required; flags optional (i,m,s,x,n,c,compiled,ecmascript).",
        "Knowledge self-maintenance only. The pattern is compiled with a timeout before storage, cannot execute commands, and does not authorize project/file access.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type":"object",
          "required":["name","pattern"],
          "properties":{
            "name":{"type":"string","minLength":1,"maxLength":128},
            "pattern":{"type":"string","maxLength":16000},
            "flags":{"type":"string","maxLength":64}
          },
          "additionalProperties":false
        }
        """,
        IsCoordinationOnly: true);

    /// <summary>
    /// Performs invoke for <see cref="UpsertRegexPatternFunction"/>, keeping the operation consistent with the state and invariants of the surrounding upsert regex pattern function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Regex catalog upsert DXFunction started.");
            var name = parameters.GetRequiredString(request.Parameters, "name");
            var pattern = parameters.GetRequiredString(request.Parameters, "pattern");
            var flags = request.Parameters.ValueKind == JsonValueKind.Object && request.Parameters.TryGetProperty("flags", out var flagsElement) && flagsElement.ValueKind == JsonValueKind.String
                ? flagsElement.GetString()
                : null;
            await regexPatterns.AddOrUpdateAsync(new RegexPatternDto(name, pattern, flags)).ConfigureAwait(false);
            logger.LogInformation("Regex catalog upsert DXFunction completed and persisted one database-backed pattern.");
            return new DxAiFunctionInvocationResult
            {
                Succeeded = true,
                Status = "Completed",
                Value = new { name, flags = flags ?? string.Empty, stored = true, knowledgeSelfMaintenance = true }
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Regex catalog upsert DXFunction failed; parameters and pattern content were omitted from logs.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = ex.Message };
        }
    }
}

/// <summary>
/// Represents a test regex pattern function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="regexPatterns">Regex pattern service dependency used by the test regex pattern function workflow to provide the corresponding application capability.</param>
/// <param name="parameters">Regex function parameter service dependency used by the test regex pattern function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class TestRegexPatternFunction(IRegexPatternService regexPatterns, IRegexFunctionParameterService parameters, ILogger<TestRegexPatternFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the test regex pattern function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="TestRegexPatternFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.regex.test",
        "POST",
        "/api/dxai/functions/localgpt.regex.test/invoke",
        "Tests a stored regex against bounded supplied text and returns named captures.",
        "JSON parameters: name and text required; maximumMatches optional from 1 to 1000.",
        "Read-only, timeout-bounded matching. Input text is not persisted by this function.",
        /// <summary>
        /// Stores the internal parameter schema JSON state used by <see cref="TestRegexPatternFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {
          "type":"object",
          "required":["name","text"],
          "properties":{
            "name":{"type":"string","minLength":1,"maxLength":128},
            "text":{"type":"string"},
            "maximumMatches":{"type":"integer","minimum":1,"maximum":1000}
          },
          "additionalProperties":false
        }
        """);

    /// <summary>
    /// Performs invoke for <see cref="TestRegexPatternFunction"/>, keeping the operation consistent with the state and invariants of the surrounding test regex pattern function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Regex catalog test DXFunction started.");
            var name = parameters.GetRequiredString(request.Parameters, "name");
            var text = parameters.GetRequiredString(request.Parameters, "text");
            var maximumMatches = request.Parameters.ValueKind == JsonValueKind.Object
                && request.Parameters.TryGetProperty("maximumMatches", out var takeElement)
                && takeElement.TryGetInt32(out var take)
                ? Math.Clamp(take, 1, 1000)
                : 100;
            var regex = await regexPatterns.GetRegexAsync(name).ConfigureAwait(false) ?? throw new KeyNotFoundException($"Regex '{name}' was not found.");
            var matches = regex.Matches(text).Cast<System.Text.RegularExpressions.Match>().Take(maximumMatches).Select(match => new
            {
                match.Index,
                match.Length,
                match.Value,
                Groups = regex.GetGroupNames().Where(groupName => !int.TryParse(groupName, out _)).ToDictionary(groupName => groupName, groupName => match.Groups[groupName].Value)
            }).ToList();
            logger.LogInformation("Regex catalog test DXFunction completed with {MatchCount} bounded match(es).", matches.Count);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = new { name, matches, truncated = matches.Count == maximumMatches } };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Regex catalog test DXFunction failed; parameters, input text, and match content were omitted from logs.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = ex.Message };
        }
    }
}
