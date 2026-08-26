using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Resolves shell executables and arguments through the host-specific console implementation.</summary>
public interface ILocalConsolePlatformService
{
    /// <summary>Resolves Auto to the host's preferred interactive automation shell.</summary>
    LocalConsoleShellKind ResolveShell(LocalConsoleShellKind requestedShell);

    /// <summary>Creates one host-specific shell command without starting a process.</summary>
    LocalConsolePlatformCommand CreateShellCommand(LocalConsoleShellKind shell, string commandText);

    /// <summary>Creates a host-specific PowerShell command that executes one script file.</summary>
    LocalConsolePlatformCommand CreatePowerShellScriptCommand(string scriptPath);
}
