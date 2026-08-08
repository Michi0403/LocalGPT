using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

public sealed class LocalPathExplorerService(ILogger<LocalPathExplorerService> logger) : ILocalPathExplorerService
{
    public IReadOnlyList<string> GetSuggestedRoots()
    {
        var roots = new List<string>();
        Add(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        Add(Environment.CurrentDirectory);
        Add(AppContext.BaseDirectory);
        try
        {
            foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.IsReady))
                Add(drive.RootDirectory.FullName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(ex, "Could not enumerate every local drive while building path suggestions.");
        }
        return roots;

        void Add(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            try
            {
                var full = Path.GetFullPath(value);
                if (!roots.Contains(full, StringComparer.OrdinalIgnoreCase)) roots.Add(full);
            }
            catch { }
        }
    }

    public string FormatWarnings(IEnumerable<string> warnings)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(warnings);
            return string.Join(" ", warnings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not format local path explorer warnings.");
            throw;
        }
    }

    public LocalPathBrowseResult Browse(LocalPathBrowseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = new LocalPathBrowseResult { RequestedPath = request.Path ?? string.Empty, SuggestedRoots = GetSuggestedRoots().ToList() };
        var requested = string.IsNullOrWhiteSpace(request.Path)
            ? result.SuggestedRoots.FirstOrDefault() ?? Environment.CurrentDirectory
            : request.Path.Trim();
        try
        {
            var full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(requested));
            if (File.Exists(full))
            {
                result.Exists = true;
                result.IsFile = true;
                result.IsReadable = true;
                result.CurrentPath = full;
                result.ParentPath = Path.GetDirectoryName(full) ?? string.Empty;
                return result;
            }
            if (!Directory.Exists(full))
            {
                result.CurrentPath = full;
                result.ParentPath = Path.GetDirectoryName(full) ?? string.Empty;
                result.Warnings.Add("The selected local path does not exist.");
                return result;
            }

            result.Exists = true;
            result.IsDirectory = true;
            result.IsReadable = true;
            result.CurrentPath = full;
            result.ParentPath = Directory.GetParent(full)?.FullName ?? string.Empty;

            var max = Math.Clamp(request.MaxEntries, 1, 1000);
            IEnumerable<FileSystemInfo> entries = new DirectoryInfo(full).EnumerateFileSystemInfos();
            if (!request.IncludeFiles)
                entries = entries.Where(item => item is DirectoryInfo);
            foreach (var item in entries.OrderBy(item => item is FileInfo).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase).Take(max))
            {
                result.Entries.Add(new LocalPathEntry
                {
                    Name = item.Name,
                    FullPath = item.FullName,
                    EntryKind = item is DirectoryInfo ? "Directory" : "File",
                    SizeBytes = item is FileInfo file ? file.Length : null,
                    ModifiedAtUtc = item.LastWriteTimeUtc,
                    CanEnter = item is DirectoryInfo
                });
            }
            result.IsWritable = !new DirectoryInfo(full).Attributes.HasFlag(FileAttributes.ReadOnly);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            result.Warnings.Add("LocalGPT could not read this path. Check that it exists and that the current user has access.");
            logger.LogDebug(ex, "Local path browse failed for a user-selected path; path text was omitted from logs.");
        }
        return result;
    }
}
