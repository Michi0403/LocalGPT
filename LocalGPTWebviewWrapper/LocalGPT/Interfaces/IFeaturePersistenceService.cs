using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Provides approval-gated CRUD access to persistent records introduced by LocalGPT feature modules.</summary>
[DocumentationUpdated("2.1.23")]
public interface IFeaturePersistenceService
{
    /// <summary>Lists Council prompt starters.</summary>
    /// <param name="includeDisabled">Whether disabled records are returned.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns the matching starter records.</returns>
    Task<IReadOnlyList<CouncilPromptStarterConfiguration>> GetCouncilPromptStartersAsync(bool includeDisabled = false, CancellationToken cancellationToken = default);
    /// <summary>Gets one Council prompt starter.</summary>
    /// <param name="id">Database identifier.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns the record or null.</returns>
    Task<CouncilPromptStarterConfiguration?> GetCouncilPromptStarterAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Creates or updates one Council prompt starter.</summary>
    /// <param name="request">Approval-gated write request.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns the stored record.</returns>
    Task<CouncilPromptStarterConfiguration> SaveCouncilPromptStarterAsync(SaveFeatureRecordRequest<CouncilPromptStarterConfiguration> request, CancellationToken cancellationToken = default);
    /// <summary>Deletes one Council prompt starter.</summary>
    /// <param name="id">Database identifier.</param>
    /// <param name="userConfirmed">Whether the user explicitly approved deletion.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns true when a row was deleted.</returns>
    Task<bool> DeleteCouncilPromptStarterAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default);

    /// <summary>Lists localization catalog registrations.</summary>
    /// <param name="includeDisabled">Whether disabled records are returned.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns the matching catalog records.</returns>
    Task<IReadOnlyList<LocalizationCatalogRegistration>> GetLocalizationCatalogsAsync(bool includeDisabled = false, CancellationToken cancellationToken = default);
    /// <summary>Gets one localization catalog registration.</summary>
    /// <param name="id">Database identifier.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns the record or null.</returns>
    Task<LocalizationCatalogRegistration?> GetLocalizationCatalogAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Creates or updates one localization catalog registration.</summary>
    /// <param name="request">Approval-gated write request.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns the stored record.</returns>
    Task<LocalizationCatalogRegistration> SaveLocalizationCatalogAsync(SaveFeatureRecordRequest<LocalizationCatalogRegistration> request, CancellationToken cancellationToken = default);
    /// <summary>Deletes one localization catalog registration.</summary>
    /// <param name="id">Database identifier.</param>
    /// <param name="userConfirmed">Whether the user explicitly approved deletion.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns true when a row was deleted.</returns>
    Task<bool> DeleteLocalizationCatalogAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default);

    /// <summary>Lists documentation build records in newest-first order.</summary>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns documentation evidence records.</returns>
    Task<IReadOnlyList<DocumentationBuildRecord>> GetDocumentationBuildsAsync(CancellationToken cancellationToken = default);
    /// <summary>Gets one documentation build record.</summary>
    /// <param name="id">Database identifier.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns the record or null.</returns>
    Task<DocumentationBuildRecord?> GetDocumentationBuildAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Creates or updates one documentation build record.</summary>
    /// <param name="request">Approval-gated write request.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns the stored record.</returns>
    Task<DocumentationBuildRecord> SaveDocumentationBuildAsync(SaveFeatureRecordRequest<DocumentationBuildRecord> request, CancellationToken cancellationToken = default);
    /// <summary>Deletes one documentation build record.</summary>
    /// <param name="id">Database identifier.</param>
    /// <param name="userConfirmed">Whether the user explicitly approved deletion.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns true when a row was deleted.</returns>
    Task<bool> DeleteDocumentationBuildAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default);

    /// <summary>Lists embedded firmware plan records.</summary>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns firmware plan records.</returns>
    Task<IReadOnlyList<EmbeddedFirmwarePlanRecord>> GetEmbeddedFirmwarePlansAsync(CancellationToken cancellationToken = default);
    /// <summary>Gets one embedded firmware plan record.</summary>
    /// <param name="id">Database identifier.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns the record or null.</returns>
    Task<EmbeddedFirmwarePlanRecord?> GetEmbeddedFirmwarePlanAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Creates or updates one embedded firmware plan record.</summary>
    /// <param name="request">Approval-gated write request.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns the stored record.</returns>
    Task<EmbeddedFirmwarePlanRecord> SaveEmbeddedFirmwarePlanAsync(SaveFeatureRecordRequest<EmbeddedFirmwarePlanRecord> request, CancellationToken cancellationToken = default);
    /// <summary>Deletes one embedded firmware plan record.</summary>
    /// <param name="id">Database identifier.</param>
    /// <param name="userConfirmed">Whether the user explicitly approved deletion.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns true when a row was deleted.</returns>
    Task<bool> DeleteEmbeddedFirmwarePlanAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default);

    /// <summary>Lists GameDirector session records.</summary>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns GameDirector session records.</returns>
    Task<IReadOnlyList<CouncilGameSessionRecord>> GetCouncilGameSessionsAsync(CancellationToken cancellationToken = default);
    /// <summary>Gets one GameDirector session record.</summary>
    /// <param name="id">Database identifier.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns the record or null.</returns>
    Task<CouncilGameSessionRecord?> GetCouncilGameSessionAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Creates or updates one GameDirector session record.</summary>
    /// <param name="request">Approval-gated write request.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns the stored record.</returns>
    Task<CouncilGameSessionRecord> SaveCouncilGameSessionAsync(SaveFeatureRecordRequest<CouncilGameSessionRecord> request, CancellationToken cancellationToken = default);
    /// <summary>Deletes one GameDirector session record.</summary>
    /// <param name="id">Database identifier.</param>
    /// <param name="userConfirmed">Whether the user explicitly approved deletion.</param>
    /// <param name="cancellationToken">Cancels the database operation.</param>
    /// <returns>A task that returns true when a row was deleted.</returns>
    Task<bool> DeleteCouncilGameSessionAsync(Guid id, bool userConfirmed, CancellationToken cancellationToken = default);
}
