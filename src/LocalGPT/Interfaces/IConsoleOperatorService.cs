using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Parses and executes the reusable meta-command vocabulary used by LocalGPT's Matrix-style ASCII operator surfaces.</summary>
public interface IConsoleOperatorService
{
    /// <summary>Returns shell backends the current host can expose behind LocalGPT's own ASCII wall.</summary>
    /// <returns>The available shell descriptor collection.</returns>
    IReadOnlyList<LocalConsoleShellDescriptor> GetAvailableShells();

    /// <summary>Submits one user-entered line, treating colon-prefixed lines as meta commands and ordinary lines as shell commands.</summary>
    /// <param name="request">Operator line and current shell selection.</param>
    /// <param name="cancellationToken">Cancellation token for parsing/control work.</param>
    /// <returns>The immediate operator-layer result.</returns>
    Task<LocalConsoleOperatorResult> SubmitAsync(LocalConsoleOperatorRequest request, CancellationToken cancellationToken = default);
}
