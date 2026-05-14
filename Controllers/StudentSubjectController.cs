
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
            
            // Get subjects from class subjects based on student's class name
            var availableSubjects = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Where(cs => cs.Class != null && cs.Class.Name == student.Class)
                .Select(cs => new
                {
                    cs.SubjectId,
                    SubjectName = cs.Subject != null ? cs.Subject.Name : "",
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
            
            return Ok(new { message = $"Successfully registered for the subject." });
        }
        
        [HttpGet("my-subjects")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMySubjects()
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var subjects = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                .Where(ss => ss.StudentId == studentId && ss.IsActive)
                .Select(ss => new
                {
                    ss.SubjectId,
                    SubjectName = ss.Subject != null ? ss.Subject.Name : "",
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