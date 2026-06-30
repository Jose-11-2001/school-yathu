using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    /// <summary>
    /// Class model
    /// </summary>
    public class Class
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty; // "Form 1", "Form 2", etc.

        [MaxLength(50)]
        public string? Stream { get; set; } // "East", "West", "North", "South"

        public int? TeacherId { get; set; }
        public int? Capacity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("TeacherId")]
        public virtual User? Teacher { get; set; }

        public virtual ICollection<ClassSubject>? ClassSubjects { get; set; }
        public virtual ICollection<Student>? Students { get; set; }
        public virtual ICollection<Marks>? Marks { get; set; }
        public virtual ICollection<Exam>? Exams { get; set; }
        public int? FormTeacherId { get; set; }

        [ForeignKey("FormTeacherId")]
        public virtual User? FormTeacher { get; set; }
    }
}