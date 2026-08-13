using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a regex pattern application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public class RegexPattern
    {
        /// <summary>
        /// Gets or sets the stable identifier used to identify or correlate this regex pattern instance with related application state.
        /// </summary>
        /// <value>The identifier value exposed by <see cref="RegexPattern"/>.</value>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the name value that forms part of the regex pattern state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The name value exposed by <see cref="RegexPattern"/>.</value>
        [Required, MaxLength(128)]
        public string Name { get; set; } = null!;
        /// <summary>
        /// Gets or sets the pattern value that forms part of the regex pattern state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The pattern value exposed by <see cref="RegexPattern"/>.</value>
        [Required]
        public string Pattern { get; set; } = null!;
        /// <summary>
        /// Gets or sets the flags value that forms part of the regex pattern state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The flags value exposed by <see cref="RegexPattern"/>.</value>
        [MaxLength(32)]
        public string? Flags { get; set; }
        /// <summary>
        /// Gets or sets the created on value that forms part of the regex pattern state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The created on value exposed by <see cref="RegexPattern"/>.</value>
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets the updated on value that forms part of the regex pattern state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The updated on value exposed by <see cref="RegexPattern"/>.</value>
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
    }
}
