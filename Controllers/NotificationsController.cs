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

        /// <summary>
        /// Get student notifications
        /// </summary>
        [HttpGet("student")]
        [SwaggerOperation(Summary = "Get student notifications", Description = "Retrieves notifications for the logged-in student")]
        [SwaggerResponse(200, "List of notifications", typeof(NotificationListResponseDTO))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetStudentNotifications()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null || user.Role != "Student")
            {
                return Unauthorized();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.AdmissionNumber == user.Email?.Split('@')[0]);

            if (student == null)
            {
                return Ok(new NotificationListResponseDTO());
            }

            // Build query without using null propagating operator in expression tree
            var query = _context.Notifications
                .Where(n => n.UserId == userId || n.Role == "Student" || n.SpecificStudentId == student.Id)
                .OrderByDescending(n => n.CreatedAt);

            var notificationsList = await query.ToListAsync();

            var notifications = new List<NotificationResponseDTO>();
            foreach (var n in notificationsList)
            {
                notifications.Add(new NotificationResponseDTO
                {
                    Id = n.Id,
                    Message = n.Message,
                    Title = n.Title,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    Type = n.Type,
                    Link = n.Link,
                    Role = n.Role,
                    StudentId = n.StudentId,
                    TeacherId = n.TeacherId,
                    TimeAgo = this.GetTimeAgo(n.CreatedAt)
                });
            }

            var unreadCount = notifications.Count(n => !n.IsRead);

            return Ok(new NotificationListResponseDTO
            {
                Notifications = notifications,
                TotalCount = notifications.Count,
                UnreadCount = unreadCount,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            });
        }

        /// <summary>
        /// Get teacher notifications
        /// </summary>
        [HttpGet("teacher")]
        [SwaggerOperation(Summary = "Get teacher notifications", Description = "Retrieves notifications for the logged-in teacher")]
        [SwaggerResponse(200, "List of notifications", typeof(NotificationListResponseDTO))]
        [SwaggerResponse(401, "Unauthorized")]
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

            // Build query without using null propagating operator
            var query = _context.Notifications
                .Where(n => n.UserId == userId || n.Role == "Teacher" || n.SpecificTeacherId == teacher.Id)
                .OrderByDescending(n => n.CreatedAt);

            var notificationsList = await query.ToListAsync();

            var notifications = new List<NotificationResponseDTO>();
            foreach (var n in notificationsList)
            {
                notifications.Add(new NotificationResponseDTO
                {
                    Id = n.Id,
                    Message = n.Message,
                    Title = n.Title,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    Type = n.Type,
                    Link = n.Link,
                    Role = n.Role,
                    StudentId = n.StudentId,
                    TeacherId = n.TeacherId,
                    TimeAgo = this.GetTimeAgo(n.CreatedAt)
                });
            }

            var unreadCount = notifications.Count(n => !n.IsRead);

            return Ok(new NotificationListResponseDTO
            {
                Notifications = notifications,
                TotalCount = notifications.Count,
                UnreadCount = unreadCount,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            });
        }

        /// <summary>
        /// Get admin notifications
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Get admin notifications", Description = "Retrieves notifications for the logged-in admin")]
        [SwaggerResponse(200, "List of notifications", typeof(NotificationListResponseDTO))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetAdminNotifications()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");

            var query = _context.Notifications
                .Where(n => n.UserId == userId || n.Role == "Admin")
                .OrderByDescending(n => n.CreatedAt);

            var notificationsList = await query.ToListAsync();

            var notifications = new List<NotificationResponseDTO>();
            foreach (var n in notificationsList)
            {
                notifications.Add(new NotificationResponseDTO
                {
                    Id = n.Id,
                    Message = n.Message,
                    Title = n.Title,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    Type = n.Type,
                    Link = n.Link,
                    Role = n.Role,
                    StudentId = n.StudentId,
                    TeacherId = n.TeacherId,
                    TimeAgo = this.GetTimeAgo(n.CreatedAt)
                });
            }

            var unreadCount = notifications.Count(n => !n.IsRead);

            return Ok(new NotificationListResponseDTO
            {
                Notifications = notifications,
                TotalCount = notifications.Count,
                UnreadCount = unreadCount,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            });
        }

        /// <summary>
        /// Get unread notification count
        /// </summary>
        [HttpGet("unread-count")]
        [SwaggerOperation(Summary = "Get unread count", Description = "Retrieves the number of unread notifications")]
        [SwaggerResponse(200, "Unread count", typeof(object))]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            
            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Ok(new { unreadCount = count });
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        [HttpPut("{id}/read")]
        [SwaggerOperation(Summary = "Mark notification as read", Description = "Marks a specific notification as read")]
        [SwaggerResponse(200, "Notification marked as read", typeof(NotificationResponseDTO))]
        [SwaggerResponse(404, "Notification not found")]
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

            return Ok(new NotificationResponseDTO
            {
                Id = notification.Id,
                Message = notification.Message,
                Title = notification.Title,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                Type = notification.Type,
                Link = notification.Link,
                Role = notification.Role,
                TimeAgo = this.GetTimeAgo(notification.CreatedAt)
            });
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        [HttpPut("read-all")]
        [SwaggerOperation(Summary = "Mark all as read", Description = "Marks all notifications as read for the current user")]
        [SwaggerResponse(200, "All notifications marked as read")]
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

        /// <summary>
        /// Send notification (Admin only)
        /// </summary>
        [HttpPost("send")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Send notification", Description = "Sends a notification to users (Admin only)")]
        [SwaggerResponse(200, "Notification sent successfully", typeof(object))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        [SwaggerResponse(500, "Server error")]
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

                var emailSent = await this.SendEmailNotifications(dto);

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