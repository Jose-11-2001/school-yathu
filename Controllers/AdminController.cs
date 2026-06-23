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
    [Authorize(Roles = "Admin")]
    [SwaggerTag("Admin Management - Teachers, Classes, Students")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Get all teachers
        /// </summary>
        [HttpGet("teachers")]
        [SwaggerOperation(Summary = "Get all teachers", Description = "Retrieves a list of all teachers in the system")]
        [SwaggerResponse(200, "List of teachers", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
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
        
        /// <summary>
        /// Add a new teacher
        /// </summary>
        [HttpPost("teachers")]
        [SwaggerOperation(Summary = "Add a new teacher", Description = "Creates a new teacher account")]
        [SwaggerResponse(200, "Teacher added successfully")]
        [SwaggerResponse(400, "Invalid request or email already exists")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
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
                HireDate = dto.HireDate.HasValue ? DateTime.SpecifyKind(dto.HireDate.Value, DateTimeKind.Utc) : DateTime.UtcNow,
                Role = "Teacher",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                MustChangePassword = true
            };
            
            _context.Users.Add(teacher);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Teacher added successfully", teacherId = teacher.Id });
        }
        
        /// <summary>
        /// Delete/Deactivate a teacher
        /// </summary>
        [HttpDelete("teachers/{id}")]
        [SwaggerOperation(Summary = "Delete a teacher", Description = "Deactivates a teacher account")]
        [SwaggerResponse(200, "Teacher deactivated successfully")]
        [SwaggerResponse(404, "Teacher not found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
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
        
        /// <summary>
        /// Get all classes
        /// </summary>
        [HttpGet("classes")]
        [SwaggerOperation(Summary = "Get all classes", Description = "Retrieves a list of all classes with their streams")]
        [SwaggerResponse(200, "List of classes", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetClasses()
        {
            var classes = await _context.Classes
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Stream,
                    c.TeacherId,
                    c.Capacity,
                    c.CreatedAt
                })
                .ToListAsync();
            
            return Ok(classes);
        }
        
        /// <summary>
        /// Add a new class
        /// </summary>
        [HttpPost("classes")]
        [SwaggerOperation(Summary = "Add a new class", Description = "Creates a new class with optional stream")]
        [SwaggerResponse(200, "Class added successfully")]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
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
        
        /// <summary>
        /// Update an existing class
        /// </summary>
        [HttpPut("classes/{id}")]
        [SwaggerOperation(Summary = "Update a class", Description = "Updates an existing class information")]
        [SwaggerResponse(200, "Class updated successfully")]
        [SwaggerResponse(404, "Class not found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
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
        
        /// <summary>
        /// Delete a class
        /// </summary>
        [HttpDelete("classes/{id}")]
        [SwaggerOperation(Summary = "Delete a class", Description = "Permanently deletes a class")]
        [SwaggerResponse(200, "Class deleted successfully")]
        [SwaggerResponse(404, "Class not found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var classEntity = await _context.Classes.FindAsync(id);
            if (classEntity == null)
                return NotFound(new { message = "Class not found" });
            
            _context.Classes.Remove(classEntity);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Class deleted successfully" });
        }
        
        /// <summary>
        /// Get all students
        /// </summary>
        [HttpGet("all-students")]
        [SwaggerOperation(Summary = "Get all students", Description = "Retrieves a list of all students")]
        [SwaggerResponse(200, "List of students", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
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
        
        /// <summary>
        /// Delete a student
        /// </summary>
        [HttpDelete("students/{id}")]
        [SwaggerOperation(Summary = "Delete a student", Description = "Permanently deletes a student")]
        [SwaggerResponse(200, "Student deleted successfully")]
        [SwaggerResponse(404, "Student not found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound(new { message = "Student not found" });
            
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Student deleted successfully" });
        }
        
        /// <summary>
        /// Allocate a teacher to a subject
        /// </summary>
        [HttpPost("allocate-teacher")]
        [SwaggerOperation(Summary = "Allocate teacher to subject", Description = "Assigns a teacher to teach a subject in a class")]
        [SwaggerResponse(200, "Teacher allocated successfully")]
        [SwaggerResponse(400, "Invalid request or teacher already allocated")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
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
        
        /// <summary>
        /// Get class allocations
        /// </summary>
        [HttpGet("class-allocations/{classId}")]
        [SwaggerOperation(Summary = "Get class allocations", Description = "Retrieves all subject allocations for a specific class")]
        [SwaggerResponse(200, "List of allocations", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetClassAllocations(int classId)
        {
            var allocations = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .Where(cs => cs.ClassId == classId)
                .Select(cs => new
                {
                    cs.Id,
                    cs.ClassId,
                    cs.SubjectId,
                    SubjectName = cs.Subject != null ? cs.Subject.Name : "Unknown",
                    cs.TeacherId,
                    TeacherName = cs.Teacher != null ? cs.Teacher.Name : "Unknown",
                    cs.AssignedAt
                })
                .ToListAsync();
            
            return Ok(allocations);
        }
        
        /// <summary>
        /// Remove an allocation
        /// </summary>
        [HttpDelete("allocations/{id}")]
        [SwaggerOperation(Summary = "Remove an allocation", Description = "Removes a teacher allocation from a subject")]
        [SwaggerResponse(200, "Allocation removed successfully")]
        [SwaggerResponse(404, "Allocation not found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
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