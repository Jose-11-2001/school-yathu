using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    public class AssignFormTeacherDTO
    {
        [Required]
        public int ClassId { get; set; }
        
        [Required]
        public int TeacherId { get; set; }
    }
}