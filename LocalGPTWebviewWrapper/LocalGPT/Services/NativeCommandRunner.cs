using DevExpress.XtraRichEdit;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using static LocalGPT.Extensions.PlainStatics.GlobalVariableSlopCollectionToRemove;

namespace LocalGPT.Services
{
    public class NativeCommandRunner(
        ILogger<NativeCommandRunner> logger,
        IMinecraftModWorkspaceService workspaceService,
        IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory) : INativeCommandRunner
    {
        public static readonly HashSet<string> AllowedExecutables = new(StringComparer.OrdinalIgnoreCase)
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

        public async Task<CommandExecutionResult?> RunAsync(string fileName, string arguments, string workingDirectory, CancellationToken cancellationToken = default)
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RunAsync fileName {fileName} arguments {arguments} workingDirectory {workingDirectory}");
                return null;
            }
        }

        public CommandPolicyDecision? ValidatePolicy(string fileName, string arguments, string workingDirectory)
        {
            try
            {
                var executable = Path.GetFileName(fileName.Trim());
                if (!AllowedExecutables.Contains(executable))
                    return CouncilChatStaticsGeneral.CommandPolicyDecisionDenied(
                        $"Executable '{executable}' is not allowed by LocalGPT native command policy.", logger);

                if (SQLLiteFunctions.ContainsPathSegment(fileName, logger))
                {
                    var executablePath = Path.GetFullPath(Path.Combine(workingDirectory, fileName));
                    if (!workspaceService.IsPathInsideWorkspaceRoot(executablePath))
                        return CouncilChatStaticsGeneral.CommandPolicyDecisionDenied(
                            "Executable paths must stay inside the LocalGPT Minecraft workspace root.", logger);
                }

                if (SQLLiteFunctions.IsPowerShell(executable, logger))
                    return ValidatePowerShellPolicy(arguments, workingDirectory) ?? null;

                var profile = SQLLiteFunctions.ClassifyCommandProfile(executable, arguments, logger);
                return CouncilChatStaticsGeneral.CommandPolicyDecisionAllow(
                    profile,
                    $"Profile '{profile}' selected for allowlisted executable '{executable}'.", logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidatePolicy fileName {fileName} arguments {arguments} workingDirectory {workingDirectory}");
                return null;
            }
            
        }

        public CommandPolicyDecision? ValidatePowerShellPolicy(string arguments, string workingDirectory)
        {
            try
            {
                if (Regex.IsMatch(arguments, @"(?i)(^|\s)-EncodedCommand(\s|$)|(^|\s)-Command(\s|$)|(^|\s)-c(\s|$)"))
                    return CouncilChatStaticsGeneral.CommandPolicyDecisionDenied(
                        "PowerShell inline commands are blocked; use -File with a workspace script.", logger);

                var match = Regex.Match(arguments, @"(?i)(^|\s)-File\s+(?:""(?<path>[^""]+)""|'(?<path>[^']+)'|(?<path>\S+))");
                if (!match.Success)
                    return CouncilChatStaticsGeneral.CommandPolicyDecisionDenied("PowerShell commands must use -File with a workspace script.", logger);

                var scriptPath = match.Groups["path"].Value;
                if (!scriptPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                    return CouncilChatStaticsGeneral.CommandPolicyDecisionDenied("PowerShell -File must target a .ps1 script.", logger);

                var fullScriptPath = Path.GetFullPath(Path.Combine(workingDirectory, scriptPath));
                if (!workspaceService.IsPathInsideWorkspaceRoot(fullScriptPath))
                    return CouncilChatStaticsGeneral.CommandPolicyDecisionDenied("PowerShell script paths must stay inside the LocalGPT Minecraft workspace root.", logger);

                if (!File.Exists(fullScriptPath))
                    return CouncilChatStaticsGeneral.CommandPolicyDecisionDenied($"PowerShell script does not exist: {fullScriptPath}", logger);

                return CouncilChatStaticsGeneral.CommandPolicyDecisionAllow(
                    "PowerShellWorkspaceScript",
                    "PowerShell -File script is inside the LocalGPT Minecraft workspace root.", logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidatePowerShellPolicy arguments {arguments} workingDirectory {workingDirectory}");
                return null;
            }
        }

        public async Task<(string StdoutPath, string StderrPath)> WriteCommandOutputAsync(
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

                var safeName = SQLLiteFunctions.SanitizeFileName(Path.GetFileNameWithoutExtension(fileName), logger);
                var stamp = startedAt.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
                var stdoutPath = Path.Combine(logDirectory, $"{stamp}-{safeName}-stdout.txt");
                var stderrPath = Path.Combine(logDirectory, $"{stamp}-{safeName}-stderr.txt");

                await File.WriteAllTextAsync(stdoutPath, stdout, cancellationToken);
                await File.WriteAllTextAsync(stderrPath, stderr, cancellationToken);
                return (stdoutPath, stderrPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidatePowerShellPolicy workingDirectory {workingDirectory} fileName {fileName} startedAt {startedAt.ToString()} stdout {stdout} stderr {stderr}");
                return (string.Empty,string.Empty);
            }
        }

        public async Task SaveCommandLogAsync(
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
                await SQLLiteTableFunctions.EnsureCreatedNativeCommandLogsAsync(db, logger, cancellationToken);
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
                logger.LogError(ex, $"Error in ValidatePowerShellPolicy fileName {fileName} arguments {arguments} workingDirectory {workingDirectory.ToString()} startedAt {startedAt.ToString()} completedAt {completedAt.ToString()} exitCode {exitCode.ToString()} stdoutPath {stdoutPath.ToString()} stderrPath {stderrPath.ToString()} policy.Decision {policy.Decision.ToString()} policy.Reason {policy.Reason.ToString()}");
         
            }
        }
    }
}
