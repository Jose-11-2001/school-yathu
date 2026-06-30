using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    public class CreateTeacherDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
        
        public string? PhoneNumber { get; set; }
        public string? EmployeeId { get; set; }
        public string? Qualification { get; set; }
        public DateTime? HireDate { get; set; }
        public int? DepartmentId { get; set; }  // ✅
    }
    
    public class TeacherResponseDTO
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? EmployeeId { get; set; }
        public string? Qualification { get; set; }
        public DateTime? HireDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int? DepartmentId { get; set; }  // ✅
        public string? DepartmentName { get; set; }  // ✅
        public bool MustChangePassword { get; set; }  // ✅
    }
    
    public class UpdateTeacherDTO  // ✅
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmployeeId { get; set; }
        public string? Qualification { get; set; }
        public int? DepartmentId { get; set; }
    }
}