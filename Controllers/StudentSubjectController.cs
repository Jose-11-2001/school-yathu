using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;
using School_Yathu.Models;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Student Subjects - Manage student subject registrations and allocations")]
    public class StudentSubjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StudentSubjectController> _logger;

        public StudentSubjectController(ApplicationDbContext context, ILogger<StudentSubjectController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get available subjects for the logged-in student
        /// </summary>
        [HttpGet("available-subjects")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "Get available subjects", Description = "Retrieves subjects available for the logged-in student to register")]
        [SwaggerResponse(200, "Available and registered subjects", typeof(object))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetAvailableSubjects()
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return BadRequest(new { message = "Student not found" });
            
            var availableSubjects = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .Where(cs => cs.Class != null && cs.Class.Name == student.Class)
                .Select(cs => new
                {
                    cs.SubjectId,
                    SubjectName = cs.Subject != null ? cs.Subject.Name : "",
                    TeacherName = cs.Teacher != null ? cs.Teacher.Name : "",
                    TeacherId = cs.TeacherId
                })
                .ToListAsync();
            
            var registeredSubjectIds = await _context.StudentSubjects
                .Where(ss => ss.StudentId == studentId && ss.IsActive)
                .Select(ss => ss.SubjectId)
                .ToListAsync();
            
            return Ok(new
            {
                AvailableSubjects = availableSubjects.Where(s => !registeredSubjectIds.Contains(s.SubjectId)),
                RegisteredSubjects = availableSubjects.Where(s => registeredSubjectIds.Contains(s.SubjectId))
            });
        }

        /// <summary>
        /// Get all student subject allocations (Form Teacher/Admin only)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "FormTeacher,Admin")]
        [SwaggerOperation(Summary = "Get all allocations", Description = "Retrieves all student subject allocations for Form Teachers and Admins")]
        [SwaggerResponse(200, "List of allocations", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetAllAllocations([FromQuery] int? classId, [FromQuery] int? year)
        {
            try
            {
                var currentYear = year ?? DateTime.Now.Year;
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var query = _context.StudentSubjects
                    .Include(ss => ss.Student)
                    .Include(ss => ss.Subject)
                    .Include(ss => ss.Teacher)
                    .Where(ss => ss.IsActive);

                // Filter by year if provided
                if (year.HasValue)
                {
                    query = query.Where(ss => ss.AcademicYear == year.Value);
                }

                // For Form Teachers, filter by their assigned classes
                if (userRole == "FormTeacher")
                {
                    var assignedClassIds = await _context.FormTeacherClasses
                        .Where(ftc => ftc.TeacherId == teacherId)
                        .Select(ftc => ftc.ClassId)
                        .ToListAsync();

                    if (assignedClassIds.Any())
                    {
                        query = query.Where(ss => ss.Student != null && 
                            assignedClassIds.Contains(ss.Student.ClassId ?? 0));
                    }
                    else
                    {
                        return Ok(new List<object>());
                    }
                }

                // Filter by class if provided
                if (classId.HasValue)
                {
                    query = query.Where(ss => ss.Student != null && ss.Student.ClassId == classId.Value);
                }

                var allocations = await query
                    .Select(ss => new
                    {
                        ss.Id,
                        ss.StudentId,
                        StudentName = ss.Student != null ? ss.Student.FullName : "",
                        AdmissionNumber = ss.Student != null ? ss.Student.AdmissionNumber : "",
                        StudentClass = ss.Student != null ? ss.Student.Class : "",
                        StudentStream = ss.Student != null ? ss.Student.Stream : "",
                        ss.SubjectId,
                        SubjectName = ss.Subject != null ? ss.Subject.Name : "",
                        SubjectCode = ss.Subject != null ? ss.Subject.Code : "",
                        ss.TeacherId,
                        TeacherName = ss.Teacher != null ? ss.Teacher.Name : "",
                        ss.AcademicYear,
                        ss.Term,
                        ss.RegisteredAt,
                        ss.IsActive
                    })
                    .OrderByDescending(ss => ss.RegisteredAt)
                    .ToListAsync();

                return Ok(allocations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting allocations");
                return StatusCode(500, new { message = "An error occurred while retrieving allocations" });
            }
        }

        /// <summary>
        /// Register a student for a subject (Form Teacher/Admin)
        /// </summary>
        [HttpPost("register")]
        [Authorize(Roles = "FormTeacher,Admin")]
        [SwaggerOperation(Summary = "Register student for subject", Description = "Registers a specific student for a subject")]
        [SwaggerResponse(200, "Registration successful", typeof(object))]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> RegisterSubject([FromBody] RegisterSubjectDTO dto)
        {
            try
            {
                // Validate student exists
                var student = await _context.Students.FindAsync(dto.StudentId);
                if (student == null)
                    return BadRequest(new { message = "Student not found" });

                // Validate subject exists
                var subject = await _context.Subjects.FindAsync(dto.SubjectId);
                if (subject == null)
                    return BadRequest(new { message = "Subject not found" });

                // Check if already registered
                var existing = await _context.StudentSubjects
                    .FirstOrDefaultAsync(ss => ss.StudentId == dto.StudentId && 
                        ss.SubjectId == dto.SubjectId && 
                        ss.AcademicYear == dto.AcademicYear &&
                        ss.Term == dto.Term &&
                        ss.IsActive);

                if (existing != null)
                    return BadRequest(new { message = "Student is already registered for this subject in the selected term" });

                // Get the teacher for this subject in the student's class
                var classSubject = await _context.ClassSubjects
                    .Include(cs => cs.Teacher)
                    .FirstOrDefaultAsync(cs => cs.ClassId == student.ClassId && cs.SubjectId == dto.SubjectId);

                if (classSubject == null)
                    return BadRequest(new { message = "Subject not available for this student's class" });

                // Create registration
                var registration = new StudentSubject
                {
                    StudentId = dto.StudentId,
                    SubjectId = dto.SubjectId,
                    TeacherId = classSubject.TeacherId,
                    AcademicYear = dto.AcademicYear,
                    Term = dto.Term,
                    RegisteredAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.StudentSubjects.Add(registration);
                await _context.SaveChangesAsync();

                // Send notifications
                await SendRegistrationNotifications(student, subject, classSubject.Teacher, dto.AcademicYear, dto.Term);

                return Ok(new { 
                    message = $"Successfully registered {student.FullName} for {subject.Name}",
                    registrationId = registration.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering subject");
                return StatusCode(500, new { message = "An error occurred while registering subject" });
            }
        }

        /// <summary>
        /// Bulk register students for a subject (Form Teacher/Admin)
        /// </summary>
        [HttpPost("bulk-register")]
        [Authorize(Roles = "FormTeacher,Admin")]
        [SwaggerOperation(Summary = "Bulk register students for subject", Description = "Registers multiple students for a subject")]
        [SwaggerResponse(200, "Bulk registration successful", typeof(object))]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> BulkRegisterSubjects([FromBody] BulkRegisterSubjectDTO dto)
        {
            try
            {
                if (dto.StudentIds == null || !dto.StudentIds.Any())
                    return BadRequest(new { message = "No students selected" });

                // Validate subject exists
                var subject = await _context.Subjects.FindAsync(dto.SubjectId);
                if (subject == null)
                    return BadRequest(new { message = "Subject not found" });

                var registrations = new List<StudentSubject>();
                var failedStudents = new List<int>();
                var notificationTasks = new List<Task>();

                foreach (var studentId in dto.StudentIds.Distinct())
                {
                    try
                    {
                        var student = await _context.Students.FindAsync(studentId);
                        if (student == null)
                        {
                            failedStudents.Add(studentId);
                            continue;
                        }

                        // Check if already registered
                        var existing = await _context.StudentSubjects
                            .FirstOrDefaultAsync(ss => ss.StudentId == studentId && 
                                ss.SubjectId == dto.SubjectId && 
                                ss.AcademicYear == dto.AcademicYear &&
                                ss.Term == dto.Term &&
                                ss.IsActive);

                        if (existing != null)
                            continue;

                        // Get teacher for this subject in student's class
                        var classSubject = await _context.ClassSubjects
                            .FirstOrDefaultAsync(cs => cs.ClassId == student.ClassId && cs.SubjectId == dto.SubjectId);

                        if (classSubject == null)
                            continue;

                        var registration = new StudentSubject
                        {
                            StudentId = studentId,
                            SubjectId = dto.SubjectId,
                            TeacherId = classSubject.TeacherId,
                            AcademicYear = dto.AcademicYear,
                            Term = dto.Term,
                            RegisteredAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        registrations.Add(registration);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing student {studentId}");
                        failedStudents.Add(studentId);
                    }
                }

                if (!registrations.Any())
                    return BadRequest(new { message = "No valid registrations to process" });

                _context.StudentSubjects.AddRange(registrations);
                await _context.SaveChangesAsync();

                // Send notifications for successful registrations
                foreach (var registration in registrations)
                {
                    var student = await _context.Students.FindAsync(registration.StudentId);
                    var teacher = await _context.Users.FindAsync(registration.TeacherId);
                    if (student != null && teacher != null)
                    {
                        await SendRegistrationNotifications(student, subject, teacher, dto.AcademicYear, dto.Term);
                    }
                }

                return Ok(new
                {
                    message = $"Successfully registered {registrations.Count} students for {subject.Name}",
                    registeredCount = registrations.Count,
                    failedCount = failedStudents.Count,
                    failedStudentIds = failedStudents
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk registration");
                return StatusCode(500, new { message = "An error occurred during bulk registration" });
            }
        }

        /// <summary>
        /// Remove a subject allocation (Form Teacher/Admin)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "FormTeacher,Admin")]
        [SwaggerOperation(Summary = "Remove allocation", Description = "Removes a student's subject allocation")]
        [SwaggerResponse(200, "Allocation removed successfully")]
        [SwaggerResponse(404, "Allocation not found")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> RemoveAllocation(int id)
        {
            try
            {
                var allocation = await _context.StudentSubjects
                    .Include(ss => ss.Student)
                    .Include(ss => ss.Subject)
                    .FirstOrDefaultAsync(ss => ss.Id == id);

                if (allocation == null)
                    return NotFound(new { message = "Allocation not found" });

                // Soft delete - mark as inactive
                allocation.IsActive = false;
                await _context.SaveChangesAsync();

                // Send notification to student
                if (allocation.Student != null)
                {
                    var notification = new Notification
                    {
                        Title = "Subject Allocation Removed",
                        Message = $"Your allocation for {allocation.Subject?.Name} has been removed.",
                        Type = "SubjectRemoval",
                        StudentId = allocation.StudentId,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Allocation removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing allocation");
                return StatusCode(500, new { message = "An error occurred while removing allocation" });
            }
        }

        /// <summary>
        /// Get allocations for a specific student
        /// </summary>
        [HttpGet("student/{studentId}")]
        [Authorize(Roles = "FormTeacher,Admin,Student")]
        [SwaggerOperation(Summary = "Get student allocations", Description = "Retrieves all subject allocations for a specific student")]
        [SwaggerResponse(200, "List of allocations", typeof(List<object>))]
        [SwaggerResponse(404, "Student not found")]
        public async Task<IActionResult> GetStudentAllocations(int studentId, [FromQuery] int? year)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                // Check authorization
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Student" && userId != studentId)
                    return Unauthorized(new { message = "You can only view your own allocations" });

                var currentYear = year ?? DateTime.Now.Year;

                var allocations = await _context.StudentSubjects
                    .Include(ss => ss.Subject)
                    .Include(ss => ss.Teacher)
                    .Where(ss => ss.StudentId == studentId && ss.IsActive)
                    .Select(ss => new
                    {
                        ss.Id,
                        ss.SubjectId,
                        SubjectName = ss.Subject != null ? ss.Subject.Name : "",
                        SubjectCode = ss.Subject != null ? ss.Subject.Code : "",
                        ss.TeacherId,
                        TeacherName = ss.Teacher != null ? ss.Teacher.Name : "",
                        ss.AcademicYear,
                        ss.Term,
                        ss.RegisteredAt
                    })
                    .ToListAsync();

                return Ok(allocations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student allocations");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get subjects for a specific class (Form Teacher/Admin)
        /// </summary>
        [HttpGet("class-subjects/{classId}")]
        [Authorize(Roles = "FormTeacher,Admin")]
        [SwaggerOperation(Summary = "Get class subjects", Description = "Retrieves all subjects offered in a specific class")]
        [SwaggerResponse(200, "List of subjects", typeof(List<object>))]
        [SwaggerResponse(404, "Class not found")]
        public async Task<IActionResult> GetClassSubjects(int classId)
        {
            try
            {
                var classEntity = await _context.Classes.FindAsync(classId);
                if (classEntity == null)
                    return NotFound(new { message = "Class not found" });

                var subjects = await _context.ClassSubjects
                    .Include(cs => cs.Subject)
                    .Include(cs => cs.Teacher)
                    .Where(cs => cs.ClassId == classId)
                    .Select(cs => new
                    {
                        cs.SubjectId,
                        SubjectName = cs.Subject != null ? cs.Subject.Name : "",
                        SubjectCode = cs.Subject != null ? cs.Subject.Code : "",
                        cs.TeacherId,
                        TeacherName = cs.Teacher != null ? cs.Teacher.Name : ""
                    })
                    .ToListAsync();

                return Ok(subjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting class subjects");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get subjects registered by the logged-in student
        /// </summary>
        [HttpGet("my-subjects")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "Get my registered subjects", Description = "Retrieves subjects registered by the logged-in student")]
        [SwaggerResponse(200, "List of registered subjects", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetMySubjects()
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var subjects = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                .Include(ss => ss.Teacher)
                .Where(ss => ss.StudentId == studentId && ss.IsActive)
                .Select(ss => new
                {
                    ss.Id,
                    ss.SubjectId,
                    SubjectName = ss.Subject != null ? ss.Subject.Name : "",
                    SubjectCode = ss.Subject != null ? ss.Subject.Code : "",
                    TeacherName = ss.Teacher != null ? ss.Teacher.Name : "",
                    TeacherEmail = ss.Teacher != null ? ss.Teacher.Email : "",
                    ss.AcademicYear,
                    ss.Term,
                    ss.RegisteredAt
                })
                .ToListAsync();
            
            return Ok(subjects);
        }

        /// <summary>
        /// Get students assigned to the logged-in teacher
        /// </summary>
        [HttpGet("teacher-students")]
        [Authorize(Roles = "Teacher")]
        [SwaggerOperation(Summary = "Get teacher's students", Description = "Retrieves students assigned to the logged-in teacher")]
        [SwaggerResponse(200, "List of students", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetTeacherStudents([FromQuery] int? subjectId)
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var query = _context.StudentSubjects
                .Include(ss => ss.Student)
                .Include(ss => ss.Subject)
                .Where(ss => ss.TeacherId == teacherId && ss.IsActive);

            if (subjectId.HasValue)
            {
                query = query.Where(ss => ss.SubjectId == subjectId.Value);
            }
            
            var students = await query
                .Select(ss => new
                {
                    ss.Id,
                    ss.StudentId,
                    StudentName = ss.Student != null ? ss.Student.FullName : "",
                    AdmissionNumber = ss.Student != null ? ss.Student.AdmissionNumber : "",
                    StudentClass = ss.Student != null ? ss.Student.Class : "",
                    StudentStream = ss.Student != null ? ss.Student.Stream : "",
                    ss.SubjectId,
                    SubjectName = ss.Subject != null ? ss.Subject.Name : "",
                    ss.AcademicYear,
                    ss.Term,
                    ss.RegisteredAt
                })
                .OrderBy(s => s.StudentName)
                .ToListAsync();
            
            return Ok(students);
        }

        /// <summary>
        /// Get allocation summary statistics
        /// </summary>
        [HttpGet("summary")]
        [Authorize(Roles = "FormTeacher,Admin")]
        [SwaggerOperation(Summary = "Get allocation summary", Description = "Retrieves summary statistics for allocations")]
        [SwaggerResponse(200, "Summary statistics", typeof(object))]
        public async Task<IActionResult> GetAllocationSummary([FromQuery] int year)
        {
            try
            {
                var currentYear = year > 0 ? year : DateTime.Now.Year;
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var query = _context.StudentSubjects
                    .Where(ss => ss.AcademicYear == currentYear && ss.IsActive);

                // For Form Teachers, filter by their classes
                if (userRole == "FormTeacher")
                {
                    var assignedClassIds = await _context.FormTeacherClasses
                        .Where(ftc => ftc.TeacherId == teacherId)
                        .Select(ftc => ftc.ClassId)
                        .ToListAsync();

                    if (assignedClassIds.Any())
                    {
                        query = query.Where(ss => ss.Student != null && 
                            assignedClassIds.Contains(ss.Student.ClassId ?? 0));
                    }
                }

                var totalAllocations = await query.CountAsync();
                var totalStudents = await query
                    .Select(ss => ss.StudentId)
                    .Distinct()
                    .CountAsync();
                var totalSubjects = await query
                    .Select(ss => ss.SubjectId)
                    .Distinct()
                    .CountAsync();

                // Get subject distribution
                var subjectDistribution = await query
                    .GroupBy(ss => ss.SubjectId)
                    .Select(g => new
                    {
                        SubjectId = g.Key,
                        SubjectName = _context.Subjects.FirstOrDefault(s => s.Id == g.Key).Name,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                return Ok(new
                {
                    TotalAllocations = totalAllocations,
                    TotalStudents = totalStudents,
                    TotalSubjects = totalSubjects,
                    SubjectDistribution = subjectDistribution,
                    AcademicYear = currentYear
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting allocation summary");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        #region Private Methods

        private async Task SendRegistrationNotifications(Student student, Subject subject, User teacher, int academicYear, string term)
        {
            try
            {
                // Notification to student
                var studentNotification = new Notification
                {
                    Title = "Subject Registration Successful",
                    Message = $"You have been registered for {subject.Name} for {term} {academicYear}.",
                    Type = "Success",
                    StudentId = student.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(studentNotification);

                // Notification to teacher
                if (teacher != null)
                {
                    var teacherNotification = new Notification
                    {
                        Title = "New Student Registered",
                        Message = $"Student {student.FullName} has been registered for {subject.Name} for {term} {academicYear}.",
                        Type = "StudentRegistration",
                        TeacherId = teacher.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    _context.Notifications.Add(teacherNotification);
                }

                // Notification to Admin
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                if (adminUser != null)
                {
                    var adminNotification = new Notification
                    {
                        Title = "Student Subject Registration",
                        Message = $"Student {student.FullName} ({student.AdmissionNumber}) registered for {subject.Name} for {term} {academicYear}.",
                        Type = "Info",
                        UserId = adminUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    _context.Notifications.Add(adminNotification);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending registration notifications");
            }
        }

        #endregion
    }

    // DTOs
    public class RegisterSubjectDTO
    {
        [Required(ErrorMessage = "Student ID is required")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Subject ID is required")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Academic year is required")]
        public int AcademicYear { get; set; }

        [Required(ErrorMessage = "Term is required")]
        public string Term { get; set; } = "Term 1";
    }

    public class BulkRegisterSubjectDTO
    {
        [Required(ErrorMessage = "Student IDs are required")]
        public List<int> StudentIds { get; set; } = new List<int>();

        [Required(ErrorMessage = "Subject ID is required")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Academic year is required")]
        public int AcademicYear { get; set; }

        [Required(ErrorMessage = "Term is required")]
        public string Term { get; set; } = "Term 1";
    }
}