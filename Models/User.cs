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
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        public string? PhoneNumber { get; set; }
        public string? EmployeeId { get; set; }
        public string? Qualification { get; set; }
        public DateTime? HireDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public bool MustChangePassword { get; set; } = false;
        public string Role { get; set; } = "Teacher";
        
        // Navigation properties - ADD THESE
        public ICollection<Class>? Classes { get; set; }
        public ICollection<TeacherSubject>? TeacherSubjects { get; set; }
        public ICollection<Notification>? Notifications { get; set; }
    }
}