using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    /// <summary>
    /// Student-Subject relationship model
    /// </summary>
    public class StudentSubject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        public int? TeacherId { get; set; }

        public int AcademicYear { get; set; }

        [MaxLength(20)]
        public string? Term { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        [ForeignKey("TeacherId")]
        public virtual User? Teacher { get; set; }
    }
}