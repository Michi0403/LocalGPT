using DevExpress.Blazor;

namespace LocalGPT.Interfaces
{
    public interface INotificationService
    {
        void Show(string providerName, string title, string message, ToastRenderStyle renderStyle);
        void ShowInfo(string providerName, string message, string title = "Info");
        void ShowSuccess(string providerName, string message, string title = "Success");
        void ShowWarning(string providerName, string message, string title = "Warning");
        void ShowError(string providerName, string message, string title = "Error");
        void ShowRegular(string providerName, string message, string title = "Error");
    }
}