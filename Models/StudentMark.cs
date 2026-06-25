using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    /// <summary>
    /// Student Marks model (New - preferred)
    /// </summary>
    public class StudentMark
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        public double? Test1 { get; set; }
        public double? Test2 { get; set; }
        public double? EndTerm { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [MaxLength(20)]
        public string Term { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }
    }
}