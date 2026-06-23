
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;
using School_Yathu.Models;
using School_Yathu.Services;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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

        // GET: api/notifications/student
        [HttpGet("student")]
        public async Task<IActionResult> GetStudentNotifications()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null || user.Role != "Student")
                return Unauthorized();

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.AdmissionNumber == user.Email.Split('@')[0]);

            if (student == null)
                return Ok(new List<NotificationDTO>());

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId || n.Role == "Student" || n.SpecificStudentId == student.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDTO
                {
                    Id = n.Id,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    Type = n.Type,
                    Link = n.Link
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // GET: api/notifications/teacher
        [HttpGet("teacher")]
        public async Task<IActionResult> GetTeacherNotifications()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null || user.Role != "Teacher")
                return Unauthorized();

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.Email == user.Email);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId || n.Role == "Teacher" || n.SpecificTeacherId == teacher?.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDTO
                {
                    Id = n.Id,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    Type = n.Type,
                    Link = n.Link
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // GET: api/notifications/admin
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminNotifications()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId || n.Role == "Admin")
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDTO
                {
                    Id = n.Id,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    Type = n.Type,
                    Link = n.Link
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // POST: api/notifications/send
        [HttpPost("send")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationDTO dto)
        {
            try
            {
                // Save notification to database
                var notification = new Notification
                {
                    Message = dto.Message,
                    Type = dto.Type ?? "general",
                    Role = dto.Role,
                    SpecificStudentId = dto.StudentId,
                    SpecificTeacherId = dto.TeacherId,
                    Link = dto.Link,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send email notifications
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
                                users.Add(user);
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

                // Send emails
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

        // PUT: api/notifications/{id}/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification marked as read" });
        }

        // PUT: api/notifications/read-all
        [HttpPut("read-all")]
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

            return Ok(new { message = "All notifications marked as read" });
        }
    }
}