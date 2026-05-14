using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public string Type { get; set; } = "Info"; // Info, Warning, Success, ExamResults
        
        public int? TeacherId { get; set; }
        
        [ForeignKey("TeacherId")]
        public User? Teacher { get; set; }
        
        public int? StudentId { get; set; }
        
        [ForeignKey("StudentId")]
        public Student? Student { get; set; }
        
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}