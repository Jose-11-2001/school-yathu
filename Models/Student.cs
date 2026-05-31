using System;
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
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string Class { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string Stream { get; set; } = string.Empty;
        
        public int? TeacherId { get; set; }
        public int? ClassId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("TeacherId")]
        public virtual User? Teacher { get; set; }
        
        [ForeignKey("ClassId")]
        public virtual Class? ClassNavigation { get; set; }
    }
}