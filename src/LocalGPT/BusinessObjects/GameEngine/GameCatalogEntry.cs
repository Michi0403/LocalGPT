namespace LocalGPT.BusinessObjects.GameEngine;

/// <summary>
/// One launchable game of the project system, listed for game launchers and Council bootstraps.
/// </summary>
public sealed class GameCatalogEntry
{
    /// <summary>
    /// Gets or sets the stable game key.
    /// </summary>
    /// <value>The normalized game key of the project profile.</value>
    public string GameKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    /// <value>The game display name.</value>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the owning game project.
    /// </summary>
    /// <value>The project identifier.</value>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Gets or sets the default Council team of the game.
    /// </summary>
    /// <value>The default team key.</value>
    public string DefaultTeamKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the game is shipped seed content.
    /// </summary>
    /// <value><c>true</c> for seeded system games.</value>
    public bool IsSystemSeed { get; set; }

    /// <summary>
    /// Gets or sets the resolved view key.
    /// </summary>
    /// <value>The view primitive key.</value>
    public string ViewKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved turn model key.
    /// </summary>
    /// <value>The turn model key; council games are started by Council workflows rather than launch buttons.</value>
    public string TurnModelKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the launch label shown on game launchers.
    /// </summary>
    /// <value>The launcher label from the game's texts, or the display name.</value>
    public string LaunchLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the launcher ordering.
    /// </summary>
    /// <value>The ascending launcher position.</value>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets whether the game offers itself as a quick launcher (it defines the text <c>launcher.order</c>).
    /// </summary>
    /// <value><c>true</c> for games shown as launcher buttons.</value>
    public bool ShowInLauncher { get; set; }

    /// <summary>
    /// Gets or sets the terminal width the game is designed for.
    /// </summary>
    /// <value>The frame width in characters.</value>
    public int FrameWidth { get; set; } = 80;

    /// <summary>
    /// Gets or sets the terminal height the game is designed for.
    /// </summary>
    /// <value>The frame height in lines.</value>
    public int FrameHeight { get; set; } = 25;
}
