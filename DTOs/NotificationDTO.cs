using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    // For GET requests - receiving notifications from API
    public class NotificationGetDTO
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Title { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Type { get; set; }
        public string? Link { get; set; }
        public string? Role { get; set; }
        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
    }

    // For POST requests - sending notifications
    public class SendNotificationDTO
    {
        public string Message { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Type { get; set; }
        public string Role { get; set; } = "All"; // All, Student, Teacher, Admin
        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
        public string? Link { get; set; }
        public string? Title { get; set; }
    }

    // For email notifications
    public class NotificationEmailDTO
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Type { get; set; }
        public string? Link { get; set; }
    }

    // For API responses
    public class NotificationResponseDTO
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Title { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Type { get; set; }
        public string? Link { get; set; }
        public string? Role { get; set; }
        public string? StudentName { get; set; }
        public string? TeacherName { get; set; }
        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
        public string? TimeAgo { get; set; }
    }

    // For paginated responses
    public class NotificationListResponseDTO
    {
        public List<NotificationResponseDTO> Notifications { get; set; } = new List<NotificationResponseDTO>();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}