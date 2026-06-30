using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    public class SubjectDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public int? MaxMarks { get; set; }
        
        // ✅ ADD THESE - Used in AdminController.cs
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    
    public class CreateSubjectDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Code { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public int? MaxMarks { get; set; }
        
        // ✅ ADD THIS - Used in AdminController.cs
        public int? DepartmentId { get; set; }
    }
    
    public class UpdateSubjectDTO
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public int? MaxMarks { get; set; }
        
        // ✅ ADD THIS - Used in AdminController.cs
        public int? DepartmentId { get; set; }
    }
}