using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for council model request lease behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ICouncilModelRequestLease : IDisposable
{
    /// <summary>
    /// Gets the plan value that forms part of the council model request lease state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The plan value exposed by <see cref="ICouncilModelRequestLease"/>.</value>
    CouncilHardwareRoadPlan Plan { get; }
    /// <summary>
    /// Gets the revision value that forms part of the council model request lease state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision value exposed by <see cref="ICouncilModelRequestLease"/>.</value>
    long Revision { get; }
    /// <summary>
    /// Gets a value indicating whether enabled applies to the council model request lease state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="ICouncilModelRequestLease"/>.</value>
    bool IsEnabled { get; }
}

/// <summary>
/// Defines the contract for council run configuration behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ICouncilRunConfigurationService
{
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="ICouncilRunConfigurationService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    event Action<Guid>? Changed;

    /// <summary>
    /// Performs ensure as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="participants">String dependency used by the council run configuration workflow to provide the corresponding application capability.</param>
    /// <returns>The council run configuration snapshot produced by the operation.</returns>
    CouncilRunConfigurationSnapshot Ensure(
        MultiModelCouncilRequest request,
        IReadOnlyCollection<string> participants);

    /// <summary>
    /// Retrieves preparation as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The council preparation configuration produced by the operation.</returns>
    CouncilPreparationConfiguration? GetPreparation();

    /// <summary>
    /// Persists preparation as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
    /// <returns>The council preparation configuration produced by the operation.</returns>
    CouncilPreparationConfiguration SavePreparation(CouncilPreparationConfiguration configuration);

    /// <summary>
    /// Performs get as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>The council run configuration snapshot produced by the operation.</returns>
    CouncilRunConfigurationSnapshot? Get(Guid runId);

    /// <summary>
    /// Performs update as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="routes">One wire council model route dependency used by the council run configuration workflow to provide the corresponding application capability.</param>
    /// <param name="resourceLoadPercent">Resource load percent value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="requestedMaxOutputTokens">Requested max output tokens value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="requestedMaxContextTokens">Requested max context tokens value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="fallbackOllamaNumGpu">Fallback ollama num gpu value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="allowParallelHardwareRoads">Value indicating whether allow parallel hardware roads should apply to this operation.</param>
    /// <param name="maxParallelModels">Max parallel models value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="modelTimeoutSeconds">Model timeout seconds value supplied to the council run configuration operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Update(
        Guid runId,
        IReadOnlyCollection<OneWireCouncilModelRoute> routes,
        int resourceLoadPercent,
        int requestedMaxOutputTokens,
        int requestedMaxContextTokens,
        int? fallbackOllamaNumGpu,
        bool allowParallelHardwareRoads,
        int maxParallelModels,
        int modelTimeoutSeconds);

    /// <summary>
    /// Performs begin round as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="round">Round value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the council run configuration operation and used when producing its result.</param>
    void BeginRound(Guid runId, int round, string phase);

    /// <summary>
    /// Retrieves round cancellation token as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="round">Round value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the council run configuration operation and used when producing its result.</param>
    /// <returns>The cancellation token produced by the operation.</returns>
    CancellationToken GetRoundCancellationToken(Guid runId, int round, string phase);

    /// <summary>
    /// Determines whether round skip requested as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="round">Round value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="phase">Phase value supplied to the council run configuration operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool IsRoundSkipRequested(Guid runId, int round, string phase);

    /// <summary>
    /// Performs request skip current round as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool RequestSkipCurrentRound(Guid runId);

    /// <summary>
    /// Performs acquire model request as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="modelName">Model name value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="fallbackPlan">Fallback plan value supplied to the council run configuration operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The i council model request lease produced by the operation.</returns>
    ValueTask<ICouncilModelRequestLease> AcquireModelRequestAsync(
        Guid runId,
        string modelName,
        CouncilHardwareRoadPlan fallbackPlan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs complete as part of the council run configuration service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    void Complete(Guid runId);
}
