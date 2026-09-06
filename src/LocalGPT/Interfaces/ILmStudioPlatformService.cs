namespace LocalGPT.Interfaces;

/// <summary>Resolves platform-specific LM Studio/llmster CLI locations without leaking host paths into shared provider bootstrap logic.</summary>
public interface ILmStudioPlatformService
{
    /// <summary>Gets the short platform name used in diagnostics and setup guidance.</summary>
    string PlatformName { get; }

    /// <summary>Resolves the local <c>lms</c> executable, or <see langword="null"/> when it is not installed in a known location.</summary>
    string? ResolveExecutable();
}
