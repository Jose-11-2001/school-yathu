using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;
using School_Yathu.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [SwaggerTag("Student Registration - Register new students with subject allocations")]
    public class StudentRegistrationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudentRegistrationController(ApplicationDbContext context)
        {
            _context = context;
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
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Name == className && c.Stream == stream);

            if (classEntity == null)
                return NotFound(new { message = "Class not found" });

            var classSubjects = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Where(cs => cs.ClassId == classEntity.Id && cs.IsActive)
                .Select(cs => cs.Subject)
                .ToListAsync();

            var coreSubjectNames = new List<string> { "Mathematics", "English", "Chichewa" };
            var humanitiesSubjects = new List<string> { "History", "Geography", "Social Studies", "Religious Education" };
            var scienceSubjects = new List<string> { "Physics", "Chemistry", "Biology", "Computer Science", "General Science" };

            var coreSubjects = classSubjects
                .Where(s => coreSubjectNames.Contains(s.Name))
                .Select(s => s.Name)
                .ToList();

            var humanities = classSubjects
                .Where(s => humanitiesSubjects.Contains(s.Name))
                .Select(s => s.Name)
                .ToList();

            var sciences = classSubjects
                .Where(s => scienceSubjects.Contains(s.Name))
                .Select(s => s.Name)
                .ToList();

            return Ok(new
            {
                coreSubjects = coreSubjects,
                humanitiesSubjects = humanities,
                scienceSubjects = sciences,
                availableSubjects = classSubjects.Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Code
                }).ToList()
            });
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
                    s.CreatedAt,
                    SubjectsCount = _context.StudentSubjects.Count(ss => ss.StudentId == s.Id && ss.IsActive)
                })
                .OrderBy(s => s.Class)
                .ThenBy(s => s.Stream)
                .ThenBy(s => s.FullName)
                .ToListAsync();

            return Ok(students);
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

                var classEntity = await _context.Classes
                    .FirstOrDefaultAsync(c => c.Name == dto.Class && c.Stream == dto.Stream);

                var student = new Student
                {
                    AdmissionNumber = dto.AdmissionNumber,
                    FullName = dto.FullName,
                    Class = dto.Class,
                    Stream = dto.Stream,
                    TeacherId = dto.TeacherId,
                    ClassId = classEntity?.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

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

                if (dto.SelectedSubjectIds != null && dto.SelectedSubjectIds.Any())
                {
                    foreach (var subjectId in dto.SelectedSubjectIds)
                    {
                        if (!subjectIdsToAllocate.Contains(subjectId))
                            subjectIdsToAllocate.Add(subjectId);
                    }
                }

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
                        SubjectsAllocated = subjectIdsToAllocate.Count,
                        LoginCredentials = userAccount
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
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
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            var allocatedSubjects = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                .Where(ss => ss.StudentId == studentId && ss.IsActive)
                .Select(ss => ss.SubjectId)
                .ToListAsync();

            bool isUpperForm = student.Class.Contains("Form 3") || student.Class.Contains("Form 4") ||
                               student.Class.Contains("Form3") || student.Class.Contains("Form4");

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
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            student.FullName = dto.FullName;
            student.Class = dto.Class;
            student.Stream = dto.Stream;
            student.TeacherId = dto.TeacherId;
            student.UpdatedAt = DateTime.Now;

            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Name == dto.Class && c.Stream == dto.Stream);
            student.ClassId = classEntity?.Id;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Student updated successfully" });
        }

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
            var subjects = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                .Where(ss => ss.StudentId == studentId && ss.IsActive)
                .Select(ss => ss.Subject!.Name)
                .ToListAsync();

            var humanitiesCount = subjects.Count(IsHumanitiesSubject);
            var sciencesCount = subjects.Count(IsScienceSubject);

            if (humanitiesCount > sciencesCount) return "Humanities";
            if (sciencesCount > humanitiesCount) return "Sciences";
            return "Balanced";
        }

        private async Task<object> CreateStudentUserAccount(Student student)
        {
            var password = GenerateRandomPassword();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == $"{student.AdmissionNumber.ToLower()}@student.school.com");

            if (existingUser != null)
            {
                return new
                {
                    Email = existingUser.Email,
                    Password = password,
                    Message = "User account already exists. Password reset sent."
                };
            }

            var user = new User
            {
                Email = $"{student.AdmissionNumber.ToLower()}@student.school.com",
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
                Message = "Student login credentials created"
            };
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}