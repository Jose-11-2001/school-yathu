
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class Marks
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int StudentId { get; set; }
        
        [ForeignKey("StudentId")]
        public Student? Student { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }
        
        [Required]
        public int ClassId { get; set; }
        
        [Required]
        public int Year { get; set; }
        
        [Required]
        public string Term { get; set; } = string.Empty;
        
        // Assessment scores
        public int? ContinuousTest1 { get; set; } // 20%
        public int? ContinuousTest2 { get; set; } // 20%
        public int? EndTermExam { get; set; } // 60%
        
        // Calculated values
        public int? TotalScore { get; set; }
        public string? Grade { get; set; }
        public string? Remark { get; set; }
        
        // Approval fields
        public bool IsApproved { get; set; } = false;
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedByAdminId { get; set; }
        
        public int? EnteredByTeacherId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}