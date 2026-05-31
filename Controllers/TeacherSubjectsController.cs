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
    [Authorize(Roles = "Teacher,Admin")]
    public class TeacherSubjectsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public TeacherSubjectsController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // Get subjects assigned to current teacher with class information
        [HttpGet("my-subjects")]
        public async Task<IActionResult> GetMySubjects()
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var currentYear = DateTime.Now.Year;
            
            var subjects = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Include(cs => cs.Class)
                .Where(cs => cs.TeacherId == teacherId)
                .Select(cs => new
                {
                    Id = cs.Subject != null ? cs.Subject.Id : 0,
                    Name = cs.Subject != null ? cs.Subject.Name : "",
                    Code = cs.Subject != null ? cs.Subject.Code : "",
                    ClassId = cs.ClassId,
                    ClassName = cs.Class != null ? cs.Class.Name : "",
                    Stream = cs.Class != null ? cs.Class.Stream : "",
                    FullClassName = cs.Class != null ? $"{cs.Class.Name} {cs.Class.Stream}" : ""
                })
                .ToListAsync();
            
            if (!subjects.Any())
            {
                // Fallback to TeacherSubjects table
                var fallbackSubjects = await _context.TeacherSubjects
                    .Include(ts => ts.Subject)
                    .Where(ts => ts.TeacherId == teacherId)
                    .Select(ts => new
                    {
                        Id = ts.Subject != null ? ts.Subject.Id : 0,
                        Name = ts.Subject != null ? ts.Subject.Name : "",
                        Code = ts.Subject != null ? ts.Subject.Code : "",
                        ClassId = (int?)null,
                        ClassName = "All Classes",
                        Stream = "",
                        FullClassName = "All Classes"
                    })
                    .ToListAsync();
                
                return Ok(fallbackSubjects);
            }
            
            return Ok(subjects);
        }
        
        // Assign subject to teacher (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost("assign")]
        public async Task<IActionResult> AssignSubjectToTeacher([FromBody] AssignSubjectDTO dto)
        {
            var exists = await _context.TeacherSubjects
                .AnyAsync(ts => ts.TeacherId == dto.TeacherId && ts.SubjectId == dto.SubjectId);
            
            if (exists)
                return BadRequest(new { message = "Subject already assigned to this teacher" });
            
            var teacherSubject = new TeacherSubject
            {
                TeacherId = dto.TeacherId,
                SubjectId = dto.SubjectId,
                AssignedAt = DateTime.UtcNow
            };
            
            _context.TeacherSubjects.Add(teacherSubject);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Subject assigned successfully" });
        }
        
        // Get all teacher-subject assignments (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet("all-assignments")]
        public async Task<IActionResult> GetAllAssignments()
        {
            var assignments = await _context.TeacherSubjects
                .Include(ts => ts.Teacher)
                .Include(ts => ts.Subject)
                .Select(ts => new
                {
                    ts.Id,
                    TeacherName = ts.Teacher != null ? ts.Teacher.Name : "",
                    TeacherEmail = ts.Teacher != null ? ts.Teacher.Email : "",
                    SubjectName = ts.Subject != null ? ts.Subject.Name : "",
                    ts.AssignedAt
                })
                .ToListAsync();
            
            return Ok(assignments);
        }
        
        // Remove subject from teacher (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpDelete("remove/{id}")]
        public async Task<IActionResult> RemoveAssignment(int id)
        {
            var assignment = await _context.TeacherSubjects.FindAsync(id);
            if (assignment == null)
                return NotFound();
            
            _context.TeacherSubjects.Remove(assignment);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Assignment removed successfully" });
        }
    }
    
    public class AssignSubjectDTO
    {
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
    }
}