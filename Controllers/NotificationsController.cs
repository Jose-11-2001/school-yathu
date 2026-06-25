using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;
using School_Yathu.Models;
using School_Yathu.Services;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

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
        /// Get notifications for the logged-in student
        /// </summary>
        [HttpGet("student")]
        [SwaggerOperation(Summary = "Get student notifications")]
        public async Task<IActionResult> GetStudentNotifications()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
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

            // Get notifications for this student - Using only the navigation properties that exist
            var notificationsList = await _context.Notifications
                .Where(n => n.UserId == userId || n.StudentId == student.Id || n.Role == "Student")
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var notifications = notificationsList.Select(n => new NotificationResponseDTO
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
                TimeAgo = GetTimeAgo(n.CreatedAt)
            }).ToList();

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
        /// Get notifications for the logged-in teacher
        /// </summary>
        [HttpGet("teacher")]
        [SwaggerOperation(Summary = "Get teacher notifications")]
        public async Task<IActionResult> GetTeacherNotifications()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null || user.Role != "Teacher")
            {
                return Unauthorized();
            }

            // Get notifications for this teacher
            var notificationsList = await _context.Notifications
                .Where(n => n.UserId == userId || n.TeacherId == userId || n.Role == "Teacher")
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var notifications = notificationsList.Select(n => new NotificationResponseDTO
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
                TimeAgo = GetTimeAgo(n.CreatedAt)
            }).ToList();

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
        /// Get notifications for the logged-in admin
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Get admin notifications")]
        public async Task<IActionResult> GetAdminNotifications()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var notificationsList = await _context.Notifications
                .Where(n => n.UserId == userId || n.Role == "Admin")
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var notifications = notificationsList.Select(n => new NotificationResponseDTO
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
                TimeAgo = GetTimeAgo(n.CreatedAt)
            }).ToList();

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
        /// Get unread notification count for the logged-in user
        /// </summary>
        [HttpGet("unread-count")]
        [SwaggerOperation(Summary = "Get unread count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Ok(new { unreadCount = count });
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        [HttpPut("{id}/read")]
        [SwaggerOperation(Summary = "Mark notification as read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var response = new NotificationResponseDTO
            {
                Id = notification.Id,
                Message = notification.Message,
                Title = notification.Title,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                Type = notification.Type,
                Link = notification.Link,
                Role = notification.Role,
                TimeAgo = GetTimeAgo(notification.CreatedAt)
            };

            return Ok(response);
        }

        /// <summary>
        /// Mark all notifications as read for the logged-in user
        /// </summary>
        [HttpPut("read-all")]
        [SwaggerOperation(Summary = "Mark all as read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "All notifications marked as read", count = notifications.Count });
        }

        /// <summary>
        /// Send a notification (Admin only)
        /// </summary>
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

        /// <summary>
        /// Send email notifications to users
        /// </summary>
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
                                .FirstOrDefaultAsync(u => u.Email == $"{student.AdmissionNumber.ToLower()}@student.school.com");
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
                        var user = await _context.Users
                            .FirstOrDefaultAsync(u => u.Id == dto.TeacherId.Value && u.Role == "Teacher");
                        if (user != null)
                        {
                            users.Add(user);
                        }
                    }
                    else
                    {
                        users = await _context.Users
                            .Where(u => u.Role == "Teacher" && u.IsActive)
                            .ToListAsync();
                    }
                }
                else if (dto.Role == "Admin")
                {
                    users = await _context.Users
                        .Where(u => u.Role == "Admin" && u.IsActive)
                        .ToListAsync();
                }

                foreach (var user in users)
                {
                    var emailDto = new NotificationEmailDTO
                    {
                        UserEmail = user.Email,
                        UserName = user.Name,
                        Message = dto.Message,
                        Subject = dto.Subject ?? "New Notification from School",
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

        /// <summary>
        /// Get human-readable time ago string
        /// </summary>
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