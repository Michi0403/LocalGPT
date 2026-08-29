using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Runs explicitly bounded local commands through one cross-platform console abstraction and publishes sanitized live output for LocalGPT UI surfaces.</summary>
/// <param name="platform">Cross-platform console adapter used to start and supervise local commands.</param>
/// <param name="runtimePolicy">Database-backed operator runtime policy.</param>
/// <param name="logger">Writes command lifecycle diagnostics without logging command arguments or output.</param>
public sealed class ConsoleCommandService(
    ILocalConsolePlatformService platform,
    ILocalGptRuntimePolicyDataService runtimePolicy,
    ILogger<ConsoleCommandService> logger) : IConsoleCommandService
{
    /// <summary>
    /// Stores the internal recent output state used by <see cref="ConsoleCommandService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly ConcurrentQueue<LocalConsoleOutputEvent> recentOutput = new();

    /// <summary>Raised after bounded console output changes so renderer-owned components can request a refresh.</summary>
    public event Action? Changed;

    /// <summary>Executes one read-only or explicitly confirmed local command through the requested shell adapter.</summary>
    /// <inheritdoc />
    public async Task<LocalConsoleCommandResult> ExecuteAsync(LocalConsoleCommandRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!request.IsReadOnly && !request.UserConfirmed)
                throw new InvalidOperationException("Fresh user confirmation is required for a consequential local console command.");

            var operationId = Guid.NewGuid();
            var resolved = ResolveStartInfo(request);
            var maximumTimeoutSeconds = Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ConsoleMaximumTimeoutSeconds));
            var timeoutSeconds = request.TimeoutSeconds <= 0 ? maximumTimeoutSeconds : Math.Min(request.TimeoutSeconds, maximumTimeoutSeconds);
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            Publish(operationId, request.DisplayName, "command", BuildDisplayCommand(request, resolved));

            using var process = new Process
            {
                StartInfo = resolved,
                EnableRaisingEvents = true
            };
            process.OutputDataReceived += (_, args) => AppendOutput(operationId, request.DisplayName, "stdout", args.Data, stdout);
            process.ErrorDataReceived += (_, args) => AppendOutput(operationId, request.DisplayName, "stderr", args.Data, stderr);

            logger.LogInformation(
                "Starting local console operation {OperationId} ({DisplayName}) using {Shell}; read-only: {IsReadOnly}.",
                operationId,
                BoundDisplayName(request.DisplayName),
                request.Shell,
                request.IsReadOnly);

            if (!process.Start())
                throw new InvalidOperationException("The local command process could not be started.");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            var timedOut = false;
            try
            {
                await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
                process.WaitForExit();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                timedOut = true;
                KillProcessTree(process);
                Publish(operationId, request.DisplayName, "system", $"Command timed out after {timeoutSeconds} second(s).");
            }
            catch (OperationCanceledException)
            {
                KillProcessTree(process);
                Publish(operationId, request.DisplayName, "system", "Command cancelled.");
                logger.LogDebug("Local console operation {OperationId} was cancelled.", operationId);
                throw;
            }

            int? exitCode = timedOut ? -2 : process.HasExited ? process.ExitCode : null;
            var succeeded = !timedOut && exitCode == 0;
            var status = timedOut ? "TimedOut" : succeeded ? "Completed" : "Failed";
            Publish(operationId, request.DisplayName, "system", $"{status}; exit code {(exitCode?.ToString() ?? "n/a")}.");
            logger.LogInformation(
                "Local console operation {OperationId} completed with status {Status} and exit code {ExitCode}.",
                operationId,
                status,
                exitCode);

            return new LocalConsoleCommandResult
            {
                OperationId = operationId,
                Succeeded = succeeded,
                ExitCode = exitCode,
                Shell = DescribeShell(request, resolved),
                StandardOutput = BoundCapture(stdout.ToString()),
                StandardError = BoundCapture(stderr.ToString()),
                Status = status
            };
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Executing local console command was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Executing local console command failed; command text, arguments and output were omitted from logs.");
            throw;
        }
    }

    /// <summary>Returns a bounded snapshot of recent console output for ASCII-console and diagnostic surfaces.</summary>
    /// <inheritdoc />
    public IReadOnlyList<LocalConsoleOutputEvent> GetRecentOutput(int maxItems = 200)
    {
        try
        {
            return recentOutput.ToArray().TakeLast(Math.Clamp(maxItems, 1, Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ConsoleMaximumRecentEvents)))).ToArray();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading recent local console output failed.");
            throw;
        }
    }

    /// <summary>Formats recent bounded console output as one monospace text buffer for renderer-owned ASCII surfaces.</summary>
    /// <inheritdoc />
    public string GetRecentDisplayText(int take = 120)
    {
        try
        {
            return string.Join(Environment.NewLine, GetRecentOutput(take).Select(item => $"[{item.TimestampUtc:HH:mm:ss}] {item.Stream,-7} {item.Text}"));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting recent local console output failed; output text was omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Resolves start info as part of the console command service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The process start info produced by the operation.</returns>
    private ProcessStartInfo ResolveStartInfo(LocalConsoleCommandRequest request)
    {
        try
        {
            var shell = platform.ResolveShell(request.Shell);
            var startInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = ResolveWorkingDirectory(request.WorkingDirectory)
            };

            switch (shell)
            {
                case LocalConsoleShellKind.Direct:
                    if (string.IsNullOrWhiteSpace(request.Executable))
                        throw new ArgumentException("A direct command requires an executable.", nameof(request));
                    startInfo.FileName = request.Executable.Trim();
                    foreach (var argument in request.Arguments.Take(128))
                        startInfo.ArgumentList.Add(argument ?? string.Empty);
                    break;
                case LocalConsoleShellKind.PowerShell:
                case LocalConsoleShellKind.Bash:
                case LocalConsoleShellKind.Cmd:
                    var shellCommand = platform.CreateShellCommand(shell, RequireCommandText(request));
                    startInfo.FileName = shellCommand.Executable;
                    foreach (var argument in shellCommand.Arguments)
                        startInfo.ArgumentList.Add(argument);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported console shell '{shell}'.");
            }

            foreach (var item in request.Environment.Where(item => item.IsEnabled).Take(64))
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                    continue;
                startInfo.Environment[item.Name.Trim()] = item.Value ?? string.Empty;
            }
            return startInfo;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving local console process start information failed; command values were omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Resolves working directory as part of the console command service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the console command operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveWorkingDirectory(string value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value))
                return Environment.CurrentDirectory;
            var fullPath = Path.GetFullPath(value);
            if (!Directory.Exists(fullPath))
                throw new DirectoryNotFoundException($"Working directory does not exist: {fullPath}");
            return fullPath;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving local console working directory failed; path details omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs require command text as part of the console command service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RequireCommandText(LocalConsoleCommandRequest request)
    {
        try
        {
            return string.IsNullOrWhiteSpace(request.CommandText)
                ? throw new ArgumentException("A shell command requires command text.", nameof(request))
                : request.CommandText;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Validating local console command text failed; command text omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs append output as part of the console command service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="operationId">Identifier of the operation to use for this operation.</param>
    /// <param name="displayName">Display name value supplied to the console command operation and used when producing its result.</param>
    /// <param name="stream">Stream value supplied to the console command operation and used when producing its result.</param>
    /// <param name="line">Line value supplied to the console command operation and used when producing its result.</param>
    /// <param name="capture">Capture value supplied to the console command operation and used when producing its result.</param>
    private void AppendOutput(Guid operationId, string displayName, string stream, string? line, StringBuilder capture)
    {
        try
        {
            if (line is null)
                return;
            if (capture.Length < Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ConsoleMaximumCaptureCharacters)))
            {
                var remaining = Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ConsoleMaximumCaptureCharacters)) - capture.Length;
                var bounded = line.Length <= remaining ? line : line[..remaining];
                capture.AppendLine(bounded);
            }
            Publish(operationId, displayName, stream, line);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Publishing bounded local console output failed; output text was omitted from logs.");
        }
    }

    /// <summary>
    /// Performs publish as part of the console command service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="operationId">Identifier of the operation to use for this operation.</param>
    /// <param name="displayName">Display name value supplied to the console command operation and used when producing its result.</param>
    /// <param name="stream">Stream value supplied to the console command operation and used when producing its result.</param>
    /// <param name="text">Text value supplied to the console command operation and used when producing its result.</param>
    private void Publish(Guid operationId, string displayName, string stream, string text)
    {
        try
        {
            recentOutput.Enqueue(new LocalConsoleOutputEvent
            {
                OperationId = operationId,
                TimestampUtc = DateTimeOffset.UtcNow,
                DisplayName = BoundDisplayName(displayName),
                Stream = stream,
                Text = text.Length <= Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ConsoleMaximumEventCharacters)) ? text : text[..Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ConsoleMaximumEventCharacters))]
            });
            while (recentOutput.Count > Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ConsoleMaximumRecentEvents)))
                recentOutput.TryDequeue(out _);
            Changed?.Invoke();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Publishing local console event failed; event text was omitted from logs.");
        }
    }

    /// <summary>
    /// Builds display command as part of the console command service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="resolved">Resolved value supplied to the console command operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildDisplayCommand(LocalConsoleCommandRequest request, ProcessStartInfo resolved)
    {
        try
        {
            var label = string.IsNullOrWhiteSpace(request.DisplayName) ? "LocalGPT command" : request.DisplayName.Trim();
            return request.IsReadOnly
                ? $"> {label} [{Path.GetFileName(resolved.FileName)}; read-only]"
                : $"> {label} [{Path.GetFileName(resolved.FileName)}; user-confirmed]";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting local console display command failed; command values omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs describe shell as part of the console command service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="resolved">Resolved value supplied to the console command operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DescribeShell(LocalConsoleCommandRequest request, ProcessStartInfo resolved)
    {
        try
        {
            return request.Shell == LocalConsoleShellKind.Auto
                ? $"Auto ({Path.GetFileName(resolved.FileName)})"
                : request.Shell.ToString();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Describing local console shell failed; command values omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs bound display name as part of the console command service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the console command operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BoundDisplayName(string value)
    {
        try
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "LocalGPT command" : value.Trim();
            return normalized[..Math.Min(normalized.Length, 120)];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bounding local console display name failed; display text omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs bound capture as part of the console command service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the console command operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BoundCapture(string value)
    {
        try
        {
            return value.Length <= Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ConsoleMaximumCaptureCharacters)) ? value : value[..Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ConsoleMaximumCaptureCharacters))];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Bounding local console capture failed; output text omitted.");
            throw;
        }
    }

    /// <summary>
    /// Performs kill process tree as part of the console command service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="process">Process value supplied to the console command operation and used when producing its result.</param>
    private void KillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Best-effort local console process-tree cleanup failed after timeout or cancellation.");
        }
    }

}
