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
    [Authorize(Roles = "Teacher")]
    [SwaggerTag("Teacher Marks - Manage student marks")]
    public class TeacherMarksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public TeacherMarksController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Get students assigned to the teacher
        /// </summary>
        [HttpGet("my-students")]
        [SwaggerOperation(Summary = "Get my students", Description = "Retrieves all students assigned to the logged-in teacher")]
        [SwaggerResponse(200, "List of students", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Teacher role required")]
        public async Task<IActionResult> GetMyStudents()
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentYear = DateTime.Now.Year;
            
            var students = await _context.StudentSubjects
                .Include(ss => ss.Student)
                .Include(ss => ss.Subject)
                .Where(ss => ss.TeacherId == teacherId && 
                             ss.IsActive && 
                             ss.AcademicYear == currentYear)
                .Select(ss => new
                {
                    Id = ss.Student != null ? ss.Student.Id : 0,
                    StudentId = ss.Student != null ? ss.Student.Id : 0,
                    AdmissionNumber = ss.Student != null ? ss.Student.AdmissionNumber : "",
                    StudentName = ss.Student != null ? ss.Student.FullName : "",
                    FullName = ss.Student != null ? ss.Student.FullName : "",
                    Class = ss.Student != null ? ss.Student.Class : "",
                    Stream = ss.Student != null ? ss.Student.Stream : "",
                    className = ss.Student != null ? $"{ss.Student.Class} {ss.Student.Stream}" : "",
                    SubjectId = ss.SubjectId,
                    SubjectName = ss.Subject != null ? ss.Subject.Name : ""
                })
                .Distinct()
                .ToListAsync();
            
            if (!students.Any())
            {
                var classTeacherStudents = await _context.Students
                    .Where(s => s.TeacherId == teacherId)
                    .Select(s => new
                    {
                        Id = s.Id,
                        StudentId = s.Id,
                        AdmissionNumber = s.AdmissionNumber,
                        StudentName = s.FullName,
                        FullName = s.FullName,
                        Class = s.Class,
                        Stream = s.Stream,
                        className = $"{s.Class} {s.Stream}",
                        SubjectId = (int?)null,
                        SubjectName = ""
                    })
                    .ToListAsync();
                
                return Ok(classTeacherStudents);
            }
            
            return Ok(students);
        }
        
        /// <summary>
        /// Get student marks for a specific subject
        /// </summary>
        [HttpGet("student-marks/{studentId}/{subjectId}/{year}/{term}")]
        [SwaggerOperation(Summary = "Get student marks", Description = "Retrieves marks for a specific student and subject")]
        [SwaggerResponse(200, "Student marks", typeof(object))]
        [SwaggerResponse(404, "No marks found")]
        [SwaggerResponse(401, "Unauthorized - Teacher role required")]
        public async Task<IActionResult> GetStudentMarks(int studentId, int subjectId, int year, string term)
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var marks = await _context.Marks
                .FirstOrDefaultAsync(m => m.StudentId == studentId &&
                                         m.SubjectId == subjectId &&
                                         m.Year == year &&
                                         m.Term == term &&
                                         m.EnteredByTeacherId == teacherId);
            
            if (marks == null)
                return NotFound(new { message = "No marks found for this student" });
            
            return Ok(new
            {
                marks.ContinuousTest1,
                marks.ContinuousTest2,
                marks.EndTermExam,
                marks.TotalScore,
                marks.Grade,
                marks.Remark
            });
        }
        
        /// <summary>
        /// Enter marks for a student
        /// </summary>
        [HttpPost("enter-marks")]
        [SwaggerOperation(Summary = "Enter marks", Description = "Enters marks for a student with grade calculation")]
        [SwaggerResponse(200, "Marks saved successfully", typeof(object))]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(401, "Unauthorized - Teacher role required")]
        public async Task<IActionResult> EnterMarks([FromBody] MarksEntryDTO dto)
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var student = await _context.Students.FindAsync(dto.StudentId);
            if (student == null)
                return BadRequest(new { message = "Student not found" });
            
            int ct1 = dto.ContinuousTest1 ?? 0;
            int ct2 = dto.ContinuousTest2 ?? 0;
            int endTerm = dto.EndTermExam ?? 0;
            double overallPercentage = (ct1 * 0.20) + (ct2 * 0.20) + (endTerm * 0.60);
            int totalScoreInt = (int)Math.Round(overallPercentage);
            
            var gradeInfo = CalculateGradeBasedOnClass(overallPercentage, student.Class);
            
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
                existingMarks.TotalScore = totalScoreInt;
                existingMarks.Grade = gradeInfo.Grade;
                existingMarks.Remark = gradeInfo.Remark;
                existingMarks.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                return Ok(new { 
                    message = "Marks updated successfully", 
                    totalScore = overallPercentage.ToString("F2"), 
                    grade = gradeInfo.Grade,
                    displayScore = $"{overallPercentage:F2}%"
                });
            }
            
            var marks = new Marks
            {
                StudentId = dto.StudentId,
                SubjectId = dto.SubjectId,
                Year = dto.Year,
                Term = dto.Term,
                ContinuousTest1 = dto.ContinuousTest1,
                ContinuousTest2 = dto.ContinuousTest2,
                EndTermExam = dto.EndTermExam,
                TotalScore = totalScoreInt,
                Grade = gradeInfo.Grade,
                Remark = gradeInfo.Remark,
                EnteredByTeacherId = teacherId,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Marks.Add(marks);
            await _context.SaveChangesAsync();
            
            return Ok(new { 
                message = "Marks saved successfully", 
                totalScore = overallPercentage.ToString("F2"), 
                grade = gradeInfo.Grade,
                displayScore = $"{overallPercentage:F2}%"
            });
        }
        
        /// <summary>
        /// Publish results and notify students
        /// </summary>
        [HttpPost("publish-results/{subjectId}/{year}/{term}")]
        [SwaggerOperation(Summary = "Publish results", Description = "Publishes results and notifies students")]
        [SwaggerResponse(200, "Results published successfully", typeof(object))]
        [SwaggerResponse(400, "No marks found to publish")]
        [SwaggerResponse(401, "Unauthorized - Teacher role required")]
        public async Task<IActionResult> PublishResults(int subjectId, int year, string term)
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var subject = await _context.Subjects.FindAsync(subjectId);
            if (subject == null)
                return BadRequest(new { message = "Subject not found" });
            
            var marks = await _context.Marks
                .Where(m => m.SubjectId == subjectId && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                .ToListAsync();
            
            if (!marks.Any())
                return BadRequest(new { message = "No marks found to publish" });
            
            foreach (var mark in marks)
            {
                var studentNotification = new Notification
                {
                    Title = "📢 Results Published",
                    Message = $"Your results for {subject.Name} ({term} {year}) have been published. Score: {mark.TotalScore}% - Grade: {mark.Grade}",
                    Type = "ExamResults",
                    StudentId = mark.StudentId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(studentNotification);
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(new { 
                message = $"Results for {subject.Name} published successfully. {marks.Count} students have been notified.",
                studentCount = marks.Count
            });
        }
        
        private (string Grade, string Remark) CalculateGradeBasedOnClass(double percentage, string? className)
        {
            if (className != null && (className.Contains("Form 1") || className.Contains("Form 2") || 
                                       className.Contains("Form1") || className.Contains("Form2")))
            {
                if (percentage >= 80) return ("A", "Excellent");
                if (percentage >= 65) return ("B", "Good");
                if (percentage >= 50) return ("C", "Average");
                if (percentage >= 45) return ("D", "Below Average");
                if (percentage >= 40) return ("E", "Poor");
                return ("F", "Fail");
            }
            
            if (percentage >= 85) return ("1 point", "Excellent (85-100%)");
            if (percentage >= 80) return ("2 points", "Very Good (80-84%)");
            if (percentage >= 65) return ("3 points", "Good (65-79%)");
            if (percentage >= 60) return ("4 points", "Above Average (60-64%)");
            if (percentage >= 55) return ("5 points", "Average (55-59%)");
            if (percentage >= 50) return ("6 points", "Satisfactory (50-54%)");
            if (percentage >= 45) return ("7 points", "Below Average (45-49%)");
            if (percentage >= 40) return ("8 points", "Poor (40-44%)");
            return ("9 points", "Fail (0-39%)");
        }
    }
}