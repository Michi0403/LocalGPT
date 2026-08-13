using LocalGPT.BusinessObjects;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace LocalGPT.Interfaces;

/// <summary>
/// Defines the local gpt runtime policy seed data service contract.
/// </summary>
public interface ILocalGptRuntimePolicySeedDataService
{
    /// <summary>
    /// Retrieves seed as part of the LocalGPT runtime policy seed service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The LocalGPT runtime policy seed model produced by the operation.</returns>
    LocalGptRuntimePolicySeedModel GetSeed();
}

/// <summary>
/// Defines the contract for LocalGPT runtime policy store behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ILocalGptRuntimePolicyStoreService
{
    /// <summary>
    /// Retrieves definition as part of the LocalGPT runtime policy store service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The LocalGPT runtime policy definition produced by the operation.</returns>
    LocalGptRuntimePolicyDefinition? GetDefinition();
}

/// <summary>
/// Defines the contract for LocalGPT runtime policy behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ILocalGptRuntimePolicyDataService
{
    /// <summary>
    /// Gets the stable LocalGPT core project identifier used to identify or correlate this LocalGPT runtime policy instance with related application state.
    /// </summary>
    /// <value>The LocalGPT core project identifier value exposed by <see cref="ILocalGptRuntimePolicyDataService"/>.</value>
    Guid LocalGptCoreProjectId { get; }
    /// <summary>
    /// Gets the regex timeout duration used to control timing in the LocalGPT runtime policy workflow.
    /// </summary>
    /// <value>The regex timeout value exposed by <see cref="ILocalGptRuntimePolicyDataService"/>.</value>
    TimeSpan RegexTimeout { get; }
    /// <summary>
    /// Gets the allowed native executables value that forms part of the LocalGPT runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The allowed native executables value exposed by <see cref="ILocalGptRuntimePolicyDataService"/>.</value>
    FrozenSet<string> AllowedNativeExecutables { get; }
    /// <summary>
    /// Gets the power shell inline command pattern value that forms part of the LocalGPT runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The power shell inline command pattern value exposed by <see cref="ILocalGptRuntimePolicyDataService"/>.</value>
    Regex PowerShellInlineCommandPattern { get; }
    /// <summary>
    /// Gets the power shell file pattern value that forms part of the LocalGPT runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The power shell file pattern value exposed by <see cref="ILocalGptRuntimePolicyDataService"/>.</value>
    Regex PowerShellFilePattern { get; }
    /// <summary>
    /// Gets the sensitive argument pattern value that forms part of the LocalGPT runtime policy state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sensitive argument pattern value exposed by <see cref="ILocalGptRuntimePolicyDataService"/>.</value>
    Regex SensitiveArgumentPattern { get; }
    /// <summary>
    /// Retrieves string as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string GetString(LocalGptRuntimeValue key);
    /// <summary>
    /// Retrieves int as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    int GetInt(LocalGptRuntimeValue key);
    /// <summary>
    /// Retrieves long as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The long produced by the operation.</returns>
    long GetLong(LocalGptRuntimeValue key);
    /// <summary>
    /// Retrieves GUID as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The GUID produced by the operation.</returns>
    Guid GetGuid(LocalGptRuntimeValue key);
    /// <summary>
    /// Retrieves JSON as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="T">Type used for t values handled by <see cref="ILocalGptRuntimePolicyDataService"/>.</typeparam>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The t produced by the operation.</returns>
    T GetJson<T>(LocalGptRuntimeValue key);
    /// <summary>
    /// Retrieves pattern as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    Regex GetPattern(LocalGptRuntimePattern key);
    /// <summary>
    /// Retrieves collection as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the LocalGPT runtime policy operation and used when producing its result.</param>
    /// <returns>The frozen set string produced by the operation.</returns>
    FrozenSet<string> GetCollection(LocalGptRuntimeCollection key);
    /// <summary>
    /// Retrieves snapshot as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The LocalGPT runtime policy snapshot produced by the operation.</returns>
    LocalGptRuntimePolicySnapshot GetSnapshot();
    /// <summary>
    /// Performs reload as part of the LocalGPT runtime policy service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The LocalGPT runtime policy snapshot produced by the operation.</returns>
    LocalGptRuntimePolicySnapshot Reload();
}

/// <summary>
/// Defines the contract for LocalGPT vocabulary behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface ILocalGptVocabularyService
{
    /// <summary>
    /// Performs get as part of the LocalGPT vocabulary service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The LocalGPT vocabulary snapshot produced by the operation.</returns>
    LocalGptVocabularySnapshot Get();
}
