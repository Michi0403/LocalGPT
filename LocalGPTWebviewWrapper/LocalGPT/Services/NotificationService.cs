using DevExpress.Blazor;
using LocalGPT.Interfaces;
namespace TacosPortal.Services
{
    public class NotificationService(IToastNotificationService toastService, ILogger<NotificationService> logger) : INotificationService
    {

        public void Show(string providerName, string title, string message, ToastRenderStyle renderStyle)
        {
            try
            {
                toastService.ShowToast(
               new ToastOptions
               {
                   ProviderName = providerName,
                   Title = title,
                   Text = message,
                   RenderStyle = renderStyle,
                   ThemeMode = ToastThemeMode.Auto,
                   ShowCloseButton = true,
                   ShowIcon = true,
                   FreezeOnClick = true,
                   SizeMode = SizeMode.Large,
               });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error showing toast notification: {MessageOriginBelongsTo}", message);
            }

        }

        public void ShowInfo(string providerName, string message, string title = "Info") => Show(providerName, title, message, ToastRenderStyle.Info);
        public void ShowSuccess(string providerName, string message, string title = "Success") => Show(providerName, title, message, ToastRenderStyle.Success);
        public void ShowWarning(string providerName, string message, string title = "Warning") => Show(providerName, title, message, ToastRenderStyle.Warning);
        public void ShowError(string providerName, string message, string title = "Error") => Show(providerName, title, message, ToastRenderStyle.Danger);
        public void ShowRegular(string providerName, string message, string title = "Error") => Show(providerName, title, message, ToastRenderStyle.Primary);
    }
}
