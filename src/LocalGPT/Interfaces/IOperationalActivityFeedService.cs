namespace LocalGPT.Interfaces;

/// <summary>Formats the shared command-console and bounded application activity into one user-facing operational feed.</summary>
public interface IOperationalActivityFeedService
{
    /// <summary>Builds one bounded combined setup/activity display without exposing secrets or unbounded logs.</summary>
    /// <param name="localText">Optional route-local status text.</param>
    /// <param name="consoleLines">Maximum recent shared-console entries to include.</param>
    /// <param name="activityEntries">Maximum recent application-activity entries to include.</param>
    /// <returns>Combined operational text for read-only UI surfaces.</returns>
    string BuildDisplayText(string? localText, int consoleLines = 120, int activityEntries = 30);
}
