using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    /// <summary>
    /// Notification model
    /// </summary>
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        // Recipient identifiers - Use these only
        public int? UserId { get; set; }
        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
        public int? AdminId { get; set; }

        [MaxLength(50)]
        public string? Type { get; set; }

        [MaxLength(20)]
        public string? Role { get; set; }

        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Navigation properties - Only ONE relationship per entity
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("TeacherId")]
        public virtual User? Teacher { get; set; }

        [ForeignKey("AdminId")]
        public virtual User? Admin { get; set; }
    }
}