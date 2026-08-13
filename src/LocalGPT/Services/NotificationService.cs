using DevExpress.Blazor;
using LocalGPT.Interfaces;
namespace TacosPortal.Services
{
    /// <summary>
    /// Coordinates notification behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    /// <param name="toastService">Toast notification service dependency used by the notification workflow to provide the corresponding application capability.</param>
    /// <param name="componentActivity">Component activity service dependency used by the notification workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public class NotificationService(
        IToastNotificationService toastService,
        IComponentActivityService componentActivity,
        ILogger<NotificationService> logger) : INotificationService
    {

        /// <summary>
        /// Performs show as part of the notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="providerName">Provider name value supplied to the notification operation and used when producing its result.</param>
        /// <param name="title">Title value supplied to the notification operation and used when producing its result.</param>
        /// <param name="message">Message value supplied to the notification operation and used when producing its result.</param>
        /// <param name="renderStyle">Render style value supplied to the notification operation and used when producing its result.</param>
        public void Show(string providerName, string title, string message, ToastRenderStyle renderStyle)
        {
            var safeProvider = NormalizeText(providerName, "ComponentSafetyToasts", 120);
            var safeTitle = NormalizeText(title, "LocalGPT", 120);
            var safeMessage = NormalizeText(message, "The operation completed without additional details.", 800);

            try
            {
                toastService.ShowToast(
               new ToastOptions
               {
                   ProviderName = safeProvider,
                   Title = safeTitle,
                   Text = safeMessage,
                   RenderStyle = renderStyle,
                   ThemeMode = ToastThemeMode.Auto,
                   ShowCloseButton = true,
                   ShowIcon = true,
                   FreezeOnClick = true,
                   SizeMode = SizeMode.Large,
               });
                componentActivity.RecordInformation(
                    "Notification",
                    renderStyle.ToString(),
                    "The UI presented a sanitized notification to the human user.");
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Could not show toast notification through provider {ProviderName} with style {RenderStyle}; notification content was omitted from logs.",
                    safeProvider,
                    renderStyle);
                componentActivity.RecordFailure("Notification", renderStyle.ToString(), ex);
            }

        }

        /// <summary>
        /// Normalizes text as part of the notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="value">Value value supplied to the notification operation and used when producing its result.</param>
        /// <param name="fallback">Fallback value supplied to the notification operation and used when producing its result.</param>
        /// <param name="maxLength">Max length value supplied to the notification operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        private string NormalizeText(string? value, string fallback, int maxLength)
        {
    try
    {
                var normalized = string.IsNullOrWhiteSpace(value)
                    ? fallback
                    : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
                return normalized[..Math.Min(normalized.Length, maxLength)];
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(NotificationService)}.{nameof(NormalizeText)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(NotificationService)}.{nameof(NormalizeText)} failed.");
        throw;
    }
}

        /// <summary>
        /// Performs show info as part of the notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="providerName">Provider name value supplied to the notification operation and used when producing its result.</param>
        /// <param name="message">Message value supplied to the notification operation and used when producing its result.</param>
        /// <param name="title">Title value supplied to the notification operation and used when producing its result.</param>
        public void ShowInfo(string providerName, string message, string title = "Info") {
    try
    {
        Show(providerName, title, message, ToastRenderStyle.Info);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(NotificationService)}.{nameof(ShowInfo)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(NotificationService)}.{nameof(ShowInfo)} failed.");
        throw;
    }
}
        /// <summary>
        /// Performs show success as part of the notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="providerName">Provider name value supplied to the notification operation and used when producing its result.</param>
        /// <param name="message">Message value supplied to the notification operation and used when producing its result.</param>
        /// <param name="title">Title value supplied to the notification operation and used when producing its result.</param>
        public void ShowSuccess(string providerName, string message, string title = "Success") {
    try
    {
        Show(providerName, title, message, ToastRenderStyle.Success);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(NotificationService)}.{nameof(ShowSuccess)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(NotificationService)}.{nameof(ShowSuccess)} failed.");
        throw;
    }
}
        /// <summary>
        /// Performs show warning as part of the notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="providerName">Provider name value supplied to the notification operation and used when producing its result.</param>
        /// <param name="message">Message value supplied to the notification operation and used when producing its result.</param>
        /// <param name="title">Title value supplied to the notification operation and used when producing its result.</param>
        public void ShowWarning(string providerName, string message, string title = "Warning") {
    try
    {
        Show(providerName, title, message, ToastRenderStyle.Warning);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(NotificationService)}.{nameof(ShowWarning)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(NotificationService)}.{nameof(ShowWarning)} failed.");
        throw;
    }
}
        /// <summary>
        /// Performs show error as part of the notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="providerName">Provider name value supplied to the notification operation and used when producing its result.</param>
        /// <param name="message">Message value supplied to the notification operation and used when producing its result.</param>
        /// <param name="title">Title value supplied to the notification operation and used when producing its result.</param>
        public void ShowError(string providerName, string message, string title = "Error") {
    try
    {
        Show(providerName, title, message, ToastRenderStyle.Danger);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(NotificationService)}.{nameof(ShowError)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(NotificationService)}.{nameof(ShowError)} failed.");
        throw;
    }
}
        /// <summary>
        /// Performs show regular as part of the notification service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="providerName">Provider name value supplied to the notification operation and used when producing its result.</param>
        /// <param name="message">Message value supplied to the notification operation and used when producing its result.</param>
        /// <param name="title">Title value supplied to the notification operation and used when producing its result.</param>
        public void ShowRegular(string providerName, string message, string title = "Error") {
    try
    {
        Show(providerName, title, message, ToastRenderStyle.Primary);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(NotificationService)}.{nameof(ShowRegular)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(NotificationService)}.{nameof(ShowRegular)} failed.");
        throw;
    }
}
    }
}
