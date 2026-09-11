using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LocalGPT.Helper;

/// <summary>Provides the dependency-light Matrix-style operator layer used while LocalGPT SETUP setup work is running.</summary>
internal static class SetupOperatorConsole
{
    /// <summary>
    /// Stores the shared read-only gate value used by <see cref="SetupOperatorConsole"/> across instances of the containing type.
    /// </summary>
    private static readonly object Gate = new();
    /// <summary>
    /// Stores the shared read-only processes value used by <see cref="SetupOperatorConsole"/> across instances of the containing type.
    /// </summary>
    private static readonly ConcurrentDictionary<int, TrackedProcess> Processes = new();
    /// <summary>
    /// Stores the cancellation source used by <see cref="SetupOperatorConsole"/> to stop its current background or asynchronous operation.
    /// </summary>
    private static CancellationTokenSource cancellation = new();
    /// <summary>
    /// Stores the logger used by <see cref="SetupOperatorConsole"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private static ILogger? logger;
    /// <summary>
    /// Stores the internal reader task state used by <see cref="SetupOperatorConsole"/> while executing its surrounding workflow.
    /// </summary>
    private static Task? readerTask;
    /// <summary>
    /// Stores the internal started state used by <see cref="SetupOperatorConsole"/> while executing its surrounding workflow.
    /// </summary>
    private static bool started;
    /// <summary>
    /// Stores the internal enabled state used by <see cref="SetupOperatorConsole"/> while executing its surrounding workflow.
    /// </summary>
    private static bool enabled = true;
    /// <summary>
    /// Stores the internal selected shell state used by <see cref="SetupOperatorConsole"/> while executing its surrounding workflow.
    /// </summary>
    private static SetupShellKind selectedShell = SetupShellKind.Auto;

    /// <summary>Gets the cancellation token requested by Ctrl+C or the embedded <c>:cancel</c> command.</summary>
    /// <value>The cancellation token value exposed by <see cref="SetupOperatorConsole"/>.</value>
    public static CancellationToken CancellationToken => cancellation.Token;

    /// <summary>Starts the interactive operator reader when the setup process owns an interactive console.</summary>
    /// <param name="setupLogger">Setup logger used only for lifecycle diagnostics; human shell text is never logged.</param>
    public static void Start(ILogger setupLogger)
    {
        lock (Gate)
        {
            if (started)
                return;
            logger = setupLogger;
            cancellation.Dispose();
            cancellation = new CancellationTokenSource();
            started = true;
            enabled = true;
            selectedShell = SetupShellKind.Auto;
            Console.CancelKeyPress += OnCancelKeyPress;
            WriteBanner();
            if (!Console.IsInputRedirected && Environment.UserInteractive)
                readerTask = Task.Run(ReadLoop);
            else
                WriteMatrixLine("operator input unavailable because setup stdin is redirected/non-interactive.", ConsoleColor.DarkGreen);
        }
    }

    /// <summary>Stops accepting new operator work and detaches the Ctrl+C router without terminating already-completed setup state.</summary>
    public static void Stop()
    {
        lock (Gate)
        {
            if (!started)
                return;
            started = false;
            Console.CancelKeyPress -= OnCancelKeyPress;
            logger = null;
        }
    }

    /// <summary>Throws when setup-wide operator cancellation has been requested.</summary>
    public static void ThrowIfCancellationRequested() => CancellationToken.ThrowIfCancellationRequested();

    /// <summary>Tracks one setup-owned child process so the operator can inspect or signal it while it is running.</summary>
    /// <param name="process">Started child process.</param>
    /// <param name="displayName">Bounded human-readable role for the process.</param>
    /// <returns>A scope that removes the process from the active registry.</returns>
    public static IDisposable TrackProcess(Process process, string displayName)
    {
        ArgumentNullException.ThrowIfNull(process);
        var tracked = new TrackedProcess(process, Bound(displayName, 80), false, DateTimeOffset.UtcNow);
        Processes[process.Id] = tracked;
        return new ProcessRegistration(process.Id);
    }

    /// <summary>Routes Ctrl+C into setup cancellation so the Matrix wall can shut down owned work consistently.</summary>
    /// <param name="sender">Sender value supplied to the setup operator console operation and used when producing its result.</param>
    /// <param name="args">Args value supplied to the setup operator console operation and used when producing its result.</param>
    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs args)
    {
        args.Cancel = true;
        RequestCancellation("Ctrl+C");
    }

    /// <summary>Reads human-entered lines concurrently with setup output; it never starts another terminal window.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private static async Task ReadLoop()
    {
        try
        {
            while (started)
            {
                var line = Console.ReadLine();
                if (line is null)
                    break;
                await HandleLineAsync(line).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "The embedded setup operator input loop stopped unexpectedly.");
        }
    }

    /// <summary>Parses one setup operator line, keeping colon commands above the selected shell backend.</summary>
    /// <param name="line">Human-entered line.</param>
    /// <returns>A task that completes after the line has been handled or queued.</returns>
    private static async Task HandleLineAsync(string line)
    {
        var input = (line ?? string.Empty).Trim();
        if (input.Length == 0)
            return;

        if (input.StartsWith(':'))
        {
            await HandleMetaAsync(input[1..]).ConfigureAwait(false);
            return;
        }
        if (!enabled)
        {
            WriteMatrixLine("operator layer is OFF; enter :operator on to re-enable shell commands.", ConsoleColor.DarkYellow);
            return;
        }
        _ = Task.Run(() => RunShellCommandAsync(input));
        WriteMatrixLine("shell job queued behind the ASCII wall.", ConsoleColor.Green);
    }

    /// <summary>Executes one setup-owned meta command without forwarding it to a shell.</summary>
    /// <param name="commandLine">Colon prefix removed from the human line.</param>
    /// <returns>A task that completes when any requested signal has been delivered.</returns>
    private static async Task HandleMetaAsync(string commandLine)
    {
        var parts = commandLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return;
        var command = parts[0].ToLowerInvariant();
        switch (command)
        {
            case "help":
                WriteMatrixLine(":operator on|off · :shells · :shell <auto|zsh|bash|sh|pwsh|cmd> · :jobs · :cancel · :signal <current|pid|pid:n> <INT|TERM|KILL|HUP> · :clear", ConsoleColor.Green);
                break;
            case "operator":
                SetEnabled(parts.Length > 1 ? parts[1] : "status");
                break;
            case "shells":
                WriteShells();
                break;
            case "shell":
                SelectShell(parts.Length > 1 ? parts[1] : string.Empty);
                break;
            case "jobs":
                WriteJobs();
                break;
            case "cancel":
                RequestCancellation("operator command");
                break;
            case "signal":
                if (parts.Length < 3)
                    WriteMatrixLine("usage: :signal <current|pid|pid:n> <INT|TERM|KILL|HUP>", ConsoleColor.DarkYellow);
                else
                    await SendSignalAsync(parts[1], parts[2]).ConfigureAwait(false);
                break;
            case "clear":
                if (!Console.IsOutputRedirected)
                    Console.Clear();
                WriteBanner();
                break;
            default:
                WriteMatrixLine($"unknown meta command :{Bound(parts[0], 24)}; enter :help.", ConsoleColor.DarkYellow);
                break;
        }
    }

    /// <summary>Turns ordinary shell command forwarding on or off while keeping meta controls reachable.</summary>
    /// <param name="value">Requested on/off/status token.</param>
    private static void SetEnabled(string value)
    {
        if (string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
            enabled = true;
        else if (string.Equals(value, "off", StringComparison.OrdinalIgnoreCase))
            enabled = false;
        WriteMatrixLine($"operator shell layer is {(enabled ? "ON" : "OFF")}; meta controls remain available.", enabled ? ConsoleColor.Green : ConsoleColor.DarkYellow);
    }

    /// <summary>Lists installed shell engines that can run invisibly behind the setup wall.</summary>
    private static void WriteShells()
    {
        var shells = GetAvailableShells();
        WriteMatrixLine("shells: " + string.Join(" · ", shells.Select(item => item.DisplayName + (item.Kind == selectedShell ? " [selected]" : string.Empty))), ConsoleColor.Green);
    }

    /// <summary>Selects one installed shell backend by friendly token.</summary>
    /// <param name="value">Requested shell token.</param>
    private static void SelectShell(string value)
    {
        if (!TryParseShell(value, out var requested))
        {
            WriteMatrixLine("shell not recognized; enter :shells.", ConsoleColor.DarkYellow);
            return;
        }
        var available = GetAvailableShells();
        if (!available.Any(item => item.Kind == requested))
        {
            WriteMatrixLine($"{requested} is not installed/available on this host.", ConsoleColor.DarkYellow);
            return;
        }
        selectedShell = requested;
        WriteMatrixLine($"embedded shell backend set to {requested}.", ConsoleColor.Green);
    }

    /// <summary>Lists setup and human shell processes currently owned by or explicitly tracked through this operator layer.</summary>
    private static void WriteJobs()
    {
        var jobs = Processes.Values.OrderBy(item => item.StartedUtc).ToArray();
        if (jobs.Length == 0)
        {
            WriteMatrixLine("no tracked child process is currently active.", ConsoleColor.DarkGreen);
            return;
        }
        foreach (var item in jobs)
            WriteMatrixLine($"pid={item.Process.Id} {(item.IsOperatorJob ? "shell" : "setup"),-5} {item.DisplayName}", ConsoleColor.Green);
    }

    /// <summary>Requests setup-wide cancellation and terminates currently tracked setup children on a best-effort basis.</summary>
    /// <param name="source">Human-readable cancellation source.</param>
    private static void RequestCancellation(string source)
    {
        if (!cancellation.IsCancellationRequested)
            cancellation.Cancel();
        WriteMatrixLine($"CANCEL requested by {source}; stopping owned setup work…", ConsoleColor.Yellow);
        foreach (var item in Processes.Values.Where(item => !item.IsOperatorJob))
            TryKill(item.Process, true);
    }

    /// <summary>Runs one explicit human shell line with redirected output so no external terminal application appears.</summary>
    /// <param name="commandText">Exact human-entered shell line; never written to setup diagnostics.</param>
    /// <returns>A task that completes when the shell process exits.</returns>
    private static async Task RunShellCommandAsync(string commandText)
    {
        Process? process = null;
        try
        {
            var shell = ResolveShell(selectedShell);
            var info = CreateShellStartInfo(shell, commandText);
            process = new Process { StartInfo = info };
            if (!process.Start())
                throw new InvalidOperationException("The embedded shell process could not be started.");
            Processes[process.Id] = new TrackedProcess(process, $"{shell} operator command", true, DateTimeOffset.UtcNow);
            WriteMatrixLine($"job pid={process.Id} started via {shell}; command text stays private.", ConsoleColor.Green);
            var stdout = PumpAsync(process.StandardOutput, "OUT", CancellationToken.None);
            var stderr = PumpAsync(process.StandardError, "ERR", CancellationToken.None);
            await process.WaitForExitAsync().ConfigureAwait(false);
            await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            WriteMatrixLine($"job pid={process.Id} exited with code {process.ExitCode}.", process.ExitCode == 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkYellow);
        }
        catch (Exception exception)
        {
            WriteMatrixLine($"embedded shell job failed: {Bound(exception.Message, 180)}", ConsoleColor.Red);
            logger?.LogWarning(exception, "An embedded setup operator shell job failed; command text was omitted.");
        }
        finally
        {
            if (process is not null)
            {
                Processes.TryRemove(process.Id, out _);
                process.Dispose();
            }
        }
    }

    /// <summary>Streams one redirected shell stream into the green ASCII wall.</summary>
    /// <param name="reader">Redirected process stream.</param>
    /// <param name="stream">Short stream label.</param>
    /// <param name="cancellationToken">Optional stream cancellation.</param>
    /// <returns>A task that completes at end-of-stream.</returns>
    private static async Task PumpAsync(StreamReader reader, string stream, CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
                break;
            WriteMatrixLine($"{stream} {Bound(line, 500)}", stream == "ERR" ? ConsoleColor.DarkYellow : ConsoleColor.DarkGreen);
        }
    }

    /// <summary>Sends one host-aware signal to a tracked process or an explicitly named <c>pid:n</c>.</summary>
    /// <param name="target">Current, tracked numeric pid, or explicit pid:n token.</param>
    /// <param name="signalToken">INT, TERM, KILL, or HUP.</param>
    /// <returns>A task that completes after signal delivery.</returns>
    private static async Task SendSignalAsync(string target, string signalToken)
    {
        try
        {
            if (!TryResolvePid(target, out var pid))
            {
                WriteMatrixLine("signal target not found; enter :jobs or use explicit pid:<number>.", ConsoleColor.DarkYellow);
                return;
            }
            var signal = NormalizeSignal(signalToken);
            if (signal is null)
            {
                WriteMatrixLine("signal not recognized; use INT, TERM, KILL, or HUP.", ConsoleColor.DarkYellow);
                return;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (signal is "INT" or "HUP")
                    throw new PlatformNotSupportedException($"{signal} is not available for redirected Windows setup jobs; use TERM, KILL, or :cancel.");
                using var targetProcess = Process.GetProcessById(pid);
                if (!targetProcess.HasExited)
                    targetProcess.Kill(entireProcessTree: signal == "KILL");
            }
            else
            {
                using var kill = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = File.Exists("/bin/kill") ? "/bin/kill" : "kill",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                kill.StartInfo.ArgumentList.Add("-s");
                kill.StartInfo.ArgumentList.Add(signal);
                kill.StartInfo.ArgumentList.Add(pid.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (!kill.Start())
                    throw new InvalidOperationException("The host signal utility could not be started.");
                await kill.WaitForExitAsync().ConfigureAwait(false);
                if (kill.ExitCode != 0)
                    throw new InvalidOperationException((await kill.StandardError.ReadToEndAsync().ConfigureAwait(false)).Trim());
            }
            WriteMatrixLine($"sent {signal} to pid {pid}.", ConsoleColor.Green);
        }
        catch (Exception exception)
        {
            WriteMatrixLine($"signal delivery failed: {Bound(exception.Message, 180)}", ConsoleColor.Red);
            logger?.LogWarning(exception, "Setup operator signal delivery failed.");
        }
    }

    /// <summary>
    /// Attempts to resolve pid for <see cref="SetupOperatorConsole"/>, keeping the operation consistent with the state and invariants of the surrounding setup operator console workflow.
    /// </summary>
    /// <param name="target">Human target token.</param>
    /// <param name="pid">Resolved process identifier.</param>
    /// <returns>True when a target can be resolved.</returns>
    private static bool TryResolvePid(string target, out int pid)
    {
        pid = 0;
        if (string.Equals(target, "current", StringComparison.OrdinalIgnoreCase))
        {
            var current = Processes.Values.OrderByDescending(item => item.StartedUtc).FirstOrDefault();
            if (current is null)
                return false;
            pid = current.Process.Id;
            return true;
        }
        if (target.StartsWith("pid:", StringComparison.OrdinalIgnoreCase) && int.TryParse(target[4..], out var explicitPid) && explicitPid > 0)
        {
            pid = explicitPid;
            return true;
        }
        if (int.TryParse(target, out var trackedPid) && Processes.ContainsKey(trackedPid))
        {
            pid = trackedPid;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Normalizes signal for <see cref="SetupOperatorConsole"/>, keeping the operation consistent with the state and invariants of the surrounding setup operator console workflow.
    /// </summary>
    /// <param name="value">Human signal token.</param>
    /// <returns>INT, TERM, KILL, HUP, or null.</returns>
    private static string? NormalizeSignal(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.StartsWith("SIG", StringComparison.Ordinal))
            normalized = normalized[3..];
        return normalized switch
        {
            "INT" or "INTERRUPT" => "INT",
            "TERM" or "TERMINATE" => "TERM",
            "KILL" => "KILL",
            "HUP" or "HANGUP" => "HUP",
            _ => null
        };
    }

    /// <summary>Returns every installed shell backend that can execute with redirected streams.</summary>
    /// <returns>Available shell descriptor collection.</returns>
    private static IReadOnlyList<SetupShellDescriptor> GetAvailableShells()
    {
        var result = new List<SetupShellDescriptor> { new(SetupShellKind.Auto, $"auto ({ResolveShell(SetupShellKind.Auto)})") };
        AddShell(result, SetupShellKind.PowerShell, "pwsh", FindExecutable(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "pwsh.exe" : "pwsh"));
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AddShell(result, SetupShellKind.Cmd, "cmd", Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe");
            AddShell(result, SetupShellKind.Bash, "bash", FindExecutable("bash.exe"));
        }
        else
        {
            AddShell(result, SetupShellKind.Zsh, "zsh", FindExecutable("zsh"));
            AddShell(result, SetupShellKind.Bash, "bash", FindExecutable("bash"));
            AddShell(result, SetupShellKind.Sh, "sh", FindExecutable("sh"));
        }
        return result;
    }

    /// <summary>
    /// Adds shell for <see cref="SetupOperatorConsole"/>, keeping the operation consistent with the state and invariants of the surrounding setup operator console workflow.
    /// </summary>
    /// <param name="list">List value supplied to the setup operator console operation and used when producing its result.</param>
    /// <param name="kind">Kind value supplied to the setup operator console operation and used when producing its result.</param>
    /// <param name="displayName">Display name value supplied to the setup operator console operation and used when producing its result.</param>
    /// <param name="executable">Executable value supplied to the setup operator console operation and used when producing its result.</param>
    private static void AddShell(List<SetupShellDescriptor> list, SetupShellKind kind, string displayName, string? executable)
    {
        if (!string.IsNullOrWhiteSpace(executable))
            list.Add(new SetupShellDescriptor(kind, displayName));
    }

    /// <summary>Resolves Auto to the host's preferred installed shell without opening it.</summary>
    /// <param name="requested">Requested shell kind.</param>
    /// <returns>A concrete installed shell.</returns>
    private static SetupShellKind ResolveShell(SetupShellKind requested)
    {
        if (requested != SetupShellKind.Auto)
            return requested;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return FindExecutable("pwsh.exe") is not null ? SetupShellKind.PowerShell : SetupShellKind.Cmd;
        var hostShell = Path.GetFileName(Environment.GetEnvironmentVariable("SHELL") ?? string.Empty);
        if (hostShell.Equals("zsh", StringComparison.OrdinalIgnoreCase) && FindExecutable("zsh") is not null)
            return SetupShellKind.Zsh;
        if (hostShell.Equals("bash", StringComparison.OrdinalIgnoreCase) && FindExecutable("bash") is not null)
            return SetupShellKind.Bash;
        if (hostShell.Equals("sh", StringComparison.OrdinalIgnoreCase) && FindExecutable("sh") is not null)
            return SetupShellKind.Sh;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && FindExecutable("zsh") is not null)
            return SetupShellKind.Zsh;
        if (FindExecutable("bash") is not null)
            return SetupShellKind.Bash;
        if (FindExecutable("sh") is not null)
            return SetupShellKind.Sh;
        if (FindExecutable("pwsh") is not null)
            return SetupShellKind.PowerShell;
        throw new FileNotFoundException("No supported shell backend is available for the setup operator layer.");
    }

    /// <summary>
    /// Attempts to parse shell for <see cref="SetupOperatorConsole"/>, keeping the operation consistent with the state and invariants of the surrounding setup operator console workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the setup operator console operation and used when producing its result.</param>
    /// <param name="shell">Shell value supplied to the setup operator console operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private static bool TryParseShell(string value, out SetupShellKind shell)
    {
        shell = (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "auto" => SetupShellKind.Auto,
            "pwsh" or "powershell" => SetupShellKind.PowerShell,
            "zsh" => SetupShellKind.Zsh,
            "bash" => SetupShellKind.Bash,
            "sh" => SetupShellKind.Sh,
            "cmd" or "cmd.exe" => SetupShellKind.Cmd,
            _ => (SetupShellKind)(-1)
        };
        return Enum.IsDefined(typeof(SetupShellKind), shell);
    }

    /// <summary>Builds a redirected shell process descriptor for one exact human command line.</summary>
    /// <param name="requested">Requested value supplied to the setup operator console operation and used when producing its result.</param>
    /// <param name="commandText">Command text value supplied to the setup operator console operation and used when producing its result.</param>
    /// <returns>The process start info produced by the operation.</returns>
    private static ProcessStartInfo CreateShellStartInfo(SetupShellKind requested, string commandText)
    {
        var shell = ResolveShell(requested);
        var executable = shell switch
        {
            SetupShellKind.PowerShell => FindExecutable(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "pwsh.exe" : "pwsh") ?? (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "powershell.exe" : "pwsh"),
            SetupShellKind.Zsh => FindExecutable("zsh") ?? "/bin/zsh",
            SetupShellKind.Bash => FindExecutable(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "bash.exe" : "bash") ?? "bash",
            SetupShellKind.Sh => FindExecutable("sh") ?? "/bin/sh",
            SetupShellKind.Cmd => Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
            _ => throw new InvalidOperationException($"Unsupported setup shell {shell}.")
        };
        var info = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            CreateNoWindow = true,
            WorkingDirectory = Environment.CurrentDirectory
        };
        if (shell == SetupShellKind.PowerShell)
        {
            info.ArgumentList.Add("-NoProfile");
            info.ArgumentList.Add("-NonInteractive");
            info.ArgumentList.Add("-Command");
            info.ArgumentList.Add(commandText);
        }
        else if (shell == SetupShellKind.Cmd)
        {
            info.ArgumentList.Add("/d"); info.ArgumentList.Add("/s"); info.ArgumentList.Add("/c"); info.ArgumentList.Add(commandText);
        }
        else
        {
            info.ArgumentList.Add("-lc"); info.ArgumentList.Add(commandText);
        }
        return info;
    }

    /// <summary>Finds one executable without invoking a shell or displaying an external window.</summary>
    /// <param name="command">Command value supplied to the setup operator console operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private static string? FindExecutable(string command)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var known = command switch
            {
                "zsh" when File.Exists("/bin/zsh") => "/bin/zsh",
                "bash" when File.Exists("/bin/bash") => "/bin/bash",
                "sh" when File.Exists("/bin/sh") => "/bin/sh",
                _ => null
            };
            if (known is not null)
                return known;
        }
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(directory.Trim('"'), command);
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);
        }
        return null;
    }

    /// <summary>Best-effort process termination used only for setup-owned child cleanup.</summary>
    /// <param name="process">Process value supplied to the setup operator console operation and used when producing its result.</param>
    /// <param name="entireTree">Value indicating whether entire tree should apply to this operation.</param>
    private static void TryKill(Process process, bool entireTree)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: entireTree);
        }
        catch (Exception exception)
        {
            logger?.LogDebug(exception, "Best-effort setup operator process cleanup failed for pid {ProcessId}.", process.Id);
        }
    }

    /// <summary>Writes the stable Matrix-style control legend without replacing the host console.</summary>
    private static void WriteBanner()
    {
        WriteMatrixLine("┌─ LocalGPT SETUP // MATRIX OPERATOR ─────────────────────────────────────────────┐", ConsoleColor.Green);
        WriteMatrixLine("│ :help meta controls · ordinary lines use the selected embedded shell         │", ConsoleColor.DarkGreen);
        WriteMatrixLine("│ no external terminal window is opened; Ctrl+C routes through :cancel         │", ConsoleColor.DarkGreen);
        WriteMatrixLine("└──────────────────────────────────────────────────────────────────────────────┘", ConsoleColor.Green);
    }

    /// <summary>Writes one synchronized green-wall line while restoring the caller's console colors.</summary>
    /// <param name="text">Text value supplied to the setup operator console operation and used when producing its result.</param>
    /// <param name="color">Color value supplied to the setup operator console operation and used when producing its result.</param>
    private static void WriteMatrixLine(string text, ConsoleColor color)
    {
        lock (Gate)
        {
            var original = Console.ForegroundColor;
            try
            {
                if (!Console.IsOutputRedirected)
                    Console.ForegroundColor = color;
                Console.WriteLine($"[MATRIX] {text}");
            }
            finally
            {
                if (!Console.IsOutputRedirected)
                    Console.ForegroundColor = original;
            }
        }
    }

    /// <summary>Bounds operator display text so arbitrary process output cannot flood one terminal frame.</summary>
    /// <param name="value">Value value supplied to the setup operator console operation and used when producing its result.</param>
    /// <param name="maximum">Maximum value supplied to the setup operator console operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private static string Bound(string value, int maximum)
    {
        var normalized = (value ?? string.Empty).Replace('\0', ' ');
        return normalized.Length <= maximum ? normalized : normalized[..maximum] + "…";
    }

    /// <summary>
    /// Defines the supported setup shell kind values used to select or describe behavior in the surrounding workflow.
    /// </summary>
    private enum SetupShellKind { Auto, PowerShell, Zsh, Bash, Sh, Cmd }
    /// <summary>
    /// Represents setup shell state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
    /// </summary>
    /// <param name="Kind">Kind value supplied to the setup operator console operation and used when producing its result.</param>
    /// <param name="DisplayName">Display name value supplied to the setup operator console operation and used when producing its result.</param>
    private sealed record SetupShellDescriptor(SetupShellKind Kind, string DisplayName);
    /// <summary>
    /// Represents a tracked process helper type nested within <see cref="SetupOperatorConsole"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="Process">Process value supplied to the setup operator console operation and used when producing its result.</param>
    /// <param name="DisplayName">Display name value supplied to the setup operator console operation and used when producing its result.</param>
    /// <param name="IsOperatorJob">Value indicating whether operator job should apply to this operation.</param>
    /// <param name="StartedUtc">Started utc value supplied to the setup operator console operation and used when producing its result.</param>
    private sealed record TrackedProcess(Process Process, string DisplayName, bool IsOperatorJob, DateTimeOffset StartedUtc);

    /// <summary>Removes one process from the active operator registry when its caller-owned lifetime ends.</summary>
    /// <param name="processId">Identifier of the process to use for this operation.</param>
    private sealed class ProcessRegistration(int processId) : IDisposable
    {
        /// <summary>Releases this registry entry without disposing the caller-owned process instance.</summary>
        public void Dispose() => Processes.TryRemove(processId, out _);
    }
}
