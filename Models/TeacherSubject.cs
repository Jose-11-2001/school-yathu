using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    /// <summary>
    /// Teacher-Subject relationship model
    /// </summary>
    public class TeacherSubject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeacherId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        public int? ClassId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("TeacherId")]
        public virtual User? Teacher { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        [ForeignKey("ClassId")]
        public virtual Class? Class { get; set; }
    }
}