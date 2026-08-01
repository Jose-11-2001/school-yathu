using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [SwaggerTag("Rankings - Student performance rankings")]
    public class RankingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RankingsController> _logger;

        public RankingsController(ApplicationDbContext context, ILogger<RankingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get class rankings for Form Teacher
        /// </summary>
        [HttpGet("class-rankings")]
        [Authorize(Roles = "FormTeacher,Admin")]
        public async Task<IActionResult> GetClassRankings([FromQuery] int classId, [FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Verify teacher is form teacher for this class
                if (userRole != "Admin")
                {
                    var isFormTeacher = await _context.FormTeacherClasses
                        .AnyAsync(ftc => ftc.TeacherId == teacherId && ftc.ClassId == classId);

                    if (!isFormTeacher)
                        return Unauthorized(new { message = "You are not the form teacher for this class" });
                }

                var classEntity = await _context.Classes.FindAsync(classId);
                if (classEntity == null)
                    return NotFound(new { message = "Class not found" });

                var students = await _context.Students
                    .Where(s => s.ClassId == classId)
                    .ToListAsync();

                var rankings = new List<RankingItemDTO>();

                foreach (var student in students)
                {
                    var marks = await _context.Marks
                        .Where(m => m.StudentId == student.Id && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                        .ToListAsync();

                    if (marks.Any())
                    {
                        var totalMarks = marks.Sum(m => m.TotalScore ?? 0);
                        var average = marks.Average(m => m.TotalScore ?? 0);
                        var grade = CalculateGrade(average, student.Class);

                        rankings.Add(new RankingItemDTO
                        {
                            Position = 0, // Will be set after sorting
                            StudentId = student.Id,
                            AdmissionNumber = student.AdmissionNumber,
                            FullName = student.FullName,
                            Average = Math.Round(average, 2),
                            Grade = grade
                        });
                    }
                }

                // Sort by average descending and assign positions
                var sortedRankings = rankings
                    .OrderByDescending(r => r.Average)
                    .Select((r, index) =>
                    {
                        r.Position = index + 1;
                        return r;
                    })
                    .ToList();

                return Ok(new
                {
                    ClassName = classEntity.Name,
                    Stream = classEntity.Stream,
                    Year = year,
                    Term = term,
                    TotalStudents = students.Count,
                    StudentsWithResults = sortedRankings.Count,
                    Rankings = sortedRankings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting class rankings");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get subject rankings for Head of Department
        /// </summary>
        [HttpGet("subject-rankings")]
        [Authorize(Roles = "HeadOfDepartment,Admin")]
        public async Task<IActionResult> GetSubjectRankings([FromQuery] int subjectId, [FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "Admin")
                {
                    var department = await _context.Departments
                        .FirstOrDefaultAsync(d => d.HeadOfDepartmentId == userId);

                    if (department == null)
                        return Unauthorized(new { message = "You are not assigned to any department" });

                    var subject = await _context.Subjects
                        .FirstOrDefaultAsync(s => s.Id == subjectId && s.DepartmentId == department.Id);

                    if (subject == null)
                        return BadRequest(new { message = "Subject not found in your department" });
                }

                var marks = await _context.Marks
                    .Include(m => m.Student)
                    .Include(m => m.Subject)
                    .Where(m => m.SubjectId == subjectId && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                    .ToListAsync();

                var subjectName = await _context.Subjects
                    .Where(s => s.Id == subjectId)
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync() ?? "Unknown";

                // Create ranking items with the properties that exist in RankingItemDTO
                var rankings = marks
                    .Select(m => new RankingItemDTO
                    {
                        Position = 0, // Will be set after sorting
                        StudentId = m.StudentId,
                        AdmissionNumber = m.Student != null ? m.Student.AdmissionNumber : "",
                        FullName = m.Student != null ? m.Student.FullName : "",
                        Average = m.TotalScore ?? 0,
                        Grade = m.Grade
                    })
                    .OrderByDescending(r => r.Average)
                    .Select((r, index) =>
                    {
                        r.Position = index + 1;
                        return r;
                    })
                    .ToList();

                return Ok(new
                {
                    SubjectId = subjectId,
                    SubjectName = subjectName,
                    Year = year,
                    Term = term,
                    TotalStudents = marks.Count,
                    Rankings = rankings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subject rankings");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get department rankings for Admin
        /// </summary>
        [HttpGet("department-rankings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDepartmentRankings([FromQuery] int departmentId, [FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var department = await _context.Departments.FindAsync(departmentId);
                if (department == null)
                    return NotFound(new { message = "Department not found" });

                var subjectIds = await _context.Subjects
                    .Where(s => s.DepartmentId == departmentId)
                    .Select(s => s.Id)
                    .ToListAsync();

                if (!subjectIds.Any())
                    return Ok(new
                    {
                        DepartmentName = department.Name,
                        Year = year,
                        Term = term,
                        TotalSubjects = 0,
                        Rankings = new List<object>()
                    });

                var marks = await _context.Marks
                    .Include(m => m.Student)
                    .Include(m => m.Subject)
                    .Where(m => subjectIds.Contains(m.SubjectId) && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                    .ToListAsync();

                var subjectAverages = marks
                    .GroupBy(m => m.SubjectId)
                    .Select(g => new
                    {
                        SubjectId = g.Key,
                        SubjectName = g.First().Subject != null ? g.First().Subject.Name : "Unknown",
                        Average = g.Average(m => m.TotalScore ?? 0),
                        TotalMarks = g.Sum(m => m.TotalScore ?? 0),
                        StudentCount = g.Select(m => m.StudentId).Distinct().Count()
                    })
                    .OrderByDescending(s => s.Average)
                    .Select((s, index) => new
                    {
                        Position = index + 1,
                        s.SubjectId,
                        s.SubjectName,
                        s.Average,
                        s.TotalMarks,
                        s.StudentCount,
                        Performance = s.Average >= 70 ? "Excellent" :
                                      s.Average >= 50 ? "Good" :
                                      s.Average >= 40 ? "Average" : "Poor"
                    })
                    .ToList();

                return Ok(new
                {
                    DepartmentName = department.Name,
                    Year = year,
                    Term = term,
                    TotalSubjects = subjectAverages.Count,
                    Rankings = subjectAverages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department rankings");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get overall school rankings for Admin
        /// </summary>
        [HttpGet("school-rankings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSchoolRankings([FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var departmentRankings = await _context.Departments
                    .Select(d => new
                    {
                        DepartmentId = d.Id,
                        DepartmentName = d.Name,
                        AverageScore = _context.Marks
                            .Where(m => m.Subject != null && m.Subject.DepartmentId == d.Id && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                            .Average(m => m.TotalScore ?? 0),
                        TotalStudents = _context.Marks
                            .Where(m => m.Subject != null && m.Subject.DepartmentId == d.Id && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                            .Select(m => m.StudentId)
                            .Distinct()
                            .Count(),
                        TotalSubjects = _context.Subjects.Count(s => s.DepartmentId == d.Id)
                    })
                    .ToListAsync();

                var rankings = departmentRankings
                    .OrderByDescending(d => d.AverageScore)
                    .Select((d, index) => new
                    {
                        Position = index + 1,
                        d.DepartmentId,
                        d.DepartmentName,
                        AverageScore = Math.Round(d.AverageScore, 2),
                        d.TotalStudents,
                        d.TotalSubjects,
                        Performance = d.AverageScore >= 70 ? "Excellent" :
                                      d.AverageScore >= 50 ? "Good" :
                                      d.AverageScore >= 40 ? "Average" : "Poor"
                    })
                    .ToList();

                return Ok(new
                {
                    Year = year,
                    Term = term,
                    TotalDepartments = rankings.Count,
                    Rankings = rankings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting school rankings");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        private string CalculateGrade(double percentage, string? className)
        {
            if (className != null && (className.Contains("Form 1") || className.Contains("Form 2") ||
                                       className.Contains("Form1") || className.Contains("Form2")))
            {
                if (percentage >= 80) return "A";
                if (percentage >= 65) return "B";
                if (percentage >= 50) return "C";
                if (percentage >= 45) return "D";
                if (percentage >= 40) return "E";
                return "F";
            }

            if (percentage >= 85) return "1 point";
            if (percentage >= 80) return "2 points";
            if (percentage >= 65) return "3 points";
            if (percentage >= 60) return "4 points";
            if (percentage >= 55) return "5 points";
            if (percentage >= 50) return "6 points";
            if (percentage >= 45) return "7 points";
            if (percentage >= 40) return "8 points";
            return "9 points";
        }
    }
}