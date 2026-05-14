using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class Exam
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty; // CAT 1, CAT 2, EndTerm 1, etc.
        
        public string? Type { get; set; } // Continuous, EndTerm
        public int? Year { get; set; }
        public string? Term { get; set; } // Term 1, Term 2, Term 3
        
        public DateTime? ExamDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<ExamResult>? ExamResults { get; set; }
    }
}
