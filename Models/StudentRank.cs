using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class StudentRank
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int StudentId { get; set; }
        
        public int TotalMarks { get; set; }
        public double AverageScore { get; set; }
        public int Position { get; set; }
        public string? Grade { get; set; }
        public string? Remarks { get; set; }
        
        public int? Year { get; set; }
        public string? Term { get; set; }
        
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}