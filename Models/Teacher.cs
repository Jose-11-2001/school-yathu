using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class Teacher
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        public string? PhoneNumber { get; set; }
        
        public string? EmployeeId { get; set; }
        
        public string? Qualification { get; set; }
        
        public DateTime? HireDate { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public ICollection<Class> Classes { get; set; } = new List<Class>();
        
        public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
        
        public ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
        
        public ICollection<StudentSubject> StudentSubjects { get; set; } = new List<StudentSubject>();
        
        public ICollection<Marks> Marks { get; set; } = new List<Marks>();
    }
}