using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Column(TypeName = "TEXT")]
        public string Text { get; set; } = null!;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
