namespace LocalGPT.BusinessObjects
{
    public sealed record ChatUploadWorkspaceInputFile(
        string Name,
        string ContentType,
        long SizeBytes,
        ReadOnlyMemory<byte> Data);

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
        public int FileCount => Files.Count;
        public int CharacterCount => ContextMarkdown.Length;
    }

    public sealed record ChatUploadWorkspaceSummary(
        string WorkspaceName,
        string RootPath,
        DateTimeOffset CreatedAtUtc,
        DateTime LastWriteTimeUtc,
        int FileCount,
        long TotalBytes,
        string ContextPath);

    public sealed record ChatUploadWorkspaceFileSummary(
        string RelativePath,
        string Kind,
        long Length,
        DateTime LastWriteTimeUtc,
        bool IncludedInPrompt,
        string Note);

    public sealed record ChatUploadWorkspaceFileReadResult(
        string WorkspaceName,
        string RelativePath,
        string FullPath,
        string Kind,
        long Length,
        string Content);
}
