using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    public class SubjectAllocationDTO
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public List<int> SubjectIds { get; set; } = new List<int>();

        [Required]
        public int AcademicYear { get; set; }

        [Required]
        public string Term { get; set; } = string.Empty;
    }

    public class BulkSubjectAllocationDTO
    {
        [Required]
        public string ClassName { get; set; } = string.Empty;

        [Required]
        public string Stream { get; set; } = string.Empty;

        [Required]
        public List<int> SubjectIds { get; set; } = new List<int>();

        [Required]
        public int AcademicYear { get; set; }

        [Required]
        public string Term { get; set; } = string.Empty;
    }

    public class TeacherSubjectAllocationDTO
    {
        [Required]
        public int ClassId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [Required]
        public int TeacherId { get; set; }
    }
}