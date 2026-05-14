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
            
            // Get all students who have marks in these subjects or are in classes where teacher teaches
            var students = await _context.Students
                .Include(s => s.Class)
                .Where(s => s.ClassId != null)
                .ToListAsync();
            
            // Filter students based on class allocations
            var classIds = await _context.ClassSubjects
                .Where(cs => teacherSubjects.Contains(cs.SubjectId))
                .Select(cs => cs.ClassId)
                .Distinct()
                .ToListAsync();
            
            var filteredStudents = students.Where(s => classIds.Contains(s.ClassId ?? 0)).ToList();
            
            return Ok(filteredStudents.Select(s => new
            {
                s.Id,
                s.AdmissionNumber,
                s.FullName,
                Class = s.Class != null ? s.Class.Name : null,
                Stream = s.Class != null ? s.Class.Stream : null
            }));
        }
        
        // Get marks for a specific student
        [HttpGet("student-marks/{studentId}")]
        public async Task<IActionResult> GetStudentMarks(int studentId, [FromQuery] int year, [FromQuery] string term)
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Check if teacher is authorized to see this student's marks
            var teacherSubjects = await _context.TeacherSubjects
                .Where(ts => ts.TeacherId == teacherId)
                .Select(ts => ts.SubjectId)
                .ToListAsync();
            
            var marks = await _context.Marks
                .Include(m => m.Subject)
                .Where(m => m.StudentId == studentId && 
                           m.Year == year && 
                           m.Term == term &&
                           teacherSubjects.Contains(m.SubjectId))
                .Select(m => new MarksResponseDTO
                {
                    Id = m.Id,
                    StudentId = m.StudentId,
                    SubjectName = m.Subject != null ? m.Subject.Name : "",
                    ContinuousTest1 = m.ContinuousTest1,
                    ContinuousTest2 = m.ContinuousTest2,
                    EndTermExam = m.EndTermExam,
                    TotalScore = m.TotalScore,
                    Grade = m.Grade,
                    Remark = m.Remark,
                    Year = m.Year,
                    Term = m.Term
                })
                .ToListAsync();
            
            return Ok(marks);
        }
        
        // Enter or update marks for a student
        [HttpPost("enter-marks")]
        public async Task<IActionResult> EnterMarks([FromBody] MarksEntryDTO dto)
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Verify teacher teaches this subject
            var isAuthorized = await _context.TeacherSubjects
                .AnyAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == dto.SubjectId);
            
            if (!isAuthorized)
                return BadRequest(new { message = "You are not authorized to enter marks for this subject" });
            
            // Get student to verify they exist
            var student = await _context.Students.FindAsync(dto.StudentId);
            if (student == null)
                return BadRequest(new { message = "Student not found" });
            
            // Calculate total based on weighting: Continuous Test 1 = 20%, Continuous Test 2 = 20%, End Term = 60%
            int? totalScore = null;
            if (dto.ContinuousTest1.HasValue || dto.ContinuousTest2.HasValue || dto.EndTermExam.HasValue)
            {
                int ct1Score = dto.ContinuousTest1 ?? 0;
                int ct2Score = dto.ContinuousTest2 ?? 0;
                int endTermScore = dto.EndTermExam ?? 0;
                
                totalScore = (int)((ct1Score * 0.20) + (ct2Score * 0.20) + (endTermScore * 0.60));
            }
            
            var gradeInfo = CalculateGrade(totalScore ?? 0);
            
            // Check if marks already exist
            var existingMarks = await _context.Marks
                .FirstOrDefaultAsync(m => m.StudentId == dto.StudentId &&
                                         m.SubjectId == dto.SubjectId &&
                                         m.Year == dto.Year &&
                                         m.Term == dto.Term);
            
            if (existingMarks != null)
            {
                // Update existing marks
                existingMarks.ContinuousTest1 = dto.ContinuousTest1;
                existingMarks.ContinuousTest2 = dto.ContinuousTest2;
                existingMarks.EndTermExam = dto.EndTermExam;
                existingMarks.TotalScore = totalScore;
                existingMarks.Grade = gradeInfo.Grade;
                existingMarks.Remark = gradeInfo.Remark;
                existingMarks.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                // Send notification about marks update
                await CreateNotification(student.Id, null, "Marks Updated", $"Your marks for {await GetSubjectName(dto.SubjectId)} have been updated.", "Info");
            }
            else
            {
                // Create new marks entry
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
                
                // Send notification about new marks
                await CreateNotification(student.Id, null, "New Marks Entered", $"Your marks for {await GetSubjectName(dto.SubjectId)} have been recorded.", "Success");
            }
            
            // Also send notification to teacher that marks were saved
            await CreateNotification(null, teacherId, "Marks Saved", $"You have successfully saved marks for {student.FullName} in {await GetSubjectName(dto.SubjectId)}.", "Success");
            
            return Ok(new { 
                message = "Marks saved successfully", 
                totalScore = totalScore,
                grade = gradeInfo.Grade,
                remark = gradeInfo.Remark
            });
        }
        
        // Get all students with marks summary for a subject
        [HttpGet("class-marks/{classId}/{subjectId}/{year}/{term}")]
        public async Task<IActionResult> GetClassMarks(int classId, int subjectId, int year, string term)
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Verify teacher teaches this subject
            var isAuthorized = await _context.TeacherSubjects
                .AnyAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == subjectId);
            
            if (!isAuthorized)
                return BadRequest(new { message = "You are not authorized to view marks for this subject" });
            
            var students = await _context.Students
                .Where(s => s.ClassId == classId)
                .ToListAsync();
            
            var results = new List<object>();
            
            foreach (var student in students)
            {
                var marks = await _context.Marks
                    .FirstOrDefaultAsync(m => m.StudentId == student.Id && 
                                              m.SubjectId == subjectId && 
                                              m.Year == year && 
                                              m.Term == term);
                
                results.Add(new
                {
                    student.Id,
                    student.AdmissionNumber,
                    student.FullName,
                    ContinuousTest1 = marks?.ContinuousTest1,
                    ContinuousTest2 = marks?.ContinuousTest2,
                    EndTermExam = marks?.EndTermExam,
                    TotalScore = marks?.TotalScore,
                    Grade = marks?.Grade,
                    Status = marks != null ? "Recorded" : "Pending"
                });
            }
            
            return Ok(results);
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
                .Select(n => new NotificationDTO
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();
            
            return Ok(notifications);
        }
        
        // Mark notification as read
        [HttpPut("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
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
        
        // Mark all notifications as read
        [HttpPut("notifications/mark-all-read")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var notifications = await _context.Notifications
                .Where(n => n.TeacherId == teacherId && !n.IsRead)
                .ToListAsync();
            
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "All notifications marked as read" });
        }
        
        // Get unread count
        [HttpGet("notifications/unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var count = await _context.Notifications
                .CountAsync(n => n.TeacherId == teacherId && !n.IsRead);
            
            return Ok(new { unreadCount = count });
        }
        
        // Publish exam results notification (send to students)
        [HttpPost("publish-results/{subjectId}/{year}/{term}")]
        public async Task<IActionResult> PublishResults(int subjectId, int year, string term)
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Verify teacher teaches this subject
            var isAuthorized = await _context.TeacherSubjects
                .AnyAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == subjectId);
            
            if (!isAuthorized)
                return BadRequest(new { message = "You are not authorized to publish results for this subject" });
            
            var subject = await _context.Subjects.FindAsync(subjectId);
            if (subject == null)
                return BadRequest(new { message = "Subject not found" });
            
            // Get all students who have marks for this subject
            var studentsWithMarks = await _context.Marks
                .Where(m => m.SubjectId == subjectId && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                .Select(m => m.StudentId)
                .Distinct()
                .ToListAsync();
            
            // Send notification to each student
            foreach (var studentId in studentsWithMarks)
            {
                await CreateNotification(studentId, null, $"📢 Results Published for {subject.Name}", 
                    $"The {term} results for {subject.Name} have been published. Check your marks now!", "ExamResults");
            }
            
            // Send notification to teacher
            await CreateNotification(null, teacherId, "Results Published", 
                $"You have successfully published the {term} results for {subject.Name}. Students can now view their marks.", "Success");
            
            return Ok(new { message = $"Results published successfully. Notifications sent to {studentsWithMarks.Count} students." });
        }
        
        private async Task CreateNotification(int? studentId, int? teacherId, string title, string message, string type)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                TeacherId = teacherId,
                StudentId = studentId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
        
        private async Task<string> GetSubjectName(int subjectId)
        {
            var subject = await _context.Subjects.FindAsync(subjectId);
            return subject?.Name ?? "Unknown Subject";
        }
        
        private (string Grade, string Remark) CalculateGrade(int totalScore)
        {
            if (totalScore >= 90) return ("A+", "Excellent performance! Outstanding work.");
            if (totalScore >= 80) return ("A", "Very good performance! Keep it up.");
            if (totalScore >= 75) return ("A-", "Good performance! Well done.");
            if (totalScore >= 70) return ("B+", "Above average performance.");
            if (totalScore >= 65) return ("B", "Satisfactory performance.");
            if (totalScore >= 60) return ("B-", "Average performance. Can improve.");
            if (totalScore >= 55) return ("C+", "Fair performance. Need more effort.");
            if (totalScore >= 50) return ("C", "Passing performance. Work harder.");
            if (totalScore >= 45) return ("C-", "Below average. Requires improvement.");
            if (totalScore >= 40) return ("D", "Needs significant improvement.");
            return ("E", "Poor performance. Please work harder next time.");
        }
    }
}
