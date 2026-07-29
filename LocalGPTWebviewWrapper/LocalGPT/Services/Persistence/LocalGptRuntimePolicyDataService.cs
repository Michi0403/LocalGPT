using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace LocalGPT.Services.Persistence;

public sealed class LocalGptRuntimePolicyDataService(
    ILogger<LocalGptRuntimePolicyDataService> logger) : ILocalGptRuntimePolicyDataService
{
    private readonly TimeSpan regexTimeout = TimeSpan.FromSeconds(2);
    private readonly FrozenSet<string> allowedNativeExecutables = new[]
    {
        "powershell.exe",
        "pwsh.exe",
        "gradle",
        "gradle.bat",
        "gradlew",
        "gradlew.bat",
        "java",
        "java.exe"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private readonly Regex powerShellInlineCommandPattern = new(
        @"(^|\s)-EncodedCommand(\s|$)|(^|\s)-Command(\s|$)|(^|\s)-c(\s|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));
    private readonly Regex powerShellFilePattern = new(
        @"(^|\s)-File\s+(?:""(?<path>[^""]+)""|'(?<path>[^']+)'|(?<path>\S+))",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));
    private readonly Regex sensitiveArgumentPattern = new(
        @"(?<name>--?(?:api[-_]?key|key|token|secret|password|passwd|pwd))(?<separator>\s+|=)(?<value>""[^""]*""|'[^']*'|\S+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));

    public Guid LocalGptCoreProjectId { get; } = Guid.Parse("7f4d7b4a-b622-4d15-8e44-9dfae2aa6101");
    public TimeSpan RegexTimeout => regexTimeout;
    public FrozenSet<string> AllowedNativeExecutables => allowedNativeExecutables;
    public Regex PowerShellInlineCommandPattern => powerShellInlineCommandPattern;
    public Regex PowerShellFilePattern => powerShellFilePattern;
    public Regex SensitiveArgumentPattern => sensitiveArgumentPattern;

    public LocalGptRuntimePolicySnapshot GetSnapshot()
    {
        try
        {
            var snapshot = new LocalGptRuntimePolicySnapshot
            {
                LocalGptCoreProjectId = LocalGptCoreProjectId,
                RegexTimeout = RegexTimeout,
                AllowedNativeExecutables = AllowedNativeExecutables.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToArray(),
                PowerShellInlineCommandPattern = PowerShellInlineCommandPattern.ToString(),
                PowerShellFilePattern = PowerShellFilePattern.ToString(),
                SensitiveArgumentPattern = SensitiveArgumentPattern.ToString()
            };
            logger.LogTrace($"Returned the LocalGPT runtime policy snapshot with {snapshot.AllowedNativeExecutables.Count} native executable entries.");
            return snapshot;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not return the LocalGPT runtime policy snapshot.");
            throw;
        }
    }
}
