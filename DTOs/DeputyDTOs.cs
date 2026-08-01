using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    public class CreateDeputyDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MinLength(2)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
        
        public string? PhoneNumber { get; set; }
        public string? EmployeeId { get; set; }
        public string? Qualification { get; set; }
        public DateTime? HireDate { get; set; }
        public int? DepartmentId { get; set; }
    }

    public class AssignDeputyDTO
    {
        [Required]
        public int TeacherId { get; set; }
        
        public bool ReplaceExisting { get; set; } = false;
    }
}