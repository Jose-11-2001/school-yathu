using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;
using School_Yathu.Models;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,DeputyHeadTeacher")]
    [SwaggerTag("Deputy Head Teacher - Manage assignments and tasks")]
    public class DeputyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeputyController> _logger;

        public DeputyController(ApplicationDbContext context, ILogger<DeputyController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all deputy assignments (Admin only)
        /// </summary>
        [HttpGet("assignments")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Get all deputy assignments", Description = "Retrieves all assignments for deputy head teachers (Admin only)")]
        public async Task<IActionResult> GetAssignments([FromQuery] string? status)
        {
            try
            {
                var query = _context.DeputyAssignments
                    .Include(da => da.Deputy)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(da => da.Status == status);

                var assignments = await query
                    .Select(da => new
                    {
                        da.Id,
                        da.Task,
                        da.Description,
                        da.Status,
                        da.AssignedAt,
                        da.CompletedAt,
                        DeputyId = da.DeputyId,
                        DeputyName = da.Deputy != null ? da.Deputy.Name : "Unknown"
                    })
                    .OrderByDescending(da => da.AssignedAt)
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deputy assignments");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get assignments for the logged-in deputy
        /// </summary>
        [HttpGet("my-assignments")]
        [Authorize(Roles = "DeputyHeadTeacher")]
        [SwaggerOperation(Summary = "Get my assignments", Description = "Retrieves assignments for the logged-in deputy head teacher")]
        public async Task<IActionResult> GetMyAssignments([FromQuery] string? status)
        {
            try
            {
                var deputyId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var query = _context.DeputyAssignments
                    .Where(da => da.DeputyId == deputyId)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(da => da.Status == status);

                var assignments = await query
                    .Select(da => new
                    {
                        da.Id,
                        da.Task,
                        da.Description,
                        da.Status,
                        da.AssignedAt,
                        da.CompletedAt
                    })
                    .OrderByDescending(da => da.AssignedAt)
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deputy assignments");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Assign a task to a deputy (Admin only)
        /// </summary>
        [HttpPost("assign-task")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Assign task to deputy", Description = "Assigns a task to a deputy head teacher (Admin only)")]
        public async Task<IActionResult> AssignTask([FromBody] AssignDeputyTaskDTO dto)
        {
            try
            {
                var deputy = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == dto.DeputyId && u.Role == "DeputyHeadTeacher");

                if (deputy == null)
                    return BadRequest(new { message = "Deputy head teacher not found" });

                var assignment = new DeputyAssignment
                {
                    DeputyId = dto.DeputyId,
                    Task = dto.Task,
                    Description = dto.Description,
                    Status = "Pending",
                    AssignedAt = DateTime.UtcNow
                };

                _context.DeputyAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                // Send notification to deputy
                var notification = new Notification
                {
                    Title = "New Task Assigned",
                    Message = $"You have been assigned a new task: {dto.Task}",
                    Type = "DeputyTask",
                    UserId = dto.DeputyId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Task assigned successfully", assignmentId = assignment.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning task");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Update assignment status (Deputy only)
        /// </summary>
        [HttpPut("update-status/{assignmentId}")]
        [Authorize(Roles = "DeputyHeadTeacher")]
        [SwaggerOperation(Summary = "Update assignment status", Description = "Updates the status of an assignment (Deputy only)")]
        public async Task<IActionResult> UpdateStatus(int assignmentId, [FromBody] UpdateAssignmentStatusDTO dto)
        {
            try
            {
                var deputyId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var assignment = await _context.DeputyAssignments
                    .FirstOrDefaultAsync(da => da.Id == assignmentId && da.DeputyId == deputyId);

                if (assignment == null)
                    return NotFound(new { message = "Assignment not found" });

                assignment.Status = dto.Status;
                if (dto.Status == "Completed")
                    assignment.CompletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send notification to Admin
                var adminUsers = await _context.Users
                    .Where(u => u.Role == "Admin")
                    .ToListAsync();

                foreach (var admin in adminUsers)
                {
                    var notification = new Notification
                    {
                        Title = "Task Status Updated",
                        Message = $"Task '{assignment.Task}' has been marked as {dto.Status}",
                        Type = "DeputyTask",
                        UserId = admin.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    _context.Notifications.Add(notification);
                }
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Task status updated to {dto.Status}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating assignment status");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get deputy dashboard statistics
        /// </summary>
        [HttpGet("dashboard-stats")]
        [Authorize(Roles = "DeputyHeadTeacher")]
        [SwaggerOperation(Summary = "Get deputy dashboard stats", Description = "Retrieves statistics for the deputy dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var deputyId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var totalAssignments = await _context.DeputyAssignments
                    .CountAsync(da => da.DeputyId == deputyId);

                var pendingAssignments = await _context.DeputyAssignments
                    .CountAsync(da => da.DeputyId == deputyId && da.Status == "Pending");

                var inProgressAssignments = await _context.DeputyAssignments
                    .CountAsync(da => da.DeputyId == deputyId && da.Status == "InProgress");

                var completedAssignments = await _context.DeputyAssignments
                    .CountAsync(da => da.DeputyId == deputyId && da.Status == "Completed");

                var recentAssignments = await _context.DeputyAssignments
                    .Where(da => da.DeputyId == deputyId)
                    .OrderByDescending(da => da.AssignedAt)
                    .Take(5)
                    .Select(da => new
                    {
                        da.Id,
                        da.Task,
                        da.Status,
                        da.AssignedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    TotalAssignments = totalAssignments,
                    PendingAssignments = pendingAssignments,
                    InProgressAssignments = inProgressAssignments,
                    CompletedAssignments = completedAssignments,
                    RecentAssignments = recentAssignments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }

    public class AssignDeputyTaskDTO
    {
        public int DeputyId { get; set; }
        public string Task { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateAssignmentStatusDTO
    {
        public string Status { get; set; } = "InProgress"; // Pending, InProgress, Completed
    }
}