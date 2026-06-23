using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public string? Title { get; set; }
        
        public string? Type { get; set; } // result, deadline, enrollment, general
        
        public string? Role { get; set; } // Student, Teacher, Admin, All
        
        public int? UserId { get; set; } // Specific user
        
        public int? StudentId { get; set; }
        
        public int? TeacherId { get; set; }
        
        public int? SpecificStudentId { get; set; }
        
        public int? SpecificTeacherId { get; set; }
        
        public string? Link { get; set; }
        
        public bool IsRead { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("UserId")]
        public User? User { get; set; }
        
        [ForeignKey("StudentId")]
        public Student? Student { get; set; }
        
        [ForeignKey("TeacherId")]
        public User? Teacher { get; set; }
        
        [ForeignKey("SpecificStudentId")]
        public Student? SpecificStudent { get; set; }
        
        [ForeignKey("SpecificTeacherId")]
        public Teacher? SpecificTeacher { get; set; }
    }
}