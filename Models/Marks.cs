
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class Marks
    {
        [Key]
        public int Id { get; set; }
        
        public int StudentId { get; set; }
        
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
        
        public int SubjectId { get; set; }
        
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }
        
        public int? ClassId { get; set; }
        
        [ForeignKey("ClassId")]
        public virtual Class? Class { get; set; }
        
        public int? ContinuousTest1 { get; set; }
        public int? ContinuousTest2 { get; set; }
        public int? EndTermExam { get; set; }
        public int? TotalScore { get; set; }
        public string? Grade { get; set; }
        public string? Remark { get; set; }
        public int Year { get; set; }
        public string Term { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = false;
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedByAdminId { get; set; }
        public int? EnteredByTeacherId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}