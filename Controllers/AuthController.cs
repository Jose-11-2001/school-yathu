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
        
        private const string ADMIN_EMAIL = "ntcheu@gmail.com";
        private const string ADMIN_PASSWORD = "admin123";
        private const string ADMIN_NAME = "Headteacher";
        
        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        
        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login", Description = "Authenticates a user and returns a JWT token")]
        [SwaggerResponse(200, "Login successful", typeof(object))]
        [SwaggerResponse(401, "Invalid credentials")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            if (loginDto.Email?.ToLower() == ADMIN_EMAIL.ToLower())
            {
                await EnsureAdminExistsAsync();
            }
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid credentials" });
            
            var token = GenerateJwtToken(user);
            
            // Check if user is assigned as Head of Department
            bool isHeadOfDepartment = await _context.Departments
                .AnyAsync(d => d.HeadOfDepartmentId == user.Id);
            
            // Check if user is assigned as Form Teacher
            bool isFormTeacher = await _context.FormTeacherClasses
                .AnyAsync(ft => ft.TeacherId == user.Id);
            
            // Check if user is assigned as Deputy Head Teacher
            bool isDeputyHeadTeacher = user.Role == "DeputyHeadTeacher";
            
            // Determine the actual role for dashboard redirection
            string dashboardRole = user.Role;
            
            // If user is a teacher but assigned as Head of Department
            if (user.Role == "Teacher" && isHeadOfDepartment)
            {
                dashboardRole = "HeadOfDepartment";
            }
            // If user is a teacher but assigned as Form Teacher
            else if (user.Role == "Teacher" && isFormTeacher)
            {
                dashboardRole = "FormTeacher";
            }
            // If user is a Deputy Head Teacher
            else if (isDeputyHeadTeacher)
            {
                dashboardRole = "DeputyHeadTeacher";
            }
            
            Console.WriteLine($"✅ Login successful for: {user.Email}, Role: {user.Role}, Dashboard Role: {dashboardRole}");
            
            return Ok(new
            {
                token,
                id = user.Id,
                name = user.Name,
                email = user.Email,
                role = user.Role,
                dashboardRole = dashboardRole,
                mustChangePassword = user.MustChangePassword,
                isHeadOfDepartment = isHeadOfDepartment,
                isFormTeacher = isFormTeacher,
                isDeputyHeadTeacher = isDeputyHeadTeacher,
                message = "Login successful"
            });
        }
        
        private async Task EnsureAdminExistsAsync()
        {
            var adminExists = await _context.Users.AnyAsync(u => u.Email == ADMIN_EMAIL);
            
            if (!adminExists)
            {
                var admin = new User
                {
                    Email = ADMIN_EMAIL,
                    Name = ADMIN_NAME,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(ADMIN_PASSWORD),
                    PhoneNumber = "+265999999999",
                    EmployeeId = "ADMIN001",
                    Qualification = "System Administrator",
                    HireDate = DateTime.UtcNow,
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    MustChangePassword = false
                };
                
                _context.Users.Add(admin);
                await _context.SaveChangesAsync();
                Console.WriteLine("✅ Default Admin created!");
            }
        }
        
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
        
        [HttpPost("generate-email")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Generate email from name", Description = "Auto-generates an email based on user name and role")]
        public IActionResult GenerateEmail([FromBody] GenerateEmailDTO dto)
        {
            var email = GenerateEmailFromName(dto.Name, dto.Role);
            return Ok(new { email, role = dto.Role, name = dto.Name });
        }
        
        [HttpPost("generate-password")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Generate random password", Description = "Generates a secure random password")]
        public IActionResult GeneratePassword()
        {
            var password = GenerateRandomPassword();
            return Ok(new { password });
        }
        
        [Authorize]
        [HttpPost("change-password")]
        [SwaggerOperation(Summary = "Change password", Description = "Changes the current user's password")]
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
        
        [Authorize(Roles = "Admin")]
        [HttpPost("reset-password/{userId}")]
        [SwaggerOperation(Summary = "Reset password", Description = "Resets a user's password (Admin only)")]
        public async Task<IActionResult> ResetPassword(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });
            
            var newPassword = GenerateRandomPassword();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.MustChangePassword = true;
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Password reset successfully", newPassword });
        }
        
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            var jwtKey = _configuration["jwt_key"] ?? 
                         _configuration["jwt_Key"] ?? 
                         "school-yathu-secret-key-32-chars-long!";
            
            Console.WriteLine($"🔑 Generating token with key: {jwtKey.Substring(0, Math.Min(15, jwtKey.Length))}...");
            
            var key = Encoding.UTF8.GetBytes(jwtKey);
            
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
                Issuer = "School_Yathu",
                Audience = "School_Yathu-client",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            
            Console.WriteLine($"✅ JWT Token generated for: {user.Email}");
            
            return tokenString;
        }
        
        private string GenerateEmailFromName(string name, string role)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            var cleanName = Regex.Replace(name.Trim(), @"\s+", " ");
            var parts = cleanName.Split(' ');
            var firstName = parts[0];
            var lastName = parts.Length > 1 ? parts[parts.Length - 1] : firstName;
            var roleLower = role?.ToLower() ?? "student";
            
            if (roleLower == "student")
            {
                var fullName = cleanName.Replace(" ", "").ToLower();
                return $"{fullName}@gmail.com";
            }
            
            var firstNameInitial = firstName.Substring(0, 1).ToLower();
            var lastNameLower = lastName.ToLower();
            var suffix = roleLower switch
            {
                "teacher" => "",
                "deputyheadteacher" => "dht",
                "headofdepartment" => "hod",
                "formteacher" => "ft",
                _ => ""
            };
            return $"{firstNameInitial}{lastNameLower}{suffix}@gmail.com";
        }
        
        private (bool IsValid, string Message) ValidateEmailByRole(string email, string role)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Email is required");

            if (!email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
                return (false, "Email must end with @gmail.com");

            var roleLower = role?.ToLower() ?? "student";
            var username = email.Replace("@gmail.com", "").ToLower();
            
            if (roleLower == "admin")
                return (true, "Valid admin email");
            
            if (roleLower == "deputyheadteacher" && !username.EndsWith("dht"))
                return (false, "Deputy Head Teacher email must end with 'dht'");
            
            if (roleLower == "headofdepartment" && !username.EndsWith("hod"))
                return (false, "Head of Department email must end with 'hod'");
            
            if (roleLower == "formteacher" && !username.EndsWith("ft"))
                return (false, "Form Teacher email must end with 'ft'");
            
            return (true, "Valid email format");
        }
        
        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 10).Select(s => s[random.Next(s.Length)]).ToArray());
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