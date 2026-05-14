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
    
    public class MarksEntryDTO
    {
        [Required]
        public int StudentId { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        public int? ContinuousTest1 { get; set; }
        public int? ContinuousTest2 { get; set; }
        public int? EndTermExam { get; set; }
        
        [Required]
        public int Year { get; set; }
        
        [Required]
        public string Term { get; set; } = string.Empty;
    }
}