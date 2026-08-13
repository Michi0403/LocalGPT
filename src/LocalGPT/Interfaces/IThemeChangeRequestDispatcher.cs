using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for theme change request dispatcher behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IThemeChangeRequestDispatcher
{
    /// <summary>
    /// Performs request theme change for <see cref="IThemeChangeRequestDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding theme change request dispatcher workflow.
    /// </summary>
    /// <param name="theme">Theme value supplied to the theme change request dispatcher operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the theme change request dispatcher operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task RequestThemeChangeAsync(Theme theme, ThemeApplicationTarget target);
    /// <summary>
    /// Performs reset fusion route for <see cref="IThemeChangeRequestDispatcher"/>, keeping the operation consistent with the state and invariants of the surrounding theme change request dispatcher workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task ResetFusionRouteAsync();
}
