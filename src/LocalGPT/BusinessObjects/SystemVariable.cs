using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents a system variable application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public class SystemVariable
    {
        /// <summary>
        /// Gets or sets the stable identifier used to identify or correlate this system variable instance with related application state.
        /// </summary>
        /// <value>The identifier value exposed by <see cref="SystemVariable"/>.</value>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the name value that forms part of the system variable state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The name value exposed by <see cref="SystemVariable"/>.</value>
        [Required, MaxLength(128)] public string Name { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value string value that forms part of the system variable state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The value string value exposed by <see cref="SystemVariable"/>.</value>
        [Required] public string ValueString { get; set; } = null!;
        /// <summary>
        /// Gets or sets the data type value that forms part of the system variable state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The data type value exposed by <see cref="SystemVariable"/>.</value>
        [MaxLength(32)] public string? DataType { get; set; }
        /// <summary>
        /// Gets or sets the last updated value that forms part of the system variable state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The last updated value exposed by <see cref="SystemVariable"/>.</value>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
