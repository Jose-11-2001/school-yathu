using System;

namespace School_Yathu.Models
{
    public class StudentSubject
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public int TeacherId { get; set; }
        public int AcademicYear { get; set; }
        public string Term { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Student? Student { get; set; }
        public virtual Subject? Subject { get; set; }
        public virtual User? Teacher { get; set; }
    }
}