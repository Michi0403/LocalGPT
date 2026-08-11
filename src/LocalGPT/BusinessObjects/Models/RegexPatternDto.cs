using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects.Models
{
    /// <summary>
    /// Represents a regex pattern dto.
    /// </summary>
    public record RegexPatternDto(string Name, string Pattern, string? Flags);
}
