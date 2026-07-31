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

    CouncilRunConfigurationSnapshot? Get(Guid runId);

    bool Update(
        Guid runId,
        IReadOnlyCollection<OneWireCouncilModelRoute> routes,
        int resourceLoadPercent);

    ValueTask<ICouncilModelRequestLease> AcquireModelRequestAsync(
        Guid runId,
        string modelName,
        CouncilHardwareRoadPlan fallbackPlan,
        CancellationToken cancellationToken = default);

    void Complete(Guid runId);
}
