using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class StudentSubjectSelection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        public int AcademicYear { get; set; }

        [MaxLength(20)]
        public string? Term { get; set; }

        public bool IsApproved { get; set; } = false;
        
        public int? ApprovedByFormTeacherId { get; set; }
        public int? ApprovedByTeacherId { get; set; }  // ✅ Added
        public DateTime? ApprovedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        [ForeignKey("ApprovedByFormTeacherId")]
        public virtual User? ApprovedByFormTeacher { get; set; }
        
        [ForeignKey("ApprovedByTeacherId")]
        public virtual User? ApprovedByTeacher { get; set; }  // ✅ Added
    }
}