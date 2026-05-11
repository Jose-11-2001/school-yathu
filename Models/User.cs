
using System.ComponentModel.DataAnnotations;

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
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public string Role { get; set; } = "Teacher"; // Teacher, Student, Parent, Admin
        
        // Navigation properties
        public ICollection<Student>? Students { get; set; } // Teachers have students
        public ICollection<Marks>? MarksEntered { get; set; } // Marks entered by teacher
    }
}