namespace ProjectConsoleIdentity.BusinessObjects;

/// <summary>
/// Carries the immutable product and repository identity emitted by project assembly metadata for operator-facing console output and repository defaults.
/// </summary>
/// <param name="Product">Operator-visible product name.</param>
/// <param name="Version">Stable semantic version without informational build metadata.</param>
/// <param name="RepositoryUrl">Canonical repository URL emitted by project metadata.</param>
/// <param name="RepositorySlug">Repository owner/name slug derived from <paramref name="RepositoryUrl"/>.</param>
/// <param name="Owner">Repository or product owner emitted by project metadata.</param>
/// <param name="License">License identifier emitted by project metadata.</param>
internal sealed record ConsoleProductIdentity(
    string Product,
    string Version,
    string RepositoryUrl,
    string RepositorySlug,
    string Owner,
    string License);
