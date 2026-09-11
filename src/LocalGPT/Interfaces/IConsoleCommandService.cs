using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Provides one bounded cross-platform console abstraction for reviewable Direct, PowerShell, Bash, Zsh, sh and cmd operations.</summary>
public interface IConsoleCommandService
{
    /// <summary>Raised after console output, active jobs, or operator state changes.</summary>
    event Action? Changed;

    /// <summary>Executes one confirmed command and captures bounded stdout/stderr before returning.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local console command result produced by the operation.</returns>
    Task<LocalConsoleCommandResult> ExecuteAsync(LocalConsoleCommandRequest request, CancellationToken cancellationToken = default);

    /// <summary>Starts one confirmed command under service supervision and returns immediately so the ASCII operator line remains usable.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The stable operation identifier assigned to the background command.</returns>
    Guid Start(LocalConsoleCommandRequest request);

    /// <summary>Returns currently running console operations with process identifiers when available.</summary>
    /// <returns>The active operation collection.</returns>
    IReadOnlyList<LocalConsoleOperationSnapshot> GetActiveOperations();

    /// <summary>Requests cancellation of one service-owned console operation.</summary>
    /// <param name="operationId">Stable operation identifier.</param>
    /// <returns><see langword="true"/> when an active operation accepted the cancellation request.</returns>
    bool Cancel(Guid operationId);

    /// <summary>Sends one host-supported signal to a service-owned running console process.</summary>
    /// <param name="operationId">Stable operation identifier.</param>
    /// <param name="signal">Signal requested by the operator.</param>
    /// <param name="cancellationToken">Cancellation token for signal delivery.</param>
    /// <returns>A task that completes after signal delivery.</returns>
    Task SendSignalAsync(Guid operationId, LocalConsoleSignalKind signal, CancellationToken cancellationToken = default);

    /// <summary>Returns shell backends available on the current host.</summary>
    /// <returns>The available shell descriptor collection.</returns>
    IReadOnlyList<LocalConsoleShellDescriptor> GetAvailableShells();

    /// <summary>Clears the bounded display/history queue without affecting active processes.</summary>
    void ClearRecentOutput();

    /// <summary>Appends one bounded system/operator message to the shared ASCII console feed.</summary>
    /// <param name="text">Message safe for direct user display.</param>
    void PublishOperatorMessage(string text);

    /// <summary>Returns the recent bounded command-output feed used by ASCII console surfaces.</summary>
    /// <param name="take">Take value supplied to the console command operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<LocalConsoleOutputEvent> GetRecentOutput(int take = 120);

    /// <summary>Formats recent bounded output for monospace UI surfaces without putting string assembly in Razor components.</summary>
    /// <param name="take">Take value supplied to the console command operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string GetRecentDisplayText(int take = 120);
}
