namespace LocalGPT.Interfaces;

/// <summary>
/// Resolves platform-specific Ollama executable locations while keeping operating-system details out of the shared process coordinator.
/// </summary>
public interface IOllamaPlatformService
{
    /// <summary>Gets the short platform name used in diagnostics and setup guidance.</summary>
    /// <value>The platform name value exposed by <see cref="IOllamaPlatformService"/>.</value>
    string PlatformName { get; }

    /// <summary>Resolves the local Ollama executable, or <see langword="null"/> when it is not installed in a known location.</summary>
    /// <returns>The absolute executable path when one can be resolved.</returns>
    string? ResolveExecutable();

    /// <summary>Determines whether the resolved executable is the desktop application rather than the command-line server executable.</summary>
    /// <param name="executable">Absolute executable path returned by <see cref="ResolveExecutable"/>.</param>
    /// <returns><see langword="true"/> when the executable should be launched as a GUI application.</returns>
    bool IsGuiExecutable(string executable);
}
