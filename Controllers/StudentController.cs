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
    public class StudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _context.Students
                .Select(s => new
                {
                    s.Id,
                    s.AdmissionNumber,
                    s.FullName,
                    s.Class,
                    s.Stream
                })
                .ToListAsync();
            return Ok(students);
        }
        
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });
                
                var teacherId = int.Parse(userIdClaim.Value);
                
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.AdmissionNumber == dto.AdmissionNumber);
                    
                if (existingStudent != null)
                    return BadRequest(new { message = $"Student with admission number '{dto.AdmissionNumber}' already exists" });
                
                var student = new Student
                {
                    AdmissionNumber = dto.AdmissionNumber,
                    FullName = dto.FullName,
                    Class = dto.Class,
                    Stream = dto.Stream,
                    TeacherId = teacherId,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Students.Add(student);
                await _context.SaveChangesAsync();
                
                return Ok(new { 
                    message = "Student added successfully", 
                    student = new
                    {
                        student.Id,
                        student.AdmissionNumber,
                        student.FullName,
                        student.Class,
                        student.Stream
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
        
        // Get subjects allocated to this student by admin
        [HttpGet("my-subjects")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMySubjects()
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentYear = DateTime.Now.Year;
            
            var subjects = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                .Include(ss => ss.Teacher)
                .Where(ss => ss.StudentId == studentId && 
                             ss.IsActive && 
                             ss.AcademicYear == currentYear)
                .Select(ss => new
                {
                    ss.Id,
                    ss.SubjectId,
                    SubjectName = ss.Subject != null ? ss.Subject.Name : "",
                    SubjectCode = ss.Subject != null ? ss.Subject.Code : "",
                    ss.TeacherId,
                    TeacherName = ss.Teacher != null ? ss.Teacher.Name : "",
                    TeacherEmail = ss.Teacher != null ? ss.Teacher.Email : "",
                    ss.AcademicYear,
                    ss.Term,
                    HasMarks = _context.Marks.Any(m => m.StudentId == studentId && 
                                                       m.SubjectId == ss.SubjectId && 
                                                       m.Year == ss.AcademicYear)
                })
                .ToListAsync();
            
            if (!subjects.Any())
            {
                // Fallback: Get subjects from class subjects if no direct allocations
                var student = await _context.Students.FindAsync(studentId);
                if (student != null)
                {
                    var classSubjects = await _context.ClassSubjects
                        .Include(cs => cs.Subject)
                        .Include(cs => cs.Teacher)
                        .Where(cs => cs.Class != null && cs.Class.Name == student.Class)
                        .Select(cs => new
                        {
                            Id = 0,
                            SubjectId = cs.Subject != null ? cs.Subject.Id : 0,
                            SubjectName = cs.Subject != null ? cs.Subject.Name : "",
                            SubjectCode = cs.Subject != null ? cs.Subject.Code : "",
                            TeacherId = cs.TeacherId,
                            TeacherName = cs.Teacher != null ? cs.Teacher.Name : "",
                            TeacherEmail = cs.Teacher != null ? cs.Teacher.Email : "",
                            AcademicYear = currentYear,
                            Term = "Term 1",
                            HasMarks = false
                        })
                        .ToListAsync();
                    
                    return Ok(classSubjects);
                }
            }
            
            return Ok(subjects);
        }
        
        // Get marks for a specific subject
        [HttpGet("my-marks/{subjectId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyMarks(int subjectId, [FromQuery] int year, [FromQuery] string term)
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var marks = await _context.Marks
                .Include(m => m.Subject)
                .FirstOrDefaultAsync(m => m.StudentId == studentId && 
                                          m.SubjectId == subjectId && 
                                          m.Year == year && 
                                          m.Term == term);
            
            if (marks == null)
                return NotFound(new { message = "No marks found for this subject" });
            
            return Ok(new
            {
                marks.ContinuousTest1,
                marks.ContinuousTest2,
                marks.EndTermExam,
                marks.TotalScore,
                marks.Grade,
                marks.Remark,
                SubjectName = marks.Subject != null ? marks.Subject.Name : "",
                SubjectCode = marks.Subject != null ? marks.Subject.Code : ""
            });
        }
        
        // Get all marks for a student (by year and term)
        [HttpGet("marks/{studentId}")]
        public async Task<IActionResult> GetStudentMarks(int studentId, [FromQuery] int year, [FromQuery] string term)
        {
            var marks = await _context.Marks
                .Include(m => m.Subject)
                .Where(m => m.StudentId == studentId && m.Year == year && m.Term == term)
                .Select(m => new
                {
                    m.Id,
                    SubjectName = m.Subject != null ? m.Subject.Name : "",
                    SubjectCode = m.Subject != null ? m.Subject.Code : "",
                    m.ContinuousTest1,
                    m.ContinuousTest2,
                    m.EndTermExam,
                    m.TotalScore,
                    m.Grade,
                    m.Remark,
                    m.Year,
                    m.Term
                })
                .ToListAsync();
            
            if (!marks.Any())
                return NotFound(new { message = "No marks found for this student" });
            
            // Calculate overall performance
            var totalScore = marks.Sum(m => m.TotalScore ?? 0);
            var averageScore = marks.Average(m => m.TotalScore ?? 0);
            var overallGrade = GetGrade(averageScore);
            
            return Ok(new
            {
                Subjects = marks,
                Summary = new
                {
                    TotalSubjects = marks.Count,
                    TotalScore = totalScore,
                    AverageScore = Math.Round(averageScore, 2),
                    OverallGrade = overallGrade
                }
            });
        }
        
        // Get student rank in class
        [HttpGet("rank/{studentId}")]
        public async Task<IActionResult> GetStudentRank(int studentId, [FromQuery] int year, [FromQuery] string term)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound(new { message = "Student not found" });
            
            var studentMarks = await _context.Marks
                .Where(m => m.StudentId == studentId && m.Year == year && m.Term == term)
                .ToListAsync();
            
            if (!studentMarks.Any())
                return Ok(new { message = "No marks found" });
            
            var total = studentMarks.Sum(m => m.TotalScore ?? 0);
            var average = studentMarks.Average(m => m.TotalScore ?? 0);
            var grade = GetGrade(average);
            
            // Get all students in the same class
            var classStudents = await _context.Students
                .Where(s => s.Class == student.Class && s.Stream == student.Stream)
                .ToListAsync();
            
            var studentScores = new List<(int StudentId, string Name, int TotalScore)>();
            
            foreach (var s in classStudents)
            {
                var marks = await _context.Marks
                    .Where(m => m.StudentId == s.Id && m.Year == year && m.Term == term)
                    .SumAsync(m => m.TotalScore ?? 0);
                studentScores.Add((s.Id, s.FullName, marks));
            }
            
            var rankedStudents = studentScores
                .OrderByDescending(s => s.TotalScore)
                .Select((s, index) => new { Rank = index + 1, s.StudentId, s.Name, s.TotalScore })
                .ToList();
            
            var position = rankedStudents.FirstOrDefault(r => r.StudentId == studentId)?.Rank ?? 0;
            
            return Ok(new
            {
                student.AdmissionNumber,
                student.FullName,
                student.Class,
                student.Stream,
                TotalMarks = total,
                Average = Math.Round(average, 2),
                Position = position,
                TotalStudents = classStudents.Count,
                Grade = grade,
                ClassRankings = rankedStudents.Take(10) // Top 10 students
            });
        }
        
        // Get student dashboard summary
        [HttpGet("dashboard")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentDashboard()
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentYear = DateTime.Now.Year;
            
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound(new { message = "Student not found" });
            
            // Get allocated subjects
            var subjects = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                .Include(ss => ss.Teacher)
                .Where(ss => ss.StudentId == studentId && ss.IsActive && ss.AcademicYear == currentYear)
                .ToListAsync();
            
            // Get marks for current year
            var marks = await _context.Marks
                .Include(m => m.Subject)
                .Where(m => m.StudentId == studentId && m.Year == currentYear)
                .ToListAsync();
            
            // Calculate performance metrics
            var totalScore = marks.Sum(m => m.TotalScore ?? 0);
            var averageScore = marks.Any() ? marks.Average(m => m.TotalScore ?? 0) : 0;
            
            // Get notifications
            var notifications = await _context.Notifications
                .Where(n => n.StudentId == studentId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();
            
            return Ok(new
            {
                Student = new
                {
                    student.Id,
                    student.AdmissionNumber,
                    student.FullName,
                    student.Class,
                    student.Stream
                },
                Subjects = subjects.Select(s => new
                {
                    s.SubjectId,
                    SubjectName = s.Subject != null ? s.Subject.Name : "",
                    TeacherName = s.Teacher != null ? s.Teacher.Name : "",
                    HasMarks = marks.Any(m => m.SubjectId == s.SubjectId)
                }),
                Performance = new
                {
                    TotalSubjects = subjects.Count,
                    SubjectsWithMarks = marks.Select(m => m.SubjectId).Distinct().Count(),
                    TotalScore = totalScore,
                    AverageScore = Math.Round(averageScore, 2),
                    OverallGrade = GetGrade(averageScore)
                },
                RecentNotifications = notifications.Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.CreatedAt
                }),
                UnreadNotificationCount = notifications.Count
            });
        }
        
        private string GetGrade(double score)
        {
            if (score >= 90) return "A+";
            if (score >= 80) return "A";
            if (score >= 75) return "A-";
            if (score >= 70) return "B+";
            if (score >= 65) return "B";
            if (score >= 60) return "B-";
            if (score >= 55) return "C+";
            if (score >= 50) return "C";
            if (score >= 45) return "C-";
            if (score >= 40) return "D";
            return "E";
        }
    }
}