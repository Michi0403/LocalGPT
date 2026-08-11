namespace LocalGPT.BusinessObjects;


/// <summary>
/// Represents a council spooler snapshot.
/// </summary>
public sealed class CouncilSpoolerSnapshot
{
    /// <summary>
    /// Gets or sets run identifier.
    /// </summary>
    public Guid RunId { get; set; }
    /// <summary>
    /// Gets or sets started at UTC.
    /// </summary>
    public DateTime StartedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets completed at UTC.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets current round.
    /// </summary>
    public int CurrentRound { get; set; }
    /// <summary>
    /// Gets or sets phase.
    /// </summary>
    public string Phase { get; set; } = "Starting";
    /// <summary>
    /// Gets or sets prompt.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets council team key.
    /// </summary>
    public string CouncilTeamKey { get; set; } = "general";
    /// <summary>
    /// Gets or sets model names.
    /// </summary>
    public List<string> ModelNames { get; set; } = [];
    /// <summary>
    /// Gets or sets steps.
    /// </summary>
    public List<MultiModelCouncilStep> Steps { get; set; } = [];
    /// <summary>
    /// Gets or sets final answer.
    /// </summary>
    public string FinalAnswer { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}
