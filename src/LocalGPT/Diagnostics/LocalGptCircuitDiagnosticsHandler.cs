using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace LocalGPT.Diagnostics;

/// <summary>
/// Records circuit transitions so a disconnected browser is distinguishable from a blocked UI operation.
/// This handler never performs UI work and therefore cannot delay rendering or disposal.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
/// <param name="componentActivity">Component activity service dependency used by the LocalGPT circuit diagnostics workflow to provide the corresponding application capability.</param>
public sealed class LocalGptCircuitDiagnosticsHandler(
    ILogger<LocalGptCircuitDiagnosticsHandler> logger,
    IComponentActivityService componentActivity) : CircuitHandler
{
    /// <summary>
    /// Handles the circuit opened async lifecycle or event notification for <see cref="LocalGptCircuitDiagnosticsHandler"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="circuit">Circuit value supplied to the LocalGPT circuit diagnostics operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit {CircuitId} opened.", circuit.Id);
        componentActivity.RecordInformation(nameof(LocalGptCircuitDiagnosticsHandler), "CircuitOpened", "A Blazor interactive circuit opened.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the connection up async lifecycle or event notification for <see cref="LocalGptCircuitDiagnosticsHandler"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="circuit">Circuit value supplied to the LocalGPT circuit diagnostics operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit {CircuitId} connection is up.", circuit.Id);
        componentActivity.RecordInformation(nameof(LocalGptCircuitDiagnosticsHandler), "ConnectionUp", "A Blazor interactive connection became available.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the connection down async lifecycle or event notification for <see cref="LocalGptCircuitDiagnosticsHandler"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="circuit">Circuit value supplied to the LocalGPT circuit diagnostics operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogWarning("Blazor circuit {CircuitId} connection went down.", circuit.Id);
        componentActivity.RecordWarning(nameof(LocalGptCircuitDiagnosticsHandler), "ConnectionDown", "A Blazor interactive connection was interrupted.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the circuit closed async lifecycle or event notification for <see cref="LocalGptCircuitDiagnosticsHandler"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="circuit">Circuit value supplied to the LocalGPT circuit diagnostics operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit {CircuitId} closed.", circuit.Id);
        componentActivity.RecordInformation(nameof(LocalGptCircuitDiagnosticsHandler), "CircuitClosed", "A Blazor interactive circuit closed.");
        return Task.CompletedTask;
    }
}
