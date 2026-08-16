using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Reads, detects, imports and persists hardware owned by configured physical AI hosts.</summary>
public interface IConfiguredAiHostHardwareService
{
    /// <summary>Returns the durable hardware profile associated with a provider endpoint's physical host.</summary>
    /// <param name="endpoint">Configured provider endpoint whose owning physical host should be resolved.</param>
    /// <param name="cancellationToken">Cancellation token for the database read.</param>
    /// <returns>The stored host-hardware profile, or <see langword="null"/> when that host has not been configured yet.</returns>
    Task<ConfiguredAiHostHardwareProfile?> GetForEndpointAsync(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>Returns all stored configured-host hardware profiles.</summary>
    /// <param name="cancellationToken">Cancellation token for the database read.</param>
    /// <returns>The configured physical-host hardware profiles in display order.</returns>
    Task<IReadOnlyList<ConfiguredAiHostHardwareProfile>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Saves explicit host hardware values for the physical host associated with the endpoint.</summary>
    /// <param name="draft">User-editable configured-host hardware values.</param>
    /// <param name="cancellationToken">Cancellation token for the database write.</param>
    /// <returns>The persisted, user-confirmed host-hardware profile.</returns>
    Task<ConfiguredAiHostHardwareProfile> SaveAsync(ConfiguredAiHostHardwareDraft draft, CancellationToken cancellationToken = default);

    /// <summary>Imports deterministic hardware facts from a pasted HWiNFO text report and saves them for the endpoint's host.</summary>
    /// <param name="endpoint">Configured provider endpoint that identifies the owning physical host.</param>
    /// <param name="reportText">Local HWiNFO text export to parse deterministically; the report is not sent to an AI.</param>
    /// <param name="cancellationToken">Cancellation token for parsing and persistence.</param>
    /// <returns>The persisted user-confirmed profile populated from the report.</returns>
    Task<ConfiguredAiHostHardwareProfile> ImportHwInfoAsync(string endpoint, string reportText, CancellationToken cancellationToken = default);

    /// <summary>Uses local read-only probes to populate the loopback host without overwriting stronger confirmed values.</summary>
    /// <param name="endpoint">Loopback provider endpoint owned by the LocalGPT machine.</param>
    /// <param name="cancellationToken">Cancellation token for local probing and persistence.</param>
    /// <returns>The existing confirmed profile or the newly detected local host profile.</returns>
    Task<ConfiguredAiHostHardwareProfile> DetectLocalAsync(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>Returns the normalized physical-host key for a provider endpoint.</summary>
    /// <param name="endpoint">Provider endpoint to normalize.</param>
    /// <returns><c>local-machine</c> for loopback endpoints or the normalized remote host name otherwise.</returns>
    string GetHostKey(string endpoint);

    /// <summary>Creates an editable Install-page draft from stored values or endpoint defaults.</summary>
    /// <param name="endpoint">Configured provider endpoint represented by the form.</param>
    /// <param name="profile">Optional existing profile whose durable values should seed the form.</param>
    /// <returns>The editable configured-host hardware draft.</returns>
    ConfiguredAiHostHardwareDraft CreateDraft(string endpoint, ConfiguredAiHostHardwareProfile? profile = null);
}
