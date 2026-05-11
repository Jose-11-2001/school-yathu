using System.ComponentModel.DataAnnotations;

namespace School_Yathu.Models
{
    public class Subject
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty; // Mathematics, English, Science
        
        public string? Code { get; set; } // MAT101, ENG101
        public int? MaxMarks { get; set; } = 100;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<Marks>? Marks { get; set; }
    }
}