
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.Models;
using System.Security.Claims;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentSubjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public StudentSubjectController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        [HttpGet("available-subjects")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetAvailableSubjects()
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return BadRequest(new { message = "Student not found" });
            
            // Get subjects allocated to student's class
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
        
        [HttpPost("register")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> RegisterSubject([FromBody] RegisterSubjectDTO dto)
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return BadRequest(new { message = "Student not found" });
            
            var existing = await _context.StudentSubjects
                .FirstOrDefaultAsync(ss => ss.StudentId == studentId && ss.SubjectId == dto.SubjectId && ss.IsActive);
            
            if (existing != null)
                return BadRequest(new { message = "You are already registered for this subject" });
            
            var classSubject = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .FirstOrDefaultAsync(cs => cs.Class != null && cs.Class.Name == student.Class && cs.SubjectId == dto.SubjectId);
            
            if (classSubject == null)
                return BadRequest(new { message = "Subject not available for your class" });
            
            var registration = new StudentSubject
            {
                StudentId = studentId,
                SubjectId = dto.SubjectId,
                TeacherId = classSubject.TeacherId,
                RegisteredAt = DateTime.UtcNow,
                IsActive = true
            };
            
            _context.StudentSubjects.Add(registration);
            await _context.SaveChangesAsync();
            
            // Send notification to teacher
            var teacherNotification = new Notification
            {
                Title = "New Student Registration",
                Message = $"Student {student.FullName} (ID: {student.AdmissionNumber}) has registered for {classSubject.Subject.Name}.",
                Type = "Success",
                TeacherId = classSubject.TeacherId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            _context.Notifications.Add(teacherNotification);
            
            // Send notification to Admin
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            if (adminUser != null)
            {
                var adminNotification = new Notification
                {
                    Title = "Student Subject Registration",
                    Message = $"Student {student.FullName} has registered for {classSubject.Subject.Name} under teacher {classSubject.Teacher?.Name}.",
                    Type = "Info",
                    TeacherId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(adminNotification);
            }
            
            // Send confirmation notification to student
            var studentNotification = new Notification
            {
                Title = "Registration Successful",
                Message = $"You have successfully registered for {classSubject.Subject.Name}. Teacher {classSubject.Teacher?.Name} has been notified.",
                Type = "Success",
                StudentId = studentId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            _context.Notifications.Add(studentNotification);
            
            await _context.SaveChangesAsync();
            
            return Ok(new { 
                message = $"Successfully registered for {classSubject.Subject.Name}. Teacher has been notified.",
                teacherName = classSubject.Teacher?.Name
            });
        }
        
        [HttpGet("my-subjects")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMySubjects()
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var subjects = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                .Include(ss => ss.Teacher)
                .Where(ss => ss.StudentId == studentId && ss.IsActive)
                .Select(ss => new
                {
                    ss.SubjectId,
                    SubjectName = ss.Subject != null ? ss.Subject.Name : "",
                    TeacherName = ss.Teacher != null ? ss.Teacher.Name : "",
                    TeacherEmail = ss.Teacher != null ? ss.Teacher.Email : "",
                    ss.RegisteredAt
                })
                .ToListAsync();
            
            return Ok(subjects);
        }
        
        [HttpGet("teacher-students")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetTeacherStudents()
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var students = await _context.StudentSubjects
                .Include(ss => ss.Student)
                .Where(ss => ss.TeacherId == teacherId && ss.IsActive)
                .Select(ss => new
                {
                    ss.StudentId,
                    StudentName = ss.Student != null ? ss.Student.FullName : "",
                    AdmissionNumber = ss.Student != null ? ss.Student.AdmissionNumber : "",
                    ss.SubjectId,
                    ss.RegisteredAt
                })
                .Distinct()
                .ToListAsync();
            
            return Ok(students);
        }
    }
    
    public class RegisterSubjectDTO
    {
        public int SubjectId { get; set; }
    }
}