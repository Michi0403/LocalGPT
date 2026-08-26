using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Windows shell resolution for the shared console service.</summary>
public sealed class WindowsLocalConsolePlatformService : ILocalConsolePlatformService
{
    /// <inheritdoc />
    public LocalConsoleShellKind ResolveShell(LocalConsoleShellKind requestedShell) =>
        requestedShell == LocalConsoleShellKind.Auto ? LocalConsoleShellKind.PowerShell : requestedShell;

    /// <inheritdoc />
    public LocalConsolePlatformCommand CreateShellCommand(LocalConsoleShellKind shell, string commandText)
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

    /// <inheritdoc />
    public LocalConsolePlatformCommand CreatePowerShellScriptCommand(string scriptPath)
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

    private string FormatDisplayArgument(string value) => value.Any(char.IsWhiteSpace) ? $"\"{value}\"" : value;

    private LocalConsolePlatformCommand Create(string executable, LocalConsoleShellKind shell, List<string> arguments) => new()
    {
        Executable = executable,
        Shell = shell,
        Arguments = arguments,
        DisplayCommand = $"{Path.GetFileName(executable)} [LocalGPT-reviewed {shell} command]"
    };

    private string ResolvePowerShellExecutable()
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
}

/// <summary>Unix/macOS/Linux shell resolution for the shared console service.</summary>
public sealed class UnixLocalConsolePlatformService : ILocalConsolePlatformService
{
    /// <inheritdoc />
    public LocalConsoleShellKind ResolveShell(LocalConsoleShellKind requestedShell) =>
        requestedShell == LocalConsoleShellKind.Auto ? LocalConsoleShellKind.Bash : requestedShell;

    /// <inheritdoc />
    public LocalConsolePlatformCommand CreateShellCommand(LocalConsoleShellKind shell, string commandText)
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

    /// <inheritdoc />
    public LocalConsolePlatformCommand CreatePowerShellScriptCommand(string scriptPath)
    {
        var arguments = new List<string> { "-NoProfile", "-NonInteractive", "-File", scriptPath };
        var command = Create("pwsh", LocalConsoleShellKind.PowerShell, arguments);
        command.DisplayCommand = $"pwsh {string.Join(' ', arguments.Select(FormatDisplayArgument))}";
        return command;
    }

    private string FormatDisplayArgument(string value) => value.Any(char.IsWhiteSpace) ? $"\"{value}\"" : value;

    private LocalConsolePlatformCommand Create(string executable, LocalConsoleShellKind shell, List<string> arguments) => new()
    {
        Executable = executable,
        Shell = shell,
        Arguments = arguments,
        DisplayCommand = $"{Path.GetFileName(executable)} [LocalGPT-reviewed {shell} command]"
    };
}
