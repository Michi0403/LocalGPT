using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces
{
    public interface IChatUploadWorkspaceService
    {
        string WorkspaceRoot { get; }

        Task<ChatUploadWorkspaceResult> CreateWorkspaceAsync(
            string prompt,
            IEnumerable<ChatUploadWorkspaceInputFile> files,
            CancellationToken cancellationToken = default);

        IReadOnlyList<ChatUploadWorkspaceSummary> ListWorkspaces(int take = 20);

        ChatUploadWorkspaceSummary? GetLatestWorkspace(TimeSpan? maxAge = null);

        string GetLatestContextMarkdown(int maxCharacters, TimeSpan? maxAge = null);

        Task<string> ReadContextMarkdownAsync(
            string workspaceName,
            int maxCharacters,
            CancellationToken cancellationToken = default);

        IReadOnlyList<ChatUploadWorkspaceFileSummary> ListFiles(string workspaceName, int take = 250);

        Task<ChatUploadWorkspaceFileReadResult?> ReadFileAsync(
            string workspaceName,
            string relativePath,
            int maxCharacters,
            CancellationToken cancellationToken = default);

        string? ResolveWorkspacePath(string workspaceName);
    }
}
