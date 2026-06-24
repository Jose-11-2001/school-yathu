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
        
        public string? Class { get; set; }
        
        public string? Stream { get; set; }
        
        public int? TeacherId { get; set; }
        
        public int? ClassId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        [ForeignKey("TeacherId")]
        public User? Teacher { get; set; }
        
        [ForeignKey("ClassId")]
        public Class? ClassEntity { get; set; }
        
        // Navigation properties
        public ICollection<StudentSubject> StudentSubjects { get; set; } = new List<StudentSubject>();
        
        public ICollection<Marks> Marks { get; set; } = new List<Marks>();
        
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        
        // Add this - Links to ExamResults
        public ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();
    }
}