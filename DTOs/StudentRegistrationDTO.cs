namespace School_Yathu.DTOs
{
    public class StudentRegistrationDTO
    {
        public string AdmissionNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string Stream { get; set; } = string.Empty;
        public string? Root { get; set; }
        public List<int> SelectedSubjectIds { get; set; } = new List<int>();
        public int? TeacherId { get; set; }
    }

    public class AvailableSubjectDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
    }

    public class ClassSubjectsDTO
    {
        public string ClassName { get; set; } = string.Empty;
        public string Stream { get; set; } = string.Empty;
        public List<AvailableSubjectDTO> AvailableSubjects { get; set; } = new List<AvailableSubjectDTO>();
        public List<string> CoreSubjects { get; set; } = new List<string>();
        public List<string> HumanitiesSubjects { get; set; } = new List<string>();
        public List<string> ScienceSubjects { get; set; } = new List<string>();
    }
}