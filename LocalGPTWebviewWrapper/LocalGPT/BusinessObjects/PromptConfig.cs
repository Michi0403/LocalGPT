using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects
{
    public class PromptConfig
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(128)]
        public string Key { get; set; } = null!;
        [MaxLength(10)]
        public string? Language { get; set; }
        [Required]
        public string Text { get; set; } = null!;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
