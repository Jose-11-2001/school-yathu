using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    /// <summary>
    /// Class-Subject relationship model
    /// </summary>
    public class ClassSubject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClassId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        public int? TeacherId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("ClassId")]
        public virtual Class? Class { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        [ForeignKey("TeacherId")]
        public virtual User? Teacher { get; set; }
    }
}