namespace LocalGPT.BusinessObjects;


public sealed class CouncilSpoolerSnapshot
{
    public Guid RunId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CurrentRound { get; set; }
    public string Phase { get; set; } = "Starting";
    public string Prompt { get; set; } = string.Empty;
    public string CouncilTeamKey { get; set; } = "general";
    public List<string> ModelNames { get; set; } = [];
    public List<MultiModelCouncilStep> Steps { get; set; } = [];
    public string FinalAnswer { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = [];
}
