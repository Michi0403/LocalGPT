using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IThemeChangeRequestDispatcher
{
    Task RequestThemeChangeAsync(Theme theme);
}
