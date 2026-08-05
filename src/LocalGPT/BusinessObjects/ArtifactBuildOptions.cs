namespace LocalGPT.BusinessObjects;

public sealed class ArtifactBuildOptions
{
    public const string SectionName = "ArtifactBuilds";

    public bool Enabled { get; set; }
    public int MaxDurationSeconds { get; set; } = 180;
}

public sealed record ArtifactBuildExecutionResult(
    string Status,
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration)
{
    public bool Succeeded => string.Equals(Status, "BuildPassed", StringComparison.Ordinal);
    public bool TimedOut => string.Equals(Status, "TimedOut", StringComparison.Ordinal);
}
