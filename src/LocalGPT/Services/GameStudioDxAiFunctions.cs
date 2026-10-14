using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Gives Council artist/curator models a small, structured, read-only scene and review context.</summary>
/// <param name="json">Devexpress ai function json service dependency used by the get game studio scene function workflow to provide the corresponding application capability.</param>
/// <param name="studio">Game studio service dependency used by the get game studio scene function workflow to provide the corresponding application capability.</param>
/// <param name="engine">Engine value supplied to the get game studio scene function operation and used when producing its result.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class GetGameStudioSceneFunction(
    IDxAiFunctionJsonService json, IGameStudioService studio, GameSceneEngine engine,
    ILogger<GetGameStudioSceneFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Read-only scene context designed for small local models and team configurations.</summary>
    /// <value>The descriptor value exposed by <see cref="GetGameStudioSceneFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.game.scene.get", "POST", "/api/dxai/functions/project.game.scene.get/invoke",
        "Read the open Game project scene, fixed-step time scale, authored scenery, parallax bands, palette timeline and independent curation findings.",
        "JSON parameters: projectId required.",
        "Read-only. Visual prompts and geometry remain project-owned; no game-specific art is injected by the engine.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"projectId":{"type":"string","format":"uuid"}},"required":["projectId"],"additionalProperties":false}
        """);

    /// <summary>
    /// Performs invoke for <see cref="GetGameStudioSceneFunction"/>, keeping the operation consistent with the state and invariants of the surrounding get game studio scene function workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var input = json.Bind<GameStudioSceneGetParameters>(request.Parameters);
            if (!input.Succeeded) return json.InvalidParameters(input.Error);
            var draft = await studio.GetDraftAsync(input.Value.ProjectId, cancellationToken).ConfigureAwait(false);
            if (draft is null) return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "Open a Game Studio worktree first." };
            IReadOnlyList<GameSceneProposal> proposals = draft.ClosedRevisionId is null
                ? await studio.GetProposalsAsync(input.Value.ProjectId, draft.DraftId, cancellationToken).ConfigureAwait(false) : [];
            return json.Success(new { draft.ProjectId, draft.DraftId, draft.ClosedRevisionId, draft.Scene,
                Findings = engine.Curate(draft.Scene), Proposals = proposals.Select(item => new { item.Id, item.Artist, item.Description }) });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the Game Studio AI scene context failed.");
            throw;
        }
    }
}

/// <summary>Allows an artist model to submit a novel scene candidate without silently applying it.</summary>
/// <param name="json">Devexpress ai function json service dependency used by the propose game studio scene function workflow to provide the corresponding application capability.</param>
/// <param name="studio">Game studio service dependency used by the propose game studio scene function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ProposeGameStudioSceneFunction(
    IDxAiFunctionJsonService json, IGameStudioService studio,
    ILogger<ProposeGameStudioSceneFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Approval-gated AI authoring tool for unique just-in-time artwork.</summary>
    /// <value>The descriptor value exposed by <see cref="ProposeGameStudioSceneFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.game.scene.propose", "POST", "/api/dxai/functions/project.game.scene.propose/invoke",
        "Submit project-specific scene art, actors, scanline parallax, palettes or open-world data as an isolated creative candidate for a different curator AI.",
        "JSON parameters: projectId, draftId, proposal containing artist, description and scene object. Keep each proposal small for low-parameter local models.",
        "Writes only an isolated proposal artifact after the standard human approval. No build, world mutation or revision closure occurs.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"projectId":{"type":"string","format":"uuid"},"draftId":{"type":"string","format":"uuid"},"proposal":{"type":"object"}},"required":["projectId","draftId","proposal"],"additionalProperties":false}
        """);

    /// <summary>Creates a new bounded project-artifact proposal on behalf of an approved Council model.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var input = json.Bind<GameStudioSceneProposalParameters>(request.Parameters);
            if (!input.Succeeded) return json.InvalidParameters(input.Error);
            var proposal = await studio.ProposeSceneAsync(input.Value.ProjectId, input.Value.DraftId, input.Value.Proposal, true, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("AI submitted isolated Game Studio scene proposal {ProposalId} to draft {DraftId}.", proposal.Id, proposal.DraftId);
            return json.Success(new { proposal.Id, proposal.DraftId, ReviewRequired = true });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The game-art proposal DXFunction failed; art content was omitted from logs.");
            throw;
        }
    }
}

/// <summary>Lets a Council member inspect one exact scene candidate before independent artistic review.</summary>
/// <param name="json">Devexpress ai function json service dependency used by the get game studio proposal function workflow to provide the corresponding application capability.</param>
/// <param name="studio">Game studio service dependency used by the get game studio proposal function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class GetGameStudioProposalFunction(
    IDxAiFunctionJsonService json, IGameStudioService studio,
    ILogger<GetGameStudioProposalFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Read-only, proposal-scoped context to avoid flooding small local models with unrelated creative work.</summary>
    /// <value>The descriptor value exposed by <see cref="GetGameStudioProposalFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.game.scene.proposal.get", "POST", "/api/dxai/functions/project.game.scene.proposal.get/invoke",
        "Read the full authored scene of one isolated proposal so an independent Council curator can inspect it before reviewing.",
        "JSON parameters: projectId, draftId, proposalId.",
        "Read-only. A candidate does not become the active world until a separate reviewer and human approve it.",
        IsReadOnly: true, AvailableToAi: true, SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"projectId":{"type":"string","format":"uuid"},"draftId":{"type":"string","format":"uuid"},"proposalId":{"type":"string","format":"uuid"}},"required":["projectId","draftId","proposalId"],"additionalProperties":false}
        """);

    /// <summary>Returns exactly one creative candidate with artist provenance and all authored visual data.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var input = json.Bind<GameStudioProposalGetParameters>(request.Parameters);
            if (!input.Succeeded) return json.InvalidParameters(input.Error);
            var proposal = await studio.GetProposalAsync(input.Value.ProjectId, input.Value.DraftId, input.Value.ProposalId, cancellationToken).ConfigureAwait(false);
            return proposal is null ? new DxAiFunctionInvocationResult { Status = "NotFound", Error = "No matching proposal exists in this draft." } : json.Success(proposal);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading a Council scene proposal for independent curation failed.");
            throw;
        }
    }
}

/// <summary>Allows even a small model to submit one novel actor, palette phase or sprite instead of regenerating the entire world.</summary>
/// <param name="json">Devexpress ai function json service dependency used by the propose game studio scene patch function workflow to provide the corresponding application capability.</param>
/// <param name="studio">Game studio service dependency used by the propose game studio scene patch function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ProposeGameStudioScenePatchFunction(
    IDxAiFunctionJsonService json, IGameStudioService studio,
    ILogger<ProposeGameStudioScenePatchFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Approval-gated upsert-only artistic patch contract backed by isolated reviewable Project artifacts.</summary>
    /// <value>The descriptor value exposed by <see cref="ProposeGameStudioScenePatchFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.game.scene.patch.propose", "POST", "/api/dxai/functions/project.game.scene.patch.propose/invoke",
        "Propose one small, unique visual change to an existing scene: upsert actors, tile cells, sprites, animation frames, HUD, scanline planes or timed palettes by identity.",
        "JSON parameters: projectId, draftId, artist, description, patch. Patch has optional arrays: tiles, actors, sprites, layers, animations, hud, paletteTimeline. Omit everything you do not change.",
        "Stores an isolated full candidate after the existing one-use human gate; no active scene or revision is mutated.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"projectId":{"type":"string","format":"uuid"},"draftId":{"type":"string","format":"uuid"},"artist":{"type":"string"},"description":{"type":"string"},"patch":{"type":"object"}},"required":["projectId","draftId","artist","description","patch"],"additionalProperties":false}
        """);

    /// <summary>Applies an artist's narrow contribution to a temporary scene copy and stores the result for another AI to review.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var input = json.Bind<GameStudioScenePatchParameters>(request.Parameters);
            if (!input.Succeeded) return json.InvalidParameters(input.Error);
            var proposal = await studio.ProposePatchAsync(input.Value.ProjectId, input.Value.DraftId,
                input.Value.Patch, input.Value.Artist, input.Value.Description, true, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Submitted an isolated narrow AI game scene patch as proposal {ProposalId}.", proposal.Id);
            return json.Success(new { proposal.Id, proposal.DraftId, ReviewRequired = true });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "A narrow Council game-scene patch proposal failed; authored content was omitted from logs.");
            throw;
        }
    }
}

/// <summary>Allows a separate curator AI to critique and independently approve or reject an exact candidate.</summary>
/// <param name="json">Devexpress ai function json service dependency used by the review game studio scene function workflow to provide the corresponding application capability.</param>
/// <param name="studio">Game studio service dependency used by the review game studio scene function workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ReviewGameStudioSceneFunction(
    IDxAiFunctionJsonService json, IGameStudioService studio,
    ILogger<ReviewGameStudioSceneFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Approval-gated independent curation tool; service geometry checks always take precedence.</summary>
    /// <value>The descriptor value exposed by <see cref="ReviewGameStudioSceneFunction"/>.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "project.game.scene.review", "POST", "/api/dxai/functions/project.game.scene.review/invoke",
        "Review another model's specific art candidate for spatial coherence, unwanted decoration, visual originality and game-design intent.",
        "JSON parameters: projectId, draftId, review with proposalId, curatorModel, approved and critique.",
        "Writes curator feedback only after the normal approval gate; the service repeats semantic collision/exclusion validation. Human acceptance remains separate.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsDeferredApprovalRequest: true, Source: "DIHandler",
        ParameterSchemaJson: """
        {"type":"object","properties":{"projectId":{"type":"string","format":"uuid"},"draftId":{"type":"string","format":"uuid"},"review":{"type":"object"}},"required":["projectId","draftId","review"],"additionalProperties":false}
        """);

    /// <summary>Records a curator decision with spatial checks and a different model identity.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The DevExpress AI function invocation result produced by the operation.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var input = json.Bind<GameStudioSceneReviewParameters>(request.Parameters);
            if (!input.Succeeded) return json.InvalidParameters(input.Error);
            var review = await studio.ReviewProposalAsync(input.Value.ProjectId, input.Value.DraftId, input.Value.Review, true, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Stored independent Game Studio curator review for proposal {ProposalId}; accepted={Approved}.", review.ProposalId, review.Approved);
            return json.Success(review);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The independent game-art review DXFunction failed; critique content was omitted from logs.");
            throw;
        }
    }
}

/// <summary>Read context parameters.</summary>
public sealed class GameStudioSceneGetParameters { public Guid ProjectId { get; set; } }
/// <summary>Isolated creative proposal parameters.</summary>
public sealed class GameStudioSceneProposalParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this game studio scene proposal parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="GameStudioSceneProposalParameters"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable draft identifier used to identify or correlate this game studio scene proposal parameters instance with related application state.
    /// </summary>
    /// <value>The draft identifier value exposed by <see cref="GameStudioSceneProposalParameters"/>.</value>
    public Guid DraftId { get; set; }
    /// <summary>
    /// Gets or sets the proposal value that forms part of the game studio scene proposal parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The proposal value exposed by <see cref="GameStudioSceneProposalParameters"/>.</value>
    public GameSceneProposal Proposal { get; set; } = new();
}
/// <summary>Independent review submission parameters.</summary>
public sealed class GameStudioSceneReviewParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this game studio scene review parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="GameStudioSceneReviewParameters"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable draft identifier used to identify or correlate this game studio scene review parameters instance with related application state.
    /// </summary>
    /// <value>The draft identifier value exposed by <see cref="GameStudioSceneReviewParameters"/>.</value>
    public Guid DraftId { get; set; }
    /// <summary>
    /// Gets or sets the review value that forms part of the game studio scene review parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The review value exposed by <see cref="GameStudioSceneReviewParameters"/>.</value>
    public GameSceneReview Review { get; set; } = new();
}
/// <summary>Read one exact pending proposal.</summary>
public sealed class GameStudioProposalGetParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this game studio proposal get parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="GameStudioProposalGetParameters"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable draft identifier used to identify or correlate this game studio proposal get parameters instance with related application state.
    /// </summary>
    /// <value>The draft identifier value exposed by <see cref="GameStudioProposalGetParameters"/>.</value>
    public Guid DraftId { get; set; }
    /// <summary>
    /// Gets or sets the stable proposal identifier used to identify or correlate this game studio proposal get parameters instance with related application state.
    /// </summary>
    /// <value>The proposal identifier value exposed by <see cref="GameStudioProposalGetParameters"/>.</value>
    public Guid ProposalId { get; set; }
}
/// <summary>Constrained patch authoring contract for low-parameter Council models.</summary>
public sealed class GameStudioScenePatchParameters
{
    /// <summary>
    /// Gets or sets the stable project identifier used to identify or correlate this game studio scene patch parameters instance with related application state.
    /// </summary>
    /// <value>The project identifier value exposed by <see cref="GameStudioScenePatchParameters"/>.</value>
    public Guid ProjectId { get; set; }
    /// <summary>
    /// Gets or sets the stable draft identifier used to identify or correlate this game studio scene patch parameters instance with related application state.
    /// </summary>
    /// <value>The draft identifier value exposed by <see cref="GameStudioScenePatchParameters"/>.</value>
    public Guid DraftId { get; set; }
    /// <summary>
    /// Gets or sets the artist value that forms part of the game studio scene patch parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The artist value exposed by <see cref="GameStudioScenePatchParameters"/>.</value>
    public string Artist { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the game studio scene patch parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="GameStudioScenePatchParameters"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the patch value that forms part of the game studio scene patch parameters state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The patch value exposed by <see cref="GameStudioScenePatchParameters"/>.</value>
    public GameScenePatch Patch { get; set; } = new();
}
