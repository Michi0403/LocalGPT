using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Owns LocalGPT per-user application storage and first-boot path documentation.</summary>
public interface ILocalGptApplicationPathService
{
    /// <summary>
    /// Retrieves layout as part of the local GPT application path service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The local GPT application path layout produced by the operation.</returns>
    LocalGptApplicationPathLayout GetLayout();
    /// <summary>
    /// Ensures and document layout as part of the local GPT application path service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The local GPT application path layout produced by the operation.</returns>
    LocalGptApplicationPathLayout EnsureAndDocumentLayout();
    /// <summary>
    /// Builds knowledge summary as part of the local GPT application path service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The string produced by the operation.</returns>
    string BuildKnowledgeSummary();
}
