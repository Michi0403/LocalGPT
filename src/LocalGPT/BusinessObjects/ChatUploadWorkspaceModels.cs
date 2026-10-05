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

    /// <summary>Represents a streamed file source used by DevExpress FileInput and other UI surfaces without buffering the complete upload in browser memory.</summary>
    /// <param name="Name">Original file name supplied by the user.</param>
    /// <param name="ContentType">Reported content type.</param>
    /// <param name="SizeBytes">Declared byte length.</param>
    /// <param name="OpenReadStream">Factory that opens the bounded input stream for immediate quarantine copy.</param>
    public sealed record ChatUploadWorkspaceStreamInput(
        string Name,
        string ContentType,
        long SizeBytes,
        Func<Stream> OpenReadStream);

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
        string Content)
    {
        /// <summary>Gets the decoded character offset at which this result segment starts.</summary>
        /// <value>The zero-based decoded character offset used for the progressive workspace read.</value>
        public long CharacterOffset { get; init; }

        /// <summary>Gets the number of decoded source characters returned before prompt sanitization.</summary>
        /// <value>The number of source characters consumed by this result segment.</value>
        public int CharactersReturned { get; init; }

        /// <summary>Gets a value indicating whether unread content remains after this result segment.</summary>
        /// <value><see langword="true"/> when the caller should continue from <see cref="NextOffsetCharacters"/> to complete the file.</value>
        public bool HasMore { get; init; }

        /// <summary>Gets the decoded character offset for the next progressive read when content remains.</summary>
        /// <value>The next zero-based decoded character offset, or <see langword="null"/> when this segment reached the end.</value>
        public long? NextOffsetCharacters { get; init; }
    }
}
