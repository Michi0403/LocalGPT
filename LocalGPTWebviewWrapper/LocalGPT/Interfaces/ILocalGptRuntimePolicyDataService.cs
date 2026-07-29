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
    LocalGptRuntimePolicyDefinition GetDefinition();
}

public interface ILocalGptRuntimePolicyDataService
{
    Guid LocalGptCoreProjectId { get; }
    TimeSpan RegexTimeout { get; }
    FrozenSet<string> AllowedNativeExecutables { get; }
    Regex PowerShellInlineCommandPattern { get; }
    Regex PowerShellFilePattern { get; }
    Regex SensitiveArgumentPattern { get; }
    LocalGptRuntimePolicySnapshot GetSnapshot();
    LocalGptRuntimePolicySnapshot Reload();
}
