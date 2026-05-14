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
            var students = await _context.Students.ToListAsync();
            return Ok(students);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound();
            return Ok(student);
        }
        
        [Authorize(Roles = "Teacher,Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDTO dto)
        {
            try
            {
                // Get teacher ID from token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                
                var teacherId = int.Parse(userIdClaim.Value);
                
                // Check if student with same admission number exists
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.AdmissionNumber == dto.AdmissionNumber);
                    
                if (existingStudent != null)
                {
                    return BadRequest(new { message = $"Student with admission number '{dto.AdmissionNumber}' already exists" });
                }
                
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
                    student = new { student.Id, student.AdmissionNumber, student.FullName, student.Class, student.Stream }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Server error: {ex.Message}" });
            }
        }
        
        // Enter marks (Teacher/Admin only)
        [Authorize(Roles = "Teacher,Admin")]
        [HttpPost("marks")]
        public async Task<IActionResult> EnterMarks([FromBody] MarksEntryDTO dto)
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                var student = await _context.Students.FindAsync(dto.StudentId);
                if (student == null)
                    return BadRequest(new { message = "Student not found" });
                
                var subject = await _context.Subjects.FindAsync(dto.SubjectId);
                if (subject == null)
                    return BadRequest(new { message = "Subject not found" });
                
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
                    existingMarks.Grade = dto.Grade;
                    existingMarks.Remark = dto.Remark;
                    existingMarks.ExamType = dto.ExamType;
                    existingMarks.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Marks updated successfully" });
                }
                
                var marks = new Marks
                {
                    StudentId = dto.StudentId,
                    SubjectId = dto.SubjectId,
                    Score = dto.Score,
                    Grade = dto.Grade,
                    Remark = dto.Remark,
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }
        
        // Get student marks
        [HttpGet("marks/{studentId}")]
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
                        m.Grade,
                        m.Remark,
                        m.Year,
                        m.Term,
                        m.ExamType,
                        m.CreatedAt
                    })
                .ToListAsync();
            
            return Ok(marks);
        }
        
        // Get student ranking
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
                return NotFound(new { message = "No marks found" });
            
            var studentTotal = studentMarks.Sum(m => m.Score);
            var studentAvg = studentMarks.Average(m => m.Score);
            var grade = GetGrade(studentAvg);
            
            return Ok(new
            {
                student.AdmissionNumber,
                student.FullName,
                student.Class,
                student.Stream,
                TotalMarks = studentTotal,
                Average = Math.Round(studentAvg, 2),
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