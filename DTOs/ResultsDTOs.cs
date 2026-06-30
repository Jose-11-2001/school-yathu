using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    public class ApproveResultsDTO
    {
        [Required]
        public int SubjectId { get; set; }
        
        [Required]
        public int Year { get; set; }
        
        [Required]
        public string Term { get; set; } = string.Empty;
    }

    public class SubmitResultsDTO
    {
        [Required]
        public string ClassName { get; set; } = string.Empty;
        
        [Required]
        public int Year { get; set; }
        
        [Required]
        public string Term { get; set; } = string.Empty;
    }
}