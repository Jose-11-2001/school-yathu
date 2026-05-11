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
    public class StudentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public StudentsController(ApplicationDbContext context)
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
            var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
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
            
            return Ok(student);
        }
        
        [HttpGet("rank/{studentId}")]
        public async Task<IActionResult> GetStudentRank(int studentId, [FromQuery] int year, [FromQuery] string term)
        {
            // Get the student
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound("Student not found");
            
            // Get student's marks
            var studentMarks = await _context.Marks
                .Where(m => m.StudentId == studentId && m.Year == year && m.Term == term)
                .ToListAsync();
            
            if (!studentMarks.Any())
                return NotFound("No marks found for this student");
            
            var studentTotal = studentMarks.Sum(m => m.Score);
            var studentAverage = studentMarks.Average(m => m.Score);
            
            // Get all students in the same class
            var allStudents = await _context.Students
                .Where(s => s.Class == student.Class)
                .ToListAsync();
            
            // Calculate totals for all students
            var rankings = new List<dynamic>();
            
            foreach (var s in allStudents)
            {
                var marks = await _context.Marks
                    .Where(m => m.StudentId == s.Id && m.Year == year && m.Term == term)
                    .ToListAsync();
                
                if (marks.Any())
                {
                    var total = marks.Sum(m => m.Score);
                    var avg = marks.Average(m => m.Score);
                    rankings.Add(new { StudentId = s.Id, Total = total, Average = avg });
                }
            }
            
            // Sort by total marks
            var sortedRankings = rankings.OrderByDescending(r => r.Total).ToList();
            var position = sortedRankings.FindIndex(r => r.StudentId == studentId) + 1;
            
            // Determine grade
            var grade = GetGrade(studentAverage);
            var remarks = GetRemarks(grade);
            
            // Get subject details with names
            var subjects = await _context.Subjects.ToListAsync();
            var subjectScores = studentMarks.Select(m => new
            {
                SubjectName = subjects.FirstOrDefault(s => s.Id == m.SubjectId)?.Name ?? "Unknown",
                m.Score,
                MaxMarks = 100
            }).ToList();
            
            return Ok(new
            {
                StudentId = studentId,
                StudentName = student.FullName,
                AdmissionNumber = student.AdmissionNumber,
                Class = student.Class,
                Stream = student.Stream,
                TotalMarks = studentTotal,
                AverageScore = Math.Round(studentAverage, 2),
                Position = position,
                TotalStudents = rankings.Count,
                Grade = grade,
                Remarks = remarks,
                SubjectScores = subjectScores
            });
        }
        
        [HttpGet("class-rankings")]
        public async Task<IActionResult> GetClassRankings(
            [FromQuery] string className, 
            [FromQuery] int year, 
            [FromQuery] string term)
        {
            // Get all students in the class
            var students = await _context.Students
                .Where(s => s.Class == className)
                .ToListAsync();
            
            var rankings = new List<dynamic>();
            
            foreach (var student in students)
            {
                var marks = await _context.Marks
                    .Where(m => m.StudentId == student.Id && m.Year == year && m.Term == term)
                    .ToListAsync();
                
                if (marks.Any())
                {
                    var total = marks.Sum(m => m.Score);
                    var avg = marks.Average(m => m.Score);
                    var grade = GetGrade(avg);
                    
                    rankings.Add(new
                    {
                        student.Id,
                        student.AdmissionNumber,
                        student.FullName,
                        TotalMarks = total,
                        AverageScore = Math.Round(avg, 2),
                        Grade = grade
                    });
                }
            }
            
            // Sort by total marks descending
            var sortedRankings = rankings.OrderByDescending(r => r.TotalMarks).ToList();
            
            // Add position
            var result = sortedRankings.Select((r, index) => new
            {
                r.Id,
                r.AdmissionNumber,
                r.FullName,
                r.TotalMarks,
                r.AverageScore,
                r.Grade,
                Position = index + 1
            }).ToList();
            
            return Ok(new
            {
                Class = className,
                Year = year,
                Term = term,
                TotalStudents = result.Count,
                Rankings = result
            });
        }
        
        [HttpGet("top-students")]
        public async Task<IActionResult> GetTopStudents([FromQuery] int year, [FromQuery] string term, [FromQuery] int top = 10)
        {
            // Get all students
            var students = await _context.Students.ToListAsync();
            
            var rankings = new List<dynamic>();
            
            foreach (var student in students)
            {
                var marks = await _context.Marks
                    .Where(m => m.StudentId == student.Id && m.Year == year && m.Term == term)
                    .ToListAsync();
                
                if (marks.Any())
                {
                    var total = marks.Sum(m => m.Score);
                    var avg = marks.Average(m => m.Score);
                    
                    rankings.Add(new
                    {
                        student.Id,
                        student.AdmissionNumber,
                        student.FullName,
                        student.Class,
                        TotalMarks = total,
                        AverageScore = Math.Round(avg, 2)
                    });
                }
            }
            
            var topStudents = rankings
                .OrderByDescending(r => r.TotalMarks)
                .Take(top)
                .Select((r, index) => new
                {
                    r.Id,
                    r.AdmissionNumber,
                    r.FullName,
                    r.Class,
                    r.TotalMarks,
                    r.AverageScore,
                    Position = index + 1
                })
                .ToList();
            
            return Ok(topStudents);
        }
        
        private string GetGrade(double averageScore)
        {
            if (averageScore >= 90) return "A+";
            if (averageScore >= 80) return "A";
            if (averageScore >= 75) return "A-";
            if (averageScore >= 70) return "B+";
            if (averageScore >= 65) return "B";
            if (averageScore >= 60) return "B-";
            if (averageScore >= 55) return "C+";
            if (averageScore >= 50) return "C";
            if (averageScore >= 45) return "C-";
            if (averageScore >= 40) return "D";
            return "E";
        }
        
        private string GetRemarks(string grade)
        {
            return grade switch
            {
                "A+" => "Excellent performance!",
                "A" => "Outstanding achievement",
                "A-" => "Very good performance",
                "B+" => "Good performance",
                "B" => "Satisfactory performance",
                "B-" => "Average performance",
                "C+" => "Fair performance",
                "C" => "Passing performance",
                "C-" => "Barely satisfactory",
                "D" => "Needs improvement",
                _ => "Requires significant improvement"
            };
        }
    }
}