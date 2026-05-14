
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
        
        // Get available subjects for student (based on their class)
        [HttpGet("available-subjects")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetAvailableSubjects()
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return BadRequest(new { message = "Student not found" });
            
            // Get subjects offered in student's class
            var availableSubjects = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .Where(cs => cs.ClassId == student.ClassId)
                .Select(cs => new
                {
                    cs.SubjectId,
                    SubjectName = cs.Subject != null ? cs.Subject.Name : "",
                    TeacherName = cs.Teacher != null ? cs.Teacher.Name : "",
                    cs.TeacherId
                })
                .ToListAsync();
            
            // Get already registered subjects
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
        
        // Register for a subject
        [HttpPost("register")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> RegisterSubject([FromBody] RegisterSubjectDTO dto)
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Check if already registered
            var existing = await _context.StudentSubjects
                .FirstOrDefaultAsync(ss => ss.StudentId == studentId && ss.SubjectId == dto.SubjectId && ss.IsActive);
            
            if (existing != null)
                return BadRequest(new { message = "You are already registered for this subject" });
            
            // Get teacher for this subject in student's class
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return BadRequest(new { message = "Student not found" });
            
            var classSubject = await _context.ClassSubjects
                .FirstOrDefaultAsync(cs => cs.ClassId == student.ClassId && cs.SubjectId == dto.SubjectId);
            
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
            
            // Notify teacher that a student registered
            var notification = new Notification
            {
                Title = "New Student Registration",
                Message = $"Student {student.FullName} has registered for {await GetSubjectName(dto.SubjectId)}",
                Type = "Info",
                TeacherId = classSubject.TeacherId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Successfully registered for subject" });
        }
        
        // Get my registered subjects with teacher info
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
        
        // Get students for a teacher (based on registered subjects)
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
        
        private async Task<string> GetSubjectName(int subjectId)
        {
            var subject = await _context.Subjects.FindAsync(subjectId);
            return subject?.Name ?? "Unknown";
        }
    }
    
    public class RegisterSubjectDTO
    {
        public int SubjectId { get; set; }
    }
}