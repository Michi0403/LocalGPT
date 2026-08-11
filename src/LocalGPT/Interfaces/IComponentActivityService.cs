using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the component activity service contract.
/// </summary>
public interface IComponentActivityService
{
    /// <summary>
    /// Runs the record navigation operation.
    /// </summary>
    void RecordNavigation(string route);
    /// <summary>
    /// Runs the record information operation.
    /// </summary>
    void RecordInformation(string component, string operation, string summary, string? route = null);
    /// <summary>
    /// Runs the record warning operation.
    /// </summary>
    void RecordWarning(string component, string operation, string summary, string? route = null);
    /// <summary>
    /// Runs the record failure operation.
    /// </summary>
    void RecordFailure(string component, string operation, Exception exception, string? route = null);
    /// <summary>
    /// Gets recent.
    /// </summary>
    IReadOnlyList<ComponentActivitySnapshot> GetRecent(int take = 20);
    /// <summary>
    /// Builds briefing.
    /// </summary>
    string BuildBriefing(int take = 12);
}
