using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects
{

    /// <summary>
    /// Represents a prompt config.
    /// </summary>
    public class PromptConfig
    {
        /// <summary>
        /// Gets or sets identifier.
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets key.
        /// </summary>
        [Required, MaxLength(128)]
        public string Key { get; set; } = null!;
        /// <summary>
        /// Gets or sets language.
        /// </summary>
        [MaxLength(10)]
        public string? Language { get; set; }
        /// <summary>
        /// Gets or sets text.
        /// </summary>
        [Required]
        [Column(TypeName = "TEXT")]
        public string Text { get; set; } = null!;
        /// <summary>
        /// Gets or sets last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
