using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
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

        // Department
        public int? DepartmentId { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        // ✅ Navigation properties

        // 1. User as Class Teacher -> Classes they teach
        public virtual ICollection<Class>? ClassesAsTeacher { get; set; }

        // 2. User as Form Teacher -> Classes they are form teacher for
        public virtual ICollection<Class>? FormTeacherClasses { get; set; }

        // 3. ✅ FormTeacherClass assignments (joining table)
        public virtual ICollection<FormTeacherClass>? FormTeacherClassAssignments { get; set; }

        // 4. Students assigned to this teacher
        public virtual ICollection<Student>? Students { get; set; }

        // 5. Subjects taught by this teacher
        public virtual ICollection<TeacherSubject>? TeacherSubjects { get; set; }

        // 6. Subject allocations for this teacher (ClassSubject)
        public virtual ICollection<ClassSubject>? ClassSubjects { get; set; }

        // 7. Student subjects where this teacher is assigned
        public virtual ICollection<StudentSubject>? StudentSubjects { get; set; }

        // 8. Notifications for this user
        public virtual ICollection<Notification>? Notifications { get; set; }

        // 9. Exam results entered by this teacher
        public virtual ICollection<ExamResult>? ExamResults { get; set; }

        // 10. Marks entered by this teacher
        public virtual ICollection<Marks>? EnteredMarks { get; set; }

        // 11. Marks approved by this admin
        public virtual ICollection<Marks>? ApprovedMarks { get; set; }
    }
}