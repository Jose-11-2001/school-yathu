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
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // ==================== TEACHER MANAGEMENT ====================
        
        // Get all teachers
        [HttpGet("teachers")]
        public async Task<IActionResult> GetTeachers()
        {
            var teachers = await _context.Users
                .Where(u => u.Role == "Teacher")
                .Select(u => new TeacherResponseDTO
                {
                    Id = u.Id,
                    Email = u.Email,
                    Name = u.Name,
                    PhoneNumber = u.PhoneNumber,
                    EmployeeId = u.EmployeeId,
                    Qualification = u.Qualification,
                    HireDate = u.HireDate,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive
                })
                .ToListAsync();
            
            return Ok(teachers);
        }
        
        // Add new teacher
        [HttpPost("teachers")]
        public async Task<IActionResult> AddTeacher([FromBody] CreateTeacherDTO dto)
        {
            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "Email already exists" });
            
            // Check if employee ID already exists
            if (!string.IsNullOrEmpty(dto.EmployeeId) && await _context.Users.AnyAsync(u => u.EmployeeId == dto.EmployeeId))
                return BadRequest(new { message = "Employee ID already exists" });
            
            var teacher = new User
            {
                Email = dto.Email,
                Name = dto.Name,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                PhoneNumber = dto.PhoneNumber,
                EmployeeId = dto.EmployeeId,
                Qualification = dto.Qualification,
                HireDate = dto.HireDate ?? DateTime.UtcNow,
                Role = "Teacher",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            
            _context.Users.Add(teacher);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Teacher added successfully", teacherId = teacher.Id });
        }
        
        // Delete teacher
        [HttpDelete("teachers/{id}")]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            var teacher = await _context.Users.FindAsync(id);
            if (teacher == null)
                return NotFound(new { message = "Teacher not found" });
            
            if (teacher.Role != "Teacher")
                return BadRequest(new { message = "User is not a teacher" });
            
            // Check if teacher has allocated classes or subjects
            var hasClasses = await _context.Classes.AnyAsync(c => c.TeacherId == id);
            var hasSubjects = await _context.ClassSubjects.AnyAsync(cs => cs.TeacherId == id);
            
            if (hasClasses || hasSubjects)
            {
                return BadRequest(new { message = "Cannot delete teacher. Teacher has allocated classes or subjects. Reassign first." });
            }
            
            _context.Users.Remove(teacher);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Teacher deleted successfully" });
        }
        
        // Update teacher
        [HttpPut("teachers/{id}")]
        public async Task<IActionResult> UpdateTeacher(int id, [FromBody] CreateTeacherDTO dto)
        {
            var teacher = await _context.Users.FindAsync(id);
            if (teacher == null)
                return NotFound(new { message = "Teacher not found" });
            
            teacher.Name = dto.Name;
            teacher.PhoneNumber = dto.PhoneNumber;
            teacher.EmployeeId = dto.EmployeeId;
            teacher.Qualification = dto.Qualification;
            teacher.HireDate = dto.HireDate;
            
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Teacher updated successfully" });
        }
        
        // ==================== CLASS MANAGEMENT ====================
        
        // Get all classes
        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses()
        {
            var classes = await _context.Classes
                .Include(c => c.Teacher)
                .Select(c => new ClassResponseDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Stream = c.Stream,
                    TeacherId = c.TeacherId,
                    TeacherName = c.Teacher != null ? c.Teacher.Name : null,
                    Capacity = c.Capacity,
                    StudentCount = c.Students != null ? c.Students.Count : 0,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
            
            return Ok(classes);
        }
        
        // Add new class
        [HttpPost("classes")]
        public async Task<IActionResult> AddClass([FromBody] CreateClassDTO dto)
        {
            var newClass = new Class
            {
                Name = dto.Name,
                Stream = dto.Stream,
                TeacherId = dto.TeacherId,
                Capacity = dto.Capacity,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Classes.Add(newClass);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Class added successfully", classId = newClass.Id });
        }
        
        // Delete class
        [HttpDelete("classes/{id}")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var classEntity = await _context.Classes
                .Include(c => c.Students)
                .Include(c => c.ClassSubjects)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (classEntity == null)
                return NotFound(new { message = "Class not found" });
            
            if (classEntity.Students != null && classEntity.Students.Any())
                return BadRequest(new { message = "Cannot delete class with enrolled students. Transfer students first." });
            
            _context.Classes.Remove(classEntity);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Class deleted successfully" });
        }
        
        // ==================== TEACHER ALLOCATION ====================
        
        // Allocate teacher to class and subject
        [HttpPost("allocate-teacher")]
        public async Task<IActionResult> AllocateTeacher([FromBody] AllocateTeacherDTO dto)
        {
            // Check if allocation already exists
            var exists = await _context.ClassSubjects
                .AnyAsync(cs => cs.ClassId == dto.ClassId && cs.SubjectId == dto.SubjectId);
            
            if (exists)
                return BadRequest(new { message = "Teacher already allocated to this subject in this class" });
            
            var allocation = new ClassSubject
            {
                ClassId = dto.ClassId,
                SubjectId = dto.SubjectId,
                TeacherId = dto.TeacherId,
                AssignedAt = DateTime.UtcNow
            };
            
            _context.ClassSubjects.Add(allocation);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Teacher allocated successfully" });
        }
        
        // Get class allocations
        [HttpGet("class-allocations/{classId}")]
        public async Task<IActionResult> GetClassAllocations(int classId)
        {
            var allocations = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .Where(cs => cs.ClassId == classId)
                .Select(cs => new ClassSubjectResponseDTO
                {
                    Id = cs.Id,
                    ClassId = cs.ClassId,
                    ClassName = cs.Class != null ? cs.Class.Name : "",
                    SubjectId = cs.SubjectId,
                    SubjectName = cs.Subject != null ? cs.Subject.Name : "",
                    TeacherId = cs.TeacherId,
                    TeacherName = cs.Teacher != null ? cs.Teacher.Name : "",
                    AssignedAt = cs.AssignedAt
                })
                .ToListAsync();
            
            return Ok(allocations);
        }
        
        // Remove teacher allocation
        [HttpDelete("allocations/{id}")]
        public async Task<IActionResult> RemoveAllocation(int id)
        {
            var allocation = await _context.ClassSubjects.FindAsync(id);
            if (allocation == null)
                return NotFound(new { message = "Allocation not found" });
            
            _context.ClassSubjects.Remove(allocation);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Allocation removed successfully" });
        }
        
        // ==================== EXAM MANAGEMENT ====================
        
        // Create exam
        [HttpPost("exams")]
        public async Task<IActionResult> CreateExam([FromBody] CreateExamDTO dto)
        {
            var exam = new Exam
            {
                Name = dto.Name,
                Type = dto.Type,
                Year = dto.Year ?? DateTime.UtcNow.Year,
                Term = dto.Term,
                ExamDate = dto.ExamDate,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Exam created successfully", examId = exam.Id });
        }
        
        // Get all exams
        [HttpGet("exams")]
        public async Task<IActionResult> GetExams()
        {
            var exams = await _context.Exams
                .OrderByDescending(e => e.Year)
                .ThenBy(e => e.Term)
                .ToListAsync();
            
            return Ok(exams);
        }
        
        // Get exam results with details
        [HttpGet("exam-results/{examId}")]
        public async Task<IActionResult> GetExamResults(int examId)
        {
            var results = await _context.ExamResults
                .Include(er => er.Student)
                .Include(er => er.Subject)
                .Include(er => er.Exam)
                .Where(er => er.ExamId == examId)
                .Select(er => new ExamResultResponseDTO
                {
                    Id = er.Id,
                    ExamName = er.Exam != null ? er.Exam.Name : "",
                    StudentName = er.Student != null ? er.Student.FullName : "",
                    AdmissionNumber = er.Student != null ? er.Student.AdmissionNumber : "",
                    Class = er.Student != null ? er.Student.Class : "",
                    SubjectName = er.Subject != null ? er.Subject.Name : "",
                    Score = er.Score,
                    Grade = er.Grade ?? "",
                    Remark = er.Remark,
                    TeacherName = "", // You can join with teacher
                    CreatedAt = er.CreatedAt
                })
                .ToListAsync();
            
            return Ok(results);
        }
        
        // Get class performance summary
        [HttpGet("class-performance/{classId}/{examId}")]
        public async Task<IActionResult> GetClassPerformance(int classId, int examId)
        {
            var students = await _context.Students
                .Where(s => s.ClassId == classId)
                .ToListAsync();
            
            var results = new List<object>();
            
            foreach (var student in students)
            {
                var studentResults = await _context.ExamResults
                    .Include(er => er.Subject)
                    .Where(er => er.StudentId == student.Id && er.ExamId == examId)
                    .ToListAsync();
                
                if (studentResults.Any())
                {
                    var total = studentResults.Sum(r => r.Score);
                    var average = studentResults.Average(r => r.Score);
                    var grade = GetGrade(average);
                    
                    results.Add(new
                    {
                        student.Id,
                        student.AdmissionNumber,
                        student.FullName,
                        TotalMarks = total,
                        Average = Math.Round(average, 2),
                        Grade = grade,
                        Subjects = studentResults.Select(r => new
                        {
                            Subject = r.Subject?.Name,
                            Score = r.Score,
                            Grade = r.Grade
                        })
                    });
                }
            }
            
            return Ok(results);
        }
        
        // ==================== STUDENT MANAGEMENT ====================
        
        // Delete student
        [HttpDelete("students/{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students
                .Include(s => s.Marks)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            if (student == null)
                return NotFound(new { message = "Student not found" });
            
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Student deleted successfully" });
        }
        
        // Get all students with details
        [HttpGet("all-students")]
        public async Task<IActionResult> GetAllStudents()
        {
            var students = await _context.Students
                .Include(s => s.Class)
                .Select(s => new
                {
                    s.Id,
                    s.AdmissionNumber,
                    s.FullName,
                    Class = s.Class != null ? s.Class.Name : null,
                    Stream = s.Class != null ? s.Class.Stream : null,
                    s.CreatedAt
                })
                .ToListAsync();
            
            return Ok(students);
        }
        
        // Get student subject results
        [HttpGet("student-subjects/{studentId}")]
        public async Task<IActionResult> GetStudentSubjects(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return NotFound(new { message = "Student not found" });
            
            // Get all subjects for the student's class
            var subjects = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .Where(cs => cs.ClassId == student.ClassId)
                .Select(cs => new
                {
                    cs.SubjectId,
                    SubjectName = cs.Subject != null ? cs.Subject.Name : "",
                    TeacherName = cs.Teacher != null ? cs.Teacher.Name : ""
                })
                .ToListAsync();
            
            return Ok(subjects);
        }
        
        // Get all end of term results
        [HttpGet("term-results/{year}/{term}")]
        public async Task<IActionResult> GetTermResults(int year, string term)
        {
            var exams = await _context.Exams
                .Where(e => e.Year == year && e.Term == term)
                .ToListAsync();
            
            var allResults = new List<object>();
            
            foreach (var exam in exams)
            {
                var results = await _context.ExamResults
                    .Include(er => er.Student)
                    .Include(er => er.Subject)
                    .Where(er => er.ExamId == exam.Id)
                    .Select(er => new
                    {
                        ExamName = exam.Name,
                        er.StudentId,
                        StudentName = er.Student != null ? er.Student.FullName : "",
                        AdmissionNumber = er.Student != null ? er.Student.AdmissionNumber : "",
                        Subject = er.Subject != null ? er.Subject.Name : "",
                        er.Score,
                        er.Grade
                    })
                    .ToListAsync();
                
                allResults.AddRange(results);
            }
            
            return Ok(allResults);
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