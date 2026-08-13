namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a chat upload workspace input file application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="Name">Name value supplied to the chat upload workspace input file operation and used when producing its result.</param>
    /// <param name="ContentType">Content type value supplied to the chat upload workspace input file operation and used when producing its result.</param>
    /// <param name="SizeBytes">Size bytes value supplied to the chat upload workspace input file operation and used when producing its result.</param>
    /// <param name="Data">Data value supplied to the chat upload workspace input file operation and used when producing its result.</param>
    public sealed record ChatUploadWorkspaceInputFile(
        string Name,
        string ContentType,
        long SizeBytes,
        ReadOnlyMemory<byte> Data);

    /// <summary>
    /// Represents the outcome of chat upload workspace, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    /// <param name="WorkspaceName">Workspace name value supplied to the chat upload workspace operation and used when producing its result.</param>
    /// <param name="RootPath">Root path value supplied to the chat upload workspace operation and used when producing its result.</param>
    /// <param name="ManifestPath">Manifest path value supplied to the chat upload workspace operation and used when producing its result.</param>
    /// <param name="ContextPath">Context path value supplied to the chat upload workspace operation and used when producing its result.</param>
    /// <param name="CreatedAtUtc">Created at utc value supplied to the chat upload workspace operation and used when producing its result.</param>
    /// <param name="Files">Chat upload workspace file summary dependency used by the chat upload workspace workflow to provide the corresponding application capability.</param>
    /// <param name="Warnings">String dependency used by the chat upload workspace workflow to provide the corresponding application capability.</param>
    /// <param name="ContextMarkdown">Context markdown value supplied to the chat upload workspace operation and used when producing its result.</param>
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
        /// Gets the file count that quantifies the associated chat upload workspace data.
        /// </summary>
        /// <value>The file count value exposed by <see cref="ChatUploadWorkspaceResult"/>.</value>
        public int FileCount => Files.Count;
        /// <summary>
        /// Gets the character count that quantifies the associated chat upload workspace data.
        /// </summary>
        /// <value>The character count value exposed by <see cref="ChatUploadWorkspaceResult"/>.</value>
        public int CharacterCount => ContextMarkdown.Length;
    }

    /// <summary>
    /// Represents a chat upload workspace summary application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="WorkspaceName">Workspace name value supplied to the chat upload workspace summary operation and used when producing its result.</param>
    /// <param name="RootPath">Root path value supplied to the chat upload workspace summary operation and used when producing its result.</param>
    /// <param name="CreatedAtUtc">Created at utc value supplied to the chat upload workspace summary operation and used when producing its result.</param>
    /// <param name="LastWriteTimeUtc">Last write time utc value supplied to the chat upload workspace summary operation and used when producing its result.</param>
    /// <param name="FileCount">File count value supplied to the chat upload workspace summary operation and used when producing its result.</param>
    /// <param name="TotalBytes">Total bytes value supplied to the chat upload workspace summary operation and used when producing its result.</param>
    /// <param name="ContextPath">Context path value supplied to the chat upload workspace summary operation and used when producing its result.</param>
    public sealed record ChatUploadWorkspaceSummary(
        string WorkspaceName,
        string RootPath,
        DateTimeOffset CreatedAtUtc,
        DateTime LastWriteTimeUtc,
        int FileCount,
        long TotalBytes,
        string ContextPath);

    /// <summary>
    /// Represents a chat upload workspace file summary application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="RelativePath">Relative path value supplied to the chat upload workspace file summary operation and used when producing its result.</param>
    /// <param name="Kind">Kind value supplied to the chat upload workspace file summary operation and used when producing its result.</param>
    /// <param name="Length">Length value supplied to the chat upload workspace file summary operation and used when producing its result.</param>
    /// <param name="LastWriteTimeUtc">Last write time utc value supplied to the chat upload workspace file summary operation and used when producing its result.</param>
    /// <param name="IncludedInPrompt">Value indicating whether included in prompt should apply to this operation.</param>
    /// <param name="Note">Note value supplied to the chat upload workspace file summary operation and used when producing its result.</param>
    public sealed record ChatUploadWorkspaceFileSummary(
        string RelativePath,
        string Kind,
        long Length,
        DateTime LastWriteTimeUtc,
        bool IncludedInPrompt,
        string Note);

    /// <summary>
    /// Represents the outcome of chat upload workspace file read, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    /// <param name="WorkspaceName">Workspace name value supplied to the chat upload workspace file read operation and used when producing its result.</param>
    /// <param name="RelativePath">Relative path value supplied to the chat upload workspace file read operation and used when producing its result.</param>
    /// <param name="FullPath">Full path value supplied to the chat upload workspace file read operation and used when producing its result.</param>
    /// <param name="Kind">Kind value supplied to the chat upload workspace file read operation and used when producing its result.</param>
    /// <param name="Length">Length value supplied to the chat upload workspace file read operation and used when producing its result.</param>
    /// <param name="Content">Content value supplied to the chat upload workspace file read operation and used when producing its result.</param>
    public sealed record ChatUploadWorkspaceFileReadResult(
        string WorkspaceName,
        string RelativePath,
        string FullPath,
        string Kind,
        long Length,
        string Content);
}
