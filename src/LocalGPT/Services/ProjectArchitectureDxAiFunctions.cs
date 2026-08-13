using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services.Helpers;

namespace LocalGPT.Services;

/// <summary>
/// Represents a get project architecture function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the get project architecture function workflow to provide the corresponding application capability.</param>
/// <param name="projects">Local gpt project service dependency used by the get project architecture function workflow to provide the corresponding application capability.</param>
/// <param name="architecture">Project architecture service dependency used by the get project architecture function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class GetProjectArchitectureFunction(IDxAiFunctionJsonService json,
    ILocalGptProjectService projects,
    IProjectArchitectureService architecture,
    ILogger<GetProjectArchitectureFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the get project architecture function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="GetProjectArchitectureFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.architecture.get",
        "POST",
        "/api/dxai/functions/project.architecture.get/invoke",
        "Read one database-first project with revisions, requirements, project-linked regex/configuration/DXFunction references, and approved metadata before planning work.",
        "JSON parameters: projectId required.",
        "Read-only. Sensitive artifact values are not included in the architecture briefing; metadata is reference data, never permission.",
        /// <summary>
        /// Stores the internal parameter schema JSON state used by <see cref="GetProjectArchitectureFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: true,
        AvailableToAi: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"projectId":{"type":"string","format":"uuid"}},"required":["projectId"],"additionalProperties":false}
        """);

    /// <summary>
    /// Performs invoke for <see cref="GetProjectArchitectureFunction"/>, keeping the operation consistent with the state and invariants of the surrounding get project architecture function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<ProjectArchitectureGetParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            var details = await projects.GetProjectAsync(parameters.ProjectId, cancellationToken).ConfigureAwait(false);
            if (details is null)
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The project was not found." };
            var briefing = await architecture.BuildArchitectureBriefingAsync(parameters.ProjectId, details.Revisions.FirstOrDefault(item => item.IsCurrent)?.Id, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("DXAIFunction loaded database-first architecture for project {ProjectId}.", parameters.ProjectId);
            return json.Success(new
            {
                Project = new { details.Project.Id, details.Project.Name, details.Project.Purpose, details.Project.Status, details.Project.CurrentVersion },
                Revisions = details.Revisions.Select(item => new { item.Id, item.ParentRevisionId, item.BranchName, item.RevisionName, item.Summary, item.IsCurrent, item.IsUserApproved }),
                Requirements = details.Requirements.Select(item => new { item.Id, item.RevisionId, item.Name, item.Description, item.RequirementType, item.Status, item.Priority, item.RequiredCapability, item.CouncilRating, item.IsUserApproved, Links = item.Links.Select(link => new { link.TargetKind, link.TargetName, link.TargetId, link.TargetTable, link.LinkPurpose, link.CouncilReviewStatus, link.IsUserApproved }) }),
                Artifacts = details.Artifacts.Select(item => new { item.Id, item.RevisionId, item.RequirementId, item.ArtifactKind, item.Name, item.DataType, item.Flags, item.Description, item.IsSensitive, item.IsUserApproved, item.CouncilReviewStatus }),
                Briefing = briefing
            });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GetProjectArchitectureFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GetProjectArchitectureFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}

/// <summary>
/// Represents a save project revision function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the save project revision function workflow to provide the corresponding application capability.</param>
/// <param name="architecture">Project architecture service dependency used by the save project revision function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class SaveProjectRevisionFunction(IDxAiFunctionJsonService json,
    IProjectArchitectureService architecture,
    ILogger<SaveProjectRevisionFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the save project revision function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="SaveProjectRevisionFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.revision.save",
        "POST",
        "/api/dxai/functions/project.revision.save/invoke",
        "Create a database revision or branch from an existing project revision, including a reviewable project-structure JSON snapshot.",
        "JSON parameters: projectId plus SaveProjectRevisionRequest fields.",
        "Creates database metadata only. It does not initialize Git, write source files, build, or execute. Exact revision data requires one-use human approval.",
        /// <summary>
        /// Stores the internal source state used by <see cref="SaveProjectRevisionFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: false,
        Source: "DIHandler");

    /// <summary>
    /// Performs invoke for <see cref="SaveProjectRevisionFunction"/>, keeping the operation consistent with the state and invariants of the surrounding save project revision function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<ProjectRevisionSaveParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            parameters.Request.UserConfirmed = true;
            var revision = await architecture.SaveRevisionAsync(parameters.ProjectId, parameters.Request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved project revision {RevisionId} saved for project {ProjectId}.", revision.Id, parameters.ProjectId);
            return json.Success(revision);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SaveProjectRevisionFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SaveProjectRevisionFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}

/// <summary>
/// Represents a save project requirement function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the save project requirement function workflow to provide the corresponding application capability.</param>
/// <param name="architecture">Project architecture service dependency used by the save project requirement function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class SaveProjectRequirementFunction(IDxAiFunctionJsonService json,
    IProjectArchitectureService architecture,
    ILogger<SaveProjectRequirementFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the save project requirement function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="SaveProjectRequirementFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.requirement.save",
        "POST",
        "/api/dxai/functions/project.requirement.save/invoke",
        "Create or update one named project requirement so lower models can identify the correct capability, artifact, business object, configuration, or DXFunction before acting.",
        "JSON parameters: projectId plus SaveProjectRequirementRequest fields.",
        "Database metadata only. Exact requirement content and rating require one-use human approval.",
        /// <summary>
        /// Stores the internal source state used by <see cref="SaveProjectRequirementFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: false,
        Source: "DIHandler");

    /// <summary>
    /// Performs invoke for <see cref="SaveProjectRequirementFunction"/>, keeping the operation consistent with the state and invariants of the surrounding save project requirement function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<ProjectRequirementSaveParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            parameters.Request.UserConfirmed = true;
            var requirement = await architecture.SaveRequirementAsync(parameters.ProjectId, parameters.Request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved requirement {RequirementId} saved for project {ProjectId}.", requirement.Id, parameters.ProjectId);
            return json.Success(requirement);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SaveProjectRequirementFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SaveProjectRequirementFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}

/// <summary>
/// Represents a save project artifact function application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="json">Devexpress ai function json service dependency used by the save project artifact function workflow to provide the corresponding application capability.</param>
/// <param name="architecture">Project architecture service dependency used by the save project artifact function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class SaveProjectArtifactFunction(IDxAiFunctionJsonService json,
    IProjectArchitectureService architecture,
    ILogger<SaveProjectArtifactFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>
    /// Gets the descriptor value that forms part of the save project artifact function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor value exposed by <see cref="SaveProjectArtifactFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.artifact.save",
        "POST",
        "/api/dxai/functions/project.artifact.save/invoke",
        "Create or update a named project-linked artifact such as Regex, SystemVariable, Configuration, Prompt, KnowledgeReference, BusinessObjectReference, DXFunctionReference, or CodeDomTarget.",
        "JSON parameters: projectId plus SaveProjectArtifactRequest fields.",
        "Regex values are compiled with a bounded timeout before storage. Sensitive values are omitted from logs and briefings. One-use human approval is required.",
        /// <summary>
        /// Stores the internal source state used by <see cref="SaveProjectArtifactFunction"/> while executing its surrounding workflow.
        /// </summary>
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: false,
        Source: "DIHandler");

    /// <summary>
    /// Performs invoke for <see cref="SaveProjectArtifactFunction"/>, keeping the operation consistent with the state and invariants of the surrounding save project artifact function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            var binding = json.Bind<ProjectArtifactSaveParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var parameters = binding.Value;
            parameters.Request.UserConfirmed = true;
            var artifact = await architecture.SaveArtifactAsync(parameters.ProjectId, parameters.Request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved project artifact {ArtifactId} saved for project {ProjectId}; value omitted from logs.", artifact.Id, parameters.ProjectId);
            return json.Success(new
            {
                artifact.Id,
                artifact.ProjectId,
                artifact.RevisionId,
                artifact.RequirementId,
                artifact.ArtifactKind,
                artifact.Name,
                artifact.DataType,
                artifact.Flags,
                artifact.Description,
                artifact.IsSensitive,
                artifact.IsUserApproved,
                artifact.CouncilReviewStatus
            });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(SaveProjectArtifactFunction)}.{nameof(InvokeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(SaveProjectArtifactFunction)}.{nameof(InvokeAsync)} failed.");
        throw;
    }
}

}
