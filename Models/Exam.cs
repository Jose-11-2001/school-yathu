using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    /// <summary>
    /// Exam model
    /// </summary>
    public class Exam
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public int? SubjectId { get; set; }

        public int? ClassId { get; set; }

        public DateTime ExamDate { get; set; }

        [MaxLength(200)]
        public string? Venue { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        [ForeignKey("ClassId")]
        public virtual Class? Class { get; set; }

        public virtual ICollection<ExamResult>? ExamResults { get; set; }
    }
}