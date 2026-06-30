using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    public class CreateClassDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Stream { get; set; }
        public int? TeacherId { get; set; }
        public int? Capacity { get; set; }
        public int? FormTeacherId { get; set; }
    }
    
    public class ClassResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Stream { get; set; }
        public int? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public int? Capacity { get; set; }
        public int StudentCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? FormTeacherId { get; set; }
        public string? FormTeacherName { get; set; }
        public string? FormTeacherEmail { get; set; }
        public string? FormTeacherPhone { get; set; }
    }
    
    public class UpdateClassDTO
    {
        public string? Name { get; set; }
        public string? Stream { get; set; }
        public int? TeacherId { get; set; }
        public int? Capacity { get; set; }
        public int? FormTeacherId { get; set; }
    }
    
    public class AllocateTeacherDTO
    {
        [Required]
        public int ClassId { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        [Required]
        public int TeacherId { get; set; }
    }
    
    public class ClassSubjectResponseDTO
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }
}