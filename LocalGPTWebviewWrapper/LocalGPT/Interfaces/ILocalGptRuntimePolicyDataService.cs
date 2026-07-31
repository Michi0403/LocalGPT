using LocalGPT.BusinessObjects;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace LocalGPT.Interfaces;

public interface ILocalGptRuntimePolicySeedDataService
{
    LocalGptRuntimePolicySeedModel GetSeed();
}

public interface ILocalGptRuntimePolicyStoreService
{
    LocalGptRuntimePolicyDefinition? GetDefinition();
}

public interface ILocalGptRuntimePolicyDataService
{
    Guid LocalGptCoreProjectId { get; }
    TimeSpan RegexTimeout { get; }
    FrozenSet<string> AllowedNativeExecutables { get; }
    Regex PowerShellInlineCommandPattern { get; }
    Regex PowerShellFilePattern { get; }
    Regex SensitiveArgumentPattern { get; }
    string GetString(LocalGptRuntimeValue key);
    int GetInt(LocalGptRuntimeValue key);
    long GetLong(LocalGptRuntimeValue key);
    Guid GetGuid(LocalGptRuntimeValue key);
    T GetJson<T>(LocalGptRuntimeValue key);
    Regex GetPattern(LocalGptRuntimePattern key);
    FrozenSet<string> GetCollection(LocalGptRuntimeCollection key);
    LocalGptRuntimePolicySnapshot GetSnapshot();
    LocalGptRuntimePolicySnapshot Reload();
}

public interface ILocalGptVocabularyService
{
    LocalGptVocabularySnapshot Get();
}
