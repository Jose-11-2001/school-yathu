using System.ComponentModel.DataAnnotations;

namespace School_Yathu.Models
{
    public class Class
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Stream { get; set; }
        public int? TeacherId { get; set; }
        public int? Capacity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual ICollection<Marks>? Marks { get; set; }
    }
}
