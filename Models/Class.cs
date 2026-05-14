using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class Class
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty; // Form 1, Form 2, Form 3, Form 4
        
        public string? Stream { get; set; } // East, West, North, South
        
        public int? TeacherId { get; set; } // Class Teacher
        
        [ForeignKey("TeacherId")]
        public User? Teacher { get; set; }
        
        public int? Capacity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<Student>? Students { get; set; }
        public ICollection<ClassSubject>? ClassSubjects { get; set; }
    }
}