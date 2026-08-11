using DevExpress.Blazor;
using LocalGPT.Interfaces;
namespace TacosPortal.Services
{
    /// <summary>
    /// Provides notification service operations.
    /// </summary>
    public class NotificationService(
        IToastNotificationService toastService,
        IComponentActivityService componentActivity,
        ILogger<NotificationService> logger) : INotificationService
    {

        /// <summary>
        /// Runs the show operation.
        /// </summary>
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
        /// Normalizes text.
        /// </summary>
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
        /// Runs the show info operation.
        /// </summary>
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
        /// Runs the show success operation.
        /// </summary>
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
        /// Runs the show warning operation.
        /// </summary>
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
        /// Runs the show error operation.
        /// </summary>
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
        /// Runs the show regular operation.
        /// </summary>
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
