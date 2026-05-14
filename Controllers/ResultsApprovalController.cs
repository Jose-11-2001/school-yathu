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
    [Authorize(Roles = "Admin")]
    public class ResultsApprovalController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public ResultsApprovalController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // Get pending results for approval
        [HttpGet("pending-results")]
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
        
        // Get results details for a specific subject/term/year
        [HttpGet("results-details/{subjectId}/{year}/{term}")]
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
        
        // Approve results for a subject/term/year
        [HttpPost("approve-results")]
        public async Task<IActionResult> ApproveResults([FromBody] ApproveResultsDTO dto)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var marks = await _context.Marks
                .Where(m => m.SubjectId == dto.SubjectId && m.Year == dto.Year && m.Term == dto.Term)
                .ToListAsync();
            
            if (!marks.Any())
                return BadRequest(new { message = "No results found for this subject/term/year" });
            
            // Mark results as approved
            foreach (var mark in marks)
            {
                mark.IsApproved = true;
                mark.ApprovedAt = DateTime.UtcNow;
                mark.ApprovedByAdminId = adminId;
            }
            
            await _context.SaveChangesAsync();
            
            // Get subject name
            var subject = await _context.Subjects.FindAsync(dto.SubjectId);
            var subjectName = subject?.Name ?? "Unknown Subject";
            
            // Get all students who have these marks
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
        
        // Get all approved results summary
        [HttpGet("approved-results")]
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
    }
    
    public class ApproveResultsDTO
    {
        public int SubjectId { get; set; }
        public int Year { get; set; }
        public string Term { get; set; } = string.Empty;
    }
}