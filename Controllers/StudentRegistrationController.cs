using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;
using School_Yathu.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [SwaggerTag("Student Registration - Register new students with subject allocations")]
    public class StudentRegistrationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StudentRegistrationController> _logger;

        public StudentRegistrationController(ApplicationDbContext context, ILogger<StudentRegistrationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all classes for registration
        /// </summary>
        [HttpGet("classes")]
        [SwaggerOperation(Summary = "Get all classes", Description = "Retrieves a list of all classes for student registration")]
        [SwaggerResponse(200, "List of classes", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetClasses()
        {
            try
            {
                var classes = await _context.Classes
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Stream
                    })
                    .OrderBy(c => c.Name)
                    .ThenBy(c => c.Stream)
                    .ToListAsync();

                return Ok(classes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting classes");
                return StatusCode(500, new { message = "An error occurred while retrieving classes" });
            }
        }

        /// <summary>
        /// Get available subjects by class and stream
        /// </summary>
        [HttpGet("available-subjects/{className}/{stream}")]
        [SwaggerOperation(Summary = "Get available subjects", Description = "Retrieves subjects available for a specific class and stream")]
        [SwaggerResponse(200, "Available subjects", typeof(object))]
        [SwaggerResponse(404, "Class not found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetAvailableSubjects(string className, string stream, [FromQuery] string? root)
        {
            try
            {
                var classEntity = await _context.Classes
                    .FirstOrDefaultAsync(c => c.Name == className && c.Stream == stream);

                if (classEntity == null)
                    return NotFound(new { message = "Class not found" });

                var classSubjects = await _context.ClassSubjects
                    .Include(cs => cs.Subject)
                    .Where(cs => cs.ClassId == classEntity.Id && cs.IsActive)
                    .Select(cs => cs.Subject)
                    .ToListAsync();

                // Define subject categories
                var coreSubjectNames = new List<string> { "Mathematics", "English", "Chichewa" };
                var humanitiesSubjectNames = new List<string> { "History", "Geography", "Social Studies", "Religious Education" };
                var scienceSubjectNames = new List<string> { "Physics", "Chemistry", "Biology", "Computer Science", "General Science" };

                var coreSubjects = classSubjects
                    .Where(s => s != null && coreSubjectNames.Contains(s.Name))
                    .Select(s => s.Name)
                    .ToList();

                var humanities = classSubjects
                    .Where(s => s != null && humanitiesSubjectNames.Contains(s.Name))
                    .Select(s => s.Name)
                    .ToList();

                var sciences = classSubjects
                    .Where(s => s != null && scienceSubjectNames.Contains(s.Name))
                    .Select(s => s.Name)
                    .ToList();

                return Ok(new
                {
                    coreSubjects = coreSubjects,
                    humanitiesSubjects = humanities,
                    scienceSubjects = sciences,
                    availableSubjects = classSubjects
                        .Where(s => s != null)
                        .Select(s => new
                        {
                            s.Id,
                            s.Name,
                            s.Code
                        })
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available subjects for class: {ClassName}, stream: {Stream}", className, stream);
                return StatusCode(500, new { message = "An error occurred while retrieving available subjects" });
            }
        }

        /// <summary>
        /// Get all registered students
        /// </summary>
        [HttpGet("registered-students")]
        [SwaggerOperation(Summary = "Get registered students", Description = "Retrieves a list of all registered students")]
        [SwaggerResponse(200, "List of students", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetRegisteredStudents([FromQuery] string? className, [FromQuery] string? stream)
        {
            try
            {
                var query = _context.Students.AsQueryable();

                if (!string.IsNullOrEmpty(className))
                    query = query.Where(s => s.Class == className);

                if (!string.IsNullOrEmpty(stream))
                    query = query.Where(s => s.Stream == stream);

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
                        SubjectsCount = _context.StudentSubjects.Count(ss => ss.StudentId == s.Id && ss.IsActive)
                    })
                    .OrderBy(s => s.Class)
                    .ThenBy(s => s.Stream)
                    .ThenBy(s => s.FullName)
                    .ToListAsync();

                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting registered students");
                return StatusCode(500, new { message = "An error occurred while retrieving students" });
            }
        }

        /// <summary>
        /// Get student subjects with teachers
        /// </summary>
        [HttpGet("student-subjects")]
        [SwaggerOperation(Summary = "Get student subjects", Description = "Retrieves subjects for a student with teacher details")]
        public async Task<IActionResult> GetStudentSubjects([FromQuery] string className, [FromQuery] string stream)
        {
            try
            {
                if (string.IsNullOrEmpty(className))
                    return BadRequest(new { message = "Class is required" });

                var classEntity = await _context.Classes
                    .FirstOrDefaultAsync(c => c.Name == className && (string.IsNullOrEmpty(stream) || c.Stream == stream));

                if (classEntity == null)
                    return NotFound(new { message = "Class not found" });

                var subjects = await _context.ClassSubjects
                    .Include(cs => cs.Subject)
                    .Include(cs => cs.Teacher)
                    .Where(cs => cs.ClassId == classEntity.Id && cs.IsActive)
                    .Select(cs => new
                    {
                        cs.Id,
                        Name = cs.Subject != null ? cs.Subject.Name : "Unknown",
                        Code = cs.Subject != null ? cs.Subject.Code : "N/A",
                        Type = cs.Subject != null ? cs.Subject.Type : "Core",
                        TeacherName = cs.Teacher != null ? cs.Teacher.Name : "Not assigned",
                        TeacherEmail = cs.Teacher != null ? cs.Teacher.Email : null,
                        ClassName = classEntity.Name,
                        Stream = classEntity.Stream
                    })
                    .ToListAsync();

                return Ok(new { subjects });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student subjects");
                return StatusCode(500, new { message = "An error occurred while retrieving subjects" });
            }
        }

        /// <summary>
        /// Register a new student with subjects
        /// </summary>
        [HttpPost("register")]
        [SwaggerOperation(Summary = "Register a new student", Description = "Registers a new student with subject allocations")]
        [SwaggerResponse(200, "Student registered successfully", typeof(object))]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> RegisterStudent([FromBody] StudentRegistrationDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validate input
                if (dto == null)
                    return BadRequest(new { message = "Invalid student data" });

                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.AdmissionNumber == dto.AdmissionNumber);

                if (existingStudent != null)
                    return BadRequest(new { message = $"Student with admission number '{dto.AdmissionNumber}' already exists" });

                bool isUpperForm = dto.Class.Contains("Form 3") || dto.Class.Contains("Form 4") ||
                                   dto.Class.Contains("Form3") || dto.Class.Contains("Form4");

                if (isUpperForm && string.IsNullOrEmpty(dto.Root))
                    return BadRequest(new { message = "Root (Humanities or Sciences) is required for Form 3 and Form 4 students" });

                if (!isUpperForm && !string.IsNullOrEmpty(dto.Root))
                    return BadRequest(new { message = "Root selection is only for Form 3 and Form 4 students" });

                // Get class entity
                var classEntity = await _context.Classes
                    .FirstOrDefaultAsync(c => c.Name == dto.Class && c.Stream == dto.Stream);

                if (classEntity == null)
                    return BadRequest(new { message = "Class not found. Please add the class first." });

                // Create student
                var student = new Student
                {
                    AdmissionNumber = dto.AdmissionNumber,
                    FullName = dto.FullName,
                    Class = dto.Class,
                    Stream = dto.Stream,
                    TeacherId = dto.TeacherId,
                    ClassId = classEntity.Id,
                    Email = $"{dto.AdmissionNumber.ToLower()}@student.school.com",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Allocate subjects
                var subjectIdsToAllocate = new List<int>();

                if (isUpperForm)
                {
                    var classSubjects = await _context.ClassSubjects
                        .Include(cs => cs.Subject)
                        .Where(cs => cs.ClassId == classEntity.Id && cs.IsActive)
                        .ToListAsync();

                    foreach (var classSubject in classSubjects)
                    {
                        var subjectName = classSubject.Subject?.Name ?? "";
                        
                        if (IsCoreSubject(subjectName))
                        {
                            subjectIdsToAllocate.Add(classSubject.SubjectId);
                        }
                        else if (dto.Root == "Humanities" && IsHumanitiesSubject(subjectName))
                        {
                            subjectIdsToAllocate.Add(classSubject.SubjectId);
                        }
                        else if (dto.Root == "Sciences" && IsScienceSubject(subjectName))
                        {
                            subjectIdsToAllocate.Add(classSubject.SubjectId);
                        }
                    }
                }
                else
                {
                    var classSubjects = await _context.ClassSubjects
                        .Where(cs => cs.ClassId == classEntity.Id && cs.IsActive)
                        .Select(cs => cs.SubjectId)
                        .ToListAsync();
                    
                    subjectIdsToAllocate.AddRange(classSubjects);
                }

                // Add selected subjects if any
                if (dto.SelectedSubjectIds != null && dto.SelectedSubjectIds.Any())
                {
                    foreach (var subjectId in dto.SelectedSubjectIds)
                    {
                        if (!subjectIdsToAllocate.Contains(subjectId))
                            subjectIdsToAllocate.Add(subjectId);
                    }
                }

                // Create student subject allocations
                var currentYear = DateTime.Now.Year;
                foreach (var subjectId in subjectIdsToAllocate.Distinct())
                {
                    var classSubject = await _context.ClassSubjects
                        .FirstOrDefaultAsync(cs => cs.ClassId == classEntity.Id && cs.SubjectId == subjectId && cs.IsActive);

                    if (classSubject != null)
                    {
                        var allocation = new StudentSubject
                        {
                            StudentId = student.Id,
                            SubjectId = subjectId,
                            TeacherId = classSubject.TeacherId,
                            AcademicYear = currentYear,
                            Term = "Term 1",
                            RegisteredAt = DateTime.UtcNow,
                            IsActive = true
                        };
                        _context.StudentSubjects.Add(allocation);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Create user account
                var userAccount = await CreateStudentUserAccount(student);

                return Ok(new
                {
                    message = $"Student {student.FullName} registered successfully!",
                    student = new
                    {
                        student.Id,
                        student.AdmissionNumber,
                        student.FullName,
                        student.Class,
                        student.Stream,
                        Root = isUpperForm ? dto.Root : null,
                        SubjectsAllocated = subjectIdsToAllocate.Distinct().Count(),
                        LoginCredentials = userAccount
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error registering student");
                return StatusCode(500, new { message = $"Error registering student: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get student details for editing
        /// </summary>
        [HttpGet("student-details/{studentId}")]
        [SwaggerOperation(Summary = "Get student details", Description = "Retrieves student details for editing")]
        [SwaggerResponse(200, "Student details", typeof(object))]
        [SwaggerResponse(404, "Student not found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetStudentDetails(int studentId)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                var allocatedSubjects = await _context.StudentSubjects
                    .Include(ss => ss.Subject)
                    .Where(ss => ss.StudentId == studentId && ss.IsActive)
                    .Select(ss => ss.SubjectId)
                    .ToListAsync();

                bool isUpperForm = student.Class != null && 
                    (student.Class.Contains("Form 3") || student.Class.Contains("Form 4") ||
                     student.Class.Contains("Form3") || student.Class.Contains("Form4"));

                return Ok(new
                {
                    student.Id,
                    student.AdmissionNumber,
                    student.FullName,
                    student.Class,
                    student.Stream,
                    student.TeacherId,
                    SelectedSubjectIds = allocatedSubjects,
                    Root = isUpperForm ? await GetStudentRoot(studentId) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student details");
                return StatusCode(500, new { message = "An error occurred while retrieving student details" });
            }
        }

        /// <summary>
        /// Update student information
        /// </summary>
        [HttpPut("update/{studentId}")]
        [SwaggerOperation(Summary = "Update student", Description = "Updates student information")]
        [SwaggerResponse(200, "Student updated successfully")]
        [SwaggerResponse(404, "Student not found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> UpdateStudent(int studentId, [FromBody] StudentRegistrationDTO dto)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                // Update fields
                student.FullName = dto.FullName;
                student.Class = dto.Class;
                student.Stream = dto.Stream;
                student.TeacherId = dto.TeacherId;
                student.UpdatedAt = DateTime.UtcNow;

                // Update class reference
                var classEntity = await _context.Classes
                    .FirstOrDefaultAsync(c => c.Name == dto.Class && c.Stream == dto.Stream);
                student.ClassId = classEntity?.Id;

                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "Student updated successfully",
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
                _logger.LogError(ex, "Error updating student");
                return StatusCode(500, new { message = "An error occurred while updating student" });
            }
        }

        #region Helper Methods

        private bool IsCoreSubject(string subjectName)
        {
            var core = new[] { "Mathematics", "English", "Chichewa" };
            return core.Contains(subjectName);
        }

        private bool IsHumanitiesSubject(string subjectName)
        {
            var humanities = new[] { "History", "Geography", "Social Studies", "Religious Education" };
            return humanities.Contains(subjectName);
        }

        private bool IsScienceSubject(string subjectName)
        {
            var sciences = new[] { "Physics", "Chemistry", "Biology", "Computer Science", "General Science" };
            return sciences.Contains(subjectName);
        }

        private async Task<string> GetStudentRoot(int studentId)
        {
            try
            {
                var subjects = await _context.StudentSubjects
                    .Include(ss => ss.Subject)
                    .Where(ss => ss.StudentId == studentId && ss.IsActive)
                    .Select(ss => ss.Subject != null ? ss.Subject.Name : "")
                    .ToListAsync();

                var humanitiesSubjects = new List<string> { "History", "Geography", "Social Studies", "Religious Education" };
                var scienceSubjects = new List<string> { "Physics", "Chemistry", "Biology", "Computer Science", "General Science" };

                var humanitiesCount = subjects.Count(s => humanitiesSubjects.Contains(s));
                var sciencesCount = subjects.Count(s => scienceSubjects.Contains(s));

                if (humanitiesCount > sciencesCount) 
                    return "Humanities";
                if (sciencesCount > humanitiesCount) 
                    return "Sciences";
                return "Balanced";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student root for student ID: {StudentId}", studentId);
                return "Unknown";
            }
        }

        private async Task<object> CreateStudentUserAccount(Student student)
        {
            try
            {
                var email = $"{student.AdmissionNumber.ToLower()}@student.school.com";
                var password = GenerateRandomPassword();
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (existingUser != null)
                {
                    // Reset password for existing user
                    existingUser.PasswordHash = passwordHash;
                    existingUser.MustChangePassword = true;
                    existingUser.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return new
                    {
                        Email = email,
                        Password = password,
                        Message = "User account already exists. Password has been reset."
                    };
                }

                var user = new User
                {
                    Email = email,
                    Name = student.FullName,
                    PasswordHash = passwordHash,
                    Role = "Student",
                    IsActive = true,
                    MustChangePassword = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return new
                {
                    Email = user.Email,
                    Password = password,
                    Message = "Student login credentials created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student user account");
                return new
                {
                    Message = "Student registered but user account creation failed",
                    Error = ex.Message
                };
            }
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        #endregion
    }
}