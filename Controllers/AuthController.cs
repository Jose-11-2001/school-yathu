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
            
            // Email validation based on role
            var emailValidation = ValidateEmailByRole(registerDto.Email, registerDto.Role);
            if (!emailValidation.IsValid)
                return BadRequest(new { message = emailValidation.Message });
            
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
            return Ok(new { 
                email = email,
                role = dto.Role,
                name = dto.Name
            });
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
        
        /// <summary>
        /// Generate email from name based on role
        /// </summary>
        private string GenerateEmailFromName(string name, string role)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Clean the name: remove extra spaces, convert to lowercase
            var cleanName = Regex.Replace(name.Trim(), @"\s+", " ");
            var parts = cleanName.Split(' ');
            
            // Get first name and last name
            var firstName = parts[0];
            var lastName = parts.Length > 1 ? parts[parts.Length - 1] : firstName;
            
            var roleLower = role?.ToLower() ?? "student";
            
            // For Students: Use full name without spaces (e.g., josephmbukwa@gmail.com)
            if (roleLower == "student")
            {
                var fullName = cleanName.Replace(" ", "").ToLower();
                return $"{fullName}@gmail.com";
            }
            
            // For Teachers: First initial + last name (e.g., jmbukwa@gmail.com)
            if (roleLower == "teacher")
            {
                var firstNameInitial = firstName.Substring(0, 1).ToLower();
                var lastNameLower = lastName.ToLower();
                var baseEmail = $"{firstNameInitial}{lastNameLower}";
                return $"{baseEmail}@gmail.com";
            }
            
            // For Deputy Head Teacher: First initial + last name + "dht"
            if (roleLower == "deputyheadteacher")
            {
                var firstNameInitial = firstName.Substring(0, 1).ToLower();
                var lastNameLower = lastName.ToLower();
                var baseEmail = $"{firstNameInitial}{lastNameLower}dht";
                return $"{baseEmail}@gmail.com";
            }
            
            // For Head of Department: First initial + last name + "hod"
            if (roleLower == "headofdepartment")
            {
                var firstNameInitial = firstName.Substring(0, 1).ToLower();
                var lastNameLower = lastName.ToLower();
                var baseEmail = $"{firstNameInitial}{lastNameLower}hod";
                return $"{baseEmail}@gmail.com";
            }
            
            // For Form Teacher: First initial + last name + "ft"
            if (roleLower == "formteacher")
            {
                var firstNameInitial = firstName.Substring(0, 1).ToLower();
                var lastNameLower = lastName.ToLower();
                var baseEmail = $"{firstNameInitial}{lastNameLower}ft";
                return $"{baseEmail}@gmail.com";
            }
            
            // Default: First initial + last name (for Admin and others)
            var defaultFirstNameInitial = firstName.Substring(0, 1).ToLower();
            var defaultLastNameLower = lastName.ToLower();
            return $"{defaultFirstNameInitial}{defaultLastNameLower}@gmail.com";
        }
        
        /// <summary>
        /// Get the email suffix based on role (for validation)
        /// </summary>
        private string GetRoleSuffix(string role)
        {
            return role switch
            {
                "admin" => "",  // Admin uses custom email like ntcheu@gmail.com
                "deputyheadteacher" => "dht",
                "headofdepartment" => "hod",
                "formteacher" => "ft",
                "teacher" => "",  // No suffix for regular teachers
                "student" => "",  // No suffix for students
                _ => "" // Default: no suffix
            };
        }
        
        /// <summary>
        /// Validate email format based on role
        /// </summary>
        private (bool IsValid, string Message) ValidateEmailByRole(string email, string role)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Email is required");

            if (!email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
                return (false, "Email must end with @gmail.com");

            var roleLower = role?.ToLower() ?? "student";
            var username = email.Replace("@gmail.com", "").ToLower();
            
            // Special case: Admin can use any email (like ntcheu@gmail.com)
            if (roleLower == "admin")
            {
                return (true, "Valid admin email");
            }
            
            // Deputy Head Teacher - must end with "dht"
            if (roleLower == "deputyheadteacher" && !username.EndsWith("dht"))
                return (false, "Deputy Head Teacher email must end with 'dht' (e.g., jmbukwadht@gmail.com)");
            
            // Head of Department - must end with "hod"
            if (roleLower == "headofdepartment" && !username.EndsWith("hod"))
                return (false, "Head of Department email must end with 'hod' (e.g., jmbukwahod@gmail.com)");
            
            // Form Teacher - must end with "ft"
            if (roleLower == "formteacher" && !username.EndsWith("ft"))
                return (false, "Form Teacher email must end with 'ft' (e.g., jmbukwaft@gmail.com)");
            
            // Teacher - should not have role suffixes
            if (roleLower == "teacher" && (username.EndsWith("hod") || username.EndsWith("ft") || username.EndsWith("dht")))
                return (false, "Teacher email should not contain role suffixes (hod, ft, dht)");
            
            // Student - should not have role suffixes
            if (roleLower == "student" && (username.EndsWith("hod") || username.EndsWith("ft") || username.EndsWith("dht")))
                return (false, "Student email should not contain role suffixes (hod, ft, dht)");

            return (true, "Valid email format");
        }
        
        /// <summary>
        /// Generate random password
        /// </summary>
        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var password = new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return password;
        }
        
        /// <summary>
        /// Generate JWT token
        /// </summary>
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