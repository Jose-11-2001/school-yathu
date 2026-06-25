using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    #region Student Registration DTO

    /// <summary>
    /// DTO for student registration with subject allocations
    /// </summary>
    public class StudentRegistrationDTO
    {
        [Required(ErrorMessage = "Admission number is required")]
        [MaxLength(50, ErrorMessage = "Admission number cannot exceed 50 characters")]
        public string AdmissionNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Class is required")]
        [MaxLength(50, ErrorMessage = "Class cannot exceed 50 characters")]
        public string Class { get; set; } = string.Empty;

        [Required(ErrorMessage = "Stream is required")]
        [MaxLength(50, ErrorMessage = "Stream cannot exceed 50 characters")]
        public string Stream { get; set; } = string.Empty;

        public int? TeacherId { get; set; }

        public string? Root { get; set; }

        public List<int> SelectedSubjectIds { get; set; } = new List<int>();
    }

    #endregion

    #region Student Management DTOs

    /// <summary>
    /// DTO for creating a new student
    /// </summary>
    public class CreateStudentDTO
    {
        [Required(ErrorMessage = "Admission number is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Admission number must be between 3 and 50 characters")]
        public string AdmissionNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Class cannot exceed 50 characters")]
        public string? Class { get; set; }

        [StringLength(50, ErrorMessage = "Stream cannot exceed 50 characters")]
        public string? Stream { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing student
    /// </summary>
    public class UpdateStudentDTO
    {
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        public string? FullName { get; set; }

        [StringLength(50, ErrorMessage = "Class cannot exceed 50 characters")]
        public string? Class { get; set; }

        [StringLength(50, ErrorMessage = "Stream cannot exceed 50 characters")]
        public string? Stream { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// DTO for student information response
    /// </summary>
    public class StudentDTO
    {
        public int Id { get; set; }
        public string AdmissionNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Class { get; set; }
        public string? Stream { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for student list response with additional info
    /// </summary>
    public class StudentListDTO
    {
        public int Id { get; set; }
        public string AdmissionNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Class { get; set; }
        public string? Stream { get; set; }
        public string? Email { get; set; }
        public int SubjectsCount { get; set; }
        public int MarksCount { get; set; }
        public double? AverageScore { get; set; }
        public string? Grade { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    #endregion

    #region Subject Management DTOs

    /// <summary>
    /// DTO for student subjects with teacher information
    /// </summary>
    public class StudentSubjectDTO
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Type { get; set; }
        public int? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public string? TeacherEmail { get; set; }
        public string? ClassName { get; set; }
        public string? Stream { get; set; }
        public int AcademicYear { get; set; }
        public string? Term { get; set; }
        public bool HasMarks { get; set; }
        public DateTime AssignedAt { get; set; }
    }

    /// <summary>
    /// DTO for subject allocation to student
    /// </summary>
    public class AllocateSubjectDTO
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        public int? TeacherId { get; set; }

        [Required]
        public int AcademicYear { get; set; }

        [Required]
        public string Term { get; set; } = string.Empty;
    }

    #endregion

    #region Marks and Results DTOs

    /// <summary>
    /// DTO for entering student marks
    /// </summary>
    public class MarksEntryDTO
    {
        [Required(ErrorMessage = "Student ID is required")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Subject ID is required")]
        public int SubjectId { get; set; }

        [Range(0, 100, ErrorMessage = "Test 1 score must be between 0 and 100")]
        public double? ContinuousTest1 { get; set; }

        [Range(0, 100, ErrorMessage = "Test 2 score must be between 0 and 100")]
        public double? ContinuousTest2 { get; set; }

        [Range(0, 100, ErrorMessage = "End term exam score must be between 0 and 100")]
        public double? EndTermExam { get; set; }

        [Range(0, 100, ErrorMessage = "Total score must be between 0 and 100")]
        public double? TotalScore { get; set; }

        [Required(ErrorMessage = "Year is required")]
        [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100")]
        public int Year { get; set; }

        [Required(ErrorMessage = "Term is required")]
        [StringLength(20, ErrorMessage = "Term cannot exceed 20 characters")]
        public string Term { get; set; } = string.Empty;

        public string? Grade { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// DTO for student marks response - UPDATED with Test1, Test2, EndTerm
    /// </summary>
    public class StudentMarkDTO
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string? SubjectCode { get; set; }
        public double? ContinuousTest1 { get; set; }
        public double? ContinuousTest2 { get; set; }
        public double? EndTermExam { get; set; }
        public double? TotalScore { get; set; }
        public double? OverallPercentage { get; set; }
        public string? Grade { get; set; }
        public string? Remark { get; set; }
        public int Year { get; set; }
        public string Term { get; set; } = string.Empty;
        
        // These properties are needed for the StudentController
        public double? Test1 { get; set; }
        public double? Test2 { get; set; }
        public double? EndTerm { get; set; }
    }

    /// <summary>
    /// DTO for adding/updating marks via admission number
    /// </summary>
    public class AddMarksDTO
    {
        [Required]
        public string AdmissionNumber { get; set; } = string.Empty;

        [Required]
        public int SubjectId { get; set; }

        [Range(0, 100)]
        public double? Test1 { get; set; }

        [Range(0, 100)]
        public double? Test2 { get; set; }

        [Range(0, 100)]
        public double? EndTerm { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public string Term { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating existing marks
    /// </summary>
    public class UpdateMarksDTO
    {
        [Range(0, 100)]
        public double? Test1 { get; set; }

        [Range(0, 100)]
        public double? Test2 { get; set; }

        [Range(0, 100)]
        public double? EndTerm { get; set; }

        public string? Grade { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// DTO for student results with ranking
    /// </summary>
    public class StudentResultDTO
    {
        public int StudentId { get; set; }
        public string AdmissionNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Class { get; set; }
        public string? Stream { get; set; }
        public int Year { get; set; }
        public string Term { get; set; } = string.Empty;
        public List<StudentMarkDTO> Marks { get; set; } = new List<StudentMarkDTO>();
        public RankingDTO? Ranking { get; set; }
        public PerformanceSummaryDTO? Summary { get; set; }
    }

    /// <summary>
    /// DTO for performance summary
    /// </summary>
    public class PerformanceSummaryDTO
    {
        public int TotalSubjects { get; set; }
        public int SubjectsWithMarks { get; set; }
        public double TotalScore { get; set; }
        public double AverageScore { get; set; }
        public string? Grade { get; set; }
        public string? Remark { get; set; }
        public int Position { get; set; }
        public int TotalStudents { get; set; }
    }

    #endregion

    #region Ranking DTOs

    /// <summary>
    /// DTO for student ranking
    /// </summary>
    public class RankingDTO
    {
        public int? StudentId { get; set; }
        public double TotalMarks { get; set; }
        public double Average { get; set; }
        public int Position { get; set; }
        public int TotalStudents { get; set; }
        public string? Class { get; set; }
        public string? Stream { get; set; }
        public string? Grade { get; set; }
        public string? Remarks { get; set; }
        public List<RankingItemDTO>? TopStudents { get; set; }
    }

    /// <summary>
    /// DTO for ranking item
    /// </summary>
    public class RankingItemDTO
    {
        public int Position { get; set; }
        public int StudentId { get; set; }
        public string AdmissionNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public double Average { get; set; }
        public string? Grade { get; set; }
    }

    /// <summary>
    /// DTO for class rankings response
    /// </summary>
    public class ClassRankingsDTO
    {
        public string Class { get; set; } = string.Empty;
        public string? Stream { get; set; }
        public int Year { get; set; }
        public string Term { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public List<RankingItemDTO> Rankings { get; set; } = new List<RankingItemDTO>();
    }

    #endregion

    #region Dashboard DTOs

    /// <summary>
    /// DTO for student dashboard response
    /// </summary>
    public class StudentDashboardDTO
    {
        public StudentDTO Student { get; set; } = new StudentDTO();
        public List<StudentSubjectDTO> Subjects { get; set; } = new List<StudentSubjectDTO>();
        public PerformanceSummaryDTO Performance { get; set; } = new PerformanceSummaryDTO();
        public List<NotificationResponseDTO> RecentNotifications { get; set; } = new List<NotificationResponseDTO>();
        public int UnreadNotificationCount { get; set; }
        public List<UpcomingExamDTO> UpcomingExams { get; set; } = new List<UpcomingExamDTO>();
    }

    /// <summary>
    /// DTO for upcoming exams
    /// </summary>
    public class UpcomingExamDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public string? Venue { get; set; }
    }

    #endregion

    #region Filter and Search DTOs

    /// <summary>
    /// DTO for filtering students
    /// </summary>
    public class StudentFilterDTO
    {
        public string? Class { get; set; }
        public string? Stream { get; set; }
        public string? SearchTerm { get; set; }
        public int? Year { get; set; }
        public string? Term { get; set; }
        public bool? IsActive { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    /// <summary>
    /// DTO for paginated student response
    /// </summary>
    public class PaginatedStudentResponseDTO
    {
        public List<StudentListDTO> Students { get; set; } = new List<StudentListDTO>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    #endregion

    #region Grade Calculation DTOs

    /// <summary>
    /// DTO for grade calculation result
    /// </summary>
    public class GradeResultDTO
    {
        public string Grade { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public double Score { get; set; }
        public string GradingSystem { get; set; } = "Letter Grades";
        public bool IsUpperForm { get; set; }
        public int? Points { get; set; }
    }

    #endregion
}