using System.ComponentModel.DataAnnotations;

namespace School_Yathu.DTOs
{
    #region Request DTOs

    /// <summary>
    /// DTO for creating a new notification
    /// </summary>
    public class CreateNotificationDTO
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        [MaxLength(1000, ErrorMessage = "Message cannot exceed 1000 characters")]
        public string Message { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
        public string? Type { get; set; }

        [MaxLength(20, ErrorMessage = "Role cannot exceed 20 characters")]
        public string? Role { get; set; }

        [MaxLength(500, ErrorMessage = "Link cannot exceed 500 characters")]
        public string? Link { get; set; }

        // Recipient identifiers
        public int? UserId { get; set; }
        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
        public int? AdminId { get; set; }
        public int? SpecificStudentId { get; set; }
        public int? SpecificTeacherId { get; set; }

        public bool SendEmail { get; set; } = false;
        public bool SendPushNotification { get; set; } = false;
    }

    /// <summary>
    /// DTO for sending a notification (simplified)
    /// </summary>
    public class SendNotificationDTO
    {
        [Required(ErrorMessage = "Message is required")]
        [MaxLength(1000, ErrorMessage = "Message cannot exceed 1000 characters")]
        public string Message { get; set; } = string.Empty;

        [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
        public string? Subject { get; set; }

        [MaxLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
        public string? Type { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [MaxLength(20, ErrorMessage = "Role cannot exceed 20 characters")]
        public string Role { get; set; } = "All";

        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
        public int? AdminId { get; set; }

        [MaxLength(500, ErrorMessage = "Link cannot exceed 500 characters")]
        public string? Link { get; set; }

        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string? Title { get; set; }

        public bool SendEmail { get; set; } = false;
        public bool SendPushNotification { get; set; } = false;
    }

    /// <summary>
    /// DTO for updating notification status
    /// </summary>
    public class UpdateNotificationStatusDTO
    {
        public bool IsRead { get; set; } = true;
    }

    /// <summary>
    /// DTO for marking multiple notifications as read
    /// </summary>
    public class MarkNotificationsReadDTO
    {
        [Required(ErrorMessage = "Notification IDs are required")]
        public List<int> NotificationIds { get; set; } = new List<int>();
    }

    /// <summary>
    /// DTO for filtering notifications
    /// </summary>
    public class NotificationFilterDTO
    {
        public string? Type { get; set; }
        public string? Role { get; set; }
        public bool? IsRead { get; set; }
        public int? UserId { get; set; }
        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortOrder { get; set; } = "DESC";
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// DTO for notification response
    /// </summary>
    public class NotificationResponseDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Role { get; set; }
        public string? Link { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? RecipientName { get; set; }
        public string? RecipientRole { get; set; }
        public int? RecipientId { get; set; }
        public string? TimeAgo { get; set; }
        public string? FormattedDate { get; set; }
        
        // These properties are needed for the controller
        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
        public int? AdminId { get; set; }
    }

    /// <summary>
    /// DTO for notification list response with pagination
    /// </summary>
    public class NotificationListResponseDTO
    {
        public List<NotificationResponseDTO> Notifications { get; set; } = new List<NotificationResponseDTO>();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// DTO for notification summary
    /// </summary>
    public class NotificationSummaryDTO
    {
        public int TotalNotifications { get; set; }
        public int UnreadCount { get; set; }
        public int ReadCount { get; set; }
        public Dictionary<string, int> CountByType { get; set; } = new Dictionary<string, int>();
        public List<NotificationResponseDTO> RecentNotifications { get; set; } = new List<NotificationResponseDTO>();
    }

    /// <summary>
    /// DTO for notification email
    /// </summary>
    public class NotificationEmailDTO
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Type { get; set; }
        public string? Link { get; set; }
        public string? Title { get; set; }
        public DateTime? SentAt { get; set; }
        public bool IsSent { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// DTO for notification statistics
    /// </summary>
    public class NotificationStatsDTO
    {
        public int TotalSent { get; set; }
        public int TotalRead { get; set; }
        public int TotalUnread { get; set; }
        public double ReadRate { get; set; }
        public Dictionary<string, int> SentByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> SentByRole { get; set; } = new Dictionary<string, int>();
        public List<DailyNotificationCount> DailyCounts { get; set; } = new List<DailyNotificationCount>();
    }

    /// <summary>
    /// DTO for daily notification count
    /// </summary>
    public class DailyNotificationCount
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public string? FormattedDate { get; set; }
    }

    #endregion

    #region Internal DTOs (For API consumption)

    /// <summary>
    /// DTO for receiving notifications from API (GET)
    /// </summary>
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
        public int? AdminId { get; set; }
        public string? TimeAgo { get; set; }
    }

    #endregion
}