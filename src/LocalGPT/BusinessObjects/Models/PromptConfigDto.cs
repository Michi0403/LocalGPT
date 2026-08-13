using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects.Models
{
    /// <summary>
    /// Represents a prompt config DTO application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="Key">Key value supplied to the prompt config DTO operation and used when producing its result.</param>
    /// <param name="Language">Language value supplied to the prompt config DTO operation and used when producing its result.</param>
    /// <param name="Text">Text value supplied to the prompt config DTO operation and used when producing its result.</param>
    public record PromptConfigDto(string Key, string? Language, string Text);
}
