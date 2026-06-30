using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    public class MarksEntryDTO  // ✅ Changed from MarksEntryDTOs
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
        public double? ContinuousTest1 { get; set; }  // ✅ Changed to double?
        
        [Range(0, 100)]
        public double? ContinuousTest2 { get; set; }  // ✅ Changed to double?
        
        [Range(0, 100)]
        public double? EndTermExam { get; set; }      // ✅ Changed to double?
    }
    
    public class MarksResponseDTO
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public double? ContinuousTest1 { get; set; }  // ✅ Changed to double?
        public double? ContinuousTest2 { get; set; }  // ✅ Changed to double?
        public double? EndTermExam { get; set; }      // ✅ Changed to double?
        public double? TotalScore { get; set; }       // ✅ Changed to double?
        public string? Grade { get; set; }
        public string? Remark { get; set; }
        public int Year { get; set; }
        public string Term { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}