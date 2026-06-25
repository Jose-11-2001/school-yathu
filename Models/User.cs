using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    /// <summary>
    /// User model for authentication and authorization
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(50)]
        public string? EmployeeId { get; set; }

        [MaxLength(100)]
        public string? Qualification { get; set; }

        public DateTime? HireDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Student";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;
        public bool MustChangePassword { get; set; } = false;

        // Navigation properties
        public virtual ICollection<Class>? ClassesAsTeacher { get; set; }
        public virtual ICollection<ClassSubject>? ClassSubjects { get; set; }
        public virtual ICollection<Student>? Students { get; set; }
        public virtual ICollection<Notification>? Notifications { get; set; }
        public virtual ICollection<StudentSubject>? StudentSubjects { get; set; }
        public virtual ICollection<TeacherSubject>? TeacherSubjects { get; set; }
        public virtual ICollection<ExamResult>? ExamResults { get; set; }
    }
}