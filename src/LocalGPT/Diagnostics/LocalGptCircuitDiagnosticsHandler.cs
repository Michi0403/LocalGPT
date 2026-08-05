using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace LocalGPT.Diagnostics;

/// <summary>
/// Records circuit transitions so a disconnected browser is distinguishable from a blocked UI operation.
/// This handler never performs UI work and therefore cannot delay rendering or disposal.
/// </summary>
public sealed class LocalGptCircuitDiagnosticsHandler(
    ILogger<LocalGptCircuitDiagnosticsHandler> logger,
    IComponentActivityService componentActivity) : CircuitHandler
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit {CircuitId} opened.", circuit.Id);
        componentActivity.RecordInformation(nameof(LocalGptCircuitDiagnosticsHandler), "CircuitOpened", "A Blazor interactive circuit opened.");
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit {CircuitId} connection is up.", circuit.Id);
        componentActivity.RecordInformation(nameof(LocalGptCircuitDiagnosticsHandler), "ConnectionUp", "A Blazor interactive connection became available.");
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogWarning("Blazor circuit {CircuitId} connection went down.", circuit.Id);
        componentActivity.RecordWarning(nameof(LocalGptCircuitDiagnosticsHandler), "ConnectionDown", "A Blazor interactive connection was interrupted.");
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit {CircuitId} closed.", circuit.Id);
        componentActivity.RecordInformation(nameof(LocalGptCircuitDiagnosticsHandler), "CircuitClosed", "A Blazor interactive circuit closed.");
        return Task.CompletedTask;
    }
}
