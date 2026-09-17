using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Owns LocalGPT's human-facing ASCII operator command vocabulary above the host shell adapters.</summary>
/// <param name="console">Bounded console command/process service used for shell work and process control.</param>
/// <param name="logger">Writes parser/control diagnostics without recording human-entered command text.</param>
public sealed class ConsoleOperatorService(IConsoleCommandService console, ILogger<ConsoleOperatorService> logger) : IConsoleOperatorService
{
    /// <summary>
    /// Retrieves available shells as part of the console operator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public IReadOnlyList<LocalConsoleShellDescriptor> GetAvailableShells()
    {
        try
        {
            return console.GetAvailableShells();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Discovering ASCII operator shell backends failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs submit as part of the console operator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<LocalConsoleOperatorResult> SubmitAsync(LocalConsoleOperatorRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            var input = (request.Input ?? string.Empty).Trim();
            if (input.Length == 0)
                return Result(request, false, "Enter :help for operator commands or type a shell command.");

            if (input.StartsWith(':'))
                return await SubmitMetaCommandAsync(request, input[1..], cancellationToken).ConfigureAwait(false);

            if (!request.UserConfirmed)
                return Result(request, false, "Human confirmation is required before an operating-system command can start.");

            var operationId = console.Start(new LocalConsoleCommandRequest
            {
                DisplayName = "ASCII operator shell",
                Shell = request.Shell,
                CommandText = input,
                WorkingDirectory = request.WorkingDirectory,
                TimeoutSeconds = 0,
                IsReadOnly = false,
                UserConfirmed = true
            });
            return Result(request, true, $"Queued job {ShortId(operationId)} behind the ASCII wall.", operationId);
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "ASCII operator submission was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ASCII operator submission failed; human-entered command text was omitted from logs.");
            throw;
        }
    }

    /// <summary>Executes one colon-prefixed control command without forwarding it to a host shell.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="commandLine">Command line value supplied to the console operator operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local console operator result produced by the operation.</returns>
    private async Task<LocalConsoleOperatorResult> SubmitMetaCommandAsync(LocalConsoleOperatorRequest request, string commandLine, CancellationToken cancellationToken)
    {
        try
        {
            var parts = commandLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return Result(request, false, "Enter :help for operator commands.");

            var command = parts[0].ToLowerInvariant();
            switch (command)
            {
                case "help":
                    return Result(request, true, ":chat <prompt> · :council <prompt|status|stop|skip> · :session <list|new|open selector> · :game <corridor|dragon|project selector|status|end> · :mode <chat|game|operator> · :fullscreen [fit|width|native] · :shells · :shell <auto|zsh|bash|sh|pwsh|cmd> · :jobs · :cancel [job|all] · :signal <job> <INT|TERM|KILL|HUP> · :cd <path> · :clear");
                case "chat":
                    return ParseChatAction(request, commandLine);
                case "council":
                    return ParseCouncilAction(request, commandLine);
                case "session":
                    return ParseSessionAction(request, commandLine);
                case "game":
                    return ParseGameAction(request, commandLine);
                case "mode":
                    return ParseModeAction(request, commandLine);
                case "fullscreen":
                    return ParseFullscreenAction(request, commandLine);
                case "shells":
                    return Result(request, true, FormatShells());
                case "shell":
                    return SelectShell(request, parts.Length > 1 ? parts[1] : string.Empty);
                case "jobs":
                    return Result(request, true, FormatJobs());
                case "cancel":
                    return Cancel(request, parts.Length > 1 ? parts[1] : string.Empty);
                case "signal":
                    return await SignalAsync(request, parts, cancellationToken).ConfigureAwait(false);
                case "cd":
                    return ChangeDirectory(request, commandLine.Length > 2 ? commandLine[2..].Trim() : string.Empty);
                case "clear":
                    console.ClearRecentOutput();
                    return Result(request, true, "ASCII console history cleared; active jobs were not affected.");
                default:
                    return Result(request, false, $"Unknown meta command :{parts[0]}. Enter :help.");
            }
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "ASCII operator meta command was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Executing ASCII operator meta command failed; command arguments were omitted from logs.");
            throw;
        }
    }

    /// <summary>Formats host shell discovery for the compact operator status row.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string FormatShells()
    {
        try
        {
            var shells = console.GetAvailableShells();
            return shells.Count == 0
                ? "No shell backend is currently available."
                : "Shells: " + string.Join(" · ", shells.Select(item => $"{ShellToken(item.Shell)}{(item.IsDefault ? "*" : string.Empty)} ({Path.GetFileName(item.Executable)})"));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting ASCII operator shell discovery failed.");
            throw;
        }
    }

    /// <summary>Updates the caller-owned shell selection when that backend is installed.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="value">Value value supplied to the console operator operation and used when producing its result.</param>
    /// <returns>The local console operator result produced by the operation.</returns>
    private LocalConsoleOperatorResult SelectShell(LocalConsoleOperatorRequest request, string value)
    {
        try
        {
            if (!TryParseShell(value, out var shell))
                return Result(request, false, "Shell name not recognized. Use :shells to see installed backends.");
            if (!console.GetAvailableShells().Any(item => item.Shell == shell))
                return Result(request, false, $"{ShellToken(shell)} is not available on this host.");
            var result = Result(request, true, $"ASCII operator shell set to {ShellToken(shell)}.");
            result.SelectedShell = shell;
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Selecting ASCII operator shell failed.");
            throw;
        }
    }

    /// <summary>Formats currently running jobs without exposing command text.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string FormatJobs()
    {
        try
        {
            var jobs = console.GetActiveOperations();
            if (jobs.Count == 0)
                return "No active operator jobs.";
            return string.Join(" | ", jobs.Select(item => $"{ShortId(item.OperationId)} pid={item.ProcessId?.ToString() ?? "-"} {item.Shell} {item.Status} {item.DisplayName}"));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting ASCII operator jobs failed.");
            throw;
        }
    }

    /// <summary>Requests cancellation for one matching job or every active job.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local console operator result produced by the operation.</returns>
    private LocalConsoleOperatorResult Cancel(LocalConsoleOperatorRequest request, string token)
    {
        try
        {
            var jobs = console.GetActiveOperations();
            if (jobs.Count == 0)
                return Result(request, false, "No active operator job can be cancelled.");
            if (string.Equals(token, "all", StringComparison.OrdinalIgnoreCase))
            {
                var count = jobs.Count(item => console.Cancel(item.OperationId));
                return Result(request, count > 0, $"Cancellation requested for {count} job(s).");
            }
            var operation = ResolveOperation(jobs, token);
            if (operation is null)
                return Result(request, false, "Job not found. Use :jobs, then pass the shown short id.");
            var accepted = console.Cancel(operation.OperationId);
            return Result(request, accepted, accepted ? $"Cancellation requested for {ShortId(operation.OperationId)}." : "The job already completed.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cancelling ASCII operator job failed.");
            throw;
        }
    }

    /// <summary>Sends one supported host signal to a service-owned process.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="parts">Parts value supplied to the console operator operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local console operator result produced by the operation.</returns>
    private async Task<LocalConsoleOperatorResult> SignalAsync(LocalConsoleOperatorRequest request, string[] parts, CancellationToken cancellationToken)
    {
        try
        {
            if (parts.Length < 3)
                return Result(request, false, "Usage: :signal <job> <INT|TERM|KILL|HUP>.");
            var operation = ResolveOperation(console.GetActiveOperations(), parts[1]);
            if (operation is null)
                return Result(request, false, "Job not found. Use :jobs, then pass the shown short id.");
            if (!TryParseSignal(parts[2], out var signal))
                return Result(request, false, "Signal not recognized. Use INT, TERM, KILL, or HUP.");
            await console.SendSignalAsync(operation.OperationId, signal, cancellationToken).ConfigureAwait(false);
            return Result(request, true, $"Sent {signal.ToString().ToUpperInvariant()} to {ShortId(operation.OperationId)} (pid {operation.ProcessId?.ToString() ?? "pending"}).");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Sending ASCII operator process signal failed.");
            return Result(request, false, exception.Message);
        }
    }

    /// <summary>Changes the working directory retained by the renderer-owned operator session.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="value">Value value supplied to the console operator operation and used when producing its result.</param>
    /// <returns>The local console operator result produced by the operation.</returns>
    private LocalConsoleOperatorResult ChangeDirectory(LocalConsoleOperatorRequest request, string value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result(request, true, string.IsNullOrWhiteSpace(request.WorkingDirectory) ? Environment.CurrentDirectory : request.WorkingDirectory);
            var basis = string.IsNullOrWhiteSpace(request.WorkingDirectory) ? Environment.CurrentDirectory : request.WorkingDirectory;
            var path = Path.GetFullPath(Path.IsPathRooted(value) ? value : Path.Combine(basis, value));
            if (!Directory.Exists(path))
                return Result(request, false, "Directory does not exist.");
            var result = Result(request, true, $"Working directory: {path}");
            result.WorkingDirectory = path;
            return result;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Changing ASCII operator working directory failed; path details omitted.");
            return Result(request, false, "Working directory could not be changed.");
        }
    }

    /// <summary>Finds one job by full/short identifier, defaulting to the newest job when no token is supplied.</summary>
    /// <param name="jobs">Local console operation snapshot dependency used by the console operator workflow to provide the corresponding application capability.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The local console operation snapshot produced by the operation.</returns>
    private LocalConsoleOperationSnapshot? ResolveOperation(IReadOnlyList<LocalConsoleOperationSnapshot> jobs, string token)
    {
        try
        {
            if (jobs.Count == 0)
                return null;
            if (string.IsNullOrWhiteSpace(token) || string.Equals(token, "current", StringComparison.OrdinalIgnoreCase))
                return jobs.OrderByDescending(item => item.StartedUtc).First();
            var normalized = token.Trim().Replace("-", string.Empty, StringComparison.Ordinal);
            return jobs.FirstOrDefault(item => item.OperationId.ToString("N").StartsWith(normalized, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving ASCII operator job identifier failed.");
            throw;
        }
    }

    /// <summary>
    /// Attempts to parse shell as part of the console operator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the console operator operation and used when producing its result.</param>
    /// <param name="shell">Shell value supplied to the console operator operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TryParseShell(string value, out LocalConsoleShellKind shell)
    {
        try
        {
            var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            shell = normalized switch
            {
                "auto" => LocalConsoleShellKind.Auto,
                "pwsh" or "powershell" => LocalConsoleShellKind.PowerShell,
                "bash" => LocalConsoleShellKind.Bash,
                "zsh" => LocalConsoleShellKind.Zsh,
                "sh" => LocalConsoleShellKind.Sh,
                "cmd" => LocalConsoleShellKind.Cmd,
                _ => (LocalConsoleShellKind)(-1)
            };
            return Enum.IsDefined(typeof(LocalConsoleShellKind), shell);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing ASCII operator shell token failed.");
            throw;
        }
    }

    /// <summary>
    /// Attempts to parse signal as part of the console operator service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the console operator operation and used when producing its result.</param>
    /// <param name="signal">Signal value supplied to the console operator operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TryParseSignal(string value, out LocalConsoleSignalKind signal)
    {
        try
        {
            var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized.StartsWith("SIG", StringComparison.Ordinal))
                normalized = normalized[3..];
            signal = normalized switch
            {
                "INT" or "INTERRUPT" => LocalConsoleSignalKind.Interrupt,
                "TERM" or "TERMINATE" => LocalConsoleSignalKind.Terminate,
                "KILL" => LocalConsoleSignalKind.Kill,
                "HUP" or "HANGUP" => LocalConsoleSignalKind.Hangup,
                _ => (LocalConsoleSignalKind)(-1)
            };
            return Enum.IsDefined(signal);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing ASCII operator signal token failed.");
            throw;
        }
    }

    /// <summary>Formats a bounded list of saved chat sessions for the ASCII Operator transcript.</summary>
    /// <param name="conversations">Saved conversations available to the current Chat page.</param>
    /// <param name="take">Maximum number of conversation rows to render.</param>
    /// <returns>A compact terminal-safe saved-session listing.</returns>
    public string FormatSavedConversations(IReadOnlyList<ChatMemoryConversationSummary> conversations, int take = 12)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(conversations);
            var lines = conversations
                .Take(Math.Clamp(take, 1, 50))
                .Select(item => $"{item.Id.ToString("N")[..8]}  {item.Title}  [{item.ProviderName}]  {item.MessageCount} message(s)")
                .ToArray();
            return lines.Length == 0
                ? "No saved conversations are available."
                : "Saved conversations:" + Environment.NewLine + string.Join(Environment.NewLine, lines);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting saved conversations for the ASCII operator failed.");
            throw;
        }
    }

    /// <summary>Resolves a saved conversation by full identifier, unique identifier prefix, or unique title fragment.</summary>
    /// <param name="conversations">Saved conversations available to the current Chat page.</param>
    /// <param name="selector">Human-entered saved-session selector.</param>
    /// <returns>The unique matching conversation, or <c>null</c> when no unique match exists.</returns>
    public ChatMemoryConversationSummary? ResolveSavedConversation(IReadOnlyList<ChatMemoryConversationSummary> conversations, string selector)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(conversations);
            var normalized = (selector ?? string.Empty).Trim();
            if (normalized.Length == 0)
                return null;

            if (Guid.TryParse(normalized, out var conversationId))
                return conversations.FirstOrDefault(item => item.Id == conversationId);

            var idMatches = conversations
                .Where(item => item.Id.ToString("N").StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();
            if (idMatches.Count == 1)
                return idMatches[0];

            var titleMatches = conversations
                .Where(item => item.Title.Contains(normalized, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();
            return titleMatches.Count == 1 ? titleMatches[0] : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving a saved conversation selector for the ASCII operator failed.");
            throw;
        }
    }

    /// <summary>Parses a chat application action while preserving the prompt as an opaque bounded argument.</summary>
    /// <param name="request">Operator request whose shell/session state is preserved in the result.</param>
    /// <param name="commandLine">Meta-command line to parse without logging its human-entered argument.</param>
    /// <returns>A typed Chat application action or a bounded usage result.</returns>
    private LocalConsoleOperatorResult ParseChatAction(LocalConsoleOperatorRequest request, string commandLine)
    {
        try
        {
            var argument = CommandArgument(commandLine);
            return string.IsNullOrWhiteSpace(argument)
                ? Result(request, false, "Usage: :chat <prompt>.")
                : ApplicationResult(request, LocalConsoleOperatorApplicationAction.ChatPrompt, argument, "Chat prompt ready for LocalGPT.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing an ASCII operator chat action failed.");
            throw;
        }
    }

    /// <summary>Parses an AI Council application action without executing it inside the shell service.</summary>
    /// <param name="request">Operator request whose shell/session state is preserved in the result.</param>
    /// <param name="commandLine">Council meta-command line to parse.</param>
    /// <returns>A typed Council action or a bounded usage result.</returns>
    private LocalConsoleOperatorResult ParseCouncilAction(LocalConsoleOperatorRequest request, string commandLine)
    {
        try
        {
            var argument = CommandArgument(commandLine);
            if (string.Equals(argument, "status", StringComparison.OrdinalIgnoreCase))
                return ApplicationResult(request, LocalConsoleOperatorApplicationAction.CouncilStatus, string.Empty, "Council status requested.");
            if (string.Equals(argument, "stop", StringComparison.OrdinalIgnoreCase))
                return ApplicationResult(request, LocalConsoleOperatorApplicationAction.CouncilStop, string.Empty, "Council stop requested.");
            if (string.Equals(argument, "skip", StringComparison.OrdinalIgnoreCase))
                return ApplicationResult(request, LocalConsoleOperatorApplicationAction.CouncilSkip, string.Empty, "Council round skip requested.");
            return string.IsNullOrWhiteSpace(argument)
                ? Result(request, false, "Usage: :council <prompt|status|stop|skip>.")
                : ApplicationResult(request, LocalConsoleOperatorApplicationAction.CouncilPrompt, argument, "Council prompt ready for LocalGPT.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing an ASCII operator Council action failed.");
            throw;
        }
    }

    /// <summary>Parses a saved-session list, create, or open action for the application dispatcher.</summary>
    /// <param name="request">Operator request whose shell/session state is preserved in the result.</param>
    /// <param name="commandLine">Session meta-command line to parse.</param>
    /// <returns>A typed saved-session action or a bounded usage result.</returns>
    private LocalConsoleOperatorResult ParseSessionAction(LocalConsoleOperatorRequest request, string commandLine)
    {
        try
        {
            var argument = CommandArgument(commandLine);
            if (string.Equals(argument, "list", StringComparison.OrdinalIgnoreCase))
                return ApplicationResult(request, LocalConsoleOperatorApplicationAction.SessionList, string.Empty, "Saved chat list requested.");
            if (string.Equals(argument, "new", StringComparison.OrdinalIgnoreCase))
                return ApplicationResult(request, LocalConsoleOperatorApplicationAction.SessionNew, string.Empty, "Fresh chat requested.");
            if (argument.StartsWith("open ", StringComparison.OrdinalIgnoreCase))
            {
                var selector = argument[5..].Trim();
                return string.IsNullOrWhiteSpace(selector)
                    ? Result(request, false, "Usage: :session open <id or title>.")
                    : ApplicationResult(request, LocalConsoleOperatorApplicationAction.SessionOpen, selector, "Saved chat open requested.");
            }
            return Result(request, false, "Usage: :session <list|new|open selector>.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing an ASCII operator saved-session action failed.");
            throw;
        }
    }

    /// <summary>Parses a game start, status, or end action for the application dispatcher.</summary>
    /// <param name="request">Operator request whose shell/session state is preserved in the result.</param>
    /// <param name="commandLine">Game meta-command line to parse.</param>
    /// <returns>A typed game action or a bounded usage result.</returns>
    private LocalConsoleOperatorResult ParseGameAction(LocalConsoleOperatorRequest request, string commandLine)
    {
        try
        {
            var argument = CommandArgument(commandLine);
            if (argument.StartsWith("project ", StringComparison.OrdinalIgnoreCase))
            {
                var selector = argument[8..].Trim();
                return string.IsNullOrWhiteSpace(selector)
                    ? Result(request, false, "Usage: :game project <project id or name>.")
                    : ApplicationResult(request, LocalConsoleOperatorApplicationAction.GameStartProject, selector, "Project game start requested.");
            }

            return argument.ToLowerInvariant() switch
            {
                "corridor" => ApplicationResult(request, LocalConsoleOperatorApplicationAction.GameStartCorridor, string.Empty, "ASCII corridor start requested."),
                "dragon" => ApplicationResult(request, LocalConsoleOperatorApplicationAction.GameStartDragon, string.Empty, "Green Dragon start requested."),
                "status" => ApplicationResult(request, LocalConsoleOperatorApplicationAction.GameStatus, string.Empty, "Game status requested."),
                "end" => ApplicationResult(request, LocalConsoleOperatorApplicationAction.GameEnd, string.Empty, "Game end requested."),
                _ => Result(request, false, "Usage: :game <corridor|dragon|project selector|status|end>.")
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing an ASCII operator game action failed.");
            throw;
        }
    }

    /// <summary>Parses an ASCII Chat, Game, or Operator presentation-mode action.</summary>
    /// <param name="request">Operator request whose shell/session state is preserved in the result.</param>
    /// <param name="commandLine">Presentation-mode meta-command line to parse.</param>
    /// <returns>A typed presentation-mode action or a bounded usage result.</returns>
    private LocalConsoleOperatorResult ParseModeAction(LocalConsoleOperatorRequest request, string commandLine)
    {
        try
        {
            return CommandArgument(commandLine).ToLowerInvariant() switch
            {
                "chat" => ApplicationResult(request, LocalConsoleOperatorApplicationAction.ModeChat, string.Empty, "ASCII chat mode requested."),
                "game" => ApplicationResult(request, LocalConsoleOperatorApplicationAction.ModeGame, string.Empty, "ASCII game mode requested."),
                "operator" => ApplicationResult(request, LocalConsoleOperatorApplicationAction.ModeOperator, string.Empty, "ASCII operator mode requested."),
                _ => Result(request, false, "Usage: :mode <chat|game|operator>.")
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing an ASCII operator mode action failed.");
            throw;
        }
    }

    /// <summary>Parses a fullscreen action and normalizes its optional scale-mode argument.</summary>
    /// <param name="request">Operator request whose shell/session state is preserved in the result.</param>
    /// <param name="commandLine">Fullscreen meta-command line to parse.</param>
    /// <returns>A typed fullscreen action containing a normalized scale mode.</returns>
    private LocalConsoleOperatorResult ParseFullscreenAction(LocalConsoleOperatorRequest request, string commandLine)
    {
        try
        {
            var argument = CommandArgument(commandLine).ToLowerInvariant();
            var scale = argument switch
            {
                "width" => "Width",
                "native" => "Native",
                _ => "Fit"
            };
            return ApplicationResult(request, LocalConsoleOperatorApplicationAction.Fullscreen, scale, $"Fullscreen requested with {scale.ToLowerInvariant()} scaling.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing an ASCII operator fullscreen action failed.");
            throw;
        }
    }

    /// <summary>Returns the text following the first command token without exposing it to diagnostics.</summary>
    /// <param name="commandLine">Meta-command line whose first token is the command name.</param>
    /// <returns>The trimmed command argument, or an empty string when no argument is present.</returns>
    private string CommandArgument(string commandLine)
    {
        try
        {
            var separator = commandLine.IndexOf(' ');
            return separator < 0 ? string.Empty : commandLine[(separator + 1)..].Trim();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Parsing an ASCII operator command argument failed; argument text was omitted from diagnostics.");
            throw;
        }
    }

    /// <summary>Creates a typed application action result so renderer components never reparse operator command text.</summary>
    /// <param name="request">Operator request whose shell/session state is preserved.</param>
    /// <param name="action">Typed application action for the renderer dispatcher.</param>
    /// <param name="argument">Bounded opaque argument associated with the action.</param>
    /// <param name="message">Human-readable parser status shown in the Operator surface.</param>
    /// <returns>A bounded accepted Operator result carrying the typed application action.</returns>
    private LocalConsoleOperatorResult ApplicationResult(
        LocalConsoleOperatorRequest request,
        LocalConsoleOperatorApplicationAction action,
        string argument,
        string message)
    {
        try
        {
            var result = Result(request, true, message);
            result.ApplicationAction = action;
            result.ApplicationArgument = argument.Length <= 100000 ? argument : argument[..100000];
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating an ASCII operator application action result failed.");
            throw;
        }
    }

    /// <summary>Creates one bounded parser result while retaining caller-owned shell and working-directory state.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="accepted">Value indicating whether accepted should apply to this operation.</param>
    /// <param name="message">Message value supplied to the console operator operation and used when producing its result.</param>
    /// <param name="operationId">Identifier of the operation to use for this operation.</param>
    /// <returns>The local console operator result produced by the operation.</returns>
    private LocalConsoleOperatorResult Result(LocalConsoleOperatorRequest request, bool accepted, string message, Guid? operationId = null)
    {
        try
        {
            return new LocalConsoleOperatorResult
            {
                Accepted = accepted,
                SelectedShell = request.Shell,
                WorkingDirectory = request.WorkingDirectory,
                Message = message.Length <= 1000 ? message : message[..1000],
                OperationId = operationId
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating ASCII operator result failed.");
            throw;
        }
    }

    /// <summary>Formats a stable eight-character operation identifier for the human-facing wall.</summary>
    /// <param name="value">Value value supplied to the console operator operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ShortId(Guid value)
    {
        try
        {
            return value.ToString("N")[..8];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting ASCII operator job identifier failed.");
            throw;
        }
    }

    /// <summary>Maps shell kinds to the concise tokens accepted by the ASCII operator command line.</summary>
    /// <param name="shell">Shell value supplied to the console operator operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ShellToken(LocalConsoleShellKind shell)
    {
        try
        {
            return shell switch
            {
                LocalConsoleShellKind.PowerShell => "pwsh",
                LocalConsoleShellKind.Zsh => "zsh",
                LocalConsoleShellKind.Bash => "bash",
                LocalConsoleShellKind.Sh => "sh",
                LocalConsoleShellKind.Cmd => "cmd",
                LocalConsoleShellKind.Direct => "direct",
                _ => "auto"
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Formatting ASCII operator shell token failed.");
            throw;
        }
    }
}
