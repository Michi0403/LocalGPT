namespace LocalGPT.BusinessObjects;

public sealed record LocalGptRuntimePolicySnapshot
{
    public Guid LocalGptCoreProjectId { get; init; }
    public TimeSpan RegexTimeout { get; init; }
    public IReadOnlyList<string> AllowedNativeExecutables { get; init; } = [];
    public string PowerShellInlineCommandPattern { get; init; } = string.Empty;
    public string PowerShellFilePattern { get; init; } = string.Empty;
    public string SensitiveArgumentPattern { get; init; } = string.Empty;
}
