using LocalGPT.BusinessObjects.EFCore;

namespace LocalGPT.Interfaces.GameEngine;

/// <summary>
/// Seeds the shipped system games as ordinary game projects from the embedded seed documents
/// (<c>GameTemplates/Seeds/*.json</c>): project, authoring profile and declarative rule rows.
/// </summary>
public interface IGameProjectSeedService
{
    /// <summary>
    /// Creates missing system game projects and refreshes unmodified ones whose seed version is older.
    /// User-modified system games are kept unchanged.
    /// </summary>
    /// <param name="db">The seed stage's database context; the caller saves the changes.</param>
    /// <param name="cancellationToken">Cancels the seed.</param>
    /// <returns>A task completing when the seed rows are staged.</returns>
    Task SeedAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that every shipped system game exists as a seeded game profile.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>A task completing when the check passed; it throws when a system game is missing.</returns>
    Task VerifyAsync(LocalGptMemoryDbContext db, CancellationToken cancellationToken = default);
}
