using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Combines the bounded shared command console and application activity feeds for setup and maintenance surfaces.</summary>
/// <param name="console">Shared bounded command-console service.</param>
/// <param name="activity">Bounded application-activity service.</param>
/// <param name="logger">Logger used for formatter diagnostics.</param>
public sealed class OperationalActivityFeedService(
    IConsoleCommandService console,
    IComponentActivityService activity,
    ILogger<OperationalActivityFeedService> logger) : IOperationalActivityFeedService
{
    /// <inheritdoc />
    public string BuildDisplayText(string? localText, int consoleLines = 120, int activityEntries = 30)
    {
        try
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(localText))
            {
                builder.AppendLine("Setup status");
                builder.AppendLine(localText.Trim());
                builder.AppendLine();
            }

            var consoleText = console.GetRecentDisplayText(Math.Max(1, consoleLines));
            builder.AppendLine("Shared operational / ASCII console");
            builder.AppendLine(string.IsNullOrWhiteSpace(consoleText) ? "No shared console activity yet." : consoleText.Trim());
            builder.AppendLine();

            var activityText = activity.BuildBriefing(Math.Max(1, activityEntries));
            builder.AppendLine("Application activity");
            builder.AppendLine(string.IsNullOrWhiteSpace(activityText) ? "No bounded application activity yet." : activityText.Trim());
            return builder.ToString().TrimEnd();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Building the combined operational activity feed failed.");
            return "The operational feed could not be rendered. Review the application log for diagnostics.";
        }
    }
}
