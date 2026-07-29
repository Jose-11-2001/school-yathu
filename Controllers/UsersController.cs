using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
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
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
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
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> GetTeachers()
        {
            var teachers = await _context.Users
                .Where(u => u.Role == "Teacher" && u.IsActive)
                .Select(u => new { u.Id, u.Name, u.Email, u.PhoneNumber, u.EmployeeId, u.Qualification, u.MustChangePassword })
                .ToListAsync();
            
            return Ok(teachers);
        }

        /// <summary>
        /// Get users by role
        /// </summary>
        [HttpGet("by-role/{role}")]
        [SwaggerOperation(Summary = "Get users by role", Description = "Retrieves users with a specific role")]
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
        /// Update a user
        /// </summary>
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update user", Description = "Updates a user's information")]
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
                    .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Id != id);
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
        /// Delete/Deactivate a user
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete user", Description = "Deactivates a user")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Don't allow deleting the default admin
            if (user.Email == "ntcheu@gmail.com")
                return BadRequest(new { message = "Cannot delete the default admin account" });

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deactivated successfully" });
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get user by ID", Description = "Retrieves a user by their ID")]
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