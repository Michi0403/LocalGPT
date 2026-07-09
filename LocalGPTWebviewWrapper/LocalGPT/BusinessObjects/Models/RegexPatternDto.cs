using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects.Models
{
    public record RegexPatternDto(string Name, string Pattern, string? Flags);
}
