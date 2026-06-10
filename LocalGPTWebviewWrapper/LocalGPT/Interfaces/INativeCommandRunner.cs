using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    public interface INativeCommandRunner
    {
        Task<CommandExecutionResult?> RunAsync(string fileName, string arguments, string workingDirectory, CancellationToken cancellationToken = default);
    }
}
