using DevExpress.Blazor;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for notification behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Performs show as part of the notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="providerName">Provider name value supplied to the notification operation and used when producing its result.</param>
        /// <param name="title">Title value supplied to the notification operation and used when producing its result.</param>
        /// <param name="message">Message value supplied to the notification operation and used when producing its result.</param>
        /// <param name="renderStyle">Render style value supplied to the notification operation and used when producing its result.</param>
        void Show(string providerName, string title, string message, ToastRenderStyle renderStyle);
        /// <summary>
        /// Performs show info as part of the notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="providerName">Provider name value supplied to the notification operation and used when producing its result.</param>
        /// <param name="message">Message value supplied to the notification operation and used when producing its result.</param>
        /// <param name="title">Title value supplied to the notification operation and used when producing its result.</param>
        void ShowInfo(string providerName, string message, string title = "Info");
        /// <summary>
        /// Performs show success as part of the notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="providerName">Provider name value supplied to the notification operation and used when producing its result.</param>
        /// <param name="message">Message value supplied to the notification operation and used when producing its result.</param>
        /// <param name="title">Title value supplied to the notification operation and used when producing its result.</param>
        void ShowSuccess(string providerName, string message, string title = "Success");
        /// <summary>
        /// Performs show warning as part of the notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="providerName">Provider name value supplied to the notification operation and used when producing its result.</param>
        /// <param name="message">Message value supplied to the notification operation and used when producing its result.</param>
        /// <param name="title">Title value supplied to the notification operation and used when producing its result.</param>
        void ShowWarning(string providerName, string message, string title = "Warning");
        /// <summary>
        /// Performs show error as part of the notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="providerName">Provider name value supplied to the notification operation and used when producing its result.</param>
        /// <param name="message">Message value supplied to the notification operation and used when producing its result.</param>
        /// <param name="title">Title value supplied to the notification operation and used when producing its result.</param>
        void ShowError(string providerName, string message, string title = "Error");
        /// <summary>
        /// Performs show regular as part of the notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="providerName">Provider name value supplied to the notification operation and used when producing its result.</param>
        /// <param name="message">Message value supplied to the notification operation and used when producing its result.</param>
        /// <param name="title">Title value supplied to the notification operation and used when producing its result.</param>
        void ShowRegular(string providerName, string message, string title = "Error");
    }
}
