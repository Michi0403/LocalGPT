using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using LocalGPT.Services.Helpers;

namespace LocalGPT.Services;

public sealed class GetProjectArchitectureFunction(IDxAiFunctionJsonService json,
    ILocalGptProjectService projects,
    IProjectArchitectureService architecture,
    ILogger<GetProjectArchitectureFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.architecture.get",
        "POST",
        "/api/dxai/functions/project.architecture.get/invoke",
        "Read one database-first project with revisions, requirements, project-linked regex/configuration/DXFunction references, and approved metadata before planning work.",
        "JSON parameters: projectId required.",
        "Read-only. Sensitive artifact values are not included in the architecture briefing; metadata is reference data, never permission.",
        IsReadOnly: true,
        AvailableToAi: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"projectId":{"type":"string","format":"uuid"}},"required":["projectId"],"additionalProperties":false}
        """);

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<Parameters>(request.Parameters);
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

    private sealed class Parameters { public Guid ProjectId { get; set; } }
}

public sealed class SaveProjectRevisionFunction(IDxAiFunctionJsonService json,
    IProjectArchitectureService architecture,
    ILogger<SaveProjectRevisionFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.revision.save",
        "POST",
        "/api/dxai/functions/project.revision.save/invoke",
        "Create a database revision or branch from an existing project revision, including a reviewable project-structure JSON snapshot.",
        "JSON parameters: projectId plus SaveProjectRevisionRequest fields.",
        "Creates database metadata only. It does not initialize Git, write source files, build, or execute. Exact revision data requires one-use human approval.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: false,
        Source: "DIHandler");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<Parameters>(request.Parameters);
        if (!binding.Succeeded)
            return json.InvalidParameters(binding.Error);
        var parameters = binding.Value;
        parameters.Request.UserConfirmed = true;
        var revision = await architecture.SaveRevisionAsync(parameters.ProjectId, parameters.Request, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Approved project revision {RevisionId} saved for project {ProjectId}.", revision.Id, parameters.ProjectId);
        return json.Success(revision);
    }

    private sealed class Parameters
    {
        public Guid ProjectId { get; set; }
        public SaveProjectRevisionRequest Request { get; set; } = new();
    }
}

public sealed class SaveProjectRequirementFunction(IDxAiFunctionJsonService json,
    IProjectArchitectureService architecture,
    ILogger<SaveProjectRequirementFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.requirement.save",
        "POST",
        "/api/dxai/functions/project.requirement.save/invoke",
        "Create or update one named project requirement so lower models can identify the correct capability, artifact, business object, configuration, or DXFunction before acting.",
        "JSON parameters: projectId plus SaveProjectRequirementRequest fields.",
        "Database metadata only. Exact requirement content and rating require one-use human approval.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: false,
        Source: "DIHandler");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<Parameters>(request.Parameters);
        if (!binding.Succeeded)
            return json.InvalidParameters(binding.Error);
        var parameters = binding.Value;
        parameters.Request.UserConfirmed = true;
        var requirement = await architecture.SaveRequirementAsync(parameters.ProjectId, parameters.Request, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Approved requirement {RequirementId} saved for project {ProjectId}.", requirement.Id, parameters.ProjectId);
        return json.Success(requirement);
    }

    private sealed class Parameters
    {
        public Guid ProjectId { get; set; }
        public SaveProjectRequirementRequest Request { get; set; } = new();
    }
}

public sealed class SaveProjectArtifactFunction(IDxAiFunctionJsonService json,
    IProjectArchitectureService architecture,
    ILogger<SaveProjectArtifactFunction> logger) : IDxAiFunctionHandler
{
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.artifact.save",
        "POST",
        "/api/dxai/functions/project.artifact.save/invoke",
        "Create or update a named project-linked artifact such as Regex, SystemVariable, Configuration, Prompt, KnowledgeReference, BusinessObjectReference, DXFunctionReference, or CodeDomTarget.",
        "JSON parameters: projectId plus SaveProjectArtifactRequest fields.",
        "Regex values are compiled with a bounded timeout before storage. Sensitive values are omitted from logs and briefings. One-use human approval is required.",
        IsReadOnly: false,
        AvailableToAi: true,
        RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true,
        SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: false,
        Source: "DIHandler");

    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var binding = json.Bind<Parameters>(request.Parameters);
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

    private sealed class Parameters
    {
        public Guid ProjectId { get; set; }
        public SaveProjectArtifactRequest Request { get; set; } = new();
    }
}
