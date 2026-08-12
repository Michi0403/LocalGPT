using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the council model request lease contract.
/// </summary>
public interface ICouncilModelRequestLease : IDisposable
{
    CouncilHardwareRoadPlan Plan { get; }
    long Revision { get; }
    bool IsEnabled { get; }
}

/// <summary>
/// Defines the council run configuration service contract.
/// </summary>
public interface ICouncilRunConfigurationService
{
    event Action<Guid>? Changed;

    /// <summary>
    /// Runs the ensure operation.
    /// </summary>
    CouncilRunConfigurationSnapshot Ensure(
        MultiModelCouncilRequest request,
        IReadOnlyCollection<string> participants);

    /// <summary>
    /// Gets preparation.
    /// </summary>
    CouncilPreparationConfiguration? GetPreparation();

    /// <summary>
    /// Saves preparation.
    /// </summary>
    CouncilPreparationConfiguration SavePreparation(CouncilPreparationConfiguration configuration);

    /// <summary>
    /// Runs the get operation.
    /// </summary>
    CouncilRunConfigurationSnapshot? Get(Guid runId);

    /// <summary>
    /// Runs the update operation.
    /// </summary>
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
    /// Runs the begin round operation.
    /// </summary>
    void BeginRound(Guid runId, int round, string phase);

    /// <summary>
    /// Gets round cancellation token.
    /// </summary>
    CancellationToken GetRoundCancellationToken(Guid runId, int round, string phase);

    /// <summary>
    /// Determines whether round skip requested.
    /// </summary>
    bool IsRoundSkipRequested(Guid runId, int round, string phase);

    /// <summary>
    /// Runs the request skip current round operation.
    /// </summary>
    bool RequestSkipCurrentRound(Guid runId);

    /// <summary>
    /// Runs the acquire model request async operation.
    /// </summary>
    ValueTask<ICouncilModelRequestLease> AcquireModelRequestAsync(
        Guid runId,
        string modelName,
        CouncilHardwareRoadPlan fallbackPlan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the complete operation.
    /// </summary>
    void Complete(Guid runId);
}
