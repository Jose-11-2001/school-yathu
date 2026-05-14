using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class TeacherSubject
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int TeacherId { get; set; }
        
        [ForeignKey("TeacherId")]
        public User? Teacher { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }
        
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
