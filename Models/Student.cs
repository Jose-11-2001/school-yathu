
using System.ComponentModel.DataAnnotations;

namespace School_Yathu.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string AdmissionNumber { get; set; } = string.Empty;
        
        [Required]
        public string FullName { get; set; } = string.Empty;
        
        public string? Class { get; set; }
        public string? Stream { get; set; }
        
        public int? TeacherId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual ICollection<Marks>? Marks { get; set; }
        // In Models/Student.cs - Add this property
        public int? ClassId { get; set; }
    }
}