using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class RemoteImportDxParameterReader(
    ILogger<RemoteImportDxParameterReader> logger)
{
    public string String(JsonElement parameters, string name, string fallback = "")
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                   parameters.TryGetProperty(name, out var value) &&
                   value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? fallback
                : fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading remote-import string parameter {ParameterName} failed; parameter content was omitted.", name);
            throw;
        }
    }

    public bool Boolean(JsonElement parameters, string name, bool fallback = false)
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                   parameters.TryGetProperty(name, out var value) &&
                   value.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? value.GetBoolean()
                : fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading remote-import Boolean parameter {ParameterName} failed; parameter content was omitted.", name);
            throw;
        }
    }

    public int Integer(JsonElement parameters, string name, int fallback)
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                   parameters.TryGetProperty(name, out var value) &&
                   value.TryGetInt32(out var parsed)
                ? parsed
                : fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading remote-import Int32 parameter {ParameterName} failed; parameter content was omitted.", name);
            throw;
        }
    }

    public List<string> Strings(JsonElement parameters, string name)
    {
        try
        {
            return parameters.ValueKind == JsonValueKind.Object &&
                   parameters.TryGetProperty(name, out var value) &&
                   value.ValueKind == JsonValueKind.Array
                ? value.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString() ?? string.Empty)
                    .Where(item => item.Length > 0)
                    .ToList()
                : [];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading remote-import string-list parameter {ParameterName} failed; parameter content was omitted.", name);
            throw;
        }
    }

    public RemoteKnowledgeImportRequest Build(JsonElement parameters, bool preview, bool confirmed)
    {
        try
        {
            return new RemoteKnowledgeImportRequest
            {
                SourceUrl = String(parameters, "sourceUrl"),
                SourceKind = String(parameters, "sourceKind", "Auto"),
                Branch = String(parameters, "branch", "main"),
                FileIncludeRegex = String(parameters, "fileIncludeRegex", @"(?i)\.(cs|razor|csproj|sln|json|xml|md|mdx|rst|adoc|txt|ps1|cmd|sh|py|js|ts|tsx|css|html|php|c|h|cpp|hpp|cc|cxx|ino|pde|cmake|kconfig|sdkconfig|toml|ini|cfg|csv|java|kt|go|rs|sql|yml|yaml)$|(^|/)(CMakeLists\.txt|platformio\.ini|library\.properties)$"),
                MaxFiles = Integer(parameters, "maxFiles", 5000),
                MaxLinkedPages = Integer(parameters, "maxLinkedPages", 20),
                SaveToKnowledge = !preview && Boolean(parameters, "saveToKnowledge", true),
                PreviewOnly = preview,
                RoleKeys = Strings(parameters, "roleKeys"),
                Topics = Strings(parameters, "topics"),
                UserConfirmed = confirmed
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building a remote-import request from DXFunction parameters failed; parameter content was omitted.");
            throw;
        }
    }
}

public sealed class InspectRemoteKnowledgeFunction(
    IRemoteKnowledgeImportService importer,
    RemoteImportDxParameterReader parameters,
    ILogger<InspectRemoteKnowledgeFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.knowledge.remote.inspect", "POST", "/api/dxai/functions/localgpt.knowledge.remote.inspect/invoke",
        "Downloads a user-selected public GitHub repository or webpage into the bounded cache and returns the exact file list plus regex matches without saving knowledge.",
        "JSON parameters: sourceUrl required; sourceKind, branch, fileIncludeRegex, maxFiles and maxLinkedPages optional.",
        "Network read with size, ZIP traversal and private-network protections. No database mutation.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["sourceUrl"],"properties":{"sourceUrl":{"type":"string"},"sourceKind":{"type":"string"},"branch":{"type":"string"},"fileIncludeRegex":{"type":"string"},"maxFiles":{"type":"integer"},"maxLinkedPages":{"type":"integer"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await importer.ImportAsync(parameters.Build(request.Parameters, preview: true, confirmed: false), cancellationToken).ConfigureAwait(false);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = result };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Remote knowledge inspection failed.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = ex.Message };
        }
    }
}

public sealed class ImportRemoteKnowledgeFunction(
    IRemoteKnowledgeImportService importer,
    RemoteImportDxParameterReader parameters,
    ILogger<ImportRemoteKnowledgeFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.knowledge.remote.import", "POST", "/api/dxai/functions/localgpt.knowledge.remote.import/invoke",
        "Imports a reviewed public GitHub repository or webpage through the existing learn-base service and associates resulting knowledge with role/topic tags.",
        "JSON parameters: sourceUrl required; sourceKind, branch, fileIncludeRegex, maxFiles, roleKeys, topics and saveToKnowledge optional.",
        "Requires fresh human confirmation before database writes. Source files remain in the local bounded cache and commercial game assets are not supplied.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["sourceUrl"],"properties":{"sourceUrl":{"type":"string"},"sourceKind":{"type":"string"},"branch":{"type":"string"},"fileIncludeRegex":{"type":"string"},"maxFiles":{"type":"integer"},"maxLinkedPages":{"type":"integer"},"roleKeys":{"type":"array","items":{"type":"string"}},"topics":{"type":"array","items":{"type":"string"}},"saveToKnowledge":{"type":"boolean"}},"additionalProperties":false}""");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await importer.ImportAsync(parameters.Build(request.Parameters, preview: false, confirmed: request.UserConfirmed), cancellationToken).ConfigureAwait(false);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = result };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Remote knowledge import failed or was rejected.");
            return new DxAiFunctionInvocationResult { Succeeded = false, Status = "Rejected", Error = ex.Message };
        }
    }
}
