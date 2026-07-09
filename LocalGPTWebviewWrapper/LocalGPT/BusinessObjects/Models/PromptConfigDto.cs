using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects.Models
{
    public record PromptConfigDto(string Key, string? Language, string Text);
}
