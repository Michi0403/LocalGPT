using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a system variable.
    /// </summary>
    public class SystemVariable
    {
        /// <summary>
        /// Gets or sets identifier.
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets name.
        /// </summary>
        [Required, MaxLength(128)] public string Name { get; set; } = null!;
        /// <summary>
        /// Gets or sets value string.
        /// </summary>
        [Required] public string ValueString { get; set; } = null!;
        /// <summary>
        /// Gets or sets data type.
        /// </summary>
        [MaxLength(32)] public string? DataType { get; set; }
        /// <summary>
        /// Gets or sets last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
