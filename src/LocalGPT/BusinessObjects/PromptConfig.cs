using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalGPT.BusinessObjects
{

    /// <summary>
    /// Represents a prompt config application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public class PromptConfig
    {
        /// <summary>
        /// Gets or sets the stable identifier used to identify or correlate this prompt config instance with related application state.
        /// </summary>
        /// <value>The identifier value exposed by <see cref="PromptConfig"/>.</value>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the stable key used to identify or correlate this prompt config instance with related application state.
        /// </summary>
        /// <value>The key value exposed by <see cref="PromptConfig"/>.</value>
        [Required, MaxLength(128)]
        public string Key { get; set; } = null!;
        /// <summary>
        /// Gets or sets the language value that forms part of the prompt config state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The language value exposed by <see cref="PromptConfig"/>.</value>
        [MaxLength(10)]
        public string? Language { get; set; }
        /// <summary>
        /// Gets or sets the text value that forms part of the prompt config state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The text value exposed by <see cref="PromptConfig"/>.</value>
        [Required]
        [Column(TypeName = "TEXT")]
        public string Text { get; set; } = null!;
        /// <summary>
        /// Gets or sets the last updated value that forms part of the prompt config state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The last updated value exposed by <see cref="PromptConfig"/>.</value>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
