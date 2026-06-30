using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;  // ✅ Add this using for DTOs
using School_Yathu.Models;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "FormTeacher,Admin")]
    [SwaggerTag("Form Teacher - Manage class, subjects, and results")]
    public class FormTeacherController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FormTeacherController> _logger;

        public FormTeacherController(ApplicationDbContext context, ILogger<FormTeacherController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all classes assigned to the form teacher
        /// </summary>
        [HttpGet("my-classes")]
        [SwaggerOperation(Summary = "Get my classes", Description = "Retrieves all classes assigned to the form teacher")]
        public async Task<IActionResult> GetMyClasses()
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var classes = await _context.FormTeacherClasses
                    .Include(ftc => ftc.Class)
                    .Where(ftc => ftc.TeacherId == teacherId)
                    .Select(ftc => new
                    {
                        ftc.Class.Id,
                        ftc.Class.Name,
                        ftc.Class.Stream,
                        ftc.Class.Capacity,
                        StudentCount = _context.Students.Count(s => s.ClassId == ftc.ClassId),
                        ftc.AssignedAt
                    })
                    .ToListAsync();

                return Ok(classes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting form teacher classes");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get all students in the form teacher's class
        /// </summary>
        [HttpGet("my-students")]
        [SwaggerOperation(Summary = "Get my students", Description = "Retrieves all students in classes assigned to the form teacher")]
        public async Task<IActionResult> GetMyStudents([FromQuery] int? classId)
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var assignedClassIds = await _context.FormTeacherClasses
                    .Where(ftc => ftc.TeacherId == teacherId)
                    .Select(ftc => ftc.ClassId)
                    .ToListAsync();

                if (!assignedClassIds.Any())
                    return Ok(new { message = "No classes assigned", students = new List<object>() });

                var query = _context.Students
                    .Include(s => s.ClassEntity)
                    .Where(s => assignedClassIds.Contains(s.ClassId.Value));

                if (classId.HasValue)
                    query = query.Where(s => s.ClassId == classId.Value);

                var students = await query
                    .Select(s => new
                    {
                        s.Id,
                        s.AdmissionNumber,
                        s.FullName,
                        s.Class,
                        s.Stream,
                        s.Email,
                        s.CreatedAt,
                        SubjectSelections = _context.StudentSubjectSelections
                            .Count(sss => sss.StudentId == s.Id && !sss.IsApproved),
                        HasResults = _context.Marks.Any(m => m.StudentId == s.Id)
                    })
                    .ToListAsync();

                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting form teacher students");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get subject selections for students in the form teacher's class
        /// </summary>
        [HttpGet("subject-selections")]
        [SwaggerOperation(Summary = "Get subject selections", Description = "Retrieves pending subject selections for students")]
        public async Task<IActionResult> GetSubjectSelections([FromQuery] int? classId, [FromQuery] bool pendingOnly = true)
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var assignedClassIds = await _context.FormTeacherClasses
                    .Where(ftc => ftc.TeacherId == teacherId)
                    .Select(ftc => ftc.ClassId)
                    .ToListAsync();

                if (!assignedClassIds.Any())
                    return Ok(new { message = "No classes assigned", selections = new List<object>() });

                var query = _context.StudentSubjectSelections
                    .Include(sss => sss.Student)
                    .Include(sss => sss.Subject)
                    .Where(sss => assignedClassIds.Contains(sss.Student.ClassId.Value));

                if (classId.HasValue)
                    query = query.Where(sss => sss.Student.ClassId == classId.Value);

                if (pendingOnly)
                    query = query.Where(sss => !sss.IsApproved);

                var selections = await query
                    .Select(sss => new
                    {
                        sss.Id,
                        sss.StudentId,
                        StudentName = sss.Student != null ? sss.Student.FullName : "",
                        AdmissionNumber = sss.Student != null ? sss.Student.AdmissionNumber : "",
                        SubjectName = sss.Subject != null ? sss.Subject.Name : "",
                        SubjectCode = sss.Subject != null ? sss.Subject.Code : "",
                        sss.AcademicYear,
                        sss.Term,
                        sss.IsApproved,
                        sss.CreatedAt
                    })
                    .ToListAsync();

                return Ok(selections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subject selections");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Approve a subject selection
        /// </summary>
        [HttpPost("approve-subject-selection/{selectionId}")]
        [SwaggerOperation(Summary = "Approve subject selection", Description = "Approves a student's subject selection")]
        public async Task<IActionResult> ApproveSubjectSelection(int selectionId)
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var selection = await _context.StudentSubjectSelections
                    .Include(sss => sss.Student)
                    .FirstOrDefaultAsync(sss => sss.Id == selectionId);

                if (selection == null)
                    return NotFound(new { message = "Selection not found" });

                var assignedClassIds = await _context.FormTeacherClasses
                    .Where(ftc => ftc.TeacherId == teacherId)
                    .Select(ftc => ftc.ClassId)
                    .ToListAsync();

                if (!assignedClassIds.Contains(selection.Student.ClassId.Value))
                    return Unauthorized(new { message = "You are not authorized to approve this selection" });

                selection.IsApproved = true;
                selection.ApprovedByFormTeacherId = teacherId;
                selection.ApprovedAt = DateTime.UtcNow;

                // Add to StudentSubjects
                var studentSubject = new StudentSubject
                {
                    StudentId = selection.StudentId,
                    SubjectId = selection.SubjectId,
                    AcademicYear = selection.AcademicYear,
                    Term = selection.Term ?? "Term 1",
                    RegisteredAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.StudentSubjects.Add(studentSubject);

                await _context.SaveChangesAsync();

                // Send notification to student
                var notification = new Notification
                {
                    Title = "✅ Subject Selection Approved",
                    Message = $"Your selection for {selection.Subject?.Name} has been approved by your form teacher.",
                    Type = "SubjectApproval",
                    StudentId = selection.StudentId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Subject selection approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving subject selection");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get class results summary for form teacher
        /// </summary>
        [HttpGet("class-results-summary")]
        [SwaggerOperation(Summary = "Get class results summary", Description = "Retrieves results summary for the form teacher's class")]
        public async Task<IActionResult> GetClassResultsSummary([FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var assignedClassIds = await _context.FormTeacherClasses
                    .Where(ftc => ftc.TeacherId == teacherId)
                    .Select(ftc => ftc.ClassId)
                    .ToListAsync();

                if (!assignedClassIds.Any())
                    return Ok(new { message = "No classes assigned" });

                var classNames = await _context.Classes
                    .Where(c => assignedClassIds.Contains(c.Id))
                    .Select(c => c.Name)
                    .Distinct()
                    .ToListAsync();

                var results = new List<object>();

                foreach (var className in classNames)
                {
                    var students = await _context.Students
                        .Where(s => s.Class == className && assignedClassIds.Contains(s.ClassId.Value))
                        .ToListAsync();

                    var studentResults = new List<object>();

                    foreach (var student in students)
                    {
                        var marks = await _context.Marks
                            .Where(m => m.StudentId == student.Id && m.Year == year && m.Term == term)
                            .ToListAsync();

                        if (marks.Any())
                        {
                            var totalScore = marks.Sum(m => m.TotalScore ?? 0);
                            var average = marks.Average(m => m.TotalScore ?? 0);

                            studentResults.Add(new
                            {
                                student.Id,
                                student.AdmissionNumber,
                                student.FullName,
                                TotalMarks = totalScore,
                                Average = Math.Round(average, 2),
                                SubjectsCount = marks.Count
                            });
                        }
                    }

                    results.Add(new
                    {
                        ClassName = className,
                        TotalStudents = students.Count,
                        StudentsWithResults = studentResults.Count,
                        Rankings = studentResults.OrderByDescending(r => ((dynamic)r).TotalMarks)
                            .Select((r, index) => new
                            {
                                Position = index + 1,
                                AdmissionNumber = ((dynamic)r).AdmissionNumber,
                                FullName = ((dynamic)r).FullName,
                                TotalMarks = ((dynamic)r).TotalMarks,
                                Average = ((dynamic)r).Average
                            })
                    });
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting class results summary");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Submit class results to Headteacher for approval
        /// </summary>
        [HttpPost("submit-results")]
        [SwaggerOperation(Summary = "Submit results to Headteacher", Description = "Form teacher submits class results for approval")]
        public async Task<IActionResult> SubmitResultsToHeadteacher([FromBody] SubmitResultsDTO dto)
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var assignedClassIds = await _context.FormTeacherClasses
                    .Where(ftc => ftc.TeacherId == teacherId)
                    .Select(ftc => ftc.ClassId)
                    .ToListAsync();

                var classEntity = await _context.Classes
                    .FirstOrDefaultAsync(c => c.Name == dto.ClassName && assignedClassIds.Contains(c.Id));

                if (classEntity == null)
                    return BadRequest(new { message = "You are not assigned to this class" });

                var students = await _context.Students
                    .Where(s => s.Class == dto.ClassName && s.ClassId == classEntity.Id)
                    .ToListAsync();

                if (!students.Any())
                    return BadRequest(new { message = "No students found in this class" });

                // Mark all marks for this class as pending approval
                foreach (var student in students)
                {
                    var marks = await _context.Marks
                        .Where(m => m.StudentId == student.Id && m.Year == dto.Year && m.Term == dto.Term)
                        .ToListAsync();

                    foreach (var mark in marks)
                    {
                        mark.IsApproved = false;
                    }
                }
                await _context.SaveChangesAsync();

                // Send notification to Headteacher
                var adminUsers = await _context.Users
                    .Where(u => u.Role == "Admin")
                    .ToListAsync();

                foreach (var admin in adminUsers)
                {
                    var notification = new Notification
                    {
                        Title = "📊 Class Results Submitted",
                        Message = $"Form Teacher has submitted results for {dto.ClassName} ({dto.Term} {dto.Year}). Please review and approve.",
                        Type = "ResultsSubmission",
                        UserId = admin.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    _context.Notifications.Add(notification);
                }
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Results for {dto.ClassName} submitted to Headteacher successfully",
                    studentCount = students.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting results");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }

    // ✅ REMOVED: SubmitResultsDTO - Now using from DTOs/ResultsDTOs.cs
}