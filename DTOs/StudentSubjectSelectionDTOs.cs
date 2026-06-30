using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    /// <summary>
    /// DTO for student selecting a subject
    /// </summary>
    public class SelectSubjectDTO
    {
        [Required(ErrorMessage = "Subject ID is required")]
        public int SubjectId { get; set; }

        public string? Term { get; set; }
    }

    /// <summary>
    /// DTO for available subjects response
    /// </summary>
    public class AvailableSubjectsResponseDTO
    {
        public List<SubjectSelectionItemDTO> AvailableSubjects { get; set; } = new List<SubjectSelectionItemDTO>();
        public List<SubjectSelectionItemDTO> SelectedSubjects { get; set; } = new List<SubjectSelectionItemDTO>();
        public List<SubjectSelectionItemDTO> ApprovedSubjects { get; set; } = new List<SubjectSelectionItemDTO>();
    }

    /// <summary>
    /// DTO for subject selection item
    /// </summary>
    public class SubjectSelectionItemDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Type { get; set; }
        public bool IsSelected { get; set; }
    }

    /// <summary>
    /// DTO for student subject selection response
    /// </summary>
    public class StudentSubjectSelectionDTO
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string? SubjectCode { get; set; }
        public int AcademicYear { get; set; }
        public string? Term { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}