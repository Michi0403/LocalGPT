using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the theme load notifier contract.
    /// </summary>
    public interface IThemeLoadNotifier
    {
        /// <summary>
        /// Runs the notify theme loaded async operation.
        /// </summary>
        Task NotifyThemeLoadedAsync(Theme theme, ThemeApplicationTarget target);
    }
}
