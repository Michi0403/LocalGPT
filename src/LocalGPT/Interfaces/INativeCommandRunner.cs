using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for native command runner behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface INativeCommandRunner
    {
        /// <summary>
        /// Performs run for <see cref="INativeCommandRunner"/>, keeping the operation consistent with the state and invariants of the surrounding native command runner workflow.
        /// </summary>
        /// <param name="fileName">File name value supplied to the native command runner operation and used when producing its result.</param>
        /// <param name="arguments">Arguments value supplied to the native command runner operation and used when producing its result.</param>
        /// <param name="workingDirectory">Working directory value supplied to the native command runner operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <returns>The command execution result produced by the operation.</returns>
        Task<CommandExecutionResult?> RunAsync(string fileName, string arguments, string workingDirectory, CancellationToken cancellationToken = default, bool userConfirmed = false);
    }
}
