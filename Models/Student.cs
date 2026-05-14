using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string AdmissionNumber { get; set; } = string.Empty;
        
        [Required]
        public string FullName { get; set; } = string.Empty;
        
        public int? ClassId { get; set; }
        
        [ForeignKey("ClassId")]
        public Class? Class { get; set; }
        
        public int? TeacherId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<Marks>? Marks { get; set; }
        public ICollection<ExamResult>? ExamResults { get; set; }
        public ICollection<StudentSubject>? StudentSubjects { get; set; }
    }
}