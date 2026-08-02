using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using Swashbuckle.AspNetCore.Annotations;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,DeputyHeadTeacher")]
    [SwaggerTag("Users - Manage system users")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        
        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Get all users with their roles
        /// </summary>
        [HttpGet("all")]
        [SwaggerOperation(Summary = "Get all users", Description = "Retrieves a list of all users with their roles")]
        [SwaggerResponse(200, "List of all users", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => new 
                { 
                    u.Id, 
                    u.Name, 
                    u.Email, 
                    u.Role, 
                    u.PhoneNumber,
                    u.EmployeeId,
                    u.Qualification,
                    u.IsActive,
                    u.MustChangePassword,
                    u.CreatedAt
                })
                .OrderBy(u => u.Role)
                .ToListAsync();
            
            return Ok(users);
        }

        /// <summary>
        /// Get all teachers
        /// </summary>
        [HttpGet("teachers")]
        [SwaggerOperation(Summary = "Get all teachers", Description = "Retrieves a list of all teachers")]
        [SwaggerResponse(200, "List of teachers", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetTeachers()
        {
            var teachers = await _context.Users
                .Where(u => u.Role == "Teacher" && u.IsActive)
                .Select(u => new 
                { 
                    u.Id, 
                    u.Name, 
                    u.Email, 
                    u.PhoneNumber, 
                    u.EmployeeId, 
                    u.Qualification, 
                    u.MustChangePassword 
                })
                .ToListAsync();
            
            return Ok(teachers);
        }

        /// <summary>
        /// Get users by role
        /// </summary>
        [HttpGet("by-role/{role}")]
        [SwaggerOperation(Summary = "Get users by role", Description = "Retrieves users with a specific role")]
        [SwaggerResponse(200, "List of users", typeof(List<object>))]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> GetUsersByRole(string role)
        {
            var users = await _context.Users
                .Where(u => u.Role == role && u.IsActive)
                .Select(u => new 
                { 
                    u.Id, 
                    u.Name, 
                    u.Email, 
                    u.PhoneNumber,
                    u.EmployeeId,
                    u.Qualification,
                    u.MustChangePassword,
                    u.CreatedAt
                })
                .ToListAsync();
            
            return Ok(users);
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get user by ID", Description = "Retrieves a user by their ID")]
        [SwaggerResponse(200, "User details", typeof(object))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "User not found")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .Where(u => u.Id == id && u.IsActive)
                .Select(u => new 
                { 
                    u.Id, 
                    u.Name, 
                    u.Email, 
                    u.Role,
                    u.PhoneNumber,
                    u.EmployeeId,
                    u.Qualification,
                    u.IsActive,
                    u.MustChangePassword,
                    u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(user);
        }

        /// <summary>
        /// Update a user
        /// </summary>
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update user", Description = "Updates a user's information")]
        [SwaggerResponse(200, "User updated successfully", typeof(object))]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "User not found")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDTO dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            if (!string.IsNullOrEmpty(dto.Name))
                user.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Email))
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Id != id && u.IsActive);
                if (existingUser != null)
                    return BadRequest(new { message = "Email already exists" });
                user.Email = dto.Email;
            }

            if (!string.IsNullOrEmpty(dto.PhoneNumber))
                user.PhoneNumber = dto.PhoneNumber;

            if (!string.IsNullOrEmpty(dto.EmployeeId))
                user.EmployeeId = dto.EmployeeId;

            if (!string.IsNullOrEmpty(dto.Qualification))
                user.Qualification = dto.Qualification;

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "User updated successfully" });
        }

        /// <summary>
        /// Delete a user - HARD DELETE with cascade
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete user", Description = "Permanently deletes a user and all related records from the database")]
        [SwaggerResponse(200, "User deleted successfully", typeof(object))]
        [SwaggerResponse(400, "Cannot delete default admin")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "User not found")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Notifications)
                .Include(u => u.Students)
                .Include(u => u.TeacherSubjects)
                .Include(u => u.ClassSubjects)
                .Include(u => u.StudentSubjects)
                .Include(u => u.EnteredMarks)
                .Include(u => u.ApprovedMarks)
                .Include(u => u.DeputyAssignments)
                .Include(u => u.FormTeacherClassAssignments)
                .Include(u => u.ClassesAsTeacher)
                .Include(u => u.FormTeacherClasses)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Don't allow deleting the default admin
            if (user.Email == "ntcheu@gmail.com")
                return BadRequest(new { message = "Cannot delete the default admin account" });

            // Remove all related records
            if (user.Notifications != null && user.Notifications.Any())
                _context.Notifications.RemoveRange(user.Notifications);
            
            if (user.Students != null && user.Students.Any())
                _context.Students.RemoveRange(user.Students);
            
            if (user.TeacherSubjects != null && user.TeacherSubjects.Any())
                _context.TeacherSubjects.RemoveRange(user.TeacherSubjects);
            
            if (user.ClassSubjects != null && user.ClassSubjects.Any())
                _context.ClassSubjects.RemoveRange(user.ClassSubjects);
            
            if (user.StudentSubjects != null && user.StudentSubjects.Any())
                _context.StudentSubjects.RemoveRange(user.StudentSubjects);
            
            if (user.EnteredMarks != null && user.EnteredMarks.Any())
                _context.Marks.RemoveRange(user.EnteredMarks);
            
            if (user.ApprovedMarks != null && user.ApprovedMarks.Any())
                _context.Marks.RemoveRange(user.ApprovedMarks);
            
            if (user.DeputyAssignments != null && user.DeputyAssignments.Any())
                _context.DeputyAssignments.RemoveRange(user.DeputyAssignments);

            if (user.FormTeacherClassAssignments != null && user.FormTeacherClassAssignments.Any())
                _context.FormTeacherClasses.RemoveRange(user.FormTeacherClassAssignments);

            // Delete the user
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully" });
        }
    }

    public class UpdateUserDTO
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmployeeId { get; set; }
        public string? Qualification { get; set; }
    }
}