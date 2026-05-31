using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;
using School_Yathu.Models;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminSubjectAllocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminSubjectAllocationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get all available subjects
        [HttpGet("available-subjects")]
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

        // Get all teachers
        [HttpGet("teachers")]
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

        // Get all classes
        [HttpGet("classes")]
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

        // Get students by class
        [HttpGet("students-by-class/{className}/{stream}")]
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

        // Get all students with their current subject allocations
        [HttpGet("student-allocations")]
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

        // Get subjects allocated to a specific student
        [HttpGet("student-subjects/{studentId}")]
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

        // Allocate subjects to a single student - FIXED (using class lookup)
        [HttpPost("allocate-subjects-to-student")]
        public async Task<IActionResult> AllocateSubjectsToStudent([FromBody] SubjectAllocationDTO dto)
        {
            var student = await _context.Students.FindAsync(dto.StudentId);
            if (student == null)
                return BadRequest(new { message = "Student not found" });

            // Find the class by name and stream instead of ClassId
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Name == student.Class && c.Stream == student.Stream);

            if (classEntity == null)
                return BadRequest(new { message = "Class not found for this student" });

            var existingAllocations = await _context.StudentSubjects
                .Where(ss => ss.StudentId == dto.StudentId && ss.AcademicYear == dto.AcademicYear && ss.IsActive)
                .ToListAsync();

            // Remove existing allocations for this year/term if updating
            if (existingAllocations.Any())
            {
                _context.StudentSubjects.RemoveRange(existingAllocations);
            }

            // Add new allocations
            foreach (var subjectId in dto.SubjectIds)
            {
                // Get teacher assigned to this subject for this class
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

            // Send notifications
            await SendAllocationNotifications(dto.StudentId, dto.SubjectIds, dto.AcademicYear, dto.Term);

            return Ok(new { message = "Subjects allocated successfully to student" });
        }

        // Bulk allocate subjects to all students in a class - FIXED
        [HttpPost("bulk-allocate-to-class")]
        public async Task<IActionResult> BulkAllocateToClass([FromBody] BulkSubjectAllocationDTO dto)
        {
            var students = await _context.Students
                .Where(s => s.Class == dto.ClassName && s.Stream == dto.Stream)
                .ToListAsync();

            if (!students.Any())
                return BadRequest(new { message = "No students found in this class" });

            // Find the class entity
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Name == dto.ClassName && c.Stream == dto.Stream);

            if (classEntity == null)
                return BadRequest(new { message = "Class not found" });

            var allocations = new List<StudentSubject>();

            foreach (var student in students)
            {
                // Remove existing allocations for this year/term
                var existing = await _context.StudentSubjects
                    .Where(ss => ss.StudentId == student.Id && ss.AcademicYear == dto.AcademicYear && ss.Term == dto.Term)
                    .ToListAsync();

                if (existing.Any())
                {
                    _context.StudentSubjects.RemoveRange(existing);
                }

                // Add new allocations
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

            // Send notifications to all students
            foreach (var student in students)
            {
                await SendAllocationNotifications(student.Id, dto.SubjectIds, dto.AcademicYear, dto.Term);
            }

            // Send notifications to teachers
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

        // Remove subject allocation from a student
        [HttpDelete("remove-allocation/{allocationId}")]
        public async Task<IActionResult> RemoveAllocation(int allocationId)
        {
            var allocation = await _context.StudentSubjects.FindAsync(allocationId);
            if (allocation == null)
                return NotFound(new { message = "Allocation not found" });

            _context.StudentSubjects.Remove(allocation);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subject allocation removed successfully" });
        }

        // Get subjects not yet allocated to a student - FIXED
        [HttpGet("available-subjects-for-student/{studentId}")]
        public async Task<IActionResult> GetAvailableSubjectsForStudent(int studentId, [FromQuery] int year)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            // Find the class entity
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

        // Get summary statistics
        [HttpGet("summary")]
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

        // Helper method to send notifications
        private async Task SendAllocationNotifications(int studentId, List<int> subjectIds, int academicYear, string term)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return;

            var subjects = await _context.Subjects
                .Where(s => subjectIds.Contains(s.Id))
                .ToListAsync();

            var subjectNames = string.Join(", ", subjects.Select(s => s.Name));

            // Find the class entity
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Name == student.Class && c.Stream == student.Stream);

            // Notification to student
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

            // Notifications to teachers
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
        }
    }
}