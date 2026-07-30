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
    #region DTOs (Local definitions for the controller)

    public class BroadcastNotificationDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Link { get; set; }
        public bool SendEmail { get; set; } = false;
    }

    public class NotificationResponseDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Info";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? Link { get; set; }
        public int? UserId { get; set; }
        public string? Role { get; set; }
        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }

    public class NotificationListResponseDTO
    {
        public List<NotificationResponseDTO> Notifications { get; set; } = new();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    #endregion

    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Notifications - Manage user notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService? _emailService;
        private readonly ILogger<NotificationsController> _logger;

        // ✅ SINGLE CONSTRUCTOR with optional IEmailService
        public NotificationsController(
            ApplicationDbContext context,
            ILogger<NotificationsController> logger,
            IEmailService? emailService = null)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        #region GET Notifications

        /// <summary>
        /// Get notifications for the current user based on their role
        /// </summary>
        [HttpGet]
        [Authorize]
        [SwaggerOperation(Summary = "Get my notifications", Description = "Retrieves all notifications for the current user based on their role")]
        [SwaggerResponse(200, "List of notifications", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetMyNotifications([FromQuery] bool unreadOnly = false)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                _logger.LogInformation($"Getting notifications for UserId: {userId}, Role: {userRole}");

                var query = _context.Notifications
                    .Where(n => n.UserId == userId || n.Role == userRole);

                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new NotificationResponseDTO
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt,
                        ReadAt = n.ReadAt,
                        Link = n.Link,
                        UserId = n.UserId,
                        Role = n.Role,
                        StudentId = n.StudentId,
                        TeacherId = n.TeacherId,
                        TimeAgo = GetTimeAgo(n.CreatedAt)
                    })
                    .ToListAsync();

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, new { message = "An error occurred while retrieving notifications" });
            }
        }

        /// <summary>
        /// Get notifications for the logged-in student
        /// </summary>
        [HttpGet("student")]
        [Authorize(Roles = "Student")]
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

            var notificationsList = await _context.Notifications
                .Where(n => n.UserId == userId || 
                            (student != null && n.StudentId == student.Id) || 
                            n.Role == "Student")
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
        [Authorize(Roles = "Teacher,FormTeacher,HeadOfDepartment")]
        [SwaggerOperation(Summary = "Get teacher notifications")]
        public async Task<IActionResult> GetTeacherNotifications()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null || !IsTeacherRole(user.Role))
            {
                return Unauthorized();
            }

            var notificationsList = await _context.Notifications
                .Where(n => n.UserId == userId || n.TeacherId == userId || n.Role == user.Role)
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
        [Authorize(Roles = "Admin,Headteacher")]
        [SwaggerOperation(Summary = "Get admin notifications")]
        public async Task<IActionResult> GetAdminNotifications()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var notificationsList = await _context.Notifications
                .Where(n => n.UserId == userId || n.Role == userRole)
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
        /// Get notifications for a specific role (Admin only)
        /// </summary>
        [HttpGet("role/{role}")]
        [Authorize(Roles = "Admin,Headteacher")]
        [SwaggerOperation(Summary = "Get notifications by role", Description = "Retrieves notifications for a specific role (Admin only)")]
        [SwaggerResponse(200, "List of notifications", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetNotificationsByRole(string role, [FromQuery] bool unreadOnly = false)
        {
            try
            {
                _logger.LogInformation($"Getting notifications for role: {role}");

                var normalizedRole = role.ToLower() switch
                {
                    "admin" => "Admin",
                    "headteacher" => "Admin",
                    "teacher" => "Teacher",
                    "formteacher" => "FormTeacher",
                    "hod" => "HeadOfDepartment",
                    "student" => "Student",
                    _ => role
                };

                var query = _context.Notifications
                    .Where(n => n.Role == normalizedRole);

                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new NotificationResponseDTO
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt,
                        ReadAt = n.ReadAt,
                        Link = n.Link,
                        UserId = n.UserId,
                        Role = n.Role,
                        StudentId = n.StudentId,
                        TeacherId = n.TeacherId,
                        TimeAgo = GetTimeAgo(n.CreatedAt)
                    })
                    .ToListAsync();

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications by role");
                return StatusCode(500, new { message = "An error occurred while retrieving notifications" });
            }
        }

        /// <summary>
        /// Get notifications for a specific user (Admin only)
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin,Headteacher")]
        [SwaggerOperation(Summary = "Get user notifications", Description = "Retrieves notifications for a specific user (Admin only)")]
        [SwaggerResponse(200, "List of notifications", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetUserNotifications(int userId, [FromQuery] bool unreadOnly = false)
        {
            try
            {
                var query = _context.Notifications
                    .Where(n => n.UserId == userId);

                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new NotificationResponseDTO
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt,
                        ReadAt = n.ReadAt,
                        Link = n.Link,
                        UserId = n.UserId,
                        Role = n.Role,
                        StudentId = n.StudentId,
                        TeacherId = n.TeacherId,
                        TimeAgo = GetTimeAgo(n.CreatedAt)
                    })
                    .ToListAsync();

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user notifications");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get unread notification count for the logged-in user
        /// </summary>
        [HttpGet("unread-count")]
        [Authorize]
        [SwaggerOperation(Summary = "Get unread count", Description = "Gets the count of unread notifications for the current user")]
        [SwaggerResponse(200, "Unread count", typeof(object))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var count = await _context.Notifications
                    .Where(n => (n.UserId == userId || n.Role == userRole) && !n.IsRead)
                    .CountAsync();

                return Ok(new { unreadCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        #endregion

        #region PUT/Update Notifications

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        [HttpPut("mark-read/{id}")]
        [Authorize]
        [SwaggerOperation(Summary = "Mark notification as read", Description = "Marks a specific notification as read")]
        [SwaggerResponse(200, "Notification marked as read")]
        [SwaggerResponse(404, "Notification not found")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                    return NotFound(new { message = "Notification not found" });

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (notification.UserId != userId && notification.Role != userRole)
                {
                    return Unauthorized(new { message = "You are not authorized to mark this notification as read" });
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Mark all notifications as read for the current user
        /// </summary>
        [HttpPut("mark-all-read")]
        [Authorize]
        [SwaggerOperation(Summary = "Mark all notifications as read", Description = "Marks all notifications for the current user as read")]
        [SwaggerResponse(200, "All notifications marked as read")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var notifications = await _context.Notifications
                    .Where(n => (n.UserId == userId || n.Role == userRole) && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Marked {notifications.Count} notifications as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        #endregion

        #region POST/Create Notifications

        /// <summary>
        /// Create a notification (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Headteacher")]
        [SwaggerOperation(Summary = "Create notification", Description = "Creates a new notification (Admin only)")]
        [SwaggerResponse(200, "Notification created", typeof(object))]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDTO dto)
        {
            try
            {
                var notification = new Notification
                {
                    Title = dto.Title,
                    Message = dto.Message,
                    Type = dto.Type ?? "Info",
                    Link = dto.Link,
                    UserId = dto.UserId,
                    Role = dto.Role,
                    StudentId = dto.StudentId,
                    TeacherId = dto.TeacherId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                if (_emailService != null && !string.IsNullOrEmpty(dto.Message))
                {
                    await SendEmailNotificationsFromCreate(dto);
                }

                return Ok(new { 
                    message = "Notification created successfully",
                    notificationId = notification.Id 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Send a notification (Admin only)
        /// </summary>
        [HttpPost("send")]
        [Authorize(Roles = "Admin,Headteacher")]
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

                var emailSent = false;
                if (_emailService != null)
                {
                    emailSent = await SendEmailNotificationsFromSend(dto);
                }

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
        /// Broadcast a notification to all users of a specific role
        /// </summary>
        [HttpPost("broadcast")]
        [Authorize(Roles = "Admin,Headteacher")]
        [SwaggerOperation(Summary = "Broadcast notification", Description = "Sends a notification to all users of a specific role (Admin only)")]
        [SwaggerResponse(200, "Broadcast sent", typeof(object))]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> BroadcastNotification([FromBody] BroadcastNotificationDTO dto)
        {
            try
            {
                var targetUsers = await _context.Users
                    .Where(u => u.Role == dto.Role && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync();

                if (!targetUsers.Any())
                    return BadRequest(new { message = $"No users found with role: {dto.Role}" });

                var notifications = targetUsers.Select(userId => new Notification
                {
                    Title = dto.Title,
                    Message = dto.Message,
                    Type = dto.Type ?? "Broadcast",
                    UserId = userId,
                    Role = dto.Role,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                }).ToList();

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = $"Broadcast sent to {targetUsers.Count} users with role {dto.Role}",
                    recipientCount = targetUsers.Count 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        #endregion

        #region DELETE Notifications

        /// <summary>
        /// Delete a notification
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        [SwaggerOperation(Summary = "Delete notification", Description = "Deletes a specific notification")]
        [SwaggerResponse(200, "Notification deleted")]
        [SwaggerResponse(404, "Notification not found")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                    return NotFound(new { message = "Notification not found" });

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (notification.UserId != userId && notification.Role != userRole && userRole != "Admin")
                {
                    return Unauthorized(new { message = "You are not authorized to delete this notification" });
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Notification deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        #endregion

        #region Private Methods

        private bool IsTeacherRole(string role)
        {
            return role == "Teacher" || role == "FormTeacher" || role == "HeadOfDepartment" || role == "DeputyHeadTeacher";
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

        private async Task<bool> SendEmailNotificationsFromSend(SendNotificationDTO dto)
        {
            try
            {
                if (_emailService == null) return false;

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
                            if (user != null) users.Add(user);
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
                        if (user != null) users.Add(user);
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
                    var emailDto = new School_Yathu.DTOs.NotificationEmailDTO
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

        private async Task SendEmailNotificationsFromCreate(CreateNotificationDTO dto)
        {
            try
            {
                if (_emailService == null) return;

                if (dto.UserId.HasValue)
                {
                    var user = await _context.Users.FindAsync(dto.UserId.Value);
                    if (user != null)
                    {
                        var emailDto = new School_Yathu.DTOs.NotificationEmailDTO
                        {
                            UserEmail = user.Email,
                            UserName = user.Name,
                            Message = dto.Message,
                            Subject = dto.Title,
                            Type = dto.Type ?? "general",
                            Link = dto.Link
                        };
                        await _emailService.SendNotificationEmailAsync(emailDto);
                    }
                }
                else if (!string.IsNullOrEmpty(dto.Role))
                {
                    var users = await _context.Users
                        .Where(u => u.Role == dto.Role && u.IsActive)
                        .ToListAsync();

                    foreach (var user in users)
                    {
                        var emailDto = new School_Yathu.DTOs.NotificationEmailDTO
                        {
                            UserEmail = user.Email,
                            UserName = user.Name,
                            Message = dto.Message,
                            Subject = dto.Title,
                            Type = dto.Type ?? "general",
                            Link = dto.Link
                        };
                        await _emailService.SendNotificationEmailAsync(emailDto);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email notifications: {ex.Message}");
            }
        }

        #endregion
    }
}