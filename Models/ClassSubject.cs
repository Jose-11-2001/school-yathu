using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class ClassSubject
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ClassId { get; set; }
        
        [ForeignKey("ClassId")]
        public Class? Class { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }
        
        [Required]
        public int TeacherId { get; set; }
        
        [ForeignKey("TeacherId")]
        public User? Teacher { get; set; }
        
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}