using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using School_Yathu.Data;
using School_Yathu.Models;
using System.Text.RegularExpressions;
using Swashbuckle.AspNetCore.Annotations;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Authentication - Login, Register, Password Management")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        
        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        
        /// <summary>
        /// Register a new user (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("register")]
        [SwaggerOperation(Summary = "Register new user", Description = "Creates a new teacher or student account (Admin only)")]
        [SwaggerResponse(200, "User registered successfully", typeof(object))]
        [SwaggerResponse(400, "Invalid request or email already exists")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
                return BadRequest(new { message = "Email already exists" });
            
            if (registerDto.Role == "Teacher" && !registerDto.Email.EndsWith("@gmail.com"))
            {
                return BadRequest(new { message = "Teacher must use a valid email address (e.g., name@gmail.com)" });
            }
            
            var user = new User
            {
                Email = registerDto.Email,
                Name = registerDto.Name,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                PhoneNumber = registerDto.PhoneNumber,
                EmployeeId = registerDto.EmployeeId,
                Qualification = registerDto.Qualification,
                HireDate = registerDto.HireDate,
                Role = registerDto.Role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                MustChangePassword = true
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            return Ok(new { 
                message = "User registered successfully", 
                email = user.Email,
                password = registerDto.Password,
                role = user.Role,
                mustChangePassword = true
            });
        }
        
        /// <summary>
        /// Generate email from name
        /// </summary>
        [HttpPost("generate-email")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Generate email from name", Description = "Auto-generates an email based on user name and role")]
        [SwaggerResponse(200, "Generated email", typeof(object))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public IActionResult GenerateEmail([FromBody] GenerateEmailDTO dto)
        {
            var email = GenerateEmailFromName(dto.Name, dto.Role);
            return Ok(new { email = email });
        }
        
        /// <summary>
        /// Generate random password
        /// </summary>
        [HttpPost("generate-password")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Generate random password", Description = "Generates a secure random password")]
        [SwaggerResponse(200, "Generated password", typeof(object))]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public IActionResult GeneratePassword()
        {
            var password = GenerateRandomPassword();
            return Ok(new { password = password });
        }
        
        /// <summary>
        /// Login user
        /// </summary>
        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login", Description = "Authenticates a user and returns a JWT token")]
        [SwaggerResponse(200, "Login successful", typeof(object))]
        [SwaggerResponse(401, "Invalid credentials")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid credentials" });
            
            var mustChangePassword = user.MustChangePassword;
            var token = GenerateJwtToken(user);
            
            return Ok(new
            {
                token,
                user.Id,
                user.Name,
                user.Email,
                user.Role,
                mustChangePassword,
                message = "Login successful"
            });
        }
        
        /// <summary>
        /// Change password
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [SwaggerOperation(Summary = "Change password", Description = "Changes the current user's password")]
        [SwaggerResponse(200, "Password changed successfully")]
        [SwaggerResponse(400, "Current password is incorrect")]
        [SwaggerResponse(404, "User not found")]
        [SwaggerResponse(401, "Unauthorized")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
                return NotFound(new { message = "User not found" });
            
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = "Current password is incorrect" });
            
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.MustChangePassword = false;
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Password changed successfully" });
        }
        
        /// <summary>
        /// Reset password (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("reset-password/{userId}")]
        [SwaggerOperation(Summary = "Reset password", Description = "Resets a user's password (Admin only)")]
        [SwaggerResponse(200, "Password reset successfully", typeof(object))]
        [SwaggerResponse(404, "User not found")]
        [SwaggerResponse(401, "Unauthorized - Admin role required")]
        public async Task<IActionResult> ResetPassword(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });
            
            var newPassword = GenerateRandomPassword();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.MustChangePassword = true;
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Password reset successfully", newPassword = newPassword });
        }
        
        private string GenerateEmailFromName(string name, string role)
        {
            var cleanName = Regex.Replace(name.ToLower(), @"\s+", "");
            
            if (role == "Teacher")
            {
                var parts = name.Trim().Split(' ');
                if (parts.Length >= 2)
                {
                    var firstNameInitial = parts[0].Substring(0, 1);
                    var lastName = parts[parts.Length - 1];
                    return $"{firstNameInitial}{lastName}@gmail.com".ToLower();
                }
                else
                {
                    return $"{cleanName}@gmail.com".ToLower();
                }
            }
            else
            {
                return $"{cleanName}@gmail.com".ToLower();
            }
        }
        
        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var password = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return password;
        }
        
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "school-yathu-secret-key-32-chars-long!");
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddDays(1),
                Issuer = _configuration["Jwt:Issuer"] ?? "SchoolYathuAPI",
                Audience = _configuration["Jwt:Audience"] ?? "SchoolYathuClient",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
    
    public class RegisterDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? EmployeeId { get; set; }
        public string? Qualification { get; set; }
        public DateTime? HireDate { get; set; }
        public string Role { get; set; } = "Student";
    }
    
    public class LoginDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    
    public class ChangePasswordDTO
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
    
    public class GenerateEmailDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}