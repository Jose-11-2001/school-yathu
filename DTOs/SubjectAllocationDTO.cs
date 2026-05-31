namespace School_Yathu.DTOs
{
    public class SubjectAllocationDTO
    {
        public int StudentId { get; set; }
        public List<int> SubjectIds { get; set; } = new List<int>();
        public int AcademicYear { get; set; }
        public string Term { get; set; } = string.Empty;
    }

    public class BulkSubjectAllocationDTO
    {
        public string ClassName { get; set; } = string.Empty;
        public string Stream { get; set; } = string.Empty;
        public List<int> SubjectIds { get; set; } = new List<int>();
        public int AcademicYear { get; set; }
        public string Term { get; set; } = string.Empty;
    }

    public class ClassSubjectTeacherDTO
    {
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int TeacherId { get; set; }
    }

    // Additional DTOs that might be needed for the allocation system
    public class StudentAllocationDTO
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string StudentClass { get; set; } = string.Empty;
        public string StudentStream { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int AcademicYear { get; set; }
        public string Term { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
    }

    public class ClassStudentsDTO
    {
        public int Id { get; set; }
        public string AdmissionNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string Stream { get; set; } = string.Empty;
    }

    public class AvailableSubjectDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsAllocated { get; set; }
    }

    public class AllocationSummaryDTO
    {
        public int TotalStudents { get; set; }
        public int StudentsWithAllocations { get; set; }
        public int StudentsWithoutAllocations { get; set; }
        public int TotalAllocations { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalTeachers { get; set; }
        public int AcademicYear { get; set; }
    }
}