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
    [SwaggerTag("Subjects - Manage subjects")]
    public class SubjectsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubjectsController> _logger;

        public SubjectsController(ApplicationDbContext context, ILogger<SubjectsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all subjects")]
        public async Task<IActionResult> GetSubjects()
        {
            var subjects = await _context.Subjects
                .Include(s => s.Department)
                .OrderBy(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Code,
                    s.Type,
                    s.DepartmentId,
                    Department = s.Department != null ? new
                    {
                        s.Department.Id,
                        s.Department.Name
                    } : null,
                    s.CreatedAt,
                    s.UpdatedAt
                })
                .ToListAsync();
            
            return Ok(subjects);
        }

        [HttpGet("department/{departmentId}")]
        public async Task<IActionResult> GetSubjectsByDepartment(int departmentId)
        {
            var department = await _context.Departments.FindAsync(departmentId);
            if (department == null)
                return NotFound(new { message = "Department not found" });

            var subjects = await _context.Subjects
                .Where(s => s.DepartmentId == departmentId)
                .OrderBy(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Code,
                    s.Type,
                    s.DepartmentId,
                    s.CreatedAt
                })
                .ToListAsync();

            return Ok(subjects);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSubject(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.Department)
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Code,
                    s.Type,
                    s.DepartmentId,
                    Department = s.Department != null ? new
                    {
                        s.Department.Id,
                        s.Department.Name
                    } : null,
                    s.CreatedAt,
                    s.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (subject == null)
                return NotFound(new { message = "Subject not found" });

            return Ok(subject);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectDTO dto)
        {
            if (!string.IsNullOrEmpty(dto.Code))
            {
                var existingCode = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.Code == dto.Code.ToUpper());
                if (existingCode != null)
                    return BadRequest(new { message = $"Subject code '{dto.Code}' already exists" });
            }

            if (dto.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(dto.DepartmentId.Value);
                if (department == null)
                    return BadRequest(new { message = "Department not found" });
            }

            var subject = new Subject
            {
                Name = dto.Name,
                Code = dto.Code?.ToUpper() ?? "",
                Type = dto.Type ?? "Core",
                DepartmentId = dto.DepartmentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            var createdSubject = await _context.Subjects
                .Include(s => s.Department)
                .Where(s => s.Id == subject.Id)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Code,
                    s.Type,
                    s.DepartmentId,
                    Department = s.Department != null ? new
                    {
                        s.Department.Id,
                        s.Department.Name
                    } : null,
                    s.CreatedAt
                })
                .FirstOrDefaultAsync();

            return Ok(new { message = "Subject created successfully", subject = createdSubject });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubject(int id, [FromBody] UpdateSubjectDTO dto)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound(new { message = "Subject not found" });

            if (!string.IsNullOrEmpty(dto.Name))
                subject.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Code))
            {
                var existingCode = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.Code == dto.Code.ToUpper() && s.Id != id);
                if (existingCode != null)
                    return BadRequest(new { message = $"Subject code '{dto.Code}' already exists" });
                subject.Code = dto.Code.ToUpper();
            }

            if (!string.IsNullOrEmpty(dto.Type))
                subject.Type = dto.Type;

            if (dto.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(dto.DepartmentId.Value);
                if (department == null)
                    return BadRequest(new { message = "Department not found" });
                subject.DepartmentId = dto.DepartmentId;
            }

            subject.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subject updated successfully" });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.ClassSubjects)
                .Include(s => s.TeacherSubjects)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
                return NotFound(new { message = "Subject not found" });

            if ((subject.ClassSubjects != null && subject.ClassSubjects.Any()) ||
                (subject.TeacherSubjects != null && subject.TeacherSubjects.Any()))
                return BadRequest(new { message = "Cannot delete subject with existing allocations" });

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subject deleted successfully" });
        }
    }
}