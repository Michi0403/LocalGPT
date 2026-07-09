using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects
{
    public class SystemVariable
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(128)] public string Name { get; set; } = null!;
        [Required] public string ValueString { get; set; } = null!;
        [MaxLength(32)] public string? DataType { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
