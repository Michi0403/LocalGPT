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

            logger.LogInformation(
                "Starting local console operation {OperationId} ({DisplayName}) using {Shell}; read-only: {IsReadOnly}.",
                operationId,
                BoundDisplayName(request.DisplayName),
                request.Shell,
                request.IsReadOnly);

            if (!process.Start())
                throw new InvalidOperationException("The local command process could not be started.");
            var stdoutPump = PumpOutputAsync(process.StandardOutput, operationId, request.DisplayName, "stdout", stdout);
            var stderrPump = PumpOutputAsync(process.StandardError, operationId, request.DisplayName, "stderr", stderr);

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            var timedOut = false;
            try
            {
                await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
                process.WaitForExit();
                await Task.WhenAll(stdoutPump, stderrPump).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                timedOut = true;
                KillProcessTree(process);
                await DrainOutputPumpsAsync(stdoutPump, stderrPump).ConfigureAwait(false);
                Publish(operationId, request.DisplayName, "system", $"Command timed out after {timeoutSeconds} second(s).");
            }
            catch (OperationCanceledException)
            {
                KillProcessTree(process);
                await DrainOutputPumpsAsync(stdoutPump, stderrPump).ConfigureAwait(false);
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
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
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
                return LocalGptApplicationDataPaths.ResolveProcessWorkingDirectory();
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

    /// <summary>Reads redirected terminal output without losing carriage-return or cursor-rewrite progress frames such as provider download percentages.</summary>
    /// <param name="reader">Redirected stdout or stderr reader.</param>
    /// <param name="operationId">Identifier of the current console operation.</param>
    /// <param name="displayName">Human-readable operation name.</param>
    /// <param name="stream">Console stream label.</param>
    /// <param name="capture">Bounded command-result capture buffer for this stream.</param>
    /// <returns>A task that completes after the redirected stream reaches end-of-file.</returns>
    private async Task PumpOutputAsync(StreamReader reader, Guid operationId, string displayName, string stream, StringBuilder capture)
    {
        try
        {
            var buffer = new char[2048];
            var pending = new StringBuilder();
            var lastProgressPercentages = new Dictionary<string, int>(StringComparer.Ordinal);
            string lastPublishedText = string.Empty;
            while (true)
            {
                var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);
                if (read <= 0)
                    break;

                for (var index = 0; index < read; index++)
                {
                    var character = buffer[index];
                    if (character is '\r' or '\n')
                    {
                        PublishOutputFrame(operationId, displayName, stream, pending, capture, character == '\r', ref lastPublishedText, lastProgressPercentages);
                        pending.Clear();
                        continue;
                    }
                    pending.Append(character);
                }

                PublishProgressSnapshot(operationId, displayName, stream, pending, capture, ref lastPublishedText, lastProgressPercentages);
            }

            PublishOutputFrame(operationId, displayName, stream, pending, capture, false, ref lastPublishedText, lastProgressPercentages);
        }
        catch (ObjectDisposedException)
        {
            // Process-tree cleanup can dispose redirected streams while a timeout/cancellation is being completed.
        }
        catch (IOException exception)
        {
            logger.LogDebug(exception, "Redirected local console stream closed while output was being drained.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading redirected local console output failed; output text was omitted from logs.");
        }
    }

    /// <summary>Publishes a complete newline/carriage-return frame while suppressing terminal spinner rewrites that contain no useful progress value.</summary>
    /// <param name="operationId">Identifier of the current console operation.</param>
    /// <param name="displayName">Human-readable operation name.</param>
    /// <param name="stream">Console stream label.</param>
    /// <param name="pending">Current redirected terminal frame.</param>
    /// <param name="capture">Bounded command-result capture buffer.</param>
    /// <param name="isRewrite">Value indicating whether a carriage return ended this frame.</param>
    /// <param name="lastPublishedText">Last normalized frame published for this stream.</param>
    /// <param name="lastProgressPercentages">Last percentage published for each stable provider progress identity.</param>
    private void PublishOutputFrame(
        Guid operationId,
        string displayName,
        string stream,
        StringBuilder pending,
        StringBuilder capture,
        bool isRewrite,
        ref string lastPublishedText,
        Dictionary<string, int> lastProgressPercentages)
    {
        try
        {
            if (pending.Length == 0)
                return;
            var normalized = NormalizeTerminalDisplayText(pending.ToString());
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            if (isRewrite)
            {
                if (!TryGetProgressPercentage(normalized, out var percentage, out var progressKey))
                    return;
                if (!ShouldPublishProgress(progressKey, percentage, lastProgressPercentages))
                    return;
            }
            else if (normalized.Equals(lastPublishedText, StringComparison.Ordinal))
            {
                return;
            }

            lastPublishedText = normalized;
            AppendOutput(operationId, displayName, stream, normalized, capture);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Publishing redirected local console frame failed; output text was omitted from logs.");
        }
    }

    /// <summary>Surfaces the latest useful percentage from a still-open terminal rewrite line without waiting for a newline.</summary>
    /// <param name="operationId">Identifier of the current console operation.</param>
    /// <param name="displayName">Human-readable operation name.</param>
    /// <param name="stream">Console stream label.</param>
    /// <param name="pending">Current redirected terminal frame.</param>
    /// <param name="capture">Bounded command-result capture buffer.</param>
    /// <param name="lastPublishedText">Last normalized frame published for this stream.</param>
    /// <param name="lastProgressPercentages">Last percentage published for each stable provider progress identity.</param>
    private void PublishProgressSnapshot(
        Guid operationId,
        string displayName,
        string stream,
        StringBuilder pending,
        StringBuilder capture,
        ref string lastPublishedText,
        Dictionary<string, int> lastProgressPercentages)
    {
        try
        {
            if (pending.Length == 0)
                return;
            var normalized = NormalizeTerminalDisplayText(pending.ToString());
            if (!TryGetProgressPercentage(normalized, out var percentage, out var progressKey))
                return;
            if (!ShouldPublishProgress(progressKey, percentage, lastProgressPercentages))
                return;

            lastPublishedText = normalized;
            AppendOutput(operationId, displayName, stream, normalized, capture);

            // Repeated ANSI cursor rewrites can otherwise keep the whole animation history in memory until EOF.
            pending.Clear();
            pending.Append(normalized);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Publishing redirected local console progress failed; output text was omitted from logs.");
        }
    }

    /// <summary>Records a percentage only when its stable progress identity moves to a different value.</summary>
    /// <param name="progressKey">Stable provider progress prefix, such as one Ollama layer identifier.</param>
    /// <param name="percentage">Current bounded integer percentage.</param>
    /// <param name="lastProgressPercentages">Last percentage published for each progress identity.</param>
    /// <returns><see langword="true"/> when this percentage should be surfaced.</returns>
    private bool ShouldPublishProgress(string progressKey, int percentage, Dictionary<string, int> lastProgressPercentages)
    {
        try
        {
            if (lastProgressPercentages.TryGetValue(progressKey, out var previous) && previous == percentage)
                return false;
            lastProgressPercentages[progressKey] = percentage;
            return true;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Deduplicating redirected local console progress failed; the current bounded percentage will remain visible.");
            return true;
        }
    }

    /// <summary>Finds a bounded integer percentage and a stable prefix identity in one normalized terminal frame.</summary>
    /// <param name="text">Normalized terminal text.</param>
    /// <param name="percentage">Parsed percentage when present.</param>
    /// <param name="progressKey">Stable text prefix before the percentage, bounded for dictionary use.</param>
    /// <returns><see langword="true"/> when a value from 0 through 100 immediately precedes a percent sign.</returns>
    private bool TryGetProgressPercentage(string text, out int percentage, out string progressKey)
    {
        percentage = 0;
        progressKey = string.Empty;
        try
        {
            for (var index = text.Length - 1; index >= 0; index--)
            {
                if (text[index] != '%')
                    continue;
                var end = index - 1;
                var start = end;
                while (start >= 0 && char.IsDigit(text[start]))
                    start--;
                start++;
                if (start > end)
                    continue;
                if (int.TryParse(text[start..(end + 1)], out var value) && value is >= 0 and <= 100)
                {
                    percentage = value;
                    var prefix = text[..start].TrimEnd();
                    if (prefix.Length > 192)
                        prefix = prefix[^192..];
                    progressKey = string.IsNullOrWhiteSpace(prefix) ? "progress" : prefix;
                    return true;
                }
            }
            return false;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Parsing redirected local console percentage failed; output text was omitted from logs.");
            percentage = 0;
            progressKey = string.Empty;
            return false;
        }
    }

    /// <summary>Best-effort drains redirected stdout/stderr after a killed process so the final diagnostic text is not lost.</summary>
    /// <param name="stdoutPump">Standard-output pump task.</param>
    /// <param name="stderrPump">Standard-error pump task.</param>
    /// <returns>A task that completes after both pumps finish or their stream cleanup is observed.</returns>
    private async Task DrainOutputPumpsAsync(Task stdoutPump, Task stderrPump)
    {
        try
        {
            await Task.WhenAll(stdoutPump, stderrPump).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Redirected local console streams could not be fully drained after process cleanup.");
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
            var normalized = NormalizeTerminalDisplayText(line);
            if (string.IsNullOrWhiteSpace(normalized))
                return;
            if (capture.Length < Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ConsoleMaximumCaptureCharacters)))
            {
                var remaining = Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ConsoleMaximumCaptureCharacters)) - capture.Length;
                var bounded = normalized.Length <= remaining ? normalized : normalized[..remaining];
                capture.AppendLine(bounded);
            }
            Publish(operationId, displayName, stream, normalized);
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
            var normalized = NormalizeTerminalDisplayText(text);
            recentOutput.Enqueue(new LocalConsoleOutputEvent
            {
                OperationId = operationId,
                TimestampUtc = DateTimeOffset.UtcNow,
                DisplayName = BoundDisplayName(displayName),
                Stream = stream,
                Text = normalized.Length <= Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ConsoleMaximumEventCharacters)) ? normalized : normalized[..Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ConsoleMaximumEventCharacters))]
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

    /// <summary>Normalizes one redirected terminal line for plain monospace UI surfaces by applying common cursor-rewrite controls and removing ANSI/OSC control sequences.</summary>
    /// <param name="text">One redirected stdout/stderr line.</param>
    /// <returns>Plain Unicode text representing the final visible terminal line state.</returns>
    private string NormalizeTerminalDisplayText(string text)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            var visible = new StringBuilder(text.Length);
            var cursor = 0;
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if (character == '\u001b')
                {
                    if (index + 1 >= text.Length)
                        break;
                    if (text[index + 1] == '[')
                    {
                        var end = index + 2;
                        while (end < text.Length && (text[end] < '@' || text[end] > '~'))
                            end++;
                        if (end >= text.Length)
                            break;
                        var final = text[end];
                        var parameters = text[(index + 2)..end];
                        if (final == 'G')
                            cursor = Math.Clamp(ResolveAnsiColumn(parameters) - 1, 0, visible.Length);
                        else if (final is 'H' or 'f')
                            cursor = 0;
                        else if (final == 'K' && cursor < visible.Length)
                            visible.Length = cursor;
                        index = end;
                        continue;
                    }
                    if (text[index + 1] == ']')
                    {
                        index += 2;
                        while (index < text.Length && text[index] != '\a')
                        {
                            if (text[index] == '\u001b' && index + 1 < text.Length && text[index + 1] == '\\')
                            {
                                index++;
                                break;
                            }
                            index++;
                        }
                        continue;
                    }
                    index++;
                    continue;
                }
                if (character == '\r')
                {
                    cursor = 0;
                    continue;
                }
                if (character == '\b')
                {
                    cursor = Math.Max(0, cursor - 1);
                    continue;
                }
                if (character == '\t')
                {
                    character = ' ';
                    var spaces = 4 - (cursor % 4);
                    for (var count = 0; count < spaces; count++)
                    {
                        if (cursor < visible.Length)
                            visible[cursor] = ' ';
                        else
                            visible.Append(' ');
                        cursor++;
                    }
                    continue;
                }
                if (char.IsControl(character))
                    continue;
                if (cursor < visible.Length)
                    visible[cursor] = character;
                else
                    visible.Append(character);
                cursor++;
            }
            return visible.ToString().TrimEnd();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Normalizing redirected terminal display text failed; output text was omitted from logs.");
            return string.Empty;
        }
    }

    /// <summary>Resolves the one-based ANSI cursor column used by the bounded plain-text terminal normalizer.</summary>
    /// <param name="parameters">CSI parameter text preceding the cursor-column command.</param>
    /// <returns>A one-based cursor column, defaulting to one when the terminal omitted it.</returns>
    private int ResolveAnsiColumn(string parameters)
    {
        try
        {
            var value = parameters.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            return int.TryParse(value, out var column) && column > 0 ? column : 1;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Resolving one ANSI cursor column failed.");
            return 1;
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
