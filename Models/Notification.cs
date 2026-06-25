using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using School_Yathu.Models.Enums;

namespace School_Yathu.Models
{
    /// <summary>
    /// Notification model for system notifications
    /// </summary>
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        // Recipient identifiers
        public int? UserId { get; set; }        // Specific user (general)
        public int? StudentId { get; set; }     // Specific student
        public int? TeacherId { get; set; }     // Specific teacher
        public int? AdminId { get; set; }       // Specific admin

        // For targeting specific users (alternative naming)
        public int? SpecificStudentId { get; set; }
        public int? SpecificTeacherId { get; set; }

        [MaxLength(50)]
        public string? Type { get; set; } // result, deadline, enrollment, general, academic, exam

        [MaxLength(20)]
        public string? Role { get; set; } // Student, Teacher, Admin, All

        public string? Link { get; set; } // Optional link for action

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("TeacherId")]
        public virtual User? Teacher { get; set; }

        [ForeignKey("AdminId")]
        public virtual User? Admin { get; set; }

        [ForeignKey("SpecificStudentId")]
        public virtual Student? SpecificStudent { get; set; }

        [ForeignKey("SpecificTeacherId")]
        public virtual User? SpecificTeacher { get; set; }
    }
}