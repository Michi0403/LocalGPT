using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for theme load notifier behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface IThemeLoadNotifier
    {
        /// <summary>
        /// Performs notify theme loaded for <see cref="IThemeLoadNotifier"/>, keeping the operation consistent with the state and invariants of the surrounding theme load notifier workflow.
        /// </summary>
        /// <param name="theme">Theme value supplied to the theme load notifier operation and used when producing its result.</param>
        /// <param name="target">Target value supplied to the theme load notifier operation and used when producing its result.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        Task NotifyThemeLoadedAsync(Theme theme, ThemeApplicationTarget target);
    }
}
