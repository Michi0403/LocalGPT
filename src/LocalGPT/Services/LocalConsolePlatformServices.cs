using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Windows shell resolution for the shared console service.</summary>
public sealed class WindowsLocalConsolePlatformService : ILocalConsolePlatformService
{
    /// <inheritdoc />
    public LocalConsoleShellKind ResolveShell(LocalConsoleShellKind requestedShell) 
    {
        try
        {
            return requestedShell == LocalConsoleShellKind.Auto ? LocalConsoleShellKind.PowerShell : requestedShell;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(ResolveShell), exception);
            throw;
        }
    }

    /// <inheritdoc />
    public LocalConsolePlatformCommand CreateShellCommand(LocalConsoleShellKind shell, string commandText)
    {
        try
        {
            var resolvedShell = ResolveShell(shell);
            return resolvedShell switch
            {
                LocalConsoleShellKind.PowerShell => Create(
                    ResolvePowerShellExecutable(),
                    resolvedShell,
                    ["-NoProfile", "-NonInteractive", "-Command", commandText]),
                LocalConsoleShellKind.Bash => Create("bash.exe", resolvedShell, ["-lc", commandText]),
                LocalConsoleShellKind.Cmd => Create("cmd.exe", resolvedShell, ["/d", "/s", "/c", commandText]),
                LocalConsoleShellKind.Direct => throw new InvalidOperationException("Direct commands are resolved by the common console service."),
                _ => throw new InvalidOperationException($"Unsupported console shell '{resolvedShell}'.")
            };
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(CreateShellCommand), exception);
            throw;
        }
    }

    /// <inheritdoc />
    public LocalConsolePlatformCommand CreatePowerShellScriptCommand(string scriptPath)
    {
        try
        {
            var executable = ResolvePowerShellExecutable();
            var arguments = new List<string> { "-NoProfile", "-NonInteractive" };
            if (Path.GetFileName(executable).Equals("powershell.exe", StringComparison.OrdinalIgnoreCase))
            {
                arguments.Add("-ExecutionPolicy");
                arguments.Add("Bypass");
            }
            arguments.Add("-File");
            arguments.Add(scriptPath);
            var command = Create(executable, LocalConsoleShellKind.PowerShell, arguments);
            command.DisplayCommand = $"{Path.GetFileName(executable)} {string.Join(' ', arguments.Select(FormatDisplayArgument))}";
            return command;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(CreatePowerShellScriptCommand), exception);
            throw;
        }
    }

    private string FormatDisplayArgument(string value) 
    {
        try
        {
            return value.Any(char.IsWhiteSpace) ? $"\"{value}\"" : value;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(FormatDisplayArgument), exception);
            throw;
        }
    }

    private LocalConsolePlatformCommand Create(string executable, LocalConsoleShellKind shell, List<string> arguments) 
    {
        try
        {
            return new()
    {
        Executable = executable,
        Shell = shell,
        Arguments = arguments,
        DisplayCommand = $"{Path.GetFileName(executable)} [LocalGPT-reviewed {shell} command]"
    };
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(Create), exception);
            throw;
        }
    }

    private string ResolvePowerShellExecutable()
    {
        try
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                var pwsh = Path.Combine(programFiles, "PowerShell", "7", "pwsh.exe");
                if (File.Exists(pwsh))
                    return pwsh;
            }
            return "powershell.exe";
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(ResolvePowerShellExecutable), exception);
            throw;
        }
    }
}

/// <summary>Unix/macOS/Linux shell resolution for the shared console service.</summary>
public sealed class UnixLocalConsolePlatformService : ILocalConsolePlatformService
{
    /// <inheritdoc />
    public LocalConsoleShellKind ResolveShell(LocalConsoleShellKind requestedShell) 
    {
        try
        {
            return requestedShell == LocalConsoleShellKind.Auto ? LocalConsoleShellKind.Bash : requestedShell;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(ResolveShell), exception);
            throw;
        }
    }

    /// <inheritdoc />
    public LocalConsolePlatformCommand CreateShellCommand(LocalConsoleShellKind shell, string commandText)
    {
        try
        {
            var resolvedShell = ResolveShell(shell);
            return resolvedShell switch
            {
                LocalConsoleShellKind.PowerShell => Create("pwsh", resolvedShell, ["-NoProfile", "-NonInteractive", "-Command", commandText]),
                LocalConsoleShellKind.Bash => Create(File.Exists("/bin/bash") ? "/bin/bash" : "bash", resolvedShell, ["-lc", commandText]),
                LocalConsoleShellKind.Cmd => throw new PlatformNotSupportedException("cmd.exe is not available on Unix hosts."),
                LocalConsoleShellKind.Direct => throw new InvalidOperationException("Direct commands are resolved by the common console service."),
                _ => throw new InvalidOperationException($"Unsupported console shell '{resolvedShell}'.")
            };
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(CreateShellCommand), exception);
            throw;
        }
    }

    /// <inheritdoc />
    public LocalConsolePlatformCommand CreatePowerShellScriptCommand(string scriptPath)
    {
        try
        {
            var arguments = new List<string> { "-NoProfile", "-NonInteractive", "-File", scriptPath };
            var command = Create("pwsh", LocalConsoleShellKind.PowerShell, arguments);
            command.DisplayCommand = $"pwsh {string.Join(' ', arguments.Select(FormatDisplayArgument))}";
            return command;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(CreatePowerShellScriptCommand), exception);
            throw;
        }
    }

    private string FormatDisplayArgument(string value) 
    {
        try
        {
            return value.Any(char.IsWhiteSpace) ? $"\"{value}\"" : value;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(FormatDisplayArgument), exception);
            throw;
        }
    }

    private LocalConsolePlatformCommand Create(string executable, LocalConsoleShellKind shell, List<string> arguments) 
    {
        try
        {
            return new()
    {
        Executable = executable,
        Shell = shell,
        Arguments = arguments,
        DisplayCommand = $"{Path.GetFileName(executable)} [LocalGPT-reviewed {shell} command]"
    };
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(Create), exception);
            throw;
        }
    }
}
