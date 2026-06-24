using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class Subject
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Code { get; set; }
        
        public int? MaxMarks { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
        
        public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
        
        public ICollection<StudentSubject> StudentSubjects { get; set; } = new List<StudentSubject>();
        
        public ICollection<Marks> Marks { get; set; } = new List<Marks>();
        
        // Add this - Links to ExamResults
        public ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();
    }
}