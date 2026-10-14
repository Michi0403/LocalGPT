namespace LocalGPT.BusinessObjects;

/// <summary>Compact two-stage public service discovery parameters shared by large and small local models.</summary>
public sealed class DiscoverPublicServiceMethodsRequest
{
    /// <summary>Optional text used to filter service method names and purpose metadata.</summary>
    /// <value>Optional text used to filter service method names and purpose metadata.</value>
    public string? Query { get; set; }
    /// <summary>Optional service identity used to limit matches to a business domain.</summary>
    /// <value>Optional service identity used to limit matches to a business domain.</value>
    public string? Service { get; set; }
    /// <summary>Zero-based paging offset.</summary>
    /// <value>Zero-based paging offset.</value>
    public int Skip { get; set; }
    /// <summary>Maximum number of method descriptors to return.</summary>
    /// <value>Maximum number of method descriptors to return.</value>
    public int Take { get; set; } = 50;
}
