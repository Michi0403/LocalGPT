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
    ILocalGptRuntimePolicyDataService runtimePolicy,
    SqliteUtilityService sqliteUtility) : INativeCommandRunner
{
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
                var confirmationPolicy = DenyDecision(
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
                runtimePolicy.GetInt(LocalGptRuntimeValue.NativeCommandMinimumTimeoutSeconds),
                runtimePolicy.GetInt(LocalGptRuntimeValue.NativeCommandMaximumTimeoutSeconds));
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
    try
    {
            var policyOptions = commandOptions.CurrentValue;
            if (!policyOptions.Enabled)
            {
                return DenyDecision(
                    "Native command execution is disabled. The repository owner must explicitly enable NativeCommands:Enabled.");
            }

            var executable = Path.GetFileName(fileName.Trim());
            if (!runtimePolicy.AllowedNativeExecutables.Contains(executable))
                return DenyDecision($"Executable '{executable}' is not allowlisted.");

            if (sqliteUtility.ContainsPathSegment(fileName, logger))
            {
                var executablePath = Path.GetFullPath(Path.Combine(workingDirectory, fileName));
                if (!workspaceService.IsPathInsideWorkspaceRoot(executablePath))
                    return DenyDecision("Executable paths must stay inside the LocalGPT Minecraft workspace root.");
            }

            if (sqliteUtility.IsPowerShell(executable, logger))
            {
                if (!policyOptions.AllowPowerShellWorkspaceScripts)
                {
                    return DenyDecision(
                        "PowerShell workspace scripts are disabled. The repository owner must explicitly enable NativeCommands:AllowPowerShellWorkspaceScripts.");
                }

                return ValidatePowerShellPolicy(arguments, workingDirectory);
            }

            var profile = sqliteUtility.ClassifyCommandProfile(executable, arguments, logger);
            return AllowDecision(
                profile,
                $"Profile '{profile}' selected for allowlisted executable '{executable}'.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(NativeCommandRunner)}.{nameof(ValidatePolicy)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(NativeCommandRunner)}.{nameof(ValidatePolicy)} failed.");
        throw;
    }
}

    private CommandPolicyDecision ValidatePowerShellPolicy(string arguments, string workingDirectory)
    {
    try
    {
            if (runtimePolicy.PowerShellInlineCommandPattern.IsMatch(arguments))
                return DenyDecision("PowerShell inline commands are blocked; use -File with a workspace script.");

            var match = runtimePolicy.PowerShellFilePattern.Match(arguments);
            if (!match.Success)
                return DenyDecision("PowerShell commands must use -File with a workspace script.");

            var scriptPath = match.Groups["path"].Value;
            if (!scriptPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                return DenyDecision("PowerShell -File must target a .ps1 script.");

            var fullScriptPath = Path.GetFullPath(Path.Combine(workingDirectory, scriptPath));
            if (!workspaceService.IsPathInsideWorkspaceRoot(fullScriptPath))
                return DenyDecision("PowerShell script paths must stay inside the LocalGPT Minecraft workspace root.");
            if (!File.Exists(fullScriptPath))
                return DenyDecision("The requested PowerShell workspace script does not exist.");

            return AllowDecision(
                "PowerShellWorkspaceScript",
                "PowerShell -File script is inside the LocalGPT Minecraft workspace root.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(NativeCommandRunner)}.{nameof(ValidatePowerShellPolicy)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(NativeCommandRunner)}.{nameof(ValidatePowerShellPolicy)} failed.");
        throw;
    }
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

    private void KillProcessTree(Process process)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(NativeCommandRunner)}.{nameof(KillProcessTree)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(NativeCommandRunner)}.{nameof(KillProcessTree)} failed.");
        throw;
    }
}

    private string RedactArguments(string arguments)
    {
    try
    {
            if (string.IsNullOrEmpty(arguments))
                return string.Empty;

            return runtimePolicy.SensitiveArgumentPattern.Replace(arguments, match =>
                $"{match.Groups["name"].Value}{match.Groups["separator"].Value}[REDACTED]");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(NativeCommandRunner)}.{nameof(RedactArguments)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(NativeCommandRunner)}.{nameof(RedactArguments)} failed.");
        throw;
    }
}
    private CommandPolicyDecision AllowDecision(string profile, string reason)
    {
        try
        {
            var decision = new CommandPolicyDecision(
                true,
                runtimePolicy.GetString(LocalGptRuntimeValue.CommandPolicyAllowedDecision),
                reason,
                profile);
            logger.LogTrace("Created allowed native-command policy decision for profile {Profile}.", profile);
            return decision;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create an allowed native-command policy decision for profile {Profile}.", profile);
            throw;
        }
    }

    private CommandPolicyDecision DenyDecision(string reason)
    {
        try
        {
            var denied = runtimePolicy.GetString(LocalGptRuntimeValue.CommandPolicyDeniedDecision);
            var decision = new CommandPolicyDecision(
                false,
                denied,
                reason,
                runtimePolicy.GetString(LocalGptRuntimeValue.CommandPolicyDeniedProfile));
            logger.LogTrace("Created denied native-command policy decision.");
            return decision;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create a denied native-command policy decision.");
            throw;
        }
    }

}
