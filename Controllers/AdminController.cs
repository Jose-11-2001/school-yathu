
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
        
        [HttpGet("teachers")]
        public async Task<IActionResult> GetTeachers()
        {
            var teachers = await _context.Users
                .Where(u => u.Role == "Teacher" && u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Name,
                    u.PhoneNumber,
                    u.EmployeeId,
                    u.Qualification,
                    u.HireDate,
                    u.CreatedAt,
                    u.IsActive
                })
                .ToListAsync();
            
            return Ok(teachers);
        }
        [HttpPut("classes/{id}")]
public async Task<IActionResult> UpdateClass(int id, [FromBody] CreateClassDTO dto)
{
    var classEntity = await _context.Classes.FindAsync(id);
    if (classEntity == null)
        return NotFound(new { message = "Class not found" });
    
    classEntity.Name = dto.Name;
    classEntity.Stream = dto.Stream;
    classEntity.TeacherId = dto.TeacherId;
    classEntity.Capacity = dto.Capacity;
    
    await _context.SaveChangesAsync();
    
    return Ok(new { message = "Class updated successfully" });
}
/*[HttpPut("classes/{id}")]
        public async Task<IActionResult> UpdateClass(int id, [FromBody] CreateClassDTO dto)
        {
            var classEntity = await _context.Classes.FindAsync(id);
            if (classEntity == null)
                return NotFound(new { message = "Class not found" });
            
            classEntity.Name = dto.Name;
            classEntity.Stream = dto.Stream;
            classEntity.TeacherId = dto.TeacherId;
            classEntity.Capacity = dto.Capacity;
            
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Class updated successfully" });
        }*/
        
        [HttpPost("teachers")]
        public async Task<IActionResult> AddTeacher([FromBody] CreateTeacherDTO dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "Email already exists" });
            
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
                IsActive = true,
                MustChangePassword = true
            };
            
            _context.Users.Add(teacher);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Teacher added successfully", teacherId = teacher.Id });
        }
        
        [HttpDelete("teachers/{id}")]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            var teacher = await _context.Users.FindAsync(id);
            if (teacher == null)
                return NotFound(new { message = "Teacher not found" });
            
            if (teacher.Role != "Teacher")
                return BadRequest(new { message = "User is not a teacher" });
            
            teacher.IsActive = false;
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Teacher deactivated successfully" });
        }
        
        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses()
        {
            var classes = await _context.Classes
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Stream,
                    c.Capacity,
                    c.CreatedAt
                })
                .ToListAsync();
            
            return Ok(classes);
        }
        
        [HttpPost("classes")]
        public async Task<IActionResult> AddClass([FromBody] CreateClassDTO dto)
        {
            var newClass = new Class
            {
                Name = dto.Name,
                Stream = dto.Stream,
                Capacity = dto.Capacity,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Classes.Add(newClass);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Class added successfully", classId = newClass.Id });
        }
        
        [HttpDelete("classes/{id}")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var classEntity = await _context.Classes.FindAsync(id);
            if (classEntity == null)
                return NotFound(new { message = "Class not found" });
            
            _context.Classes.Remove(classEntity);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Class deleted successfully" });
        }
        
        [HttpGet("all-students")]
        public async Task<IActionResult> GetAllStudents()
        {
            var students = await _context.Students
                .Select(s => new
                {
                    s.Id,
                    s.AdmissionNumber,
                    s.FullName,
                    s.Class,
                    s.Stream,
                    s.CreatedAt
                })
                .ToListAsync();
            
            return Ok(students);
        }
        
        [HttpDelete("students/{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound(new { message = "Student not found" });
            
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Student deleted successfully" });
        }
        
        [HttpPost("allocate-teacher")]
        public async Task<IActionResult> AllocateTeacher([FromBody] AllocateTeacherDTO dto)
        {
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
        
        [HttpGet("class-allocations/{classId}")]
        public async Task<IActionResult> GetClassAllocations(int classId)
        {
            var allocations = await _context.ClassSubjects
                .Where(cs => cs.ClassId == classId)
                .Select(cs => new
                {
                    cs.Id,
                    cs.ClassId,
                    cs.SubjectId,
                    cs.TeacherId,
                    cs.AssignedAt
                })
                .ToListAsync();
            
            return Ok(allocations);
        }
        
        [HttpDelete("allocations/{id}")]
        public async Task<IActionResult> RemoveAllocation(int id)
        {
            var allocation = await _context.ClassSubjects.FindAsync(id);
            if (allocation == null)
                return NotFound();
            
            _context.ClassSubjects.Remove(allocation);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Allocation removed successfully" });
        }
    }
}