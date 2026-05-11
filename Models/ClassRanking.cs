using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace School_Yathu.Models
{
    public class ClassRanking
    {
        [Key]
        public int Id { get; set; }
        
        public string? Class { get; set; }
        public string? Stream { get; set; }
        public int? Year { get; set; }
        public string? Term { get; set; }
        
        // Store rankings as JSON string
        public string RankingsJson { get; set; } = string.Empty;
        
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}