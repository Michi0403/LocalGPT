using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Represents a remote import DevExpress parameter application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RemoteImportDxParameterReader(
    ILogger<RemoteImportDxParameterReader> logger)
{
    /// <summary>
    /// Performs string for <see cref="RemoteImportDxParameterReader"/>, keeping the operation consistent with the state and invariants of the surrounding remote import DevExpress parameter workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the remote import DevExpress parameter operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the remote import DevExpress parameter operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the remote import DevExpress parameter operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Performs boolean for <see cref="RemoteImportDxParameterReader"/>, keeping the operation consistent with the state and invariants of the surrounding remote import DevExpress parameter workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the remote import DevExpress parameter operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the remote import DevExpress parameter operation and used when producing its result.</param>
    /// <param name="fallback">Value indicating whether fallback should apply to this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Performs integer for <see cref="RemoteImportDxParameterReader"/>, keeping the operation consistent with the state and invariants of the surrounding remote import DevExpress parameter workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the remote import DevExpress parameter operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the remote import DevExpress parameter operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the remote import DevExpress parameter operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
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

    /// <summary>
    /// Performs strings for <see cref="RemoteImportDxParameterReader"/>, keeping the operation consistent with the state and invariants of the surrounding remote import DevExpress parameter workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the remote import DevExpress parameter operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the remote import DevExpress parameter operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Performs build for <see cref="RemoteImportDxParameterReader"/>, keeping the operation consistent with the state and invariants of the surrounding remote import DevExpress parameter workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the remote import DevExpress parameter operation and used when producing its result.</param>
    /// <param name="preview">Value indicating whether preview should apply to this operation.</param>
    /// <param name="confirmed">Value indicating whether confirmed should apply to this operation.</param>
    /// <returns>The remote knowledge import request produced by the operation.</returns>
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
                MaxFiles = Integer(parameters, "maxFiles", 0),
                MaxLinkedPages = Integer(parameters, "maxLinkedPages", 0),
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

/// <summary>
/// Represents an inspect remote knowledge function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="importer">Remote knowledge import service dependency used by the inspect remote knowledge function workflow to provide the corresponding application capability.</param>
/// <param name="parameters">Parameters value supplied to the inspect remote knowledge function operation and used when producing its result.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class InspectRemoteKnowledgeFunction(
    IRemoteKnowledgeImportService importer,
    RemoteImportDxParameterReader parameters,
    ILogger<InspectRemoteKnowledgeFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the inspect remote knowledge function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="InspectRemoteKnowledgeFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.knowledge.remote.inspect", "POST", "/api/dxai/functions/localgpt.knowledge.remote.inspect/invoke",
        "Downloads a user-selected public GitHub repository or webpage into the bounded cache and returns the exact file list plus regex matches without saving knowledge.",
        "JSON parameters: sourceUrl required; sourceKind, branch, fileIncludeRegex, maxFiles and maxLinkedPages optional. Omit maxFiles/maxLinkedPages or use non-positive values to use the database-backed MaxFiles policy; LocalGPT no longer imposes source-code repository or 50-page crawl ceilings.",
        "Network read with size, ZIP traversal and private-network protections. No database mutation.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["sourceUrl"],"properties":{"sourceUrl":{"type":"string"},"sourceKind":{"type":"string"},"branch":{"type":"string"},"fileIncludeRegex":{"type":"string"},"maxFiles":{"type":"integer"},"maxLinkedPages":{"type":"integer"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="InspectRemoteKnowledgeFunction"/>, keeping the operation consistent with the state and invariants of the surrounding inspect remote knowledge function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
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

/// <summary>Refreshes the database-backed project knowledge for a public GitHub repository without writing to that repository.</summary>
/// <param name="importer">Remote knowledge importer that owns bounded public GitHub retrieval.</param>
/// <param name="parameters">Parameter reader used to build the existing bounded remote import request.</param>
/// <param name="projectWorkspaceSync">Project synchronization service that materializes source-backed repository evidence.</param>
/// <param name="logger">Logger used to record refresh diagnostics without logging repository content.</param>
public sealed class RefreshRepositoryKnowledgeFunction(
    IRemoteKnowledgeImportService importer,
    RemoteImportDxParameterReader parameters,
    ILearningProjectWorkspaceSyncService projectWorkspaceSync,
    ILogger<RefreshRepositoryKnowledgeFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the runtime descriptor for the explicit repository knowledge refresh operation.</summary>
    /// <value>The descriptor exposed by <see cref="RefreshRepositoryKnowledgeFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.repository.knowledge.refresh",
        "POST",
        "/api/dxai/functions/localgpt.repository.knowledge.refresh/invoke",
        "Downloads the user-selected public GitHub repository through LocalGPT's bounded remote cache and synchronizes its project identity, exact source version, revision, workspace, SDK/framework requirements and full tracked-file structure into the project database.",
        "JSON parameters: sourceUrl required and must be a public github.com repository URL; branch optional and defaults to main; maxFiles optional and uses the database-backed remote-import policy when omitted or non-positive.",
        "User-invoked project knowledge maintenance only. Reads public GitHub source and updates LocalGPT's local project knowledge; it never writes to GitHub or the source repository and is not eligible for automatic model invocation.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: false,
        Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["sourceUrl"],"properties":{"sourceUrl":{"type":"string"},"branch":{"type":"string"},"maxFiles":{"type":"integer"}},"additionalProperties":false}""",
        IsCoordinationOnly: true);

    /// <summary>Invokes the bounded GitHub refresh and materializes the retrieved repository as project knowledge.</summary>
    /// <param name="request">DXFunction request containing the repository URL and optional branch.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The repository refresh result including the source revision and synchronized projects.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceUrl = parameters.String(request.Parameters, "sourceUrl").Trim();
            if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var sourceUri) ||
                !string.Equals(sourceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(sourceUri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
            {
                return new DxAiFunctionInvocationResult
                {
                    Succeeded = false,
                    Status = "Rejected",
                    Error = "Repository knowledge refresh requires an HTTPS github.com repository URL."
                };
            }

            var importRequest = parameters.Build(request.Parameters, preview: true, confirmed: false);
            importRequest.SourceUrl = sourceUrl;
            importRequest.SourceKind = "GitHub";
            importRequest.SaveToKnowledge = false;
            importRequest.PreviewOnly = true;
            importRequest.UserConfirmed = false;

            var remote = await importer.ImportAsync(importRequest, cancellationToken).ConfigureAwait(false);
            var projects = await projectWorkspaceSync.SynchronizeRemoteRepositoryAsync(remote, cancellationToken).ConfigureAwait(false);
            var value = new RepositoryKnowledgeRefreshResult(
                remote.SourceUrl,
                remote.ResolvedRevision,
                remote.DownloadedFileCount,
                projects);

            logger.LogInformation(
                "Repository knowledge refresh synchronized {ProjectCount} project(s) from public GitHub source host {SourceHost}.",
                projects.Count,
                sourceUri.Host);
            return new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = value };
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Repository knowledge refresh was cancelled.");
            else
                logger.LogError(exception, "Repository knowledge refresh failed; repository content and URL were omitted from logs.");
            throw;
        }
    }
}

/// <summary>
/// Represents an import remote knowledge function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="importer">Remote knowledge import service dependency used by the import remote knowledge function workflow to provide the corresponding application capability.</param>
/// <param name="parameters">Parameters value supplied to the import remote knowledge function operation and used when producing its result.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ImportRemoteKnowledgeFunction(
    IRemoteKnowledgeImportService importer,
    RemoteImportDxParameterReader parameters,
    ILogger<ImportRemoteKnowledgeFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the import remote knowledge function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ImportRemoteKnowledgeFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.knowledge.remote.import", "POST", "/api/dxai/functions/localgpt.knowledge.remote.import/invoke",
        "Imports a reviewed public GitHub repository or webpage through the existing learn-base service and associates resulting knowledge with role/topic tags.",
        "JSON parameters: sourceUrl required; sourceKind, branch, fileIncludeRegex, maxFiles, roleKeys, topics and saveToKnowledge optional. Omit maxFiles or use a non-positive value to use the database-backed MaxFiles policy.",
        "Requires fresh human confirmation before database writes. Source files remain in the local bounded cache and commercial game assets are not supplied.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["sourceUrl"],"properties":{"sourceUrl":{"type":"string"},"sourceKind":{"type":"string"},"branch":{"type":"string"},"fileIncludeRegex":{"type":"string"},"maxFiles":{"type":"integer"},"maxLinkedPages":{"type":"integer"},"roleKeys":{"type":"array","items":{"type":"string"}},"topics":{"type":"array","items":{"type":"string"}},"saveToKnowledge":{"type":"boolean"}},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ImportRemoteKnowledgeFunction"/>, keeping the operation consistent with the state and invariants of the surrounding import remote knowledge function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
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
