namespace LocalGPT.Interfaces;

/// <summary>Applies host-specific private-file permissions without leaking OS-specific APIs into security workflows.</summary>
public interface IRuntimeSecretFileProtectionService
{
    /// <summary>Restricts a secret file to the current user as far as the host filesystem supports.</summary>
    /// <param name="path">Path value supplied to the runtime secret file protection operation and used when producing its result.</param>
    void RestrictToCurrentUser(string path);
}
