using DevExpress.Blazor;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the notification service contract.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Runs the show operation.
        /// </summary>
        void Show(string providerName, string title, string message, ToastRenderStyle renderStyle);
        /// <summary>
        /// Runs the show info operation.
        /// </summary>
        void ShowInfo(string providerName, string message, string title = "Info");
        /// <summary>
        /// Runs the show success operation.
        /// </summary>
        void ShowSuccess(string providerName, string message, string title = "Success");
        /// <summary>
        /// Runs the show warning operation.
        /// </summary>
        void ShowWarning(string providerName, string message, string title = "Warning");
        /// <summary>
        /// Runs the show error operation.
        /// </summary>
        void ShowError(string providerName, string message, string title = "Error");
        /// <summary>
        /// Runs the show regular operation.
        /// </summary>
        void ShowRegular(string providerName, string message, string title = "Error");
    }
}