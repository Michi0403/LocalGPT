namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a chat upload workspace input file.
    /// </summary>
    public sealed record ChatUploadWorkspaceInputFile(
        string Name,
        string ContentType,
        long SizeBytes,
        ReadOnlyMemory<byte> Data);

    /// <summary>
    /// Represents a chat upload workspace result.
    /// </summary>
    public sealed record ChatUploadWorkspaceResult(
        string WorkspaceName,
        string RootPath,
        string ManifestPath,
        string ContextPath,
        DateTimeOffset CreatedAtUtc,
        IReadOnlyList<ChatUploadWorkspaceFileSummary> Files,
        IReadOnlyList<string> Warnings,
        string ContextMarkdown)
    {
        /// <summary>
        /// Gets or sets file count.
        /// </summary>
        public int FileCount => Files.Count;
        /// <summary>
        /// Gets or sets character count.
        /// </summary>
        public int CharacterCount => ContextMarkdown.Length;
    }

    /// <summary>
    /// Represents a chat upload workspace summary.
    /// </summary>
    public sealed record ChatUploadWorkspaceSummary(
        string WorkspaceName,
        string RootPath,
        DateTimeOffset CreatedAtUtc,
        DateTime LastWriteTimeUtc,
        int FileCount,
        long TotalBytes,
        string ContextPath);

    /// <summary>
    /// Represents a chat upload workspace file summary.
    /// </summary>
    public sealed record ChatUploadWorkspaceFileSummary(
        string RelativePath,
        string Kind,
        long Length,
        DateTime LastWriteTimeUtc,
        bool IncludedInPrompt,
        string Note);

    /// <summary>
    /// Represents a chat upload workspace file read result.
    /// </summary>
    public sealed record ChatUploadWorkspaceFileReadResult(
        string WorkspaceName,
        string RelativePath,
        string FullPath,
        string Kind,
        long Length,
        string Content);
}
