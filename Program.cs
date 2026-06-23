using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using School_Yathu.Data;
using School_Yathu.Models;
using School_Yathu.Services;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure the port FIRST – Render requires this
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
Console.WriteLine($"Configuring web host to listen on port: {port}");

// 2. Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 4. JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "school-yathu-secret-key-32-chars-long!"))
        };
    });

// 5. CORS – Allow all for initial deployment
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { 
    message = "School Yathu API is running!", 
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow 
}));

// 7. Configure pipeline – NO UseHttpsRedirection()
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // ❌ IMPORTANT: Comment this out for Render!

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 8. Database seeding (this runs successfully, as seen in logs)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        Console.WriteLine("Checking database connection...");
        dbContext.Database.EnsureCreated();
        Console.WriteLine("Database connection successful");
        
        // Check admin
        var adminExists = dbContext.Users.Any(u => u.Email == "ntcheu@gmail.com");
        Console.WriteLine($"Admin exists: {adminExists}");
        
        if (!adminExists)
        {
            var admin = new User
            {
                Email = "ntcheu@gmail.com",
                Name = "Headteacher",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                MustChangePassword = false
            };
            
            dbContext.Users.Add(admin);
            dbContext.SaveChanges();
            Console.WriteLine("Default Admin created!");
        }
        
        // Seed subjects
        if (!dbContext.Subjects.Any())
        {
            Console.WriteLine("Seeding subjects...");
            dbContext.Subjects.AddRange(
                new Subject { Name = "Mathematics", Code = "MATH101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "English", Code = "ENG101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "Chichewa", Code = "CHI101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "Science", Code = "SCI101", MaxMarks = 100, CreatedAt = DateTime.UtcNow }
            );
            dbContext.SaveChanges();
            Console.WriteLine("Subjects created.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database error: {ex.Message}");
    }
}
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

Console.WriteLine($"Application starting successfully on port: {port}");
app.Run();

internal class EmailSettings
{
    public string? SmtpServer { get; set; }
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; }
    public string? FromEmail { get; set; }
}
