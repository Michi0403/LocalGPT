using System.Diagnostics;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Windows shell resolution and process-control semantics for the shared LocalGPT ASCII console.</summary>
public sealed class WindowsLocalConsolePlatformService : ILocalConsolePlatformService
{
    /// <summary>
    /// Retrieves available shells as part of the windows local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public IReadOnlyList<LocalConsoleShellDescriptor> GetAvailableShells()
    {
        try
        {
            var signals = GetSupportedSignals();
            var powerShell = ResolvePowerShellExecutable();
            var descriptors = new List<LocalConsoleShellDescriptor>
            {
                CreateDescriptor(LocalConsoleShellKind.Auto, $"Auto ({Path.GetFileName(powerShell)})", powerShell, true, signals),
                CreateDescriptor(LocalConsoleShellKind.PowerShell, "PowerShell", powerShell, true, signals),
                CreateDescriptor(LocalConsoleShellKind.Cmd, "Command Prompt", Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe", false, signals)
            };

            AddIfAvailable(descriptors, LocalConsoleShellKind.Bash, "Bash", FindExecutableOnPath("bash.exe"), signals);
            AddIfAvailable(descriptors, LocalConsoleShellKind.Zsh, "Z shell", FindExecutableOnPath("zsh.exe"), signals);
            AddIfAvailable(descriptors, LocalConsoleShellKind.Sh, "POSIX sh", FindExecutableOnPath("sh.exe"), signals);
            return descriptors;
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(GetAvailableShells), exception);
            throw;
        }
    }

    /// <summary>
    /// Resolves shell as part of the windows local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public LocalConsoleShellKind ResolveShell(LocalConsoleShellKind requestedShell)
    {
        try
        {
            return requestedShell == LocalConsoleShellKind.Auto ? LocalConsoleShellKind.PowerShell : requestedShell;
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(ResolveShell), exception);
            throw;
        }
    }

    /// <summary>
    /// Creates shell command as part of the windows local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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
                LocalConsoleShellKind.Bash => Create(RequireExecutable("bash.exe"), resolvedShell, ["-lc", commandText]),
                LocalConsoleShellKind.Zsh => Create(RequireExecutable("zsh.exe"), resolvedShell, ["-lc", commandText]),
                LocalConsoleShellKind.Sh => Create(RequireExecutable("sh.exe"), resolvedShell, ["-lc", commandText]),
                LocalConsoleShellKind.Cmd => Create(Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe", resolvedShell, ["/d", "/s", "/c", commandText]),
                LocalConsoleShellKind.Direct => throw new InvalidOperationException("Direct commands are resolved by the common console service."),
                _ => throw new InvalidOperationException($"Unsupported console shell '{resolvedShell}'.")
            };
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(CreateShellCommand), exception);
            throw;
        }
    }

    /// <summary>
    /// Creates power shell script command as part of the windows local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(CreatePowerShellScriptCommand), exception);
            throw;
        }
    }

    /// <summary>
    /// Performs send signal as part of the windows local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public Task SendSignalAsync(int processId, LocalConsoleSignalKind signal, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (signal is LocalConsoleSignalKind.Interrupt or LocalConsoleSignalKind.Hangup)
                throw new PlatformNotSupportedException($"{signal} is not available for redirected Windows console jobs. Use Terminate, Kill, or the operator cancellation control.");

            using var process = Process.GetProcessById(processId);
            if (!process.HasExited)
                process.Kill(entireProcessTree: signal == LocalConsoleSignalKind.Kill);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(SendSignalAsync), exception);
            throw;
        }
    }

    /// <summary>Formats one diagnostic-only script argument without exposing command text through service logs.</summary>
    /// <param name="value">Value value supplied to the windows local console platform operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string FormatDisplayArgument(string value)
    {
        try
        {
            return value.Any(char.IsWhiteSpace) ? $"\"{value}\"" : value;
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(FormatDisplayArgument), exception);
            throw;
        }
    }

    /// <summary>
    /// Performs create as part of the windows local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="executable">Executable value supplied to the windows local console platform operation and used when producing its result.</param>
    /// <param name="shell">Shell value supplied to the windows local console platform operation and used when producing its result.</param>
    /// <param name="arguments">Arguments value supplied to the windows local console platform operation and used when producing its result.</param>
    /// <returns>The local console platform command produced by the operation.</returns>
    private LocalConsolePlatformCommand Create(string executable, LocalConsoleShellKind shell, List<string> arguments)
    {
        try
        {
            return new LocalConsolePlatformCommand
            {
                Executable = executable,
                Shell = shell,
                Arguments = arguments,
                DisplayCommand = $"{Path.GetFileName(executable)} [LocalGPT-reviewed {shell} command]"
            };
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(Create), exception);
            throw;
        }
    }

    /// <summary>Resolves the preferred PowerShell executable, preferring modern pwsh when installed.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string ResolvePowerShellExecutable()
    {
        try
        {
            var onPath = FindExecutableOnPath("pwsh.exe");
            if (!string.IsNullOrWhiteSpace(onPath))
                return onPath;
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
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(ResolvePowerShellExecutable), exception);
            throw;
        }
    }

    /// <summary>Requires an optional Windows shell executable to be installed before use.</summary>
    /// <param name="fileName">File name value supplied to the windows local console platform operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RequireExecutable(string fileName)
    {
        try
        {
            return FindExecutableOnPath(fileName)
                ?? throw new FileNotFoundException($"Shell executable '{fileName}' is not available on PATH.");
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(RequireExecutable), exception);
            throw;
        }
    }

    /// <summary>Finds one executable using Windows PATH semantics.</summary>
    /// <param name="fileName">File name value supplied to the windows local console platform operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string? FindExecutableOnPath(string fileName)
    {
        try
        {
            foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                         .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var candidate = Path.Combine(directory.Trim('"'), fileName);
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
            }
            return null;
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(FindExecutableOnPath), exception);
            throw;
        }
    }

    /// <summary>
    /// Adds if available as part of the windows local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="descriptors">Descriptors value supplied to the windows local console platform operation and used when producing its result.</param>
    /// <param name="shell">Shell value supplied to the windows local console platform operation and used when producing its result.</param>
    /// <param name="displayName">Display name value supplied to the windows local console platform operation and used when producing its result.</param>
    /// <param name="executable">Executable value supplied to the windows local console platform operation and used when producing its result.</param>
    /// <param name="signals">Signals value supplied to the windows local console platform operation and used when producing its result.</param>
    private void AddIfAvailable(List<LocalConsoleShellDescriptor> descriptors, LocalConsoleShellKind shell, string displayName, string? executable, List<LocalConsoleSignalKind> signals)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(executable))
                descriptors.Add(CreateDescriptor(shell, displayName, executable, false, signals));
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(AddIfAvailable), exception);
            throw;
        }
    }

    /// <summary>Creates a shell descriptor with a private copy of host signal capabilities.</summary>
    /// <param name="shell">Shell value supplied to the windows local console platform operation and used when producing its result.</param>
    /// <param name="displayName">Display name value supplied to the windows local console platform operation and used when producing its result.</param>
    /// <param name="executable">Executable value supplied to the windows local console platform operation and used when producing its result.</param>
    /// <param name="isDefault">Value indicating whether is default should apply to this operation.</param>
    /// <param name="signals">Signals value supplied to the windows local console platform operation and used when producing its result.</param>
    /// <returns>The local console shell descriptor produced by the operation.</returns>
    private LocalConsoleShellDescriptor CreateDescriptor(LocalConsoleShellKind shell, string displayName, string executable, bool isDefault, List<LocalConsoleSignalKind> signals)
    {
        try
        {
            return new LocalConsoleShellDescriptor { Shell = shell, DisplayName = displayName, Executable = executable, IsDefault = isDefault, SupportedSignals = [.. signals] };
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(CreateDescriptor), exception);
            throw;
        }
    }

    /// <summary>Returns signals that can be represented safely for redirected Windows child processes.</summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<LocalConsoleSignalKind> GetSupportedSignals()
    {
        try
        {
            return [LocalConsoleSignalKind.Terminate, LocalConsoleSignalKind.Kill];
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsLocalConsolePlatformService), nameof(GetSupportedSignals), exception);
            throw;
        }
    }
}

/// <summary>Unix/macOS/Linux shell resolution and POSIX signal delivery for the shared LocalGPT ASCII console.</summary>
public sealed class UnixLocalConsolePlatformService : ILocalConsolePlatformService
{
    /// <summary>
    /// Retrieves available shells as part of the unix local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public IReadOnlyList<LocalConsoleShellDescriptor> GetAvailableShells()
    {
        try
        {
            var signals = GetSupportedSignals();
            var defaultShell = ResolveShell(LocalConsoleShellKind.Auto);
            var defaultExecutable = ResolveShellExecutable(defaultShell);
            var descriptors = new List<LocalConsoleShellDescriptor>
            {
                CreateDescriptor(LocalConsoleShellKind.Auto, $"Auto ({Path.GetFileName(defaultExecutable)})", defaultExecutable, true, signals)
            };

            AddIfAvailable(descriptors, LocalConsoleShellKind.Zsh, "Z shell", FindExecutable("zsh"), defaultShell == LocalConsoleShellKind.Zsh, signals);
            AddIfAvailable(descriptors, LocalConsoleShellKind.Bash, "Bash", FindExecutable("bash"), defaultShell == LocalConsoleShellKind.Bash, signals);
            AddIfAvailable(descriptors, LocalConsoleShellKind.Sh, "POSIX sh", FindExecutable("sh"), defaultShell == LocalConsoleShellKind.Sh, signals);
            AddIfAvailable(descriptors, LocalConsoleShellKind.PowerShell, "PowerShell", FindExecutable("pwsh"), defaultShell == LocalConsoleShellKind.PowerShell, signals);
            return descriptors;
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(GetAvailableShells), exception);
            throw;
        }
    }

    /// <summary>
    /// Resolves shell as part of the unix local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public LocalConsoleShellKind ResolveShell(LocalConsoleShellKind requestedShell)
    {
        try
        {
            if (requestedShell != LocalConsoleShellKind.Auto)
                return requestedShell;

            var configured = Environment.GetEnvironmentVariable("SHELL") ?? string.Empty;
            var name = Path.GetFileName(configured);
            if (name.Equals("zsh", StringComparison.OrdinalIgnoreCase) && FindExecutable("zsh") is not null)
                return LocalConsoleShellKind.Zsh;
            if (name.Equals("bash", StringComparison.OrdinalIgnoreCase) && FindExecutable("bash") is not null)
                return LocalConsoleShellKind.Bash;
            if ((name.Equals("sh", StringComparison.OrdinalIgnoreCase) || name.Equals("dash", StringComparison.OrdinalIgnoreCase)) && FindExecutable("sh") is not null)
                return LocalConsoleShellKind.Sh;
            if (name.Equals("pwsh", StringComparison.OrdinalIgnoreCase) && FindExecutable("pwsh") is not null)
                return LocalConsoleShellKind.PowerShell;
            if (OperatingSystem.IsMacOS() && FindExecutable("zsh") is not null)
                return LocalConsoleShellKind.Zsh;
            if (FindExecutable("bash") is not null)
                return LocalConsoleShellKind.Bash;
            return LocalConsoleShellKind.Sh;
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(ResolveShell), exception);
            throw;
        }
    }

    /// <summary>
    /// Creates shell command as part of the unix local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public LocalConsolePlatformCommand CreateShellCommand(LocalConsoleShellKind shell, string commandText)
    {
        try
        {
            var resolvedShell = ResolveShell(shell);
            return resolvedShell switch
            {
                LocalConsoleShellKind.PowerShell => Create(RequireExecutable("pwsh"), resolvedShell, ["-NoProfile", "-NonInteractive", "-Command", commandText]),
                LocalConsoleShellKind.Bash => Create(RequireExecutable("bash"), resolvedShell, ["-lc", commandText]),
                LocalConsoleShellKind.Zsh => Create(RequireExecutable("zsh"), resolvedShell, ["-lc", commandText]),
                LocalConsoleShellKind.Sh => Create(RequireExecutable("sh"), resolvedShell, ["-lc", commandText]),
                LocalConsoleShellKind.Cmd => throw new PlatformNotSupportedException("cmd.exe is not available on Unix-like hosts."),
                LocalConsoleShellKind.Direct => throw new InvalidOperationException("Direct commands are resolved by the common console service."),
                _ => throw new InvalidOperationException($"Unsupported console shell '{resolvedShell}'.")
            };
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(CreateShellCommand), exception);
            throw;
        }
    }

    /// <summary>
    /// Creates power shell script command as part of the unix local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public LocalConsolePlatformCommand CreatePowerShellScriptCommand(string scriptPath)
    {
        try
        {
            var executable = RequireExecutable("pwsh");
            var arguments = new List<string> { "-NoProfile", "-NonInteractive", "-File", scriptPath };
            var command = Create(executable, LocalConsoleShellKind.PowerShell, arguments);
            command.DisplayCommand = $"{Path.GetFileName(executable)} {string.Join(' ', arguments.Select(FormatDisplayArgument))}";
            return command;
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(CreatePowerShellScriptCommand), exception);
            throw;
        }
    }

    /// <summary>
    /// Performs send signal as part of the unix local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task SendSignalAsync(int processId, LocalConsoleSignalKind signal, CancellationToken cancellationToken = default)
    {
        try
        {
            var kill = File.Exists("/bin/kill") ? "/bin/kill" : RequireExecutable("kill");
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = kill,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.StartInfo.ArgumentList.Add("-s");
            process.StartInfo.ArgumentList.Add(signal switch
            {
                LocalConsoleSignalKind.Interrupt => "INT",
                LocalConsoleSignalKind.Terminate => "TERM",
                LocalConsoleSignalKind.Kill => "KILL",
                LocalConsoleSignalKind.Hangup => "HUP",
                _ => throw new InvalidOperationException($"Unsupported signal '{signal}'.")
            });
            process.StartInfo.ArgumentList.Add(processId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (!process.Start())
                throw new InvalidOperationException("The host kill utility could not be started.");
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException($"Signal delivery failed with exit code {process.ExitCode}: {error.Trim()}");
            }
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(SendSignalAsync), exception);
            throw;
        }
    }

    /// <summary>Formats one diagnostic-only script argument without exposing command text through service logs.</summary>
    /// <param name="value">Value value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string FormatDisplayArgument(string value)
    {
        try
        {
            return value.Any(char.IsWhiteSpace) ? $"\"{value}\"" : value;
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(FormatDisplayArgument), exception);
            throw;
        }
    }

    /// <summary>
    /// Performs create as part of the unix local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="executable">Executable value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <param name="shell">Shell value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <param name="arguments">Arguments value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <returns>The local console platform command produced by the operation.</returns>
    private LocalConsolePlatformCommand Create(string executable, LocalConsoleShellKind shell, List<string> arguments)
    {
        try
        {
            return new LocalConsolePlatformCommand
            {
                Executable = executable,
                Shell = shell,
                Arguments = arguments,
                DisplayCommand = $"{Path.GetFileName(executable)} [LocalGPT-reviewed {shell} command]"
            };
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(Create), exception);
            throw;
        }
    }

    /// <summary>Resolves the executable for one concrete Unix shell kind.</summary>
    /// <param name="shell">Shell value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveShellExecutable(LocalConsoleShellKind shell)
    {
        try
        {
            return shell switch
            {
                LocalConsoleShellKind.PowerShell => RequireExecutable("pwsh"),
                LocalConsoleShellKind.Zsh => RequireExecutable("zsh"),
                LocalConsoleShellKind.Bash => RequireExecutable("bash"),
                LocalConsoleShellKind.Sh => RequireExecutable("sh"),
                _ => throw new InvalidOperationException($"Shell '{shell}' cannot be used as an automatic Unix shell.")
            };
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(ResolveShellExecutable), exception);
            throw;
        }
    }

    /// <summary>Requires one optional Unix shell executable to be installed before use.</summary>
    /// <param name="command">Command value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RequireExecutable(string command)
    {
        try
        {
            return FindExecutable(command)
                ?? throw new FileNotFoundException($"Shell executable '{command}' is not available on PATH.");
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(RequireExecutable), exception);
            throw;
        }
    }

    /// <summary>
    /// Finds executable as part of the unix local console platform service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="command">Command value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string? FindExecutable(string command)
    {
        try
        {
            var known = command switch
            {
                "zsh" when File.Exists("/bin/zsh") => "/bin/zsh",
                "bash" when File.Exists("/bin/bash") => "/bin/bash",
                "sh" when File.Exists("/bin/sh") => "/bin/sh",
                "kill" when File.Exists("/bin/kill") => "/bin/kill",
                _ => null
            };
            if (known is not null)
                return known;

            foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                         .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var candidate = Path.Combine(directory, command);
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
            }
            return null;
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(FindExecutable), exception);
            throw;
        }
    }

    /// <summary>Adds one optional Unix shell descriptor when its executable is available.</summary>
    /// <param name="descriptors">Descriptors value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <param name="shell">Shell value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <param name="displayName">Display name value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <param name="executable">Executable value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <param name="isDefault">Value indicating whether is default should apply to this operation.</param>
    /// <param name="signals">Signals value supplied to the unix local console platform operation and used when producing its result.</param>
    private void AddIfAvailable(List<LocalConsoleShellDescriptor> descriptors, LocalConsoleShellKind shell, string displayName, string? executable, bool isDefault, List<LocalConsoleSignalKind> signals)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(executable))
                descriptors.Add(CreateDescriptor(shell, displayName, executable, isDefault, signals));
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(AddIfAvailable), exception);
            throw;
        }
    }

    /// <summary>Creates a shell descriptor with a private copy of host signal capabilities.</summary>
    /// <param name="shell">Shell value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <param name="displayName">Display name value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <param name="executable">Executable value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <param name="isDefault">Value indicating whether is default should apply to this operation.</param>
    /// <param name="signals">Signals value supplied to the unix local console platform operation and used when producing its result.</param>
    /// <returns>The local console shell descriptor produced by the operation.</returns>
    private LocalConsoleShellDescriptor CreateDescriptor(LocalConsoleShellKind shell, string displayName, string executable, bool isDefault, List<LocalConsoleSignalKind> signals)
    {
        try
        {
            return new LocalConsoleShellDescriptor { Shell = shell, DisplayName = displayName, Executable = executable, IsDefault = isDefault, SupportedSignals = [.. signals] };
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(CreateDescriptor), exception);
            throw;
        }
    }

    /// <summary>Returns POSIX signals exposed by the LocalGPT operator layer on Unix-like hosts.</summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<LocalConsoleSignalKind> GetSupportedSignals()
    {
        try
        {
            return [LocalConsoleSignalKind.Interrupt, LocalConsoleSignalKind.Terminate, LocalConsoleSignalKind.Hangup, LocalConsoleSignalKind.Kill];
        }
        catch (Exception exception)
        {
            Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(UnixLocalConsolePlatformService), nameof(GetSupportedSignals), exception);
            throw;
        }
    }
}
