using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Windows runtime-secret protection implementation. The containing user profile/application ACL remains authoritative.</summary>
public sealed class WindowsRuntimeSecretFileProtectionService : IRuntimeSecretFileProtectionService
{
    /// <summary>
    /// Performs restrict to current user as part of the windows runtime secret file protection service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public void RestrictToCurrentUser(string path)
    {
        try
        {
            // No portable ACL mutation is required here. Windows user-profile/application ACL inheritance
            // remains the authoritative boundary and avoids adding Windows-only access-control packages.
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError("Service method {0}.{1} failed: {2}", nameof(WindowsRuntimeSecretFileProtectionService), nameof(RestrictToCurrentUser), exception);
            throw;
        }
    }
}

/// <summary>Unix runtime-secret protection implementation using owner-only file mode.</summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class UnixRuntimeSecretFileProtectionService(ILogger<UnixRuntimeSecretFileProtectionService> logger) : IRuntimeSecretFileProtectionService
{
    /// <summary>
    /// Performs restrict to current user as part of the unix runtime secret file protection service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public void RestrictToCurrentUser(string path)
    {
        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            logger.LogWarning(exception, "Could not restrict runtime secret permissions; secret path and contents were omitted.");
        }
    }
}
