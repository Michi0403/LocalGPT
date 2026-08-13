using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    /// <summary>
    /// Defines the contract for chat upload workspace behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    public interface IChatUploadWorkspaceService
    {
        /// <summary>
        /// Gets the workspace root value that forms part of the chat upload workspace state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The workspace root value exposed by <see cref="IChatUploadWorkspaceService"/>.</value>
        string WorkspaceRoot { get; }

        /// <summary>
        /// Creates workspace as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="prompt">Prompt value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="files">Chat upload workspace input file dependency used by the chat upload workspace workflow to provide the corresponding application capability.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The chat upload workspace result produced by the operation.</returns>
        Task<ChatUploadWorkspaceResult> CreateWorkspaceAsync(
            string prompt,
            IEnumerable<ChatUploadWorkspaceInputFile> files,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists workspaces as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="take">Take value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <returns>The collection produced by the operation.</returns>
        IReadOnlyList<ChatUploadWorkspaceSummary> ListWorkspaces(int take = 20);

        /// <summary>
        /// Retrieves latest workspace as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="maxAge">Max age value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <returns>The chat upload workspace summary produced by the operation.</returns>
        ChatUploadWorkspaceSummary? GetLatestWorkspace(TimeSpan? maxAge = null);

        /// <summary>
        /// Retrieves latest context markdown as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="maxCharacters">Max characters value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="maxAge">Max age value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        string GetLatestContextMarkdown(int maxCharacters, TimeSpan? maxAge = null);

        /// <summary>
        /// Reads context markdown as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="maxCharacters">Max characters value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The string produced by the operation.</returns>
        Task<string> ReadContextMarkdownAsync(
            string workspaceName,
            int maxCharacters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists files as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="take">Take value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <returns>The collection produced by the operation.</returns>
        IReadOnlyList<ChatUploadWorkspaceFileSummary> ListFiles(string workspaceName, int take = 250);

        /// <summary>
        /// Reads file as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="relativePath">Relative path value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="maxCharacters">Max characters value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The chat upload workspace file read result produced by the operation.</returns>
        Task<ChatUploadWorkspaceFileReadResult?> ReadFileAsync(
            string workspaceName,
            string relativePath,
            int maxCharacters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves workspace path as part of the chat upload workspace service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="workspaceName">Workspace name value supplied to the chat upload workspace operation and used when producing its result.</param>
        /// <returns>The string produced by the operation.</returns>
        string? ResolveWorkspacePath(string workspaceName);
    }
}
