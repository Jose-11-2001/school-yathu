using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    public class AssignSecondaryRolesDTO
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public List<string> SecondaryRoles { get; set; } = new List<string>();
    }
}