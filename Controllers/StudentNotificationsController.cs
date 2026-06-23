using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.Models;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Student")]
    [SwaggerTag("Student Notifications - Manage student notifications")]
    public class StudentNotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public StudentNotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Get all notifications for the logged-in student
        /// </summary>
        [HttpGet("my-notifications")]
        [SwaggerOperation(Summary = "Get my notifications", Description = "Retrieves all notifications for the logged-in student")]
        [SwaggerResponse(200, "List of notifications", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var notifications = await _context.Notifications
                .Where(n => n.StudentId == studentId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Type,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();
            
            return Ok(notifications);
        }
        
        /// <summary>
        /// Get unread notification count
        /// </summary>
        [HttpGet("unread-count")]
        [SwaggerOperation(Summary = "Get unread count", Description = "Retrieves the number of unread notifications for the logged-in student")]
        [SwaggerResponse(200, "Unread count", typeof(object))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var count = await _context.Notifications
                .CountAsync(n => n.StudentId == studentId && !n.IsRead);
            
            return Ok(new { unreadCount = count });
        }
        
        /// <summary>
        /// Mark a notification as read
        /// </summary>
        [HttpPut("{id}/read")]
        [SwaggerOperation(Summary = "Mark notification as read", Description = "Marks a specific notification as read")]
        [SwaggerResponse(200, "Notification marked as read")]
        [SwaggerResponse(404, "Notification not found")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.StudentId == studentId);
            
            if (notification == null)
                return NotFound();
            
            notification.IsRead = true;
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Notification marked as read" });
        }
        
        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        [HttpPut("mark-all-read")]
        [SwaggerOperation(Summary = "Mark all as read", Description = "Marks all notifications as read for the logged-in student")]
        [SwaggerResponse(200, "All notifications marked as read")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var notifications = await _context.Notifications
                .Where(n => n.StudentId == studentId && !n.IsRead)
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