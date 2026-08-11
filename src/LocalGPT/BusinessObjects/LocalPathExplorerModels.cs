namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a local path browse request.
/// </summary>
public sealed class LocalPathBrowseRequest
{
    /// <summary>
    /// Gets or sets path.
    /// </summary>
    public string Path { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets include files.
    /// </summary>
    public bool IncludeFiles { get; set; } = true;
    /// <summary>
    /// Gets or sets max entries.
    /// </summary>
    public int MaxEntries { get; set; } = 250;
}

/// <summary>
/// Represents a local path entry.
/// </summary>
public sealed class LocalPathEntry
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets full path.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets entry kind.
    /// </summary>
    public string EntryKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets size bytes.
    /// </summary>
    public long? SizeBytes { get; set; }
    /// <summary>
    /// Gets or sets modified at UTC.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets can enter.
    /// </summary>
    public bool CanEnter { get; set; }
}

/// <summary>
/// Represents a local path browse result.
/// </summary>
public sealed class LocalPathBrowseResult
{
    /// <summary>
    /// Gets or sets requested path.
    /// </summary>
    public string RequestedPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets current path.
    /// </summary>
    public string CurrentPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets parent path.
    /// </summary>
    public string ParentPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets exists.
    /// </summary>
    public bool Exists { get; set; }
    /// <summary>
    /// Gets or sets is directory.
    /// </summary>
    public bool IsDirectory { get; set; }
    /// <summary>
    /// Gets or sets is file.
    /// </summary>
    public bool IsFile { get; set; }
    /// <summary>
    /// Gets or sets is readable.
    /// </summary>
    public bool IsReadable { get; set; }
    /// <summary>
    /// Gets or sets is writable.
    /// </summary>
    public bool IsWritable { get; set; }
    /// <summary>
    /// Gets or sets entries.
    /// </summary>
    public List<LocalPathEntry> Entries { get; set; } = [];
    /// <summary>
    /// Gets or sets suggested roots.
    /// </summary>
    public List<string> SuggestedRoots { get; set; } = [];
    /// <summary>
    /// Gets or sets warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}
