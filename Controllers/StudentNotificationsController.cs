using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.Models;
using System.Security.Claims;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Student")]
    public class StudentNotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public StudentNotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // Get all notifications for the logged-in student
        [HttpGet("my-notifications")]
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
        
        // Get unread notification count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var count = await _context.Notifications
                .CountAsync(n => n.StudentId == studentId && !n.IsRead);
            
            return Ok(new { unreadCount = count });
        }
        
        // Mark a notification as read
        [HttpPut("{id}/read")]
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
        
        // Mark all notifications as read
        [HttpPut("mark-all-read")]
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