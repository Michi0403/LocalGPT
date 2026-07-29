using LocalGPT.BusinessObjects;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace LocalGPT.Interfaces;

public interface ILocalGptRuntimePolicyDataService
{
    Guid LocalGptCoreProjectId { get; }
    TimeSpan RegexTimeout { get; }
    FrozenSet<string> AllowedNativeExecutables { get; }
    Regex PowerShellInlineCommandPattern { get; }
    Regex PowerShellFilePattern { get; }
    Regex SensitiveArgumentPattern { get; }
    LocalGptRuntimePolicySnapshot GetSnapshot();
}
