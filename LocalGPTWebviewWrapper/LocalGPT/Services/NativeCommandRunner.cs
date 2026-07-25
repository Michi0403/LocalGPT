using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LocalGPT.Services;

public sealed class NativeCommandRunner(
    ILogger<NativeCommandRunner> logger,
    IMinecraftModWorkspaceService workspaceService,
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IOptionsMonitor<NativeCommandOptions> commandOptions,
        SqliteUtilityService sqliteUtility) : INativeCommandRunner
{
    private const int MinimumTimeoutSeconds = 5;
    private const int MaximumTimeoutSeconds = 3600;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    private static readonly FrozenSet<string> AllowedExecutables = new[]
    {
        "powershell.exe",
        "pwsh.exe",
        "gradle",
        "gradle.bat",
        "gradlew",
        "gradlew.bat",
        "java",
        "java.exe"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly Regex PowerShellInlineCommandPattern = new(
        @"(^|\s)-EncodedCommand(\s|$)|(^|\s)-Command(\s|$)|(^|\s)-c(\s|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        RegexTimeout);

    private static readonly Regex PowerShellFilePattern = new(
        @"(^|\s)-File\s+(?:""(?<path>[^""]+)""|'(?<path>[^']+)'|(?<path>\S+))",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        RegexTimeout);

    private static readonly Regex SensitiveArgumentPattern = new(
        @"(?<name>--?(?:api[-_]?key|key|token|secret|password|passwd|pwd))(?<separator>\s+|=)(?<value>""[^""]*""|'[^']*'|\S+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        RegexTimeout);

    public async Task<CommandExecutionResult?> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken = default,
        bool userConfirmed = false)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Command is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(workingDirectory))
            throw new ArgumentException("Working directory is required.", nameof(workingDirectory));

        var startedAt = DateTime.UtcNow;
        var normalizedWorkingDirectory = Path.GetFullPath(workingDirectory);
        var redactedArguments = RedactArguments(arguments);

        try
        {
            if (!userConfirmed)
            {
                var confirmationPolicy = CommandPolicyDecision.Deny(
                    "Fresh human confirmation is required for this exact native command.");
                await SaveCommandLogAsync(
                    fileName,
                    redactedArguments,
                    normalizedWorkingDirectory,
                    startedAt,
                    DateTime.UtcNow,
                    -1,
                    string.Empty,
                    string.Empty,
                    confirmationPolicy,
                    cancellationToken).ConfigureAwait(false);
                logger.LogWarning("Native command denied because fresh human confirmation was not supplied.");
                return null;
            }

            var policy = ValidatePolicy(fileName, arguments, normalizedWorkingDirectory);
            if (!policy.Allowed)
            {
                await SaveCommandLogAsync(
                    fileName,
                    redactedArguments,
                    normalizedWorkingDirectory,
                    startedAt,
                    DateTime.UtcNow,
                    -1,
                    string.Empty,
                    string.Empty,
                    policy,
                    cancellationToken).ConfigureAwait(false);

                logger.LogWarning(
                    "Native command denied for executable {Executable}. Profile: {Profile}. Reason: {PolicyReason}",
                    Path.GetFileName(fileName),
                    policy.Profile,
                    policy.Reason);
                return null;
            }

            if (!Directory.Exists(normalizedWorkingDirectory))
                throw new DirectoryNotFoundException($"Working directory does not exist: {normalizedWorkingDirectory}");
            if (!workspaceService.IsPathInsideWorkspaceRoot(normalizedWorkingDirectory))
                throw new InvalidOperationException("Commands can only run inside the LocalGPT Minecraft workspace root.");

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = normalizedWorkingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            logger.LogInformation(
                "Running allowlisted native executable {Executable} in {WorkingDirectory}. Profile: {CommandProfile}.",
                Path.GetFileName(fileName),
                normalizedWorkingDirectory,
                policy.Profile);

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            var timeoutSeconds = Math.Clamp(
                commandOptions.CurrentValue.MaxDurationSeconds,
                MinimumTimeoutSeconds,
                MaximumTimeoutSeconds);
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var timedOut = false;
            try
            {
                await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                timedOut = true;
                KillProcessTree(process);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                KillProcessTree(process);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                await SaveCommandLogAsync(
                    fileName,
                    redactedArguments,
                    normalizedWorkingDirectory,
                    startedAt,
                    DateTime.UtcNow,
                    -3,
                    string.Empty,
                    string.Empty,
                    policy with { Decision = "Cancelled", Reason = "Command cancelled by caller." },
                    CancellationToken.None).ConfigureAwait(false);
                throw;
            }

            var completedAt = DateTime.UtcNow;
            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);
            var (stdoutPath, stderrPath) = await WriteCommandOutputAsync(
                normalizedWorkingDirectory,
                fileName,
                startedAt,
                output,
                error,
                cancellationToken).ConfigureAwait(false);

            var effectivePolicy = timedOut
                ? policy with { Decision = "TimedOut", Reason = $"Command exceeded the configured {timeoutSeconds}-second limit." }
                : policy;
            var exitCode = timedOut ? -2 : process.ExitCode;

            await SaveCommandLogAsync(
                fileName,
                redactedArguments,
                normalizedWorkingDirectory,
                startedAt,
                completedAt,
                exitCode,
                stdoutPath,
                stderrPath,
                effectivePolicy,
                cancellationToken).ConfigureAwait(false);

            if (timedOut)
            {
                logger.LogWarning(
                    "Native command {Executable} exceeded the configured {TimeoutSeconds}-second limit and was terminated.",
                    Path.GetFileName(fileName),
                    timeoutSeconds);
            }

            return new CommandExecutionResult
            {
                FileName = fileName,
                Arguments = redactedArguments,
                WorkingDirectory = normalizedWorkingDirectory,
                StartedAtUtc = startedAt,
                CompletedAtUtc = completedAt,
                ExitCode = exitCode,
                StandardOutput = output,
                StandardError = error,
                Duration = completedAt - startedAt,
                StdoutPath = stdoutPath,
                StderrPath = stderrPath,
                CommandProfile = effectivePolicy.Profile,
                PolicyDecision = effectivePolicy.Decision,
                PolicyReason = effectivePolicy.Reason
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Native command execution failed for executable {Executable} in {WorkingDirectory}.",
                Path.GetFileName(fileName),
                normalizedWorkingDirectory);
            return null;
        }
    }

    private CommandPolicyDecision ValidatePolicy(string fileName, string arguments, string workingDirectory)
    {
        var policyOptions = commandOptions.CurrentValue;
        if (!policyOptions.Enabled)
        {
            return CommandPolicyDecision.Deny(
                "Native command execution is disabled. The repository owner must explicitly enable NativeCommands:Enabled.");
        }

        var executable = Path.GetFileName(fileName.Trim());
        if (!AllowedExecutables.Contains(executable))
            return CommandPolicyDecision.Deny($"Executable '{executable}' is not allowlisted.");

        if (sqliteUtility.ContainsPathSegment(fileName, logger))
        {
            var executablePath = Path.GetFullPath(Path.Combine(workingDirectory, fileName));
            if (!workspaceService.IsPathInsideWorkspaceRoot(executablePath))
                return CommandPolicyDecision.Deny("Executable paths must stay inside the LocalGPT Minecraft workspace root.");
        }

        if (sqliteUtility.IsPowerShell(executable, logger))
        {
            if (!policyOptions.AllowPowerShellWorkspaceScripts)
            {
                return CommandPolicyDecision.Deny(
                    "PowerShell workspace scripts are disabled. The repository owner must explicitly enable NativeCommands:AllowPowerShellWorkspaceScripts.");
            }

            return ValidatePowerShellPolicy(arguments, workingDirectory);
        }

        var profile = sqliteUtility.ClassifyCommandProfile(executable, arguments, logger);
        return CommandPolicyDecision.Allow(
            profile,
            $"Profile '{profile}' selected for allowlisted executable '{executable}'.");
    }

    private CommandPolicyDecision ValidatePowerShellPolicy(string arguments, string workingDirectory)
    {
        if (PowerShellInlineCommandPattern.IsMatch(arguments))
            return CommandPolicyDecision.Deny("PowerShell inline commands are blocked; use -File with a workspace script.");

        var match = PowerShellFilePattern.Match(arguments);
        if (!match.Success)
            return CommandPolicyDecision.Deny("PowerShell commands must use -File with a workspace script.");

        var scriptPath = match.Groups["path"].Value;
        if (!scriptPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
            return CommandPolicyDecision.Deny("PowerShell -File must target a .ps1 script.");

        var fullScriptPath = Path.GetFullPath(Path.Combine(workingDirectory, scriptPath));
        if (!workspaceService.IsPathInsideWorkspaceRoot(fullScriptPath))
            return CommandPolicyDecision.Deny("PowerShell script paths must stay inside the LocalGPT Minecraft workspace root.");
        if (!File.Exists(fullScriptPath))
            return CommandPolicyDecision.Deny("The requested PowerShell workspace script does not exist.");

        return CommandPolicyDecision.Allow(
            "PowerShellWorkspaceScript",
            "PowerShell -File script is inside the LocalGPT Minecraft workspace root.");
    }

    private async Task<(string StdoutPath, string StderrPath)> WriteCommandOutputAsync(
        string workingDirectory,
        string fileName,
        DateTime startedAt,
        string stdout,
        string stderr,
        CancellationToken cancellationToken)
    {
        try
        {
            var logDirectory = Path.Combine(workingDirectory, ".localgpt", "command-logs");
            Directory.CreateDirectory(logDirectory);

            var safeName = sqliteUtility.SanitizeFileName(Path.GetFileNameWithoutExtension(fileName), logger);
            var stamp = startedAt.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
            var stdoutPath = Path.Combine(logDirectory, $"{stamp}-{safeName}-stdout.txt");
            var stderrPath = Path.Combine(logDirectory, $"{stamp}-{safeName}-stderr.txt");

            await File.WriteAllTextAsync(stdoutPath, stdout, cancellationToken).ConfigureAwait(false);
            await File.WriteAllTextAsync(stderrPath, stderr, cancellationToken).ConfigureAwait(false);
            return (stdoutPath, stderrPath);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Could not persist native command output for executable {Executable} in {WorkingDirectory}.",
                Path.GetFileName(fileName),
                workingDirectory);
            return (string.Empty, string.Empty);
        }
    }

    private async Task SaveCommandLogAsync(
        string fileName,
        string redactedArguments,
        string workingDirectory,
        DateTime startedAt,
        DateTime completedAt,
        int exitCode,
        string stdoutPath,
        string stderrPath,
        CommandPolicyDecision policy,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            db.NativeCommandLogs.Add(new NativeCommandLogEntry
            {
                StartedAtUtc = startedAt,
                CompletedAtUtc = completedAt,
                Executable = Path.GetFileName(fileName.Trim()),
                CommandProfile = policy.Profile,
                Arguments = redactedArguments,
                WorkingDirectory = workingDirectory,
                ExitCode = exitCode,
                DurationMilliseconds = Math.Max(0, (completedAt - startedAt).TotalMilliseconds),
                StdoutPath = stdoutPath,
                StderrPath = stderrPath,
                PolicyDecision = policy.Decision,
                PolicyReason = policy.Reason
            });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Could not persist native command audit record for executable {Executable}; exit code {ExitCode}; policy {PolicyDecision}.",
                Path.GetFileName(fileName),
                exitCode,
                policy.Decision);
        }
    }

    private static void KillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The process exited between the state check and the termination request.
        }
    }

    private static string RedactArguments(string arguments)
    {
        if (string.IsNullOrEmpty(arguments))
            return string.Empty;

        return SensitiveArgumentPattern.Replace(arguments, match =>
            $"{match.Groups["name"].Value}{match.Groups["separator"].Value}[REDACTED]");
    }
}
