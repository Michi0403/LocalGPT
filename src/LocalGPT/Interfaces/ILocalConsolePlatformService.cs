using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Resolves shell executables, host-supported signals, and shell arguments for LocalGPT's shared ASCII console.</summary>
public interface ILocalConsolePlatformService
{
    /// <summary>Returns the shell backends currently available on this host, including the resolved automatic default.</summary>
    /// <returns>The available shell descriptor collection.</returns>
    IReadOnlyList<LocalConsoleShellDescriptor> GetAvailableShells();

    /// <summary>
    /// Resolves shell as part of the local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requestedShell">Requested shell value supplied to the local console platform operation and used when producing its result.</param>
    /// <returns>The local console shell kind produced by the operation.</returns>
    LocalConsoleShellKind ResolveShell(LocalConsoleShellKind requestedShell);

    /// <summary>
    /// Creates shell command as part of the local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="shell">Shell value supplied to the local console platform operation and used when producing its result.</param>
    /// <param name="commandText">Command text value supplied to the local console platform operation and used when producing its result.</param>
    /// <returns>The local console platform command produced by the operation.</returns>
    LocalConsolePlatformCommand CreateShellCommand(LocalConsoleShellKind shell, string commandText);

    /// <summary>Creates a host-specific PowerShell command that executes one script file.</summary>
    /// <param name="scriptPath">Script path value supplied to the local console platform operation and used when producing its result.</param>
    /// <returns>The local console platform command produced by the operation.</returns>
    LocalConsolePlatformCommand CreatePowerShellScriptCommand(string scriptPath);

    /// <summary>Sends one host-supported signal to an already-running process without opening an external terminal window.</summary>
    /// <param name="processId">Operating-system process identifier.</param>
    /// <param name="signal">Signal requested by the operator.</param>
    /// <param name="cancellationToken">Cancellation token for the signal-delivery operation itself.</param>
    /// <returns>A task that completes after signal delivery succeeds or fails.</returns>
    Task SendSignalAsync(int processId, LocalConsoleSignalKind signal, CancellationToken cancellationToken = default);
}
