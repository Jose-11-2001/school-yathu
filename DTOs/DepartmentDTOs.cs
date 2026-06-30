using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    public class CreateDepartmentDTO
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Description { get; set; }
    }
    
    public class UpdateDepartmentDTO
    {
        [MaxLength(100)]
        public string? Name { get; set; }
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        public int? HeadOfDepartmentId { get; set; }
    }
    
    public class DepartmentResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? HeadOfDepartmentId { get; set; }
        public string? HeadOfDepartmentName { get; set; }
        public string? HeadOfDepartmentEmail { get; set; }
        public string? HeadOfDepartmentPhone { get; set; }
        public int TeacherCount { get; set; }
        public int SubjectCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    
    public class AssignHeadOfDepartmentDTO
    {
        [Required]
        public int DepartmentId { get; set; }
        
        [Required]
        public int TeacherId { get; set; }
    }
    
    public class AssignTeacherSubjectDTO
    {
        [Required]
        public int TeacherId { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        public int? ClassId { get; set; }
    }
}