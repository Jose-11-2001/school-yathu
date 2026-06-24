using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class Exam
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Type { get; set; }
        
        public int? Year { get; set; }
        
        public string? Term { get; set; }
        
        public DateTime? ExamDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property - Links to ExamResults
        public ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();
    }
}