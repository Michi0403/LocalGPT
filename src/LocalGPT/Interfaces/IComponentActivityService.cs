using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the contract for component activity behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IComponentActivityService
{
    /// <summary>
    /// Performs record navigation as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="route">Route value supplied to the component activity operation and used when producing its result.</param>
    void RecordNavigation(string route);
    /// <summary>
    /// Performs record information as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="component">Component value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="summary">Summary value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="route">Route value supplied to the component activity operation and used when producing its result.</param>
    void RecordInformation(string component, string operation, string summary, string? route = null);
    /// <summary>
    /// Performs record warning as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="component">Component value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="summary">Summary value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="route">Route value supplied to the component activity operation and used when producing its result.</param>
    void RecordWarning(string component, string operation, string summary, string? route = null);
    /// <summary>
    /// Performs record failure as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="component">Component value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="operation">Operation value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="exception">Exception value supplied to the component activity operation and used when producing its result.</param>
    /// <param name="route">Route value supplied to the component activity operation and used when producing its result.</param>
    void RecordFailure(string component, string operation, Exception exception, string? route = null);
    /// <summary>
    /// Retrieves recent as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="take">Take value supplied to the component activity operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<ComponentActivitySnapshot> GetRecent(int take = 20);
    /// <summary>
    /// Builds briefing as part of the component activity service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="take">Take value supplied to the component activity operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string BuildBriefing(int take = 12);
}
