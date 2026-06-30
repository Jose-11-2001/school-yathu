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
    [Authorize(Roles = "HeadOfDepartment,Admin")]
    [SwaggerTag("Head of Department - Manage department, teachers, and results")]
    public class HeadOfDepartmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HeadOfDepartmentController> _logger;

        public HeadOfDepartmentController(ApplicationDbContext context, ILogger<HeadOfDepartmentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get the department for the current Head of Department
        /// </summary>
        [HttpGet("my-department")]
        [SwaggerOperation(Summary = "Get my department")]
        [SwaggerResponse(200, "Department info retrieved successfully")]
        [SwaggerResponse(404, "Department not found")]
        public async Task<IActionResult> GetMyDepartment()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var department = await _context.Departments
                    .Where(d => d.HeadOfDepartmentId == userId)
                    .Select(d => new
                    {
                        d.Id,
                        d.Name,
                        d.Description,
                        TeacherCount = _context.Users.Count(u => u.DepartmentId == d.Id && u.Role == "Teacher" && u.IsActive),
                        SubjectCount = _context.Subjects.Count(s => s.DepartmentId == d.Id),
                        HeadOfDepartmentName = d.HeadOfDepartment != null ? d.HeadOfDepartment.Name : "Not Assigned",
                        HeadOfDepartmentPhone = d.HeadOfDepartment != null ? d.HeadOfDepartment.PhoneNumber : null,
                        HeadOfDepartmentEmail = d.HeadOfDepartment != null ? d.HeadOfDepartment.Email : null
                    })
                    .FirstOrDefaultAsync();

                if (department == null)
                    return NotFound(new { message = "You are not assigned to any department" });

                return Ok(department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department");
                return StatusCode(500, new { message = "An error occurred while retrieving department information" });
            }
        }

        /// <summary>
        /// Get teachers in the department
        /// </summary>
        [HttpGet("department-teachers")]
        [SwaggerOperation(Summary = "Get department teachers")]
        [SwaggerResponse(200, "Teachers retrieved successfully")]
        [SwaggerResponse(404, "Department not found")]
        public async Task<IActionResult> GetDepartmentTeachers()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.HeadOfDepartmentId == userId);

                if (department == null)
                    return NotFound(new { message = "You are not assigned to any department" });

                var teachers = await _context.Users
                    .Where(u => u.DepartmentId == department.Id && u.Role == "Teacher" && u.IsActive)
                    .Select(u => new
                    {
                        u.Id,
                        u.Name,
                        u.Email,
                        u.PhoneNumber,
                        u.EmployeeId,
                        u.Qualification,
                        SubjectsCount = _context.TeacherSubjects.Count(ts => ts.TeacherId == u.Id && ts.IsActive)
                    })
                    .ToListAsync();

                return Ok(teachers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department teachers");
                return StatusCode(500, new { message = "An error occurred while retrieving teachers" });
            }
        }

        /// <summary>
        /// Get subjects in the department
        /// </summary>
        [HttpGet("department-subjects")]
        [SwaggerOperation(Summary = "Get department subjects")]
        [SwaggerResponse(200, "Subjects retrieved successfully")]
        [SwaggerResponse(404, "Department not found")]
        public async Task<IActionResult> GetDepartmentSubjects()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.HeadOfDepartmentId == userId);

                if (department == null)
                    return NotFound(new { message = "You are not assigned to any department" });

                var subjects = await _context.Subjects
                    .Where(s => s.DepartmentId == department.Id)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Code,
                        s.Type,
                        TeacherCount = _context.TeacherSubjects.Count(ts => ts.SubjectId == s.Id && ts.IsActive)
                    })
                    .ToListAsync();

                return Ok(subjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department subjects");
                return StatusCode(500, new { message = "An error occurred while retrieving subjects" });
            }
        }

        /// <summary>
        /// Assign a teacher to a subject in the department
        /// </summary>
        [HttpPost("assign-teacher-to-subject")]
        [SwaggerOperation(Summary = "Assign teacher to subject")]
        [SwaggerResponse(200, "Teacher assigned successfully")]
        [SwaggerResponse(400, "Invalid request or already assigned")]
        [SwaggerResponse(404, "Teacher, subject, or department not found")]
        public async Task<IActionResult> AssignTeacherToSubject([FromBody] AssignTeacherSubjectDTO dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.HeadOfDepartmentId == userId);

                if (department == null)
                    return NotFound(new { message = "You are not assigned to any department" });

                var subject = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.Id == dto.SubjectId && s.DepartmentId == department.Id);

                if (subject == null)
                    return BadRequest(new { message = "Subject not found in your department" });

                var teacher = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == dto.TeacherId && u.DepartmentId == department.Id && u.Role == "Teacher");

                if (teacher == null)
                    return BadRequest(new { message = "Teacher not found in your department" });

                var existing = await _context.TeacherSubjects
                    .FirstOrDefaultAsync(ts => ts.TeacherId == dto.TeacherId && ts.SubjectId == dto.SubjectId && ts.IsActive);

                if (existing != null)
                    return BadRequest(new { message = "Teacher already assigned to this subject" });

                var assignment = new TeacherSubject
                {
                    TeacherId = dto.TeacherId,
                    SubjectId = dto.SubjectId,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.TeacherSubjects.Add(assignment);
                await _context.SaveChangesAsync();

                // Send notification to teacher
                var notification = new Notification
                {
                    Title = "📚 Subject Assignment",
                    Message = $"You have been assigned to teach {subject.Name} by the Head of Department.",
                    Type = "SubjectAssignment",
                    TeacherId = dto.TeacherId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "Teacher assigned to subject successfully",
                    assignmentId = assignment.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning teacher to subject");
                return StatusCode(500, new { message = "An error occurred while assigning teacher" });
            }
        }

        /// <summary>
        /// Remove a teacher from a subject
        /// </summary>
        [HttpDelete("remove-teacher-from-subject/{assignmentId}")]
        [SwaggerOperation(Summary = "Remove teacher from subject")]
        [SwaggerResponse(200, "Teacher removed successfully")]
        [SwaggerResponse(404, "Assignment not found")]
        public async Task<IActionResult> RemoveTeacherFromSubject(int assignmentId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.HeadOfDepartmentId == userId);

                if (department == null)
                    return NotFound(new { message = "You are not assigned to any department" });

                var assignment = await _context.TeacherSubjects
                    .Include(ts => ts.Subject)
                    .FirstOrDefaultAsync(ts => ts.Id == assignmentId && ts.Teacher.DepartmentId == department.Id);

                if (assignment == null)
                    return NotFound(new { message = "Assignment not found in your department" });

                assignment.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Teacher removed from subject successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing teacher from subject");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get all students with results in the department
        /// </summary>
        [HttpGet("department-student-results")]
        [SwaggerOperation(Summary = "Get department student results")]
        [SwaggerResponse(200, "Results retrieved successfully")]
        [SwaggerResponse(404, "Department not found")]
        public async Task<IActionResult> GetDepartmentStudentResults([FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.HeadOfDepartmentId == userId);

                if (department == null)
                    return NotFound(new { message = "You are not assigned to any department" });

                var subjectIds = await _context.Subjects
                    .Where(s => s.DepartmentId == department.Id)
                    .Select(s => s.Id)
                    .ToListAsync();

                if (!subjectIds.Any())
                    return Ok(new List<object>());

                var results = await _context.Marks
                    .Include(m => m.Student)
                    .Include(m => m.Subject)
                    .Where(m => subjectIds.Contains(m.SubjectId) && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                    .Select(m => new
                    {
                        m.StudentId,
                        StudentName = m.Student != null ? m.Student.FullName : "",
                        AdmissionNumber = m.Student != null ? m.Student.AdmissionNumber : "",
                        StudentClass = m.Student != null ? m.Student.Class : "",
                        StudentStream = m.Student != null ? m.Student.Stream : "",
                        SubjectName = m.Subject != null ? m.Subject.Name : "",
                        m.TotalScore,
                        m.Grade,
                        m.Year,
                        m.Term,
                        m.IsApproved
                    })
                    .OrderByDescending(r => r.TotalScore)
                    .ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department results");
                return StatusCode(500, new { message = "An error occurred while retrieving results" });
            }
        }

        /// <summary>
        /// Get department statistics
        /// </summary>
        [HttpGet("department-stats")]
        [SwaggerOperation(Summary = "Get department statistics")]
        public async Task<IActionResult> GetDepartmentStats()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.HeadOfDepartmentId == userId);

                if (department == null)
                    return NotFound(new { message = "You are not assigned to any department" });

                var subjectIds = await _context.Subjects
                    .Where(s => s.DepartmentId == department.Id)
                    .Select(s => s.Id)
                    .ToListAsync();

                var stats = new
                {
                    DepartmentName = department.Name,
                    TotalTeachers = await _context.Users.CountAsync(u => u.DepartmentId == department.Id && u.Role == "Teacher" && u.IsActive),
                    TotalSubjects = subjectIds.Count,
                    TotalStudents = await _context.Students
                        .Where(s => _context.Marks.Any(m => m.StudentId == s.Id && subjectIds.Contains(m.SubjectId)))
                        .CountAsync(),
                    TotalMarks = await _context.Marks
                        .Where(m => subjectIds.Contains(m.SubjectId) && m.TotalScore.HasValue)
                        .CountAsync(),
                    PendingApprovals = await _context.Marks
                        .Where(m => subjectIds.Contains(m.SubjectId) && !m.IsApproved && m.TotalScore.HasValue)
                        .CountAsync(),
                    TopSubjects = await _context.Marks
                        .Where(m => subjectIds.Contains(m.SubjectId) && m.TotalScore.HasValue)
                        .GroupBy(m => m.SubjectId)
                        .Select(g => new
                        {
                            SubjectId = g.Key,
                            SubjectName = _context.Subjects.FirstOrDefault(s => s.Id == g.Key).Name,
                            Average = g.Average(m => m.TotalScore)
                        })
                        .OrderByDescending(x => x.Average)
                        .Take(5)
                        .ToListAsync()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department stats");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get pending approvals for the department
        /// </summary>
        [HttpGet("pending-approvals")]
        [SwaggerOperation(Summary = "Get pending approvals")]
        public async Task<IActionResult> GetPendingApprovals([FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.HeadOfDepartmentId == userId);

                if (department == null)
                    return NotFound(new { message = "You are not assigned to any department" });

                var subjectIds = await _context.Subjects
                    .Where(s => s.DepartmentId == department.Id)
                    .Select(s => s.Id)
                    .ToListAsync();

                var pending = await _context.Marks
                    .Include(m => m.Student)
                    .Include(m => m.Subject)
                    .Where(m => subjectIds.Contains(m.SubjectId) && 
                                m.Year == year && 
                                m.Term == term && 
                                !m.IsApproved &&
                                m.TotalScore.HasValue)
                    .Select(m => new
                    {
                        m.Id,
                        m.StudentId,
                        StudentName = m.Student != null ? m.Student.FullName : "",
                        AdmissionNumber = m.Student != null ? m.Student.AdmissionNumber : "",
                        SubjectName = m.Subject != null ? m.Subject.Name : "",
                        m.TotalScore,
                        m.Grade,
                        m.Term,
                        m.Year,
                        m.CreatedAt
                    })
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();

                return Ok(pending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending approvals");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get department activity log
        /// </summary>
        [HttpGet("activity-log")]
        [SwaggerOperation(Summary = "Get department activity log")]
        public async Task<IActionResult> GetActivityLog()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.HeadOfDepartmentId == userId);

                if (department == null)
                    return NotFound(new { message = "You are not assigned to any department" });

                var activities = new List<object>();

                // Recent teacher assignments
                var teacherAssignments = await _context.TeacherSubjects
                    .Include(ts => ts.Teacher)
                    .Include(ts => ts.Subject)
                    .Where(ts => ts.Teacher.DepartmentId == department.Id && ts.IsActive)
                    .OrderByDescending(ts => ts.AssignedAt)
                    .Take(10)
                    .Select(ts => new
                    {
                        Type = "TeacherAssignment",
                        Message = $"{ts.Teacher.Name} was assigned to teach {ts.Subject.Name}",
                        CreatedAt = ts.AssignedAt
                    })
                    .ToListAsync();

                // Recent results entered
                var recentResults = await _context.Marks
                    .Include(m => m.Student)
                    .Include(m => m.Subject)
                    .Where(m => m.Subject.DepartmentId == department.Id)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(10)
                    .Select(m => new
                    {
                        Type = "ResultEntered",
                        Message = $"{m.Student.FullName} scored {m.TotalScore}% in {m.Subject.Name}",
                        CreatedAt = m.CreatedAt
                    })
                    .ToListAsync();

                activities.AddRange(teacherAssignments);
                activities.AddRange(recentResults);

                return Ok(activities
                    .OrderByDescending(a => ((dynamic)a).CreatedAt)
                    .Take(20)
                    .ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting activity log");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get department performance report
        /// </summary>
        [HttpGet("performance-report")]
        [SwaggerOperation(Summary = "Get department performance report")]
        public async Task<IActionResult> GetPerformanceReport([FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.HeadOfDepartmentId == userId);

                if (department == null)
                    return NotFound(new { message = "You are not assigned to any department" });

                var subjectIds = await _context.Subjects
                    .Where(s => s.DepartmentId == department.Id)
                    .Select(s => s.Id)
                    .ToListAsync();

                var report = new
                {
                    DepartmentName = department.Name,
                    Year = year,
                    Term = term,
                    SubjectPerformance = await _context.Marks
                        .Where(m => subjectIds.Contains(m.SubjectId) && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                        .GroupBy(m => m.SubjectId)
                        .Select(g => new
                        {
                            SubjectId = g.Key,
                            SubjectName = _context.Subjects.FirstOrDefault(s => s.Id == g.Key).Name,
                            AverageScore = g.Average(m => m.TotalScore),
                            HighestScore = g.Max(m => m.TotalScore),
                            LowestScore = g.Min(m => m.TotalScore),
                            StudentCount = g.Select(m => m.StudentId).Distinct().Count(),
                            TotalMarks = g.Count()
                        })
                        .OrderByDescending(x => x.AverageScore)
                        .ToListAsync(),
                    OverallAverage = await _context.Marks
                        .Where(m => subjectIds.Contains(m.SubjectId) && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                        .AverageAsync(m => m.TotalScore),
                    TotalStudents = await _context.Marks
                        .Where(m => subjectIds.Contains(m.SubjectId) && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                        .Select(m => m.StudentId)
                        .Distinct()
                        .CountAsync(),
                    TotalMarks = await _context.Marks
                        .Where(m => subjectIds.Contains(m.SubjectId) && m.Year == year && m.Term == term && m.TotalScore.HasValue)
                        .CountAsync()
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance report");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }

    // ✅ REMOVED: AssignTeacherSubjectDTO - Now using from DTOs/DepartmentDTOs.cs
}