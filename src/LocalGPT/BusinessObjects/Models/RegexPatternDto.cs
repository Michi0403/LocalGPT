using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects.Models
{
    /// <summary>
    /// Represents a regex pattern DTO application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="Name">Name value supplied to the regex pattern DTO operation and used when producing its result.</param>
    /// <param name="Pattern">Pattern value supplied to the regex pattern DTO operation and used when producing its result.</param>
    /// <param name="Flags">Flags value supplied to the regex pattern DTO operation and used when producing its result.</param>
    public record RegexPatternDto(string Name, string Pattern, string? Flags);
}
