using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class Class
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;  // "Form 1", "Form 2", "Form 3", "Form 4"
        
        [MaxLength(20)]
        public string? Stream { get; set; }  // "East", "West", "North", "South"
        
        public int? TeacherId { get; set; }  // Class teacher ID
        public int? Capacity { get; set; }  // Maximum number of students
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("TeacherId")]
        public virtual User? Teacher { get; set; }  // The teacher who is class teacher
        
        public virtual ICollection<Marks>? Marks { get; set; }
        public virtual ICollection<ClassSubject>? ClassSubjects { get; set; }  // Subjects taught in this class
        public virtual ICollection<Student>? Students { get; set; }  // Students in this class
    }
}