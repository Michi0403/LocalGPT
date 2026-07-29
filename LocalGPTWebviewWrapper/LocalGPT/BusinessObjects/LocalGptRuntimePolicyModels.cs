namespace LocalGPT.BusinessObjects;

public sealed record LocalGptRuntimeSystemVariableSeed(
    string Name,
    string Value,
    string DataType);

public sealed record LocalGptRuntimeRegexSeed(
    string Name,
    string Pattern,
    string Flags);

public sealed record LocalGptRuntimePolicySeedModel
{
    public string LocalGptCoreProjectIdVariableName { get; init; } = string.Empty;
    public string AllowedNativeExecutablesVariableName { get; init; } = string.Empty;
    public string RegexTimeoutVariableName { get; init; } = string.Empty;
    public string PowerShellInlineCommandRegexName { get; init; } = string.Empty;
    public string PowerShellFileRegexName { get; init; } = string.Empty;
    public string SensitiveArgumentRegexName { get; init; } = string.Empty;
    public Guid LocalGptCoreProjectId { get; init; }
    public IReadOnlyList<LocalGptRuntimeSystemVariableSeed> SystemVariables { get; init; } = [];
    public IReadOnlyList<LocalGptRuntimeRegexSeed> RegexPatterns { get; init; } = [];
}

public sealed record LocalGptRuntimeRegexDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Pattern { get; init; } = string.Empty;
    public string Flags { get; init; } = string.Empty;
    public DateTime UpdatedOn { get; init; }
}

public sealed record LocalGptRuntimePolicyDefinition
{
    public Guid LocalGptCoreProjectId { get; init; }
    public int RegexTimeoutMilliseconds { get; init; }
    public IReadOnlyList<string> AllowedNativeExecutables { get; init; } = [];
    public LocalGptRuntimeRegexDefinition PowerShellInlineCommandPattern { get; init; } = new();
    public LocalGptRuntimeRegexDefinition PowerShellFilePattern { get; init; } = new();
    public LocalGptRuntimeRegexDefinition SensitiveArgumentPattern { get; init; } = new();
}

public sealed record LocalGptRuntimePolicySnapshot
{
    public Guid LocalGptCoreProjectId { get; init; }
    public TimeSpan RegexTimeout { get; init; }
    public IReadOnlyList<string> AllowedNativeExecutables { get; init; } = [];
    public string PowerShellInlineCommandPattern { get; init; } = string.Empty;
    public string PowerShellFilePattern { get; init; } = string.Empty;
    public string SensitiveArgumentPattern { get; init; } = string.Empty;
}
