
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;
using School_Yathu.Models;
using System.Security.Claims;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Teacher")]
    public class TeacherMarksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public TeacherMarksController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // Get students for teacher's subjects
        [HttpGet("my-students")]
        public async Task<IActionResult> GetMyStudents()
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Get subjects taught by this teacher
            var teacherSubjects = await _context.TeacherSubjects
                .Where(ts => ts.TeacherId == teacherId)
                .Select(ts => ts.SubjectId)
                .ToListAsync();
            
            // Get class IDs where teacher teaches
            var classIds = await _context.ClassSubjects
                .Where(cs => teacherSubjects.Contains(cs.SubjectId))
                .Select(cs => cs.ClassId)
                .Distinct()
                .ToListAsync();
            
            // Get students in those classes
            var students = await _context.Students
                .Where(s => classIds.Contains(s.ClassId ?? 0))
                .Select(s => new
                {
                    s.Id,
                    s.AdmissionNumber,
                    s.FullName,
                    ClassName = s.Class != null ? s.Class.Name : null,
                    s.ClassId
                })
                .ToListAsync();
            
            return Ok(students);
        }
        
        // Enter marks for a student
        [HttpPost("enter-marks")]
        public async Task<IActionResult> EnterMarks([FromBody] MarksEntryDTO dto)
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Verify teacher teaches this subject
            var isAuthorized = await _context.TeacherSubjects
                .AnyAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == dto.SubjectId);
            
            if (!isAuthorized)
                return BadRequest(new { message = "You are not authorized to enter marks for this subject" });
            
            var student = await _context.Students.FindAsync(dto.StudentId);
            if (student == null)
                return BadRequest(new { message = "Student not found" });
            
            // Calculate total (20% + 20% + 60%)
            int ct1 = dto.ContinuousTest1 ?? 0;
            int ct2 = dto.ContinuousTest2 ?? 0;
            int endTerm = dto.EndTermExam ?? 0;
            int totalScore = (int)((ct1 * 0.20) + (ct2 * 0.20) + (endTerm * 0.60));
            var gradeInfo = CalculateGrade(totalScore);
            
            // Check if marks already exist
            var existingMarks = await _context.Marks
                .FirstOrDefaultAsync(m => m.StudentId == dto.StudentId &&
                                         m.SubjectId == dto.SubjectId &&
                                         m.Year == dto.Year &&
                                         m.Term == dto.Term);
            
            if (existingMarks != null)
            {
                existingMarks.ContinuousTest1 = dto.ContinuousTest1;
                existingMarks.ContinuousTest2 = dto.ContinuousTest2;
                existingMarks.EndTermExam = dto.EndTermExam;
                existingMarks.TotalScore = totalScore;
                existingMarks.Grade = gradeInfo.Grade;
                existingMarks.Remark = gradeInfo.Remark;
                existingMarks.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                return Ok(new { message = "Marks updated successfully", totalScore = totalScore, grade = gradeInfo.Grade });
            }
            
            var marks = new Marks
            {
                StudentId = dto.StudentId,
                SubjectId = dto.SubjectId,
                ClassId = student.ClassId ?? 0,
                Year = dto.Year,
                Term = dto.Term,
                ContinuousTest1 = dto.ContinuousTest1,
                ContinuousTest2 = dto.ContinuousTest2,
                EndTermExam = dto.EndTermExam,
                TotalScore = totalScore,
                Grade = gradeInfo.Grade,
                Remark = gradeInfo.Remark,
                EnteredByTeacherId = teacherId,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Marks.Add(marks);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Marks saved successfully", totalScore = totalScore, grade = gradeInfo.Grade });
        }
        
        // Publish results and notify students
        [HttpPost("publish-results/{subjectId}/{year}/{term}")]
        public async Task<IActionResult> PublishResults(int subjectId, int year, string term)
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var isAuthorized = await _context.TeacherSubjects
                .AnyAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == subjectId);
            
            if (!isAuthorized)
                return BadRequest(new { message = "You are not authorized to publish results for this subject" });
            
            var subject = await _context.Subjects.FindAsync(subjectId);
            if (subject == null)
                return BadRequest(new { message = "Subject not found" });
            
            var studentIds = await _context.Marks
                .Where(m => m.SubjectId == subjectId && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                .Select(m => m.StudentId)
                .Distinct()
                .ToListAsync();
            
            // Mark as pending approval
            var marks = await _context.Marks
                .Where(m => m.SubjectId == subjectId && m.Year == year && m.Term == term)
                .ToListAsync();
            
            foreach (var mark in marks)
            {
                mark.IsApproved = false;
            }
            await _context.SaveChangesAsync();
            
            return Ok(new { message = $"Results for {subject.Name} are pending Headteacher approval. {studentIds.Count} students will be notified after approval." });
        }
        
        // Get teacher's notifications
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var notifications = await _context.Notifications
                .Where(n => n.TeacherId == teacherId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
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
        
        [HttpPut("notifications/{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.TeacherId == teacherId);
            
            if (notification == null)
                return NotFound();
            
            notification.IsRead = true;
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Notification marked as read" });
        }
        
        [HttpGet("notifications/unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var count = await _context.Notifications
                .CountAsync(n => n.TeacherId == teacherId && !n.IsRead);
            
            return Ok(new { unreadCount = count });
        }
        
        private (string Grade, string Remark) CalculateGrade(int totalScore)
        {
            if (totalScore >= 90) return ("A+", "Excellent!");
            if (totalScore >= 80) return ("A", "Very Good!");
            if (totalScore >= 75) return ("A-", "Good!");
            if (totalScore >= 70) return ("B+", "Above Average");
            if (totalScore >= 65) return ("B", "Satisfactory");
            if (totalScore >= 60) return ("B-", "Average");
            if (totalScore >= 55) return ("C+", "Fair");
            if (totalScore >= 50) return ("C", "Pass");
            if (totalScore >= 45) return ("C-", "Below Average");
            if (totalScore >= 40) return ("D", "Needs Improvement");
            return ("E", "Poor");
        }
    }
}