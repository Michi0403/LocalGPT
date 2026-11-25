namespace LocalGPT.BusinessObjects;

/// <summary>A durable review of a provider-published LoRA model for an exact Council role.</summary>
public sealed class ModelAdapterRevision
{
    public Guid Id { get; set; }
    public Guid BenchmarkRunId { get; set; }
    public string BaseModelKey { get; set; } = string.Empty;
    public string AdapterModelKey { get; set; } = string.Empty;
    public string BaseDigest { get; set; } = string.Empty;
    public string AdapterDigest { get; set; } = string.Empty;
    public string ScenarioSha256 { get; set; } = string.Empty;
    public string TeamKey { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public int BasePassed { get; set; }
    public int AdapterPassed { get; set; }
    public int CaseCount { get; set; }
    public bool Qualified { get; set; }
    public DateTime ReviewedAtUtc { get; set; }
}
