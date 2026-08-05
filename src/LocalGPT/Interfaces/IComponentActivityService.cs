using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface IComponentActivityService
{
    void RecordNavigation(string route);
    void RecordInformation(string component, string operation, string summary, string? route = null);
    void RecordWarning(string component, string operation, string summary, string? route = null);
    void RecordFailure(string component, string operation, Exception exception, string? route = null);
    IReadOnlyList<ComponentActivitySnapshot> GetRecent(int take = 20);
    string BuildBriefing(int take = 12);
}
