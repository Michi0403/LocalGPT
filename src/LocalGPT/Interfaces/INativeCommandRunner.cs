using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the native command runner contract.
    /// </summary>
    public interface INativeCommandRunner
    {
        /// <summary>
        /// Runs the run async operation.
        /// </summary>
        Task<CommandExecutionResult?> RunAsync(string fileName, string arguments, string workingDirectory, CancellationToken cancellationToken = default, bool userConfirmed = false);
    }
}
