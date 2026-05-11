
using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    public class CreateStudentDTO
    {
        [Required]
        public string AdmissionNumber { get; set; } = string.Empty;
        
        [Required]
        public string FullName { get; set; } = string.Empty;
        
        public string? Class { get; set; }
        public string? Stream { get; set; }
    }
    
    public class StudentResponseDTO
    {
        public int Id { get; set; }
        public string AdmissionNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Class { get; set; }
        public string? Stream { get; set; }
        public int? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
    public class MarksEntryDTO
    {
        [Required]
        public int StudentId { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        [Required]
        [Range(0, 100)]
        public int Score { get; set; }
        
        public int? Year { get; set; }
        public string? Term { get; set; }
        public string? ExamType { get; set; }
    }
}