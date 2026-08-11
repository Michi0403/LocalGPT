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
    /// Gets seed.
    /// </summary>
    LocalGptRuntimePolicySeedModel GetSeed();
}

/// <summary>
/// Defines the local gpt runtime policy store service contract.
/// </summary>
public interface ILocalGptRuntimePolicyStoreService
{
    /// <summary>
    /// Gets definition.
    /// </summary>
    LocalGptRuntimePolicyDefinition? GetDefinition();
}

/// <summary>
/// Defines the local gpt runtime policy data service contract.
/// </summary>
public interface ILocalGptRuntimePolicyDataService
{
    Guid LocalGptCoreProjectId { get; }
    TimeSpan RegexTimeout { get; }
    FrozenSet<string> AllowedNativeExecutables { get; }
    Regex PowerShellInlineCommandPattern { get; }
    Regex PowerShellFilePattern { get; }
    Regex SensitiveArgumentPattern { get; }
    /// <summary>
    /// Gets string.
    /// </summary>
    string GetString(LocalGptRuntimeValue key);
    /// <summary>
    /// Gets int.
    /// </summary>
    int GetInt(LocalGptRuntimeValue key);
    /// <summary>
    /// Gets long.
    /// </summary>
    long GetLong(LocalGptRuntimeValue key);
    /// <summary>
    /// Gets guid.
    /// </summary>
    Guid GetGuid(LocalGptRuntimeValue key);
    /// <summary>
    /// Gets JSON.
    /// </summary>
    T GetJson<T>(LocalGptRuntimeValue key);
    /// <summary>
    /// Gets pattern.
    /// </summary>
    Regex GetPattern(LocalGptRuntimePattern key);
    /// <summary>
    /// Gets collection.
    /// </summary>
    FrozenSet<string> GetCollection(LocalGptRuntimeCollection key);
    /// <summary>
    /// Gets snapshot.
    /// </summary>
    LocalGptRuntimePolicySnapshot GetSnapshot();
    /// <summary>
    /// Runs the reload operation.
    /// </summary>
    LocalGptRuntimePolicySnapshot Reload();
}

/// <summary>
/// Defines the local gpt vocabulary service contract.
/// </summary>
public interface ILocalGptVocabularyService
{
    /// <summary>
    /// Runs the get operation.
    /// </summary>
    LocalGptVocabularySnapshot Get();
}
