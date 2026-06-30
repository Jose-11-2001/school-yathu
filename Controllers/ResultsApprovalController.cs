using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;  // ✅ Add this using
using School_Yathu.Models;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [SwaggerTag("Results Approval - Approve and manage exam results")]
    public class ResultsApprovalController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResultsApprovalController> _logger;

        public ResultsApprovalController(ApplicationDbContext context, ILogger<ResultsApprovalController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get pending results for approval
        /// </summary>
        [HttpGet("pending-results")]
        [SwaggerOperation(Summary = "Get pending results", Description = "Retrieves all results waiting for headteacher approval")]
        [SwaggerResponse(200, "List of pending results", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetPendingResults()
        {
            var pendingResults = await _context.Marks
                .Include(m => m.Subject)
                .Where(m => m.IsApproved == false && m.TotalScore.HasValue)
                .GroupBy(m => new { m.SubjectId, m.Year, m.Term })
                .Select(g => new
                {
                    SubjectId = g.Key.SubjectId,
                    SubjectName = g.First().Subject != null ? g.First().Subject.Name : "",
                    Year = g.Key.Year,
                    Term = g.Key.Term,
                    StudentCount = g.Count()
                })
                .ToListAsync();

            return Ok(pendingResults);
        }

        /// <summary>
        /// Get results details for a specific subject/term/year
        /// </summary>
        [HttpGet("results-details/{subjectId}/{year}/{term}")]
        [SwaggerOperation(Summary = "Get results details", Description = "Retrieves detailed results for a specific subject, term and year")]
        [SwaggerResponse(200, "Results details", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetResultsDetails(int subjectId, int year, string term)
        {
            var results = await _context.Marks
                .Include(m => m.Student)
                .Where(m => m.SubjectId == subjectId && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                .Select(m => new
                {
                    m.StudentId,
                    StudentName = m.Student != null ? m.Student.FullName : "",
                    AdmissionNumber = m.Student != null ? m.Student.AdmissionNumber : "",
                    m.ContinuousTest1,
                    m.ContinuousTest2,
                    m.EndTermExam,
                    m.TotalScore,
                    m.Grade,
                    m.Remark
                })
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// Approve results for a subject/term/year
        /// </summary>
        [HttpPost("approve-results")]
        [SwaggerOperation(Summary = "Approve results", Description = "Approves results for a specific subject, term and year")]
        [SwaggerResponse(200, "Results approved successfully", typeof(object))]
        [SwaggerResponse(400, "No results found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> ApproveResults([FromBody] ApproveResultsDTO dto)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var marks = await _context.Marks
                .Where(m => m.SubjectId == dto.SubjectId && m.Year == dto.Year && m.Term == dto.Term)
                .ToListAsync();

            if (!marks.Any())
                return BadRequest(new { message = "No results found for this subject/term/year" });

            foreach (var mark in marks)
            {
                mark.IsApproved = true;
                mark.ApprovedAt = DateTime.UtcNow;
                mark.ApprovedByAdminId = adminId;
            }

            await _context.SaveChangesAsync();

            var subject = await _context.Subjects.FindAsync(dto.SubjectId);
            var subjectName = subject?.Name ?? "Unknown Subject";

            var studentIds = marks.Select(m => m.StudentId).Distinct().ToList();

            // Send notifications to students
            foreach (var studentId in studentIds)
            {
                var studentNotification = new Notification
                {
                    Title = "📢 Exam Results Published",
                    Message = $"The results for {subjectName} ({dto.Term} {dto.Year}) have been approved and published. You can now view your results.",
                    Type = "ExamResults",
                    StudentId = studentId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(studentNotification);
            }

            // Send notification to teachers
            var teacherIds = marks.Select(m => m.EnteredByTeacherId).Where(t => t.HasValue).Select(t => t.Value).Distinct().ToList();
            foreach (var teacherId in teacherIds)
            {
                var teacherNotification = new Notification
                {
                    Title = "✅ Results Approved",
                    Message = $"The results for {subjectName} ({dto.Term} {dto.Year}) have been approved by the Headteacher and published to students.",
                    Type = "Success",
                    TeacherId = teacherId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(teacherNotification);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Results for {subjectName} ({dto.Term} {dto.Year}) have been approved and published.",
                studentCount = studentIds.Count,
                teacherCount = teacherIds.Count
            });
        }

        /// <summary>
        /// Get all approved results summary
        /// </summary>
        [HttpGet("approved-results")]
        [SwaggerOperation(Summary = "Get approved results", Description = "Retrieves a summary of all approved results")]
        [SwaggerResponse(200, "List of approved results", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetApprovedResults()
        {
            var approvedResults = await _context.Marks
                .Include(m => m.Subject)
                .Where(m => m.IsApproved == true)
                .GroupBy(m => new { m.SubjectId, m.Year, m.Term })
                .Select(g => new
                {
                    SubjectId = g.Key.SubjectId,
                    SubjectName = g.First().Subject != null ? g.First().Subject.Name : "",
                    Year = g.Key.Year,
                    Term = g.Key.Term,
                    StudentCount = g.Count(),
                    ApprovedAt = g.Max(m => m.ApprovedAt)
                })
                .ToListAsync();

            return Ok(approvedResults);
        }

        /// <summary>
        /// Submit results for approval by teacher
        /// </summary>
        [HttpPost("submit-for-approval")]
        [Authorize(Roles = "Teacher")]
        [SwaggerOperation(Summary = "Submit results for approval", Description = "Teacher submits results to headteacher for approval")]
        public async Task<IActionResult> SubmitForApproval([FromBody] SubmitResultsDTO dto)
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Verify teacher is assigned to this class
                var teacherClass = await _context.Classes
                    .FirstOrDefaultAsync(c => c.TeacherId == teacherId && c.Name == dto.ClassName);

                if (teacherClass == null)
                    return BadRequest(new { message = "You are not assigned to this class" });

                // Get all marks for this class/term/year
                var marks = await _context.Marks
                    .Include(m => m.Student)
                    .Where(m => m.Student != null &&
                               m.Student.Class == dto.ClassName &&
                               m.Year == dto.Year &&
                               m.Term == dto.Term &&
                               m.TotalScore.HasValue)
                    .ToListAsync();

                if (!marks.Any())
                    return BadRequest(new { message = "No marks found for this class/term/year" });

                // Mark them as pending approval
                foreach (var mark in marks)
                {
                    mark.IsApproved = false;
                    mark.EnteredByTeacherId = teacherId;
                }
                await _context.SaveChangesAsync();

                // Send notification to Headteacher (Admin)
                var adminUsers = await _context.Users
                    .Where(u => u.Role == "Admin")
                    .ToListAsync();

                foreach (var admin in adminUsers)
                {
                    var notification = new Notification
                    {
                        Title = "📊 Results Submitted for Approval",
                        Message = $"Teacher {User.Identity?.Name} has submitted results for {dto.ClassName} ({dto.Term} {dto.Year}). Please review and approve.",
                        Type = "ResultsApproval",
                        UserId = admin.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    _context.Notifications.Add(notification);
                }
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Results submitted for approval successfully",
                    studentCount = marks.Select(m => m.StudentId).Distinct().Count(),
                    marksCount = marks.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting results for approval");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }

    // ❌ REMOVE THESE DTOs - They should be in the DTOs folder
    // public class ApproveResultsDTO { ... }
    // public class SubmitResultsDTO { ... }
}