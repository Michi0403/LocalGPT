using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Resolves shell executables and arguments through the host-specific console implementation.</summary>
public interface ILocalConsolePlatformService
{
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
}
