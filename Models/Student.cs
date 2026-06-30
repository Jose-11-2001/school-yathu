using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string AdmissionNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Class { get; set; }

        [MaxLength(50)]
        public string? Stream { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        public int? TeacherId { get; set; }
        public int? ClassId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("TeacherId")]
        public virtual User? Teacher { get; set; }

        [ForeignKey("ClassId")]
        public virtual Class? ClassEntity { get; set; }

        public virtual ICollection<StudentMark>? StudentMarks { get; set; }
        public virtual ICollection<StudentSubject>? StudentSubjects { get; set; }
        public virtual ICollection<Notification>? Notifications { get; set; } // Only ONE notification collection
        public virtual ICollection<Marks>? Marks { get; set; }
        public virtual ICollection<ExamResult>? ExamResults { get; set; }
        public int? DepartmentId { get; set; }

[ForeignKey("DepartmentId")]
public virtual Department? Department { get; set; }
    }
}