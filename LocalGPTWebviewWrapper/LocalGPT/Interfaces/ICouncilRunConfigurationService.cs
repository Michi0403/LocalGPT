using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ICouncilModelRequestLease : IDisposable
{
    CouncilHardwareRoadPlan Plan { get; }
    long Revision { get; }
    bool IsEnabled { get; }
}

public interface ICouncilRunConfigurationService
{
    event Action<Guid>? Changed;

    CouncilRunConfigurationSnapshot Ensure(
        MultiModelCouncilRequest request,
        IReadOnlyCollection<string> participants);

    CouncilPreparationConfiguration? GetPreparation();

    CouncilPreparationConfiguration SavePreparation(CouncilPreparationConfiguration configuration);

    CouncilRunConfigurationSnapshot? Get(Guid runId);

    bool Update(
        Guid runId,
        IReadOnlyCollection<OneWireCouncilModelRoute> routes,
        int resourceLoadPercent,
        int requestedMaxOutputTokens,
        int requestedMaxContextTokens,
        int? fallbackOllamaNumGpu,
        bool allowParallelHardwareRoads);

    void BeginRound(Guid runId, int round, string phase);

    CancellationToken GetRoundCancellationToken(Guid runId, int round, string phase);

    bool IsRoundSkipRequested(Guid runId, int round, string phase);

    bool RequestSkipCurrentRound(Guid runId);

    ValueTask<ICouncilModelRequestLease> AcquireModelRequestAsync(
        Guid runId,
        string modelName,
        CouncilHardwareRoadPlan fallbackPlan,
        CancellationToken cancellationToken = default);

    void Complete(Guid runId);
}
