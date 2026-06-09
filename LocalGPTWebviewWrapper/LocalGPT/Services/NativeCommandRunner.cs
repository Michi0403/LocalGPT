using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Extensions.PlainStatics.CouncilData.Data;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services
{
    public class NativeCommandRunner(
        ILogger<NativeCommandRunner> logger,
        IMinecraftModWorkspaceService workspaceService,
        IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory) : INativeCommandRunner
    {
        private static readonly HashSet<string> AllowedExecutables = new(StringComparer.OrdinalIgnoreCase)
        {
            "powershell.exe",
            "pwsh.exe",
            "gradle",
            "gradle.bat",
            "gradlew",
            "gradlew.bat",
            "java",
            "java.exe"
        };

        public async Task<CommandExecutionResult> RunAsync(string fileName, string arguments, string workingDirectory, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Command is required.", nameof(fileName));

            var startedAt = DateTime.UtcNow;
            var policy = ValidatePolicy(fileName, arguments, workingDirectory);
            if (!policy.Allowed)
            {
                await SaveCommandLogAsync(fileName, arguments, workingDirectory, startedAt, DateTime.UtcNow, -1, string.Empty, string.Empty, policy, cancellationToken);
                throw new InvalidOperationException(policy.Reason);
            }

            if (!Directory.Exists(workingDirectory))
                throw new DirectoryNotFoundException($"Working directory does not exist: {workingDirectory}");

            if (!workspaceService.IsPathInsideWorkspaceRoot(workingDirectory))
                throw new InvalidOperationException("Commands can only run inside the LocalGPT Minecraft workspace root.");

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            logger.LogInformation(
                "Running native command: {FileName} {Arguments} in {WorkingDirectory}. Profile: {CommandProfile}. Policy: {PolicyReason}",
                fileName,
                arguments,
                workingDirectory,
                policy.Profile,
                policy.Reason);

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var completedAt = DateTime.UtcNow;
            var output = await outputTask;
            var error = await errorTask;
            var (stdoutPath, stderrPath) = await WriteCommandOutputAsync(
                workingDirectory,
                fileName,
                startedAt,
                output,
                error,
                cancellationToken);
            await SaveCommandLogAsync(
                fileName,
                arguments,
                workingDirectory,
                startedAt,
                completedAt,
                process.ExitCode,
                stdoutPath,
                stderrPath,
                policy,
                cancellationToken);

            return new CommandExecutionResult
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                StartedAtUtc = startedAt,
                CompletedAtUtc = completedAt,
                ExitCode = process.ExitCode,
                StandardOutput = output,
                StandardError = error,
                Duration = completedAt - startedAt,
                StdoutPath = stdoutPath,
                StderrPath = stderrPath,
                CommandProfile = policy.Profile,
                PolicyDecision = policy.Decision,
                PolicyReason = policy.Reason
            };
        }

        private CommandPolicyDecision ValidatePolicy(string fileName, string arguments, string workingDirectory)
        {
            var executable = Path.GetFileName(fileName.Trim());
            if (!AllowedExecutables.Contains(executable))
                return CommandPolicyDecision.Denied(
                    $"Executable '{executable}' is not allowed by LocalGPT native command policy.");

            if (ContainsPathSegment(fileName))
            {
                var executablePath = Path.GetFullPath(Path.Combine(workingDirectory, fileName));
                if (!workspaceService.IsPathInsideWorkspaceRoot(executablePath))
                    return CommandPolicyDecision.Denied(
                        "Executable paths must stay inside the LocalGPT Minecraft workspace root.");
            }

            if (IsPowerShell(executable))
                return ValidatePowerShellPolicy(arguments, workingDirectory);

            var profile = ClassifyCommandProfile(executable, arguments);
            return CommandPolicyDecision.Allow(
                profile,
                $"Profile '{profile}' selected for allowlisted executable '{executable}'.");
        }

        private CommandPolicyDecision ValidatePowerShellPolicy(string arguments, string workingDirectory)
        {
            if (Regex.IsMatch(arguments, @"(?i)(^|\s)-EncodedCommand(\s|$)|(^|\s)-Command(\s|$)|(^|\s)-c(\s|$)"))
                return CommandPolicyDecision.Denied(
                    "PowerShell inline commands are blocked; use -File with a workspace script.");

            var match = Regex.Match(arguments, @"(?i)(^|\s)-File\s+(?:""(?<path>[^""]+)""|'(?<path>[^']+)'|(?<path>\S+))");
            if (!match.Success)
                return CommandPolicyDecision.Denied("PowerShell commands must use -File with a workspace script.");

            var scriptPath = match.Groups["path"].Value;
            if (!scriptPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                return CommandPolicyDecision.Denied("PowerShell -File must target a .ps1 script.");

            var fullScriptPath = Path.GetFullPath(Path.Combine(workingDirectory, scriptPath));
            if (!workspaceService.IsPathInsideWorkspaceRoot(fullScriptPath))
                return CommandPolicyDecision.Denied("PowerShell script paths must stay inside the LocalGPT Minecraft workspace root.");

            if (!File.Exists(fullScriptPath))
                return CommandPolicyDecision.Denied($"PowerShell script does not exist: {fullScriptPath}");

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
            var logDirectory = Path.Combine(workingDirectory, ".localgpt", "command-logs");
            Directory.CreateDirectory(logDirectory);

            var safeName = SanitizeFileName(Path.GetFileNameWithoutExtension(fileName));
            var stamp = startedAt.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
            var stdoutPath = Path.Combine(logDirectory, $"{stamp}-{safeName}-stdout.txt");
            var stderrPath = Path.Combine(logDirectory, $"{stamp}-{safeName}-stderr.txt");

            await File.WriteAllTextAsync(stdoutPath, stdout, cancellationToken);
            await File.WriteAllTextAsync(stderrPath, stderr, cancellationToken);
            return (stdoutPath, stderrPath);
        }

        private async Task SaveCommandLogAsync(
            string fileName,
            string arguments,
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
                await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
                await NativeCommandLogSchema.EnsureCreatedAsync(db, cancellationToken);
                db.NativeCommandLogs.Add(new NativeCommandLogEntry
                {
                    StartedAtUtc = startedAt,
                    CompletedAtUtc = completedAt,
                    Executable = Path.GetFileName(fileName.Trim()),
                    CommandProfile = policy.Profile,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    ExitCode = exitCode,
                    DurationMilliseconds = Math.Max(0, (completedAt - startedAt).TotalMilliseconds),
                    StdoutPath = stdoutPath,
                    StderrPath = stderrPath,
                    PolicyDecision = policy.Decision,
                    PolicyReason = policy.Reason
                });
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not persist native command ledger entry for {FileName}.", fileName);
            }
        }

        private static bool IsPowerShell(string executable)
        {
            return executable.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase) ||
                executable.Equals("pwsh.exe", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsGradle(string executable)
        {
            return executable.Equals("gradle", StringComparison.OrdinalIgnoreCase) ||
                executable.Equals("gradle.bat", StringComparison.OrdinalIgnoreCase) ||
                executable.Equals("gradlew", StringComparison.OrdinalIgnoreCase) ||
                executable.Equals("gradlew.bat", StringComparison.OrdinalIgnoreCase);
        }

        private static string ClassifyCommandProfile(string executable, string arguments)
        {
            if (IsGradle(executable))
            {
                return arguments.Contains("runClient", StringComparison.OrdinalIgnoreCase)
                    ? "GradleRunClient"
                    : "GradleBuildOnly";
            }

            if (executable.Equals("java", StringComparison.OrdinalIgnoreCase) ||
                executable.Equals("java.exe", StringComparison.OrdinalIgnoreCase))
            {
                var normalized = arguments.Trim();
                return normalized.Equals("-version", StringComparison.OrdinalIgnoreCase) ||
                    normalized.Equals("--version", StringComparison.OrdinalIgnoreCase)
                    ? "JavaVersionOnly"
                    : "JavaAllowlistedCommand";
            }

            return "CustomAllowlistedCommand";
        }

        private static bool ContainsPathSegment(string fileName)
        {
            return fileName.Contains(Path.DirectorySeparatorChar) ||
                fileName.Contains(Path.AltDirectorySeparatorChar);
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
            return string.IsNullOrWhiteSpace(safe) ? "command" : safe;
        }

        public sealed record CommandPolicyDecision(bool Allowed, string Decision, string Reason, string Profile)
        {
            public static CommandPolicyDecision Allow(string profile, string reason) =>
                new(true, "Allowed", reason, profile);

            public static CommandPolicyDecision Denied(string reason) =>
                new(false, "Denied", reason, "Denied");
        }
    }
}
