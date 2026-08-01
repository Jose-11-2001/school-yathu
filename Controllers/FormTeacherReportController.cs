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
    [Authorize(Roles = "FormTeacher,Admin")]
    [SwaggerTag("Form Teacher Reports - Generate class and student reports")]
    public class FormTeacherReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FormTeacherReportController> _logger;

        public FormTeacherReportController(ApplicationDbContext context, ILogger<FormTeacherReportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get class performance report for form teacher
        /// </summary>
        [HttpGet("class-report")]
        [SwaggerOperation(Summary = "Get class performance report")]
        public async Task<IActionResult> GetClassReport([FromQuery] int classId, [FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Verify teacher is form teacher for this class
                var isFormTeacher = await _context.FormTeacherClasses
                    .AnyAsync(ftc => ftc.TeacherId == teacherId && ftc.ClassId == classId);

                if (!isFormTeacher)
                    return Unauthorized(new { message = "You are not the form teacher for this class" });

                var classEntity = await _context.Classes.FindAsync(classId);
                if (classEntity == null)
                    return NotFound(new { message = "Class not found" });

                var students = await _context.Students
                    .Where(s => s.ClassId == classId)
                    .ToListAsync();

                var studentReports = new List<object>();

                foreach (var student in students)
                {
                    var marks = await _context.Marks
                        .Include(m => m.Subject)
                        .Where(m => m.StudentId == student.Id && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                        .ToListAsync();

                    if (marks.Any())
                    {
                        var totalScore = marks.Sum(m => m.TotalScore ?? 0);
                        var average = marks.Average(m => m.TotalScore ?? 0);
                        var subjects = marks.Select(m => new
                        {
                            SubjectName = m.Subject != null ? m.Subject.Name : "Unknown",
                            Score = m.TotalScore,
                            Grade = m.Grade,
                            Remark = m.Remark
                        }).ToList();

                        studentReports.Add(new
                        {
                            student.Id,
                            student.AdmissionNumber,
                            student.FullName,
                            student.Class,
                            student.Stream,
                            TotalMarks = totalScore,
                            AverageScore = Math.Round(average, 2),
                            Grade = CalculateOverallGrade(average, student.Class),
                            Subjects = subjects,
                            SubjectCount = subjects.Count
                        });
                    }
                    else
                    {
                        studentReports.Add(new
                        {
                            student.Id,
                            student.AdmissionNumber,
                            student.FullName,
                            student.Class,
                            student.Stream,
                            TotalMarks = 0,
                            AverageScore = 0,
                            Grade = "N/A",
                            Subjects = new List<object>(),
                            SubjectCount = 0
                        });
                    }
                }

                // Sort by average score descending
                var sortedReports = studentReports
                    .OrderByDescending(r => ((dynamic)r).AverageScore)
                    .Select((r, index) => new
                    {
                        Position = index + 1,
                        StudentId = ((dynamic)r).Id,
                        AdmissionNumber = ((dynamic)r).AdmissionNumber,
                        FullName = ((dynamic)r).FullName,
                        Class = ((dynamic)r).Class,
                        Stream = ((dynamic)r).Stream,
                        TotalMarks = ((dynamic)r).TotalMarks,
                        AverageScore = ((dynamic)r).AverageScore,
                        Grade = ((dynamic)r).Grade,
                        Subjects = ((dynamic)r).Subjects,
                        SubjectCount = ((dynamic)r).SubjectCount
                    })
                    .ToList();

                var classStats = new
                {
                    ClassName = classEntity.Name,
                    Stream = classEntity.Stream,
                    Year = year,
                    Term = term,
                    TotalStudents = students.Count,
                    StudentsWithResults = studentReports.Count(r => ((dynamic)r).SubjectCount > 0),
                    AverageClassScore = studentReports.Any() ? studentReports.Average(r => ((dynamic)r).AverageScore) : 0,
                    TopStudent = sortedReports.FirstOrDefault(),
                    BottomStudent = sortedReports.LastOrDefault(),
                    StudentReports = sortedReports
                };

                return Ok(classStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating class report");
                return StatusCode(500, new { message = "An error occurred generating the report" });
            }
        }

        /// <summary>
        /// Get individual student report for form teacher
        /// </summary>
        [HttpGet("student-report")]
        [SwaggerOperation(Summary = "Get individual student report")]
        public async Task<IActionResult> GetStudentReport([FromQuery] int studentId, [FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var student = await _context.Students
                    .Include(s => s.ClassEntity)
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null)
                    return NotFound(new { message = "Student not found" });

                // Verify teacher is form teacher for this student's class
                var isFormTeacher = await _context.FormTeacherClasses
                    .AnyAsync(ftc => ftc.TeacherId == teacherId && ftc.ClassId == student.ClassId);

                if (!isFormTeacher)
                    return Unauthorized(new { message = "You are not the form teacher for this student" });

                var marks = await _context.Marks
                    .Include(m => m.Subject)
                    .Where(m => m.StudentId == studentId && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                    .ToListAsync();

                var subjectMarks = marks.Select(m => new
                {
                    SubjectName = m.Subject != null ? m.Subject.Name : "Unknown",
                    SubjectCode = m.Subject != null ? m.Subject.Code : "",
                    ContinuousTest1 = m.ContinuousTest1,
                    ContinuousTest2 = m.ContinuousTest2,
                    EndTermExam = m.EndTermExam,
                    TotalScore = m.TotalScore,
                    Grade = m.Grade,
                    Remark = m.Remark
                }).ToList();

                var totalScore = marks.Sum(m => m.TotalScore ?? 0);
                var average = marks.Any() ? marks.Average(m => m.TotalScore ?? 0) : 0;

                // Get class ranking
                var classStudents = await _context.Students
                    .Where(s => s.ClassId == student.ClassId)
                    .Select(s => s.Id)
                    .ToListAsync();

                var classResults = await _context.Marks
                    .Where(m => classStudents.Contains(m.StudentId) && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                    .GroupBy(m => m.StudentId)
                    .Select(g => new
                    {
                        StudentId = g.Key,
                        Average = g.Average(m => m.TotalScore ?? 0)
                    })
                    .OrderByDescending(x => x.Average)
                    .ToListAsync();

                var position = classResults.FindIndex(x => x.StudentId == studentId) + 1;

                var report = new
                {
                    Student = new
                    {
                        student.Id,
                        student.AdmissionNumber,
                        student.FullName,
                        student.Class,
                        student.Stream,
                        student.Email,
                        student.PhoneNumber
                    },
                    Performance = new
                    {
                        TotalMarks = totalScore,
                        AverageScore = Math.Round(average, 2),
                        Position = position > 0 ? position : classResults.Count + 1,
                        TotalStudents = classResults.Count,
                        Grade = CalculateOverallGrade(average, student.Class),
                        SubjectCount = marks.Count
                    },
                    Subjects = subjectMarks,
                    ClassName = student.ClassEntity?.Name ?? student.Class,
                    Stream = student.ClassEntity?.Stream ?? student.Stream,
                    Year = year,
                    Term = term,
                    GeneratedAt = DateTime.UtcNow
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating student report");
                return StatusCode(500, new { message = "An error occurred generating the report" });
            }
        }

        /// <summary>
        /// Get class subject performance report
        /// </summary>
        [HttpGet("subject-performance")]
        [SwaggerOperation(Summary = "Get subject performance report")]
        public async Task<IActionResult> GetSubjectPerformance([FromQuery] int classId, [FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var isFormTeacher = await _context.FormTeacherClasses
                    .AnyAsync(ftc => ftc.TeacherId == teacherId && ftc.ClassId == classId);

                if (!isFormTeacher)
                    return Unauthorized(new { message = "You are not the form teacher for this class" });

                var classEntity = await _context.Classes.FindAsync(classId);
                if (classEntity == null)
                    return NotFound(new { message = "Class not found" });

                var subjectPerformance = await _context.Marks
                    .Include(m => m.Subject)
                    .Where(m => m.ClassId == classId && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                    .GroupBy(m => m.SubjectId)
                    .Select(g => new
                    {
                        SubjectId = g.Key,
                        SubjectName = g.First().Subject != null ? g.First().Subject.Name : "Unknown",
                        AverageScore = g.Average(m => m.TotalScore ?? 0),
                        HighestScore = g.Max(m => m.TotalScore ?? 0),
                        LowestScore = g.Min(m => m.TotalScore ?? 0),
                        StudentCount = g.Count(),
                        PassRate = g.Count(m => (m.TotalScore ?? 0) >= 50) * 100.0 / g.Count(),
                        GradeDistribution = g.GroupBy(m => m.Grade)
                            .Select(gr => new
                            {
                                Grade = gr.Key ?? "N/A",
                                Count = gr.Count()
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    ClassName = classEntity.Name,
                    Stream = classEntity.Stream,
                    Year = year,
                    Term = term,
                    SubjectPerformance = subjectPerformance.OrderByDescending(s => s.AverageScore),
                    TotalSubjects = subjectPerformance.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating subject performance report");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        private string CalculateOverallGrade(double average, string? className)
        {
            if (className != null && (className.Contains("Form 1") || className.Contains("Form 2") ||
                                       className.Contains("Form1") || className.Contains("Form2")))
            {
                if (average >= 80) return "A";
                if (average >= 65) return "B";
                if (average >= 50) return "C";
                if (average >= 45) return "D";
                if (average >= 40) return "E";
                return "F";
            }

            if (average >= 85) return "1 point";
            if (average >= 80) return "2 points";
            if (average >= 65) return "3 points";
            if (average >= 60) return "4 points";
            if (average >= 55) return "5 points";
            if (average >= 50) return "6 points";
            if (average >= 45) return "7 points";
            if (average >= 40) return "8 points";
            return "9 points";
        }
    }
}