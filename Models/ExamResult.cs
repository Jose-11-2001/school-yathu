using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class ExamResult
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        [ForeignKey("ExamId")]
        public Exam? Exam { get; set; }
        
        [Required]
        public int StudentId { get; set; }
        
        [ForeignKey("StudentId")]
        public Student? Student { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }
        
        [Required]
        public int Score { get; set; }
        
        public string? Grade { get; set; }
        public string? Remark { get; set; }
        
        public int? EnteredByTeacherId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}