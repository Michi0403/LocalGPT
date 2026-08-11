using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the chat upload workspace service contract.
    /// </summary>
    public interface IChatUploadWorkspaceService
    {
        string WorkspaceRoot { get; }

        /// <summary>
        /// Creates workspace async.
        /// </summary>
        Task<ChatUploadWorkspaceResult> CreateWorkspaceAsync(
            string prompt,
            IEnumerable<ChatUploadWorkspaceInputFile> files,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs the list workspaces operation.
        /// </summary>
        IReadOnlyList<ChatUploadWorkspaceSummary> ListWorkspaces(int take = 20);

        /// <summary>
        /// Gets latest workspace.
        /// </summary>
        ChatUploadWorkspaceSummary? GetLatestWorkspace(TimeSpan? maxAge = null);

        /// <summary>
        /// Gets latest context markdown.
        /// </summary>
        string GetLatestContextMarkdown(int maxCharacters, TimeSpan? maxAge = null);

        /// <summary>
        /// Reads context markdown async.
        /// </summary>
        Task<string> ReadContextMarkdownAsync(
            string workspaceName,
            int maxCharacters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs the list files operation.
        /// </summary>
        IReadOnlyList<ChatUploadWorkspaceFileSummary> ListFiles(string workspaceName, int take = 250);

        /// <summary>
        /// Reads file async.
        /// </summary>
        Task<ChatUploadWorkspaceFileReadResult?> ReadFileAsync(
            string workspaceName,
            string relativePath,
            int maxCharacters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves workspace path.
        /// </summary>
        string? ResolveWorkspacePath(string workspaceName);
    }
}
