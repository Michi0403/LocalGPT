using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Resolves, creates, and documents the LocalGPT per-user storage layout.</summary>
public sealed class LocalGptApplicationPathService(
    IPlatformRuntimeService platform,
    LocalGptDatabaseOptions databaseOptions,
    ILogger<LocalGptApplicationPathService> logger) : ILocalGptApplicationPathService
{
    public LocalGptApplicationPathLayout GetLayout()
    {
        try
        {
            var reportFile = LocalGptApplicationDataPaths.ResolveUserPath("runtime", "path-layout.json");
            return new LocalGptApplicationPathLayout
            {
                Platform = platform.ProviderBootstrapToken,
                UserDataRoot = LocalGptApplicationDataPaths.ResolveUserRoot(),
                ConfigurationFile = LocalGptApplicationDataPaths.ResolveUserPath("appsettings.user.json"),
                DatabaseFile = Path.GetFullPath(databaseOptions.DatabasePath),
                RuntimeDirectory = LocalGptApplicationDataPaths.ResolveUserPath("runtime"),
                LogsDirectory = LocalGptApplicationDataPaths.ResolveUserPath("logs"),
                KnowledgeDirectory = LocalGptApplicationDataPaths.ResolveUserPath("LearningBase"),
                PortableApplicationRoot = LocalGptApplicationDataPaths.ResolvePortableRoot(),
                SystemWideDiscoveryRoots = LocalGptApplicationDataPaths.EnumerateSystemWideRoots(),
                LayoutReportFile = reportFile,
                FirstBootDetected = !File.Exists(reportFile),
                GeneratedAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not resolve the LocalGPT application path layout.");
            throw;
        }
    }

    public LocalGptApplicationPathLayout EnsureAndDocumentLayout()
    {
        var layout = GetLayout();
        try
        {
            foreach (var directory in new[]
            {
                layout.UserDataRoot,
                layout.RuntimeDirectory,
                layout.LogsDirectory,
                layout.KnowledgeDirectory,
                Path.GetDirectoryName(layout.ConfigurationFile),
                Path.GetDirectoryName(layout.DatabaseFile)
            }.Where(item => !string.IsNullOrWhiteSpace(item)))
            {
                Directory.CreateDirectory(directory!);
            }

            var json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
            var temporary = layout.LayoutReportFile + ".tmp";
            File.WriteAllText(temporary, json);
            File.Move(temporary, layout.LayoutReportFile, overwrite: true);
            logger.LogInformation("LocalGPT user-data layout: {UserDataRoot}. Path report: {ReportFile}", layout.UserDataRoot, layout.LayoutReportFile);
            return layout;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create or document the LocalGPT application path layout.");
            throw;
        }
    }

    public string BuildKnowledgeSummary()
    {
        try
        {
            var layout = GetLayout();
            var systemRoots = layout.SystemWideDiscoveryRoots.Count == 0
                ? "none detected for this platform policy"
                : string.Join("; ", layout.SystemWideDiscoveryRoots);
            return $"""
LocalGPT runtime storage layout for this installation:
- Platform: {layout.Platform}
- Per-user writable root (default and authoritative): {layout.UserDataRoot}
- User configuration: {layout.ConfigurationFile}
- LocalGPT database: {layout.DatabaseFile}
- Runtime state: {layout.RuntimeDirectory}
- Logs: {layout.LogsDirectory}
- Local knowledge/work data: {layout.KnowledgeDirectory}
- Portable application root (discovery/read-only unless explicitly configured): {layout.PortableApplicationRoot}
- System-wide discovery roots (never the default mutable user state): {systemRoots}

Policy: user-scoped configuration and mutable state remain under the per-user root. Portable and system-wide locations are supported for application/tool discovery and explicit overrides, not as the normal writable default. On Windows the normal root is %LOCALAPPDATA%\LocalGPT. On macOS it resolves through the user's Application Support directory. On Linux it resolves through XDG_DATA_HOME when configured, then the host's LocalApplicationData location, with ~/.local/share as the durable fallback.
""";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not build the LocalGPT runtime path-layout knowledge summary.");
            throw;
        }
    }
}
