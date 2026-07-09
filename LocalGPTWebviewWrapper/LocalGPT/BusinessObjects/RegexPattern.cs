using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects
{
    public class RegexPattern
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(128)]
        public string Name { get; set; } = null!;
        [Required]
        public string Pattern { get; set; } = null!;
        [MaxLength(32)]
        public string? Flags { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
    }
}