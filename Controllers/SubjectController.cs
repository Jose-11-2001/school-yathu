using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
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
        
        public SubjectsController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Get all subjects
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Get all subjects", Description = "Retrieves a list of all subjects")]
        [SwaggerResponse(200, "List of subjects", typeof(List<Subject>))]
        public async Task<IActionResult> GetSubjects()
        {
            var subjects = await _context.Subjects
                .OrderBy(s => s.Name)
                .ToListAsync();
            
            return Ok(subjects);
        }
        
        /// <summary>
        /// Create a new subject (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [SwaggerOperation(Summary = "Create a new subject", Description = "Creates a new subject (Admin only)")]
        [SwaggerResponse(200, "Subject created successfully", typeof(Subject))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> CreateSubject([FromBody] Subject subject)
        {
            subject.CreatedAt = DateTime.UtcNow;
            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();
            
            return Ok(subject);
        }
    }
}