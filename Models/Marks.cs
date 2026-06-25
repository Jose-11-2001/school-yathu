using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    /// <summary>
    /// Marks model - Stores student marks for subjects
    /// </summary>
    public class Marks
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        public int? ClassId { get; set; }

        // Test scores
        public double? ContinuousTest1 { get; set; }
        public double? ContinuousTest2 { get; set; }
        public double? EndTermExam { get; set; }
        public double? TotalScore { get; set; }

        // Grade and remark
        [MaxLength(5)]
        public string? Grade { get; set; }

        [MaxLength(200)]
        public string? Remark { get; set; }

        // Academic period
        [Required]
        public int Year { get; set; }

        [Required]
        [MaxLength(20)]
        public string Term { get; set; } = string.Empty;

        // Approval properties
        public bool IsApproved { get; set; } = false;
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedByAdminId { get; set; }
        public int? EnteredByTeacherId { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        [ForeignKey("ClassId")]
        public virtual Class? Class { get; set; }

        [ForeignKey("EnteredByTeacherId")]
        public virtual User? EnteredByTeacher { get; set; }

        [ForeignKey("ApprovedByAdminId")]
        public virtual User? ApprovedByAdmin { get; set; }
    }
}