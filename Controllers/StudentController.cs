
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
    public class StudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _context.Students
                .Select(s => new
                {
                    s.Id,
                    s.AdmissionNumber,
                    s.FullName,
                    s.Class,
                    s.Stream
                })
                .ToListAsync();
            return Ok(students);
        }
        
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });
                
                var teacherId = int.Parse(userIdClaim.Value);
                
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.AdmissionNumber == dto.AdmissionNumber);
                    
                if (existingStudent != null)
                    return BadRequest(new { message = $"Student with admission number '{dto.AdmissionNumber}' already exists" });
                
                var student = new Student
                {
                    AdmissionNumber = dto.AdmissionNumber,
                    FullName = dto.FullName,
                    Class = dto.Class,
                    Stream = dto.Stream,
                    TeacherId = teacherId,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Students.Add(student);
                await _context.SaveChangesAsync();
                
                return Ok(new { 
                    message = "Student added successfully", 
                    student = new
                    {
                        student.Id,
                        student.AdmissionNumber,
                        student.FullName,
                        student.Class,
                        student.Stream
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
        
        [HttpGet("marks/{studentId}")]
        public async Task<IActionResult> GetStudentMarks(int studentId, [FromQuery] int year, [FromQuery] string term)
        {
            var marks = await _context.Marks
                .Include(m => m.Subject)
                .Where(m => m.StudentId == studentId && m.Year == year && m.Term == term)
                .Select(m => new
                {
                    m.Id,
                    SubjectName = m.Subject != null ? m.Subject.Name : "",
                    m.ContinuousTest1,
                    m.ContinuousTest2,
                    m.EndTermExam,
                    m.TotalScore,
                    m.Grade,
                    m.Remark,
                    m.Year,
                    m.Term
                })
                .ToListAsync();
            
            return Ok(marks);
        }
        
        [HttpGet("rank/{studentId}")]
        public async Task<IActionResult> GetStudentRank(int studentId, [FromQuery] int year, [FromQuery] string term)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound(new { message = "Student not found" });
            
            var studentMarks = await _context.Marks
                .Where(m => m.StudentId == studentId && m.Year == year && m.Term == term)
                .ToListAsync();
            
            if (!studentMarks.Any())
                return Ok(new { message = "No marks found" });
            
            var total = studentMarks.Sum(m => m.TotalScore ?? 0);
            var average = studentMarks.Average(m => m.TotalScore ?? 0);
            var grade = GetGrade(average);
            
            // Get all students
            var allStudents = await _context.Students.ToListAsync();
            
            var totals = new List<int>();
            foreach (var s in allStudents)
            {
                var marks = await _context.Marks
                    .Where(m => m.StudentId == s.Id && m.Year == year && m.Term == term)
                    .SumAsync(m => m.TotalScore ?? 0);
                totals.Add(marks);
            }
            
            var position = totals.OrderByDescending(t => t).ToList().IndexOf(total) + 1;
            
            return Ok(new
            {
                student.AdmissionNumber,
                student.FullName,
                student.Class,
                student.Stream,
                TotalMarks = total,
                Average = Math.Round(average, 2),
                Position = position,
                Grade = grade
            });
        }
        
        private string GetGrade(double score)
        {
            if (score >= 90) return "A+";
            if (score >= 80) return "A";
            if (score >= 75) return "A-";
            if (score >= 70) return "B+";
            if (score >= 65) return "B";
            if (score >= 60) return "B-";
            if (score >= 55) return "C+";
            if (score >= 50) return "C";
            if (score >= 45) return "C-";
            if (score >= 40) return "D";
            return "E";
        }
    }
}