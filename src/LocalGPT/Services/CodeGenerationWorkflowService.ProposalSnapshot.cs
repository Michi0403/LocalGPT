using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;

namespace LocalGPT.Services
{
    /// <summary>
    /// Writes the reviewable proposal of a code-generation review to the user's CouncilArtifacts folder
    /// as soon as the review is created, so the owner can inspect the proposed files on disk before approving.
    /// </summary>
    public sealed partial class CodeGenerationWorkflowService
    {
        /// <summary>
        /// Writes CHANGE_REVIEW.md and every proposed source file to
        /// CouncilArtifacts/&lt;output name&gt;/&lt;UTC time&gt;-review-&lt;id&gt;-proposed. Nothing is built or executed.
        /// A failure is logged and never blocks the review itself.
        /// </summary>
        /// <param name="review">The persisted review awaiting the owner's decision.</param>
        /// <param name="payload">Exact proposed files, CodeDOM types and outputs.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The proposal folder, or an empty string when it could not be written.</returns>
        private async Task<string> WriteProposalSnapshotAsync(
            CodeGenerationChangeReview review,
            CodeGenerationReviewPayload payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var outputName = payload.Outputs.Select(output => output.Name).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? review.Title;
                var invalid = Path.GetInvalidFileNameChars();
                var safeName = new string(outputName.Select(character => invalid.Contains(character) || char.IsWhiteSpace(character) ? '-' : character).ToArray()).Trim('-', '.');
                if (string.IsNullOrWhiteSpace(safeName))
                    safeName = "CodeGeneration";
                var folder = Path.Combine(councilArtifacts.ArtifactRoot, safeName,
                    $"{review.CreatedAtUtc:yyyyMMdd-HHmmss}-review-{review.Id:N}-proposed");
                Directory.CreateDirectory(folder);
                await WriteReviewDocumentAsync(folder, review, payload, cancellationToken).ConfigureAwait(false);
                foreach (var file in payload.Files)
                {
                    var fullPath = ResolveInsideRoot(folder, NormalizeRelativePath(file.RelativePath));
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? folder);
                    await File.WriteAllTextAsync(fullPath, file.Content, cancellationToken).ConfigureAwait(false);
                }
                foreach (var typeSpec in payload.CodeDomTypes)
                {
                    var fullPath = ResolveInsideRoot(folder, NormalizeRelativePath(typeSpec.RelativePath));
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? folder);
                    await File.WriteAllTextAsync(fullPath, GenerateCodeDomSource(typeSpec), cancellationToken).ConfigureAwait(false);
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "REVIEW_STATUS.md"),
                    $"# Awaiting your decision{Environment.NewLine}{Environment.NewLine}This folder is the proposal of review `{review.Id}`. Nothing here was built or executed. " +
                    $"Approve the codegen.review.execute request in LocalGPT (Approvals & team) to build it; the built workspace then appears in LocalGPT-CodeGeneration/review-{review.Id:N} and the ZIP as localgpt-change-review-{review.Id:N}.zip.{Environment.NewLine}",
                    cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Wrote proposal snapshot of code-generation review {ReviewId} with {FileCount} file(s) to {Folder}.", review.Id, payload.Files.Count, folder);
                return folder;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Could not write the proposal snapshot of code-generation review {ReviewId}; the review itself is unaffected.", review.Id);
                return string.Empty;
            }
        }
    }
}
