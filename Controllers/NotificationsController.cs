
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;
using School_Yathu.Models;
using School_Yathu.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [SwaggerTag("Notifications - Manage user notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<NotificationsController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet("student")]
        [SwaggerOperation(Summary = "Get student notifications")]
        public async Task<IActionResult> GetStudentNotifications()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null || user.Role != "Student")
            {
                return Unauthorized();
            }

            var email = user.Email ?? string.Empty;
            var admissionNumber = email.Split('@')[0];
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.AdmissionNumber == admissionNumber);

            if (student == null)
            {
                return Ok(new NotificationListResponseDTO());
            }

            var notificationsList = await _context.Notifications
                .Where(n => n.UserId == userId || n.Role == "Student" || n.SpecificStudentId == student.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var notifications = new List<NotificationResponseDTO>();
            foreach (var n in notificationsList)
            {
                var response = new NotificationResponseDTO();
                response.Id = n.Id;
                response.Message = n.Message;
                response.Title = n.Title;
                response.IsRead = n.IsRead;
                response.CreatedAt = n.CreatedAt;
                response.Type = n.Type;
                response.Link = n.Link;
                response.Role = n.Role;
                response.StudentId = n.StudentId;
                response.TeacherId = n.TeacherId;
                response.TimeAgo = GetTimeAgo(n.CreatedAt);
                notifications.Add(response);
            }

            var unreadCount = 0;
            foreach (var n in notifications)
            {
                if (!n.IsRead) unreadCount++;
            }

            var result = new NotificationListResponseDTO();
            result.Notifications = notifications;
            result.TotalCount = notifications.Count;
            result.UnreadCount = unreadCount;
            result.Page = 1;
            result.PageSize = 50;
            result.TotalPages = 1;

            return Ok(result);
        }

        [HttpGet("teacher")]
        [SwaggerOperation(Summary = "Get teacher notifications")]
        public async Task<IActionResult> GetTeacherNotifications()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null || user.Role != "Teacher")
            {
                return Unauthorized();
            }

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.Email == user.Email);

            var notificationsList = await _context.Notifications
                .Where(n => n.UserId == userId || n.Role == "Teacher" || n.SpecificTeacherId == teacher.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var notifications = new List<NotificationResponseDTO>();
            foreach (var n in notificationsList)
            {
                var response = new NotificationResponseDTO();
                response.Id = n.Id;
                response.Message = n.Message;
                response.Title = n.Title;
                response.IsRead = n.IsRead;
                response.CreatedAt = n.CreatedAt;
                response.Type = n.Type;
                response.Link = n.Link;
                response.Role = n.Role;
                response.StudentId = n.StudentId;
                response.TeacherId = n.TeacherId;
                response.TimeAgo = GetTimeAgo(n.CreatedAt);
                notifications.Add(response);
            }

            var unreadCount = 0;
            foreach (var n in notifications)
            {
                if (!n.IsRead) unreadCount++;
            }

            var result = new NotificationListResponseDTO();
            result.Notifications = notifications;
            result.TotalCount = notifications.Count;
            result.UnreadCount = unreadCount;
            result.Page = 1;
            result.PageSize = 50;
            result.TotalPages = 1;

            return Ok(result);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Get admin notifications")]
        public async Task<IActionResult> GetAdminNotifications()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");

            var notificationsList = await _context.Notifications
                .Where(n => n.UserId == userId || n.Role == "Admin")
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var notifications = new List<NotificationResponseDTO>();
            foreach (var n in notificationsList)
            {
                var response = new NotificationResponseDTO();
                response.Id = n.Id;
                response.Message = n.Message;
                response.Title = n.Title;
                response.IsRead = n.IsRead;
                response.CreatedAt = n.CreatedAt;
                response.Type = n.Type;
                response.Link = n.Link;
                response.Role = n.Role;
                response.StudentId = n.StudentId;
                response.TeacherId = n.TeacherId;
                response.TimeAgo = GetTimeAgo(n.CreatedAt);
                notifications.Add(response);
            }

            var unreadCount = 0;
            foreach (var n in notifications)
            {
                if (!n.IsRead) unreadCount++;
            }

            var result = new NotificationListResponseDTO();
            result.Notifications = notifications;
            result.TotalCount = notifications.Count;
            result.UnreadCount = unreadCount;
            result.Page = 1;
            result.PageSize = 50;
            result.TotalPages = 1;

            return Ok(result);
        }

        [HttpGet("unread-count")]
        [SwaggerOperation(Summary = "Get unread count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            
            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Ok(new { unreadCount = count });
        }

        [HttpPut("{id}/read")]
        [SwaggerOperation(Summary = "Mark notification as read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            var response = new NotificationResponseDTO();
            response.Id = notification.Id;
            response.Message = notification.Message;
            response.Title = notification.Title;
            response.IsRead = notification.IsRead;
            response.CreatedAt = notification.CreatedAt;
            response.Type = notification.Type;
            response.Link = notification.Link;
            response.Role = notification.Role;
            response.TimeAgo = GetTimeAgo(notification.CreatedAt);

            return Ok(response);
        }

        [HttpPut("read-all")]
        [SwaggerOperation(Summary = "Mark all as read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "All notifications marked as read", count = notifications.Count });
        }

        [HttpPost("send")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Send notification")]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationDTO dto)
        {
            try
            {
                var notification = new Notification
                {
                    Title = dto.Title ?? "Notification",
                    Message = dto.Message,
                    Type = dto.Type ?? "general",
                    Role = dto.Role,
                    StudentId = dto.StudentId,
                    TeacherId = dto.TeacherId,
                    Link = dto.Link,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                var emailSent = await SendEmailNotifications(dto);

                return Ok(new
                {
                    success = true,
                    message = "Notification sent successfully",
                    emailSent = emailSent,
                    notificationId = notification.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending notification: {ex.Message}");
                return StatusCode(500, new { message = "Failed to send notification" });
            }
        }

        private async Task<bool> SendEmailNotifications(SendNotificationDTO dto)
        {
            try
            {
                var users = new List<User>();

                if (dto.Role == "All")
                {
                    users = await _context.Users.Where(u => u.IsActive).ToListAsync();
                }
                else if (dto.Role == "Student")
                {
                    if (dto.StudentId.HasValue)
                    {
                        var student = await _context.Students.FindAsync(dto.StudentId.Value);
                        if (student != null)
                        {
                            var user = await _context.Users
                                .FirstOrDefaultAsync(u => u.Email == $"{student.AdmissionNumber.ToLower()}@maranatha.ac.mw");
                            if (user != null)
                            {
                                users.Add(user);
                            }
                        }
                    }
                    else
                    {
                        users = await _context.Users
                            .Where(u => u.Role == "Student" && u.IsActive)
                            .ToListAsync();
                    }
                }
                else if (dto.Role == "Teacher")
                {
                    if (dto.TeacherId.HasValue)
                    {
                        var teacher = await _context.Teachers.FindAsync(dto.TeacherId.Value);
                        if (teacher != null)
                        {
                            var user = await _context.Users
                                .FirstOrDefaultAsync(u => u.Email == teacher.Email);
                            if (user != null)
                            {
                                users.Add(user);
                            }
                        }
                    }
                    else
                    {
                        users = await _context.Users
                            .Where(u => u.Role == "Teacher" && u.IsActive)
                            .ToListAsync();
                    }
                }

                foreach (var user in users)
                {
                    var emailDto = new NotificationEmailDTO
                    {
                        UserEmail = user.Email,
                        UserName = user.Name,
                        Message = dto.Message,
                        Subject = dto.Subject ?? "New Notification from Maranatha Secondary School",
                        Type = dto.Type ?? "general",
                        Link = dto.Link
                    };

                    await _emailService.SendNotificationEmailAsync(emailDto);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email notifications: {ex.Message}");
                return false;
            }
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)}w ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)}mo ago";
            
            return dateTime.ToString("MMM d, yyyy");
        }
    }
}