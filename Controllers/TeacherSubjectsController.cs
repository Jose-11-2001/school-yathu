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
    [Authorize(Roles = "Teacher,Admin")]
    [SwaggerTag("Teacher Subjects - Manage teacher subject assignments")]
    public class TeacherSubjectsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public TeacherSubjectsController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Get subjects assigned to the current teacher
        /// </summary>
        [HttpGet("my-subjects")]
        [SwaggerOperation(Summary = "Get my subjects", Description = "Retrieves all subjects assigned to the logged-in teacher")]
        [SwaggerResponse(200, "List of subjects", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized")]
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
        
        /// <summary>
        /// Assign subject to teacher (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("assign")]
        [SwaggerOperation(Summary = "Assign subject to teacher", Description = "Assigns a subject to a teacher (Admin only)")]
        [SwaggerResponse(200, "Subject assigned successfully")]
        [SwaggerResponse(400, "Subject already assigned to this teacher")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
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
        
        /// <summary>
        /// Get all teacher-subject assignments (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("all-assignments")]
        [SwaggerOperation(Summary = "Get all assignments", Description = "Retrieves all teacher-subject assignments (Admin only)")]
        [SwaggerResponse(200, "List of assignments", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
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
        
        /// <summary>
        /// Remove subject from teacher (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("remove/{id}")]
        [SwaggerOperation(Summary = "Remove assignment", Description = "Removes a subject assignment from a teacher (Admin only)")]
        [SwaggerResponse(200, "Assignment removed successfully")]
        [SwaggerResponse(404, "Assignment not found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
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