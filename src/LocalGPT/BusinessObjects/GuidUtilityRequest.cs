using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects;

/// <summary>Request for generating an unbound identifier or checking a supplied GUID string.</summary>
public sealed class GuidUtilityRequest
{
    /// <summary>Operation key: new or validate.</summary>
    [Required, MaxLength(16)]
    public string Operation { get; set; } = "new";
    /// <summary>GUID supplied for validation; not used to create or identify a database project.</summary>
    [MaxLength(100)]
    public string? Value { get; set; }
}
