using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Yathu.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        public int? HeadOfDepartmentId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("HeadOfDepartmentId")]
        public virtual User? HeadOfDepartment { get; set; }

        // ✅ ADD THIS - Navigation to Subjects
        public virtual ICollection<Subject>? Subjects { get; set; }

        // ✅ ADD THIS - Navigation to Teachers
        public virtual ICollection<User>? Teachers { get; set; }
    }
}