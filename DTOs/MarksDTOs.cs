
using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    public class MarksEntryDTO
    {
        [Required]
        public int StudentId { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        [Required]
        public int Year { get; set; }
        
        [Required]
        public string Term { get; set; } = string.Empty;
        
        [Range(0, 100)]
        public int? ContinuousTest1 { get; set; }
        
        [Range(0, 100)]
        public int? ContinuousTest2 { get; set; }
        
        [Range(0, 100)]
        public int? EndTermExam { get; set; }
    }
    
    public class MarksResponseDTO
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int? ContinuousTest1 { get; set; }
        public int? ContinuousTest2 { get; set; }
        public int? EndTermExam { get; set; }
        public int? TotalScore { get; set; }
        public string? Grade { get; set; }
        public string? Remark { get; set; }
        public int Year { get; set; }
        public string Term { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
    
    public class NotificationDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}