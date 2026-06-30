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
    [SwaggerTag("Admin Subject Allocation - Manage student subject allocations")]
    public class AdminSubjectAllocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminSubjectAllocationController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all available subjects
        /// </summary>
        [HttpGet("available-subjects")]
        [SwaggerOperation(Summary = "Get all available subjects", Description = "Retrieves a list of all subjects in the system")]
        [SwaggerResponse(200, "List of subjects", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetAvailableSubjects()
        {
            var subjects = await _context.Subjects
                .OrderBy(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Code
                })
                .ToListAsync();

            return Ok(subjects);
        }

        /// <summary>
        /// Get all teachers
        /// </summary>
        [HttpGet("teachers")]
        [SwaggerOperation(Summary = "Get all teachers", Description = "Retrieves a list of all active teachers")]
        [SwaggerResponse(200, "List of teachers", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetTeachers()
        {
            var teachers = await _context.Users
                .Where(u => u.Role == "Teacher" && u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.EmployeeId
                })
                .ToListAsync();

            return Ok(teachers);
        }

        /// <summary>
        /// Get all classes
        /// </summary>
        [HttpGet("classes")]
        [SwaggerOperation(Summary = "Get all classes", Description = "Retrieves a list of all classes with their streams")]
        [SwaggerResponse(200, "List of classes", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetClasses()
        {
            var classes = await _context.Classes
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Stream,
                    FullName = $"{c.Name} {c.Stream}"
                })
                .ToListAsync();

            return Ok(classes);
        }

        /// <summary>
        /// Get students by class
        /// </summary>
        [HttpGet("students-by-class/{className}/{stream}")]
        [SwaggerOperation(Summary = "Get students by class", Description = "Retrieves all students in a specific class and stream")]
        [SwaggerResponse(200, "List of students", typeof(List<object>))]
        [SwaggerResponse(404, "No students found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetStudentsByClass(string className, string stream)
        {
            var students = await _context.Students
                .Where(s => s.Class == className && s.Stream == stream)
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

        /// <summary>
        /// Get student allocations
        /// </summary>
        [HttpGet("student-allocations")]
        [SwaggerOperation(Summary = "Get student allocations", Description = "Retrieves all student subject allocations for a year")]
        [SwaggerResponse(200, "List of allocations", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetStudentAllocations([FromQuery] int? classId, [FromQuery] int? year)
        {
            var currentYear = year ?? DateTime.Now.Year;

            var query = _context.StudentSubjects
                .Include(ss => ss.Student)
                .Include(ss => ss.Subject)
                .Include(ss => ss.Teacher)
                .Where(ss => ss.AcademicYear == currentYear && ss.IsActive);

            if (classId.HasValue)
            {
                query = query.Where(ss => ss.Student != null && ss.Student.Id == classId);
            }

            var allocations = await query
                .Select(ss => new
                {
                    ss.Id,
                    StudentId = ss.StudentId,
                    StudentName = ss.Student != null ? ss.Student.FullName : "",
                    AdmissionNumber = ss.Student != null ? ss.Student.AdmissionNumber : "",
                    StudentClass = ss.Student != null ? ss.Student.Class : "",
                    StudentStream = ss.Student != null ? ss.Student.Stream : "",
                    SubjectId = ss.SubjectId,
                    SubjectName = ss.Subject != null ? ss.Subject.Name : "",
                    TeacherId = ss.TeacherId,
                    TeacherName = ss.Teacher != null ? ss.Teacher.Name : "",
                    ss.AcademicYear,
                    ss.Term,
                    ss.RegisteredAt
                })
                .ToListAsync();

            return Ok(allocations);
        }

        /// <summary>
        /// Get subjects allocated to a specific student
        /// </summary>
        [HttpGet("student-subjects/{studentId}")]
        [SwaggerOperation(Summary = "Get student subjects", Description = "Retrieves all subjects allocated to a specific student")]
        [SwaggerResponse(200, "Student subjects", typeof(object))]
        [SwaggerResponse(404, "Student not found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetStudentSubjects(int studentId, [FromQuery] int year)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            var allocatedSubjects = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                .Include(ss => ss.Teacher)
                .Where(ss => ss.StudentId == studentId && ss.AcademicYear == year && ss.IsActive)
                .Select(ss => new
                {
                    ss.Id,
                    ss.SubjectId,
                    SubjectName = ss.Subject != null ? ss.Subject.Name : "",
                    ss.TeacherId,
                    TeacherName = ss.Teacher != null ? ss.Teacher.Name : "",
                    ss.AcademicYear,
                    ss.Term
                })
                .ToListAsync();

            var allSubjects = await _context.Subjects
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Code,
                    IsAllocated = allocatedSubjects.Any(a => a.SubjectId == s.Id)
                })
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
                AllocatedSubjects = allocatedSubjects,
                AvailableSubjects = allSubjects
            });
        }

        /// <summary>
        /// Allocate subjects to a student
        /// </summary>
        [HttpPost("allocate-subjects-to-student")]
        [SwaggerOperation(Summary = "Allocate subjects to student", Description = "Allocates selected subjects to a specific student")]
        [SwaggerResponse(200, "Allocation successful", typeof(object))]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> AllocateSubjectsToStudent([FromBody] SubjectAllocationDTO dto)
        {
            var student = await _context.Students.FindAsync(dto.StudentId);
            if (student == null)
                return BadRequest(new { message = "Student not found" });

            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Name == student.Class && c.Stream == student.Stream);

            if (classEntity == null)
                return BadRequest(new { message = "Class not found for this student" });

            var existingAllocations = await _context.StudentSubjects
                .Where(ss => ss.StudentId == dto.StudentId && ss.AcademicYear == dto.AcademicYear && ss.IsActive)
                .ToListAsync();

            if (existingAllocations.Any())
            {
                _context.StudentSubjects.RemoveRange(existingAllocations);
            }

            foreach (var subjectId in dto.SubjectIds)
            {
                var classSubject = await _context.ClassSubjects
                    .FirstOrDefaultAsync(cs => cs.ClassId == classEntity.Id && cs.SubjectId == subjectId);

                if (classSubject == null)
                    continue;

                var allocation = new StudentSubject
                {
                    StudentId = dto.StudentId,
                    SubjectId = subjectId,
                    TeacherId = classSubject.TeacherId,
                    AcademicYear = dto.AcademicYear,
                    Term = dto.Term,
                    RegisteredAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.StudentSubjects.Add(allocation);
            }

            await _context.SaveChangesAsync();
            await SendAllocationNotifications(dto.StudentId, dto.SubjectIds, dto.AcademicYear, dto.Term);

            return Ok(new { message = "Subjects allocated successfully to student" });
        }

        /// <summary>
        /// Bulk allocate subjects to a class
        /// </summary>
        [HttpPost("bulk-allocate-to-class")]
        [SwaggerOperation(Summary = "Bulk allocate subjects to class", Description = "Allocates subjects to all students in a class")]
        [SwaggerResponse(200, "Bulk allocation successful", typeof(object))]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> BulkAllocateToClass([FromBody] BulkSubjectAllocationDTO dto)
        {
            var students = await _context.Students
                .Where(s => s.Class == dto.ClassName && s.Stream == dto.Stream)
                .ToListAsync();

            if (!students.Any())
                return BadRequest(new { message = "No students found in this class" });

            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Name == dto.ClassName && c.Stream == dto.Stream);

            if (classEntity == null)
                return BadRequest(new { message = "Class not found" });

            var allocations = new List<StudentSubject>();

            foreach (var student in students)
            {
                var existing = await _context.StudentSubjects
                    .Where(ss => ss.StudentId == student.Id && ss.AcademicYear == dto.AcademicYear && ss.Term == dto.Term)
                    .ToListAsync();

                if (existing.Any())
                {
                    _context.StudentSubjects.RemoveRange(existing);
                }

                foreach (var subjectId in dto.SubjectIds)
                {
                    var classSubject = await _context.ClassSubjects
                        .FirstOrDefaultAsync(cs => cs.ClassId == classEntity.Id && cs.SubjectId == subjectId);

                    if (classSubject == null)
                        continue;

                    var allocation = new StudentSubject
                    {
                        StudentId = student.Id,
                        SubjectId = subjectId,
                        TeacherId = classSubject.TeacherId,
                        AcademicYear = dto.AcademicYear,
                        Term = dto.Term,
                        RegisteredAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    allocations.Add(allocation);
                }
            }

            _context.StudentSubjects.AddRange(allocations);
            await _context.SaveChangesAsync();

            foreach (var student in students)
            {
                await SendAllocationNotifications(student.Id, dto.SubjectIds, dto.AcademicYear, dto.Term);
            }

            var teacherIds = allocations.Select(a => a.TeacherId).Distinct();
            foreach (var teacherId in teacherIds)
            {
                var teacher = await _context.Users.FindAsync(teacherId);
                if (teacher != null)
                {
                    var teacherNotification = new Notification
                    {
                        Title = "New Students Allocated",
                        Message = $"Students from {dto.ClassName} {dto.Stream} have been allocated to your subjects for {dto.Term} {dto.AcademicYear}.",
                        Type = "SubjectAllocation",
                        TeacherId = teacherId,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    _context.Notifications.Add(teacherNotification);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = $"Subjects allocated successfully to {students.Count} students in {dto.ClassName} {dto.Stream}",
                studentCount = students.Count
            });
        }

        /// <summary>
        /// Remove subject allocation
        /// </summary>
        [HttpDelete("remove-allocation/{allocationId}")]
        [SwaggerOperation(Summary = "Remove subject allocation", Description = "Removes a subject allocation from a student")]
        [SwaggerResponse(200, "Allocation removed successfully")]
        [SwaggerResponse(404, "Allocation not found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> RemoveAllocation(int allocationId)
        {
            var allocation = await _context.StudentSubjects.FindAsync(allocationId);
            if (allocation == null)
                return NotFound(new { message = "Allocation not found" });

            _context.StudentSubjects.Remove(allocation);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subject allocation removed successfully" });
        }

        /// <summary>
        /// Get available subjects for a student
        /// </summary>
        [HttpGet("available-subjects-for-student/{studentId}")]
        [SwaggerOperation(Summary = "Get available subjects for student", Description = "Retrieves subjects available for allocation to a student")]
        [SwaggerResponse(200, "List of available subjects", typeof(List<object>))]
        [SwaggerResponse(404, "Student not found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetAvailableSubjectsForStudent(int studentId, [FromQuery] int year)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Name == student.Class && c.Stream == student.Stream);

            if (classEntity == null)
                return Ok(new List<object>());

            var allocatedSubjectIds = await _context.StudentSubjects
                .Where(ss => ss.StudentId == studentId && ss.AcademicYear == year && ss.IsActive)
                .Select(ss => ss.SubjectId)
                .ToListAsync();

            var availableSubjects = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .Where(cs => cs.ClassId == classEntity.Id && !allocatedSubjectIds.Contains(cs.SubjectId))
                .Select(cs => new
                {
                    cs.SubjectId,
                    SubjectName = cs.Subject != null ? cs.Subject.Name : "",
                    cs.TeacherId,
                    TeacherName = cs.Teacher != null ? cs.Teacher.Name : ""
                })
                .ToListAsync();

            return Ok(availableSubjects);
        }

        /// <summary>
        /// Allocate teacher to subject
        /// </summary>
        [HttpPost("allocate-teacher-to-subject")]
        [SwaggerOperation(Summary = "Allocate teacher to subject", Description = "Assigns a teacher to teach a specific subject in a class")]
        [SwaggerResponse(200, "Teacher allocated successfully", typeof(object))]
        [SwaggerResponse(500, "Error allocating teacher")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> AllocateTeacherToSubject([FromBody] TeacherSubjectAllocationDTO dto)
        {
            try
            {
                var existing = await _context.TeacherSubjectAllocations
                    .FirstOrDefaultAsync(a => a.ClassId == dto.ClassId && a.SubjectId == dto.SubjectId);

                if (existing != null)
                {
                    existing.TeacherId = dto.TeacherId;
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Teacher updated successfully for this subject" });
                }

                var allocation = new TeacherSubjectAllocation
                {
                    ClassId = dto.ClassId,
                    SubjectId = dto.SubjectId,
                    TeacherId = dto.TeacherId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.TeacherSubjectAllocations.Add(allocation);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Teacher allocated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error allocating teacher: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get allocation summary statistics
        /// </summary>
        [HttpGet("summary")]
        [SwaggerOperation(Summary = "Get allocation summary", Description = "Retrieves summary statistics for allocations")]
        [SwaggerResponse(200, "Summary statistics", typeof(object))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetAllocationSummary()
        {
            var currentYear = DateTime.Now.Year;
            
            var totalStudents = await _context.Students.CountAsync();
            var studentsWithAllocations = await _context.StudentSubjects
                .Where(ss => ss.AcademicYear == currentYear)
                .Select(ss => ss.StudentId)
                .Distinct()
                .CountAsync();
            
            var totalAllocations = await _context.StudentSubjects
                .Where(ss => ss.AcademicYear == currentYear && ss.IsActive)
                .CountAsync();
            
            var totalSubjects = await _context.Subjects.CountAsync();
            var totalTeachers = await _context.Users.CountAsync(u => u.Role == "Teacher" && u.IsActive);
            
            return Ok(new
            {
                TotalStudents = totalStudents,
                StudentsWithAllocations = studentsWithAllocations,
                StudentsWithoutAllocations = totalStudents - studentsWithAllocations,
                TotalAllocations = totalAllocations,
                TotalSubjects = totalSubjects,
                TotalTeachers = totalTeachers,
                AcademicYear = currentYear
            });
        }

        private async Task SendAllocationNotifications(int studentId, List<int> subjectIds, int academicYear, string term)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return;

            var subjects = await _context.Subjects
                .Where(s => subjectIds.Contains(s.Id))
                .ToListAsync();

            var subjectNames = string.Join(", ", subjects.Select(s => s.Name));

            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Name == student.Class && c.Stream == student.Stream);

            var studentNotification = new Notification
            {
                Title = "Subjects Allocated",
                Message = $"You have been allocated the following subjects for {term} {academicYear}: {subjectNames}. Please check your dashboard.",
                Type = "SubjectAllocation",
                StudentId = studentId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            _context.Notifications.Add(studentNotification);

            if (classEntity != null)
            {
                foreach (var subject in subjects)
                {
                    var classSubject = await _context.ClassSubjects
                        .FirstOrDefaultAsync(cs => cs.ClassId == classEntity.Id && cs.SubjectId == subject.Id);

                    if (classSubject != null)
                    {
                        var teacherNotification = new Notification
                        {
                            Title = "New Student Allocated",
                            Message = $"Student {student.FullName} ({student.AdmissionNumber}) has been allocated to {subject.Name} for {term} {academicYear}.",
                            Type = "StudentAllocation",
                            TeacherId = classSubject.TeacherId,
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        };
                        _context.Notifications.Add(teacherNotification);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }

    // ❌ REMOVE THIS INNER CLASS - It's now in DTOs/SubjectAllocationDTOs.cs
    // public class TeacherSubjectAllocationDTO
    // {
    //     public int ClassId { get; set; }
    //     public int SubjectId { get; set; }
    //     public int TeacherId { get; set; }
    // }
}