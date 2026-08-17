using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Provides one bounded cross-platform console abstraction for reviewable Direct, PowerShell, Bash and cmd operations.</summary>
public interface IConsoleCommandService
{
    /// <summary>Raised after a new bounded console-output event is appended.</summary>
    event Action? Changed;
    /// <summary>Executes one confirmed command and captures bounded stdout/stderr.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local console command result produced by the operation.</returns>
    Task<LocalConsoleCommandResult> ExecuteAsync(LocalConsoleCommandRequest request, CancellationToken cancellationToken = default);
    /// <summary>Returns the recent bounded command-output feed used by ASCII console surfaces.</summary>
    /// <param name="take">Take value supplied to the console command operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<LocalConsoleOutputEvent> GetRecentOutput(int take = 120);
    /// <summary>Formats recent bounded output for monospace UI surfaces without putting string assembly in Razor components.</summary>
    /// <param name="take">Take value supplied to the console command operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string GetRecentDisplayText(int take = 120);
}
