using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class TeacherSubjectAllocation
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ClassId { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        [Required]
        public int TeacherId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        [ForeignKey("ClassId")]
        public Class? Class { get; set; }
        
        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }
        
        [ForeignKey("TeacherId")]
        public User? Teacher { get; set; }
    }
}