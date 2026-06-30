using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [SwaggerTag("Contacts - Get school contact information")]
    public class ContactsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(ApplicationDbContext context, ILogger<ContactsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all contact information for the logged-in user
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Get contact information")]
        public async Task<IActionResult> GetContacts()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                var response = new
                {
                    // Headteacher (Admin)
                    Headteacher = await _context.Users
                        .Where(u => u.Role == "Admin" && u.IsActive)
                        .Select(u => new { u.Name, u.PhoneNumber })
                        .FirstOrDefaultAsync(),

                    // Head of Department (if student has one)
                    HeadOfDepartment = user.Role == "Student" ? await GetStudentHeadOfDepartment(userId) : null,

                    // Form Teacher (if student has one)
                    FormTeacher = user.Role == "Student" ? await GetStudentFormTeacher(userId) : null,

                    // Subject Teachers (if student has subjects)
                    SubjectTeachers = user.Role == "Student" ? await GetStudentSubjectTeachers(userId) : null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contacts");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        private async Task<object?> GetStudentHeadOfDepartment(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return null;

            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Name == student.Class && c.Stream == student.Stream);

            if (classEntity == null) return null;

            var classSubject = await _context.ClassSubjects
                .FirstOrDefaultAsync(cs => cs.ClassId == classEntity.Id);

            if (classSubject == null) return null;

            var subject = await _context.Subjects
                .Include(s => s.Department)
                .ThenInclude(d => d.HeadOfDepartment)
                .FirstOrDefaultAsync(s => s.Id == classSubject.SubjectId);

            if (subject?.Department?.HeadOfDepartment == null) return null;

            return new
            {
                subject.Department.HeadOfDepartment.Name,
                subject.Department.HeadOfDepartment.PhoneNumber
            };
        }

        private async Task<object?> GetStudentFormTeacher(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return null;

            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Name == student.Class && c.Stream == student.Stream);

            if (classEntity?.FormTeacherId == null) return null;

            var formTeacher = await _context.Users.FindAsync(classEntity.FormTeacherId);

            if (formTeacher == null) return null;

            return new
            {
                formTeacher.Name,
                formTeacher.PhoneNumber
            };
        }

        private async Task<List<object>> GetStudentSubjectTeachers(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return new List<object>();

            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Name == student.Class && c.Stream == student.Stream);

            if (classEntity == null) return new List<object>();

            var subjectTeachers = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .Where(cs => cs.ClassId == classEntity.Id && cs.TeacherId != null)
                .Select(cs => new
                {
                    Subject = cs.Subject != null ? cs.Subject.Name : "Unknown",
                    Name = cs.Teacher != null ? cs.Teacher.Name : "Not Assigned",
                    PhoneNumber = cs.Teacher != null ? cs.Teacher.PhoneNumber : null
                })
                .ToListAsync();

            return subjectTeachers.Cast<object>().ToList();
        }
    }
}