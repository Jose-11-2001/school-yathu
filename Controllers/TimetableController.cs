using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.Models;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [SwaggerTag("Timetable - Manage and view timetables")]
    public class TimetableController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TimetableController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Student")]
        [SwaggerOperation(Summary = "Get student timetable")]
        public async Task<IActionResult> GetStudentTimetable([FromQuery] int userId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == userId);

            if (student == null)
                return NotFound(new { message = "Student not found" });

            var classSubjects = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .Include(cs => cs.Class)
                .Where(cs => cs.ClassId == student.ClassId && cs.IsActive)
                .Select(cs => new
                {
                    SubjectName = cs.Subject != null ? cs.Subject.Name : "",
                    TeacherName = cs.Teacher != null ? cs.Teacher.Name : "",
                    ClassName = cs.Class != null ? cs.Class.Name : "",
                    Stream = cs.Class != null ? cs.Class.Stream : "",
                    cs.AssignedAt
                })
                .ToListAsync();

            return Ok(classSubjects);
        }

        [HttpGet("Teacher")]
        [SwaggerOperation(Summary = "Get teacher timetable")]
        public async Task<IActionResult> GetTeacherTimetable([FromQuery] int userId)
        {
            var classSubjects = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .Include(cs => cs.Class)
                .Where(cs => cs.TeacherId == userId && cs.IsActive)
                .Select(cs => new
                {
                    SubjectName = cs.Subject != null ? cs.Subject.Name : "",
                    TeacherName = cs.Teacher != null ? cs.Teacher.Name : "",
                    ClassName = cs.Class != null ? cs.Class.Name : "",
                    Stream = cs.Class != null ? cs.Class.Stream : "",
                    cs.AssignedAt
                })
                .ToListAsync();

            return Ok(classSubjects);
        }
    }
}