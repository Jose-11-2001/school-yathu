using System.ComponentModel.DataAnnotations;

namespace School_Yathu.Models
{
    public class Subject
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Code { get; set; }
        public int? MaxMarks { get; set; } = 100;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Marks>? Marks { get; set; }
        public virtual ICollection<ClassSubject>? ClassSubjects { get; set; }
        public virtual ICollection<TeacherSubject>? TeacherSubjects { get; set; }
        public virtual ICollection<StudentSubject>? StudentSubjects { get; set; }
    }
}