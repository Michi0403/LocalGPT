using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects.Models
{
    /// <summary>
    /// Represents a prompt config dto.
    /// </summary>
    public record PromptConfigDto(string Key, string? Language, string Text);
}
