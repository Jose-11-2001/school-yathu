using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;  // ✅ Add this
using School_Yathu.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Subject - Manage subjects")]
    public class SubjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public SubjectController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Get all subjects
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Get all subjects", Description = "Retrieves a list of all subjects")]
        [SwaggerResponse(200, "List of subjects", typeof(List<object>))]
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
                    s.CreatedAt
                })
                .ToListAsync();
            
            return Ok(subjects);
        }
        
        /// <summary>
        /// Create a new subject (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [SwaggerOperation(Summary = "Create a new subject", Description = "Creates a new subject (Admin only)")]
        [SwaggerResponse(200, "Subject created successfully", typeof(object))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectDTO dto)
        {
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
            
            return Ok(createdSubject);
        }
    }
    // ❌ REMOVE DTO definitions from here - they're in DTOs folder
}