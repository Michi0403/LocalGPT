using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a regex pattern.
    /// </summary>
    public class RegexPattern
    {
        /// <summary>
        /// Gets or sets identifier.
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets name.
        /// </summary>
        [Required, MaxLength(128)]
        public string Name { get; set; } = null!;
        /// <summary>
        /// Gets or sets pattern.
        /// </summary>
        [Required]
        public string Pattern { get; set; } = null!;
        /// <summary>
        /// Gets or sets flags.
        /// </summary>
        [MaxLength(32)]
        public string? Flags { get; set; }
        /// <summary>
        /// Gets or sets created on.
        /// </summary>
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets updated on.
        /// </summary>
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
    }
}