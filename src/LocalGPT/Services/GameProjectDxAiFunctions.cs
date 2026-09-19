using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Reads the persisted editable authoring profile owned by one LocalGPT Game project.</summary>
/// <param name="json">DX function JSON binding service.</param>
/// <param name="gameProjects">Game project service that owns project-level authoring data.</param>
/// <param name="logger">Logger used for bounded diagnostics.</param>
public sealed class GetProjectGameProfileFunction(
    IDxAiFunctionJsonService json,
    IGameProjectService gameProjects,
    ILogger<GetProjectGameProfileFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the stable DX function contract used to read project-owned game authoring data.</summary>
    /// <value>The read-only game-project profile descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.game.profile.get", "POST", "/api/dxai/functions/project.game.profile.get/invoke",
        "Reads the persisted editable authoring profile for one LocalGPT Game project without building or starting it.",
        "JSON parameters: projectId required.",
        "Read-only project metadata. User requirements remain separate first-class Project requirement records.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"projectId":{"type":"string","format":"uuid"}},"required":["projectId"],"additionalProperties":false}
        """);

    /// <summary>Invokes the read-only authoring-profile lookup.</summary>
    /// <param name="request">AI function invocation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authoring profile or a not-found result.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ProjectGameProfileGetParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var profile = await gameProjects.GetProfileAsync(binding.Value.ProjectId, cancellationToken).ConfigureAwait(false);
            if (profile is null)
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The Game project has no persisted authoring profile yet." };
            logger.LogDebug("DXAIFunction read the game authoring profile for project {ProjectId}; profile content was omitted from logs.", binding.Value.ProjectId);
            return json.Success(profile);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Reading a project game authoring profile was cancelled.");
            else
                logger.LogError(exception, "Reading a project game authoring profile failed; profile content was omitted.");
            throw;
        }
    }
}

/// <summary>Saves the editable authoring profile for one LocalGPT Game project after the normal human approval gate.</summary>
/// <param name="json">DX function JSON binding service.</param>
/// <param name="gameProjects">Game project service that owns authoring policy and persistence.</param>
/// <param name="logger">Logger used for bounded diagnostics.</param>
public sealed class SaveProjectGameProfileFunction(
    IDxAiFunctionJsonService json,
    IGameProjectService gameProjects,
    ILogger<SaveProjectGameProfileFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the stable DX function contract used to save project-owned game authoring data.</summary>
    /// <value>The approval-gated game-project profile descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.game.profile.save", "POST", "/api/dxai/functions/project.game.profile.save/invoke",
        "Creates or updates the durable authoring profile for one LocalGPT Game project without building or starting a runtime session.",
        "JSON parameters: projectId plus request containing game key, title, runtime profile, Council/control/director defaults, frame size, ASCII color mode/default foreground/background indexes, map seed and scenario.",
        "Writes project-owned authoring metadata only after one-use human approval. Requirements must be persisted through project.requirement.save rather than hidden in this profile.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: false, Source: "DIHandler");

    /// <summary>Invokes the approved game-project authoring-profile save.</summary>
    /// <param name="request">AI function invocation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted authoring profile.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ProjectGameProfileSaveParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            binding.Value.Request.UserConfirmed = true;
            var result = await gameProjects.SaveProfileAsync(binding.Value.ProjectId, binding.Value.Request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved game authoring profile saved for project {ProjectId} as profile {ProfileId}.", binding.Value.ProjectId, result.Id);
            return json.Success(result);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Saving a project game authoring profile was cancelled.");
            else
                logger.LogError(exception, "Saving a project game authoring profile failed; profile content was omitted.");
            throw;
        }
    }
}

/// <summary>Reads the latest compiled game definition owned by one LocalGPT project.</summary>
/// <param name="json">DX function JSON binding service.</param>
/// <param name="gameProjects">Game project service that owns project-level build data.</param>
/// <param name="logger">Logger used for bounded diagnostics.</param>
public sealed class GetProjectGameFunction(
    IDxAiFunctionJsonService json,
    IGameProjectService gameProjects,
    ILogger<GetProjectGameFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the stable DX function contract used to read a project-owned game build.</summary>
    /// <value>The read-only project game lookup descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.game.get", "POST", "/api/dxai/functions/project.game.get/invoke",
        "Reads the latest project-owned LocalGPT game build without starting a runtime session.",
        "JSON parameters: projectId required.",
        "Read-only project metadata. The game runtime consumes the compiled definition but does not own project authoring.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"projectId":{"type":"string","format":"uuid"}},"required":["projectId"],"additionalProperties":false}
        """);

    /// <summary>Invokes the read-only project game lookup.</summary>
    /// <param name="request">AI function invocation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest build or a not-found result.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ProjectGameGetParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var build = await gameProjects.GetBuildAsync(binding.Value.ProjectId, cancellationToken).ConfigureAwait(false);
            if (build is null)
                return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "The game project has no approved runtime definition yet." };
            logger.LogDebug("DXAIFunction read the game build for project {ProjectId}; definition content was omitted from logs.", binding.Value.ProjectId);
            return json.Success(build);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Reading a project game build was cancelled.");
            else
                logger.LogError(exception, "Reading a project game build failed; definition content was omitted.");
            throw;
        }
    }
}

/// <summary>Builds a project-owned game definition after the normal human approval gate.</summary>
/// <param name="json">DX function JSON binding service.</param>
/// <param name="gameProjects">Game project service that owns build policy and persistence.</param>
/// <param name="logger">Logger used for bounded diagnostics.</param>
public sealed class BuildProjectGameFunction(
    IDxAiFunctionJsonService json,
    IGameProjectService gameProjects,
    ILogger<BuildProjectGameFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the stable DX function contract used to build a project-owned game definition.</summary>
    /// <value>The approval-gated project game build descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.game.build", "POST", "/api/dxai/functions/project.game.build/invoke",
        "Builds one LocalGPT Game project into a persisted runtime definition that GameDirector can consume.",
        "JSON parameters: projectId plus an approval request. Editable game settings must already be persisted through project.game.profile.save.",
        "Compiles the persisted authoring profile and user-approved Project requirement baseline into a reviewed project artifact after one-use human approval. It does not mutate design state, start a game session, or write source files.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true,
        ApprovalRequiredBeforeCompletion: false, Source: "DIHandler");

    /// <summary>Invokes the approved game-project build.</summary>
    /// <param name="request">AI function invocation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted build result.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ProjectGameBuildParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            binding.Value.Request.UserConfirmed = true;
            var result = await gameProjects.BuildAsync(binding.Value.ProjectId, binding.Value.Request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved project game build completed for project {ProjectId} as artifact {ArtifactId}.", binding.Value.ProjectId, result.ArtifactId);
            return json.Success(result);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Building a project game was cancelled.");
            else
                logger.LogError(exception, "Building a project game failed; definition content was omitted.");
            throw;
        }
    }
}

/// <summary>Starts the latest approved build of one Game project inside the current LocalGPT chat/Council context.</summary>
/// <param name="json">DX function JSON binding service.</param>
/// <param name="gameProjects">Game project service used to resolve the project build.</param>
/// <param name="ambientContext">Ambient Council context used to bind runtime ownership.</param>
/// <param name="logger">Logger used for bounded diagnostics.</param>
public sealed class StartProjectGameFunction(
    IDxAiFunctionJsonService json,
    IGameProjectService gameProjects,
    IAmbientLocalGptContext ambientContext,
    ILogger<StartProjectGameFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the stable DX function contract used to launch an approved project game build.</summary>
    /// <value>The coordination-only project game launch descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.game.start", "POST", "/api/dxai/functions/project.game.start/invoke",
        "Starts the latest approved build of a LocalGPT Game project in the current /Chat game runtime.",
        "JSON parameters: projectId required.",
        "Coordination-only. It loads an already-built project definition and starts the shared game runtime; it does not modify project authoring data.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        IsCoordinationOnly: true,
        ParameterSchemaJson: """
        {"type":"object","properties":{"projectId":{"type":"string","format":"uuid"}},"required":["projectId"],"additionalProperties":false}
        """);

    /// <summary>Invokes the project-game launch.</summary>
    /// <param name="request">AI function invocation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authoritative game runtime snapshot.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<ProjectGameStartParameters>(request.Parameters);
            if (!binding.Succeeded)
                return json.InvalidParameters(binding.Error);
            var snapshot = await gameProjects.LaunchAsync(binding.Value.ProjectId, new LaunchProjectGameRequest
            {
                ConversationId = request.ConversationId,
                CouncilRunId = ambientContext.Current.CouncilRunId,
                StartedBy = "LocalGPT AI Council"
            }, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("DXAIFunction started project game {GameSessionId} from project {ProjectId}.", snapshot.Id, binding.Value.ProjectId);
            return json.Success(snapshot);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Starting a project game was cancelled.");
            else
                logger.LogError(exception, "Starting a project game failed.");
            throw;
        }
    }
}
