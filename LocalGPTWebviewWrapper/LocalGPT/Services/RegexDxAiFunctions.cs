using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.Models;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Database-backed regular-expression functions used by LocalGPT itself and advertised through the same
/// DI/DX-function/1-Wire discovery path as every other council capability.
/// </summary>
public sealed class ListRegexPatternsFunction(IRegexPatternService regexPatterns, ILogger<ListRegexPatternsFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.regex.list",
        "POST",
        "/api/dxai/functions/localgpt.regex.list/invoke",
        "Lists the database-maintained regular-expression catalog used for project, architecture, protocol and response analysis.",
        "JSON parameters: take optional integer from 1 to 5000; prefix optional name prefix.",
        "Read-only. Patterns are data, not authorization or executable code.",
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

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Regex catalog list DXFunction started.");
        var parameters = Deserialize<ListParameters>(request.Parameters);
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

    private sealed class ListParameters { public int Take { get; set; } = 5000; public string Prefix { get; set; } = string.Empty; }
    private T Deserialize<T>(JsonElement element) where T : new() => element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
        ? new T()
        : element.Deserialize<T>(JsonOptions) ?? new T();
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    private DxAiFunctionInvocationResult Completed(object value) => new() { Succeeded = true, Status = "Completed", Value = value };
}

public sealed class GetRegexPatternFunction(IRegexPatternService regexPatterns, IRegexFunctionParameterService parameters, ILogger<GetRegexPatternFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.regex.get",
        "POST",
        "/api/dxai/functions/localgpt.regex.get/invoke",
        "Reads one exact database-backed regular-expression definition by stable name.",
        "JSON parameters: name required.",
        "Read-only. The returned pattern is untrusted matching data and grants no file or command access.",
        IsReadOnly: true,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","required":["name"],"properties":{"name":{"type":"string","minLength":1,"maxLength":128}},"additionalProperties":false}
        """);

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

public sealed class UpsertRegexPatternFunction(IRegexPatternService regexPatterns, IRegexFunctionParameterService parameters, ILogger<UpsertRegexPatternFunction> logger) : IDxAiFunctionHandler
{
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

public sealed class TestRegexPatternFunction(IRegexPatternService regexPatterns, IRegexFunctionParameterService parameters, ILogger<TestRegexPatternFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.regex.test",
        "POST",
        "/api/dxai/functions/localgpt.regex.test/invoke",
        "Tests a stored regex against bounded supplied text and returns named captures.",
        "JSON parameters: name and text required; maximumMatches optional from 1 to 1000.",
        "Read-only, timeout-bounded matching. Input text is not persisted by this function.",
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
