using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;
using Swashbuckle.AspNetCore.Annotations;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [SwaggerTag("Results Management")]
    public class ResultsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResultsController> _logger;

        public ResultsController(ApplicationDbContext context, ILogger<ResultsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get student results
        /// </summary>
        [HttpGet("student-results")]
        [SwaggerOperation(Summary = "Get student results", Description = "Retrieves all results for a specific student")]
        public async Task<IActionResult> GetStudentResults(
            [FromQuery] string admissionNumber,
            [FromQuery] int year,
            [FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrEmpty(admissionNumber))
                    return BadRequest(new { message = "Admission number is required" });

                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.AdmissionNumber == admissionNumber);

                if (student == null)
                    return NotFound(new { message = "Student not found" });

                var results = await _context.StudentMarks
                    .Include(sm => sm.Subject)
                    .Where(sm => sm.StudentId == student.Id && 
                                 sm.Year == year && 
                                 sm.Term == term)
                    .Select(sm => new
                    {
                        sm.SubjectId,
                        SubjectName = sm.Subject != null ? sm.Subject.Name : "Unknown",
                        sm.Test1,
                        sm.Test2,
                        sm.EndTerm,
                        OverallPercentage = (sm.Test1 * 0.2 + sm.Test2 * 0.2 + sm.EndTerm * 0.6),
                        sm.Year,
                        sm.Term
                    })
                    .ToListAsync();

                // Calculate ranking
                var classStudents = await _context.Students
                    .Where(s => s.Class == student.Class && s.Stream == student.Stream)
                    .Select(s => s.Id)
                    .ToListAsync();

                var classResults = await _context.StudentMarks
                    .Where(sm => classStudents.Contains(sm.StudentId) && 
                                 sm.Year == year && 
                                 sm.Term == term)
                    .GroupBy(sm => sm.StudentId)
                    .Select(g => new
                    {
                        StudentId = g.Key,
                        Average = g.Average(sm => sm.Test1 * 0.2 + sm.Test2 * 0.2 + sm.EndTerm * 0.6)
                    })
                    .OrderByDescending(x => x.Average)
                    .ToListAsync();

                var position = classResults.FindIndex(x => x.StudentId == student.Id) + 1;
                var studentAverage = classResults.FirstOrDefault(x => x.StudentId == student.Id)?.Average ?? 0;

                var ranking = new RankingDTO
                {
                    StudentId = student.Id,
                    TotalMarks = results.Sum(r => r.OverallPercentage ?? 0),
                    Average = studentAverage,
                    Position = position > 0 ? position : classResults.Count + 1,
                    TotalStudents = classResults.Count,
                    Class = student.Class,
                    Stream = student.Stream,
                    Grade = CalculateGrade(studentAverage),
                    Remarks = GetRemarks(studentAverage)
                };

                return Ok(new
                {
                    results,
                    ranking
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student results");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        private string CalculateGrade(double percentage)
        {
            if (percentage >= 80) return "A";
            if (percentage >= 65) return "B";
            if (percentage >= 50) return "C";
            if (percentage >= 45) return "D";
            if (percentage >= 40) return "E";
            return "F";
        }

        private string GetRemarks(double percentage)
        {
            if (percentage >= 80) return "Excellent performance!";
            if (percentage >= 65) return "Good performance!";
            if (percentage >= 50) return "Average performance.";
            if (percentage >= 45) return "Below average. Needs improvement.";
            if (percentage >= 40) return "Poor performance!";
            return "Failed. Must work harder!";
        }
    }
}