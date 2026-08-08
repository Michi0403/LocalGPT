namespace LocalGPT.BusinessObjects;

public sealed class LocalPathBrowseRequest
{
    public string Path { get; set; } = string.Empty;
    public bool IncludeFiles { get; set; } = true;
    public int MaxEntries { get; set; } = 250;
}

public sealed class LocalPathEntry
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string EntryKind { get; set; } = string.Empty;
    public long? SizeBytes { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public bool CanEnter { get; set; }
}

public sealed class LocalPathBrowseResult
{
    public string RequestedPath { get; set; } = string.Empty;
    public string CurrentPath { get; set; } = string.Empty;
    public string ParentPath { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public bool IsDirectory { get; set; }
    public bool IsFile { get; set; }
    public bool IsReadable { get; set; }
    public bool IsWritable { get; set; }
    public List<LocalPathEntry> Entries { get; set; } = [];
    public List<string> SuggestedRoots { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
