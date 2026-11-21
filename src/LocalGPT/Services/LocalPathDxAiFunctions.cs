using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Represents a browse local path function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="paths">Local path explorer service dependency used by the browse local path function workflow to provide the corresponding application capability.</param>
/// <param name="artifacts">Council artifact service that supplies the generated-workspace root guarded for project-bound automatic calls.</param>
/// <param name="dbContextFactory">Database context factory used to resolve which reviewed workspaces belong to the caller's bound project.</param>
/// <param name="runtimePolicy">Runtime policy data service that supplies the refusal text for foreign generated workspaces.</param>
public sealed class BrowseLocalPathFunction(
    ILocalPathExplorerService paths,
    ICouncilArtifactService artifacts,
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    ILocalGptRuntimePolicyDataService runtimePolicy) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the browse local path function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="BrowseLocalPathFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.path.browse", "POST", "/api/dxai/functions/localgpt.path.browse/invoke",
        "Browses one local folder so Chat or Council can present real filesystem choices instead of guessing paths.",
        "JSON parameters: path optional; includeFiles optional; maxEntries optional 1..1000.",
        "Read-only filesystem metadata. This function cannot create, edit, delete, execute, upload, or approve anything.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"path":{"type":"string","maxLength":2048},"includeFiles":{"type":"boolean"},"maxEntries":{"type":"integer","minimum":1,"maximum":1000}},"additionalProperties":false}
        """);

    /// <summary>
    /// Performs invoke for <see cref="BrowseLocalPathFunction"/>, keeping the operation consistent with the state and invariants of the surrounding browse local path function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var value = request.Parameters.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? new LocalPathBrowseRequest()
                : request.Parameters.Deserialize<LocalPathBrowseRequest>(new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true }) ?? new();
            if (request.AutomaticInvocation && request.ProjectId is Guid boundProjectId
                && !string.IsNullOrWhiteSpace(value.Path) && !IsOwnedOrOutsideArtifactRoot(value.Path, boundProjectId))
            {
                return Task.FromResult(new DxAiFunctionInvocationResult
                {
                    Succeeded = false,
                    Status = "Denied",
                    Error = runtimePolicy.GetString("CouncilForeignArtifactBrowseDenied")
                });
            }
            return Task.FromResult(new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = paths.Browse(value) });
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method BrowseLocalPathFunction.InvokeAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Determines whether a requested path is outside the generated-artifact root, or inside it only within a
    /// reviewed workspace that belongs to the caller's bound project. The artifact root itself is not owned.
    /// </summary>
    /// <param name="requestedPath">Path requested by the automatic function call.</param>
    /// <param name="boundProjectId">Project bound to the calling Council run.</param>
    /// <returns><see langword="true"/> when the path may be browsed by the bound automatic caller.</returns>
    private bool IsOwnedOrOutsideArtifactRoot(string requestedPath, Guid boundProjectId)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(artifacts.ArtifactRoot));
        string full;
        try { full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(requestedPath)); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { return true; }
        if (!full.Equals(root, StringComparison.OrdinalIgnoreCase)
            && !full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return true;
        using var db = dbContextFactory.CreateDbContext();
        var ownedReviewIds = db.CodeGenerationChangeReviews.AsNoTracking()
            .Where(review => review.ProjectId == boundProjectId)
            .Select(review => review.Id)
            .ToList();
        return ownedReviewIds.Any(id => full.Contains(id.ToString("N"), StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Represents a list local path roots function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="paths">Local path explorer service dependency used by the list local path roots function workflow to provide the corresponding application capability.</param>
public sealed class ListLocalPathRootsFunction(ILocalPathExplorerService paths) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the list local path roots function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="ListLocalPathRootsFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "localgpt.path.roots", "POST", "/api/dxai/functions/localgpt.path.roots/invoke",
        "Lists platform-appropriate local root suggestions for path selection.", "No parameters.",
        "Read-only filesystem metadata. Suggested roots are choices, not permission grants.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{},"additionalProperties":false}""");

    /// <summary>
    /// Performs invoke for <see cref="ListLocalPathRootsFunction"/>, keeping the operation consistent with the state and invariants of the surrounding list local path roots function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default) {
    try
    {
        return Task.FromResult(new DxAiFunctionInvocationResult { Succeeded = true, Status = "Completed", Value = paths.GetSuggestedRoots() });
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method ListLocalPathRootsFunction.InvokeAsync failed: {__serviceMethodException}");
        throw;
    }
}
}
