using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Code { get; set; }

        [MaxLength(50)]
        public string? Type { get; set; } // Core, Humanities, Science, Elective

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? MaxMarks { get; set; }

        // ✅ ADD THESE PROPERTIES
        public int? DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<ClassSubject>? ClassSubjects { get; set; }
        public virtual ICollection<StudentMark>? StudentMarks { get; set; }
        public virtual ICollection<Marks>? Marks { get; set; }
        public virtual ICollection<StudentSubject>? StudentSubjects { get; set; }
        public virtual ICollection<TeacherSubject>? TeacherSubjects { get; set; }
        public virtual ICollection<Exam>? Exams { get; set; }
        public virtual ICollection<ExamResult>? ExamResults { get; set; }
    }
}