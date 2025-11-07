using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    public interface IThemeLoadNotifier
    {
        Task NotifyThemeLoadedAsync(Theme theme);
    }

}
