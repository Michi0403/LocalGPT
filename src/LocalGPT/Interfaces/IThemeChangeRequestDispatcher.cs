using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the theme change request dispatcher contract.
/// </summary>
public interface IThemeChangeRequestDispatcher
{
    /// <summary>
    /// Runs the request theme change async operation.
    /// </summary>
    Task RequestThemeChangeAsync(Theme theme, ThemeApplicationTarget target);
    /// <summary>
    /// Runs the reset fusion route async operation.
    /// </summary>
    Task ResetFusionRouteAsync();
}
