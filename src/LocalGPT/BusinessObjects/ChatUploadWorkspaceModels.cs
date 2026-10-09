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

    /// <summary>One complete, searchable inventory view with explicit paging and exact project-file paths.</summary>
    /// <param name="WorkspaceName">Workspace whose paths were enumerated.</param>
    /// <param name="TotalFiles">Total files physically present across all source roots.</param>
    /// <param name="MatchingFiles">Number of files matching the caller's path and extension filters.</param>
    /// <param name="Offset">Zero-based offset into filtered, sorted results.</param>
    /// <param name="HasMore">Whether more matching paths remain after this page.</param>
    /// <param name="NextOffset">Offset for the next page or null when this page is the last.</param>
    /// <param name="Files">Exact workspace-relative paths and metadata in this page.</param>
    /// <param name="OriginalUploads">All original uploaded archive/file summaries, independent of paging.</param>
    /// <param name="GeneratedWorkspaceArtifacts">LocalGPT-generated curation/manifest/context metadata summaries.</param>
    /// <param name="ArchiveRoots">Separate extraction roots for the original ZIPs.</param>
    /// <param name="ProjectFiles">Exact paths of discovered solution and project descriptors across all archive roots.</param>
    public sealed record ChatUploadWorkspaceFilePage(
        string WorkspaceName,
        int TotalFiles,
        int MatchingFiles,
        int Offset,
        bool HasMore,
        int? NextOffset,
        IReadOnlyList<ChatUploadWorkspaceFileSummary> Files,
        IReadOnlyList<ChatUploadWorkspaceFileSummary> OriginalUploads,
        IReadOnlyList<ChatUploadWorkspaceFileSummary> GeneratedWorkspaceArtifacts,
        IReadOnlyList<string> ArchiveRoots,
        IReadOnlyList<string> ProjectFiles);

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
    /// <summary>Represents one file whose complete byte stream was read by the upload-workspace curation gate.</summary>
    /// <param name="RelativePath">Workspace-relative source path.</param>
    /// <param name="Length">Observed file length in bytes.</param>
    /// <param name="Sha256">SHA-256 digest calculated while reading the complete file.</param>
    public sealed record ChatUploadWorkspaceCurationFileEvidence(
        string RelativePath,
        long Length,
        string Sha256);

    /// <summary>Represents deterministic extraction and full-read coverage for one uploaded ZIP archive.</summary>
    /// <param name="ArchiveRelativePath">Workspace-relative path of the original archive.</param>
    /// <param name="ExtractionRootRelativePath">Workspace-relative extraction root assigned to the archive.</param>
    /// <param name="ArchiveEntryCount">Number of file entries observed in the archive.</param>
    /// <param name="ExpectedExtractedFileCount">Number of archive file entries that must exist after the configured safe extraction policy is applied.</param>
    /// <param name="ExtractedFileCount">Number of files currently present below this archive's extraction root.</param>
    /// <param name="FullyReadFileCount">Number of extracted files whose complete byte stream was read by the curator.</param>
    /// <param name="SkippedOrInvalidEntryCount">Number of archive file entries that could not be represented by the safe extraction policy.</param>
    /// <param name="IsComplete">Whether every archive file entry is safely extracted and fully readable.</param>
    /// <param name="Warnings">Archive-specific curation warnings.</param>
    public sealed record ChatUploadWorkspaceArchiveCuration(
        string ArchiveRelativePath,
        string ExtractionRootRelativePath,
        int ArchiveEntryCount,
        int ExpectedExtractedFileCount,
        int ExtractedFileCount,
        int FullyReadFileCount,
        int SkippedOrInvalidEntryCount,
        bool IsComplete,
        IReadOnlyList<string> Warnings);

    /// <summary>Represents the deterministic control-gateway result that proves upload and extracted-source coverage before a gated Council step may continue.</summary>
    /// <param name="WorkspaceName">Workspace inspected by the curator.</param>
    /// <param name="CompletedAtUtc">UTC completion time of the curation pass.</param>
    /// <param name="OriginalUploadCount">Number of original user uploads inspected.</param>
    /// <param name="ArchiveUploadCount">Number of original ZIP uploads inspected.</param>
    /// <param name="ExtractedFileCount">Number of safely extracted files discovered across all archive roots.</param>
    /// <param name="FullyReadFileCount">Number of original and extracted files whose complete byte streams were read.</param>
    /// <param name="FullyReadBytes">Total number of bytes read by the curator.</param>
    /// <param name="IsComplete">Whether every original upload and every safely expected archive file passed the full-read coverage gate.</param>
    /// <param name="Archives">Per-archive extraction coverage.</param>
    /// <param name="Files">Complete file-digest evidence retained in the local curation report.</param>
    /// <param name="Warnings">Workspace-level curation warnings.</param>
    /// <param name="SummaryMarkdown">Bounded human/model-readable curation summary.</param>
    public sealed record ChatUploadWorkspaceCurationReport(
        string WorkspaceName,
        DateTimeOffset CompletedAtUtc,
        int OriginalUploadCount,
        int ArchiveUploadCount,
        int ExtractedFileCount,
        int FullyReadFileCount,
        long FullyReadBytes,
        bool IsComplete,
        IReadOnlyList<ChatUploadWorkspaceArchiveCuration> Archives,
        IReadOnlyList<ChatUploadWorkspaceCurationFileEvidence> Files,
        IReadOnlyList<string> Warnings,
        string SummaryMarkdown);

}
