
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class StudentSubject
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int StudentId { get; set; }
        
        [ForeignKey("StudentId")]
        public Student? Student { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }
        
        public int? TeacherId { get; set; }
        
        [ForeignKey("TeacherId")]
        public User? Teacher { get; set; }
        
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
