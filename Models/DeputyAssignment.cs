using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class DeputyAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DeputyId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Task { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        [ForeignKey("DeputyId")]
        public virtual User? Deputy { get; set; }
    }
}