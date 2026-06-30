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
        private readonly ILogger<TeacherMarksController> _logger;

        public TeacherMarksController(ApplicationDbContext context, ILogger<TeacherMarksController> logger)
        {
            _context = context;
            _logger = logger;
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
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my students for teacher");
                return StatusCode(500, new { message = "An error occurred while retrieving students" });
            }
        }
        
        /// <summary>
        /// Get student marks for a specific subject
        /// </summary>
        [HttpGet("student-marks/{studentId}/{subjectId}/{year}/{term}")]
        [SwaggerOperation(Summary = "Get student marks", Description = "Retrieves marks for a specific student and subject")]
        [SwaggerResponse(200, "Student marks", typeof(MarksResponseDTO))]
        [SwaggerResponse(404, "No marks found")]
        [SwaggerResponse(401, "Unauthorized - Teacher role required")]
        public async Task<IActionResult> GetStudentMarks(int studentId, int subjectId, int year, string term)
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                var marks = await _context.Marks
                    .Include(m => m.Student)
                    .Include(m => m.Subject)
                    .FirstOrDefaultAsync(m => m.StudentId == studentId &&
                                             m.SubjectId == subjectId &&
                                             m.Year == year &&
                                             m.Term == term &&
                                             m.EnteredByTeacherId == teacherId);
                
                if (marks == null)
                    return NotFound(new { message = "No marks found for this student" });
                
                var response = new MarksResponseDTO
                {
                    Id = marks.Id,
                    StudentId = marks.StudentId,
                    StudentName = marks.Student != null ? marks.Student.FullName : "",
                    AdmissionNumber = marks.Student != null ? marks.Student.AdmissionNumber : "",
                    SubjectName = marks.Subject != null ? marks.Subject.Name : "",
                    ContinuousTest1 = marks.ContinuousTest1,
                    ContinuousTest2 = marks.ContinuousTest2,
                    EndTermExam = marks.EndTermExam,
                    TotalScore = marks.TotalScore,
                    Grade = marks.Grade,
                    Remark = marks.Remark,
                    Year = marks.Year,
                    Term = marks.Term,
                    CreatedAt = marks.CreatedAt
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student marks for student {StudentId}, subject {SubjectId}", studentId, subjectId);
                return StatusCode(500, new { message = "An error occurred while retrieving marks" });
            }
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
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                var student = await _context.Students.FindAsync(dto.StudentId);
                if (student == null)
                    return BadRequest(new { message = "Student not found" });
                
                // Calculate weighted score: Test1(20%) + Test2(20%) + EndTerm(60%)
                double ct1 = dto.ContinuousTest1 ?? 0;
                double ct2 = dto.ContinuousTest2 ?? 0;
                double endTerm = dto.EndTermExam ?? 0;
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
                    
                    _logger.LogInformation("Marks updated for student {StudentId}, subject {SubjectId}", dto.StudentId, dto.SubjectId);
                    
                    return Ok(new 
                    { 
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
                
                _logger.LogInformation("Marks saved for student {StudentId}, subject {SubjectId}", dto.StudentId, dto.SubjectId);
                
                return Ok(new 
                { 
                    message = "Marks saved successfully", 
                    totalScore = overallPercentage.ToString("F2"), 
                    grade = gradeInfo.Grade,
                    displayScore = $"{overallPercentage:F2}%"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error entering marks for student {StudentId}", dto.StudentId);
                return StatusCode(500, new { message = "An error occurred while saving marks" });
            }
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
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                var subject = await _context.Subjects.FindAsync(subjectId);
                if (subject == null)
                    return BadRequest(new { message = "Subject not found" });
                
                var marks = await _context.Marks
                    .Include(m => m.Student)
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
                
                _logger.LogInformation("Results published for subject {SubjectId}, {Term} {Year}", subjectId, term, year);
                
                return Ok(new 
                { 
                    message = $"Results for {subject.Name} published successfully. {marks.Count} students have been notified.",
                    studentCount = marks.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing results for subject {SubjectId}", subjectId);
                return StatusCode(500, new { message = "An error occurred while publishing results" });
            }
        }
        
        /// <summary>
        /// Submit results for approval to Headteacher
        /// </summary>
        [HttpPost("submit-for-approval")]
        [SwaggerOperation(Summary = "Submit results for approval", Description = "Teacher submits results to headteacher for approval")]
        [SwaggerResponse(200, "Results submitted for approval", typeof(object))]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(401, "Unauthorized - Teacher role required")]
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

                _logger.LogInformation("Results submitted for approval by teacher {TeacherId} for class {ClassName}", teacherId, dto.ClassName);

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
        
        /// <summary>
        /// Get all marks entered by the teacher for a specific term/year
        /// </summary>
        [HttpGet("my-marks")]
        [SwaggerOperation(Summary = "Get my marks", Description = "Retrieves all marks entered by the teacher for a specific term/year")]
        [SwaggerResponse(200, "List of marks", typeof(List<MarksResponseDTO>))]
        [SwaggerResponse(401, "Unauthorized - Teacher role required")]
        public async Task<IActionResult> GetMyMarks([FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var marks = await _context.Marks
                    .Include(m => m.Student)
                    .Include(m => m.Subject)
                    .Where(m => m.EnteredByTeacherId == teacherId && 
                               m.Year == year && 
                               m.Term == term &&
                               m.TotalScore.HasValue)
                    .Select(m => new MarksResponseDTO
                    {
                        Id = m.Id,
                        StudentId = m.StudentId,
                        StudentName = m.Student != null ? m.Student.FullName : "",
                        AdmissionNumber = m.Student != null ? m.Student.AdmissionNumber : "",
                        SubjectName = m.Subject != null ? m.Subject.Name : "",
                        ContinuousTest1 = m.ContinuousTest1,
                        ContinuousTest2 = m.ContinuousTest2,
                        EndTermExam = m.EndTermExam,
                        TotalScore = m.TotalScore,
                        Grade = m.Grade,
                        Remark = m.Remark,
                        Year = m.Year,
                        Term = m.Term,
                        CreatedAt = m.CreatedAt
                    })
                    .ToListAsync();

                return Ok(marks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my marks");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
        
        private (string Grade, string Remark) CalculateGradeBasedOnClass(double percentage, string? className)
        {
            // Form 1 & Form 2 - Letter Grades
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
            
            // Form 3 & Form 4 - Points System
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