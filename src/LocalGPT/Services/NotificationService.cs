using DevExpress.Blazor;
using LocalGPT.Interfaces;
namespace TacosPortal.Services
{
    public class NotificationService(
        IToastNotificationService toastService,
        IComponentActivityService componentActivity,
        ILogger<NotificationService> logger) : INotificationService
    {

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

        private string NormalizeText(string? value, string fallback, int maxLength)
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return normalized[..Math.Min(normalized.Length, maxLength)];
        }

        public void ShowInfo(string providerName, string message, string title = "Info") => Show(providerName, title, message, ToastRenderStyle.Info);
        public void ShowSuccess(string providerName, string message, string title = "Success") => Show(providerName, title, message, ToastRenderStyle.Success);
        public void ShowWarning(string providerName, string message, string title = "Warning") => Show(providerName, title, message, ToastRenderStyle.Warning);
        public void ShowError(string providerName, string message, string title = "Error") => Show(providerName, title, message, ToastRenderStyle.Danger);
        public void ShowRegular(string providerName, string message, string title = "Error") => Show(providerName, title, message, ToastRenderStyle.Primary);
    }
}
