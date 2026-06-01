using System.Diagnostics;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services
{
    public class NativeCommandRunner(
        ILogger<NativeCommandRunner> logger,
        IMinecraftModWorkspaceService workspaceService) : INativeCommandRunner
    {
        public async Task<CommandExecutionResult> RunAsync(string fileName, string arguments, string workingDirectory, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Command is required.", nameof(fileName));

            if (!Directory.Exists(workingDirectory))
                throw new DirectoryNotFoundException($"Working directory does not exist: {workingDirectory}");

            if (!workspaceService.IsPathInsideWorkspaceRoot(workingDirectory))
                throw new InvalidOperationException("Commands can only run inside the LocalGPT Minecraft workspace root.");

            var startedAt = DateTimeOffset.UtcNow;
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
            logger.LogInformation("Running native command: {FileName} {Arguments} in {WorkingDirectory}", fileName, arguments, workingDirectory);

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return new CommandExecutionResult
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                ExitCode = process.ExitCode,
                StandardOutput = await outputTask,
                StandardError = await errorTask,
                Duration = DateTimeOffset.UtcNow - startedAt
            };
        }
    }
}
