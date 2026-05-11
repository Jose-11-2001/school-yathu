using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;
using School_Yathu.Models;
using System.Security.Claims;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Teacher,Admin")]
    public class MarksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public MarksController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        [HttpPost]
        public async Task<IActionResult> EnterMarks([FromBody] MarksEntryDTO dto)
        {
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var student = await _context.Students.FindAsync(dto.StudentId);
            if (student == null)
                return BadRequest("Student not found");
            
            var subject = await _context.Subjects.FindAsync(dto.SubjectId);
            if (subject == null)
                return BadRequest("Subject not found");
            
            var year = dto.Year ?? DateTime.UtcNow.Year;
            var term = dto.Term ?? "Term 1";
            
            var existingMarks = await _context.Marks
                .FirstOrDefaultAsync(m => m.StudentId == dto.StudentId &&
                                         m.SubjectId == dto.SubjectId &&
                                         m.Year == year &&
                                         m.Term == term);
            
            if (existingMarks != null)
            {
                existingMarks.Score = dto.Score;
                existingMarks.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Marks updated successfully" });
            }
            
            var marks = new Marks
            {
                StudentId = dto.StudentId,
                SubjectId = dto.SubjectId,
                Score = dto.Score,
                Year = year,
                Term = term,
                ExamType = dto.ExamType,
                EnteredByTeacherId = teacherId,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Marks.Add(marks);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Marks entered successfully" });
        }
        
        [HttpGet("student/{studentId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStudentMarks(int studentId, [FromQuery] int? year, [FromQuery] string? term)
        {
            var query = _context.Marks.Where(m => m.StudentId == studentId);
            
            if (year.HasValue)
                query = query.Where(m => m.Year == year.Value);
            
            if (!string.IsNullOrEmpty(term))
                query = query.Where(m => m.Term == term);
            
            var marks = await query
                .Join(_context.Subjects,
                    m => m.SubjectId,
                    s => s.Id,
                    (m, s) => new
                    {
                        m.Id,
                        SubjectName = s.Name,
                        m.Score,
                        m.Year,
                        m.Term,
                        m.ExamType,
                        m.CreatedAt
                    })
                .ToListAsync();
            
            return Ok(marks);
        }
    }
}