
namespace School_Yathu.DTOs
{
    public class NotificationDTO
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Type { get; set; }
        public string Link { get; set; }
    }

    public class SendNotificationDTO
    {
        public string Message { get; set; }
        public string Subject { get; set; }
        public string Type { get; set; }
        public string Role { get; set; } // All, Student, Teacher, Admin
        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
        public string Link { get; set; }
    }
}