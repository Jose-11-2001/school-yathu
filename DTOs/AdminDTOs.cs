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
    }
    
    public class CreateClassDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Stream { get; set; }
        public int? TeacherId { get; set; }
        public int? Capacity { get; set; }
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
    
    public class CreateExamDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Type { get; set; }
        public int? Year { get; set; }
        public string? Term { get; set; }
        public DateTime? ExamDate { get; set; }
    }
    
    public class ExamResultDTO
    {
        [Required]
        public int ExamId { get; set; }
        
        [Required]
        public int StudentId { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        [Required]
        [Range(0, 100)]
        public int Score { get; set; }
    }
    
    public class ExamResultResponseDTO
    {
        public int Id { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Grade { get; set; } = string.Empty;
        public string? Remark { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}