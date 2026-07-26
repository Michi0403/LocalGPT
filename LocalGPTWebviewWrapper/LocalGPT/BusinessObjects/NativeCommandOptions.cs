namespace LocalGPT.BusinessObjects;

public sealed class NativeCommandOptions
{
    public const string SectionName = "NativeCommands";

    public bool Enabled { get; set; }
    public bool AllowPowerShellWorkspaceScripts { get; set; }
    public int MaxDurationSeconds { get; set; } = 600;
}
