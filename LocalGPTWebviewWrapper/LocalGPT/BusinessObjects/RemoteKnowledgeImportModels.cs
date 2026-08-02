namespace LocalGPT.BusinessObjects;

public sealed class RemoteKnowledgeImportRequest
{
    public string SourceUrl { get; set; } = string.Empty;
    public string SourceKind { get; set; } = "Auto";
    public string Branch { get; set; } = "main";
    public string FileIncludeRegex { get; set; } = @"(?i)\.(cs|razor|csproj|sln|json|xml|md|txt|ps1|cmd|sh|py|js|ts|tsx|css|scss|html|htm|php|c|h|cpp|hpp|java|kt|go|rs|sql|yml|yaml)$";
    public int MaxFiles { get; set; } = 5000;
    public int MaxLinkedPages { get; set; } = 20;
    public bool SaveToKnowledge { get; set; } = true;
    public bool PreviewOnly { get; set; }
    public List<string> RoleKeys { get; set; } = [];
    public List<string> Topics { get; set; } = [];
    public bool UserConfirmed { get; set; }
}

public sealed class RemoteKnowledgeImportFile
{
    public string RelativePath { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public long Length { get; set; }
    public bool MatchesFilePolicy { get; set; }
    public bool Imported { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class RemoteKnowledgeImportResult
{
    public string SourceUrl { get; set; } = string.Empty;
    public string SourceKind { get; set; } = string.Empty;
    public string CacheRoot { get; set; } = string.Empty;
    public string ResolvedRevision { get; set; } = string.Empty;
    public int DownloadedFileCount { get; set; }
    public int MatchedFileCount { get; set; }
    public int ImportedKnowledgeCount { get; set; }
    public List<RemoteKnowledgeImportFile> Files { get; set; } = [];
    public LearnBaseImportResult? LearnBaseResult { get; set; }
    public List<string> AppliedTags { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
