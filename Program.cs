using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using School_Yathu.Data;
using School_Yathu.Models;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// Add port binding for Render - MUST be BEFORE builder.Build()
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
Console.WriteLine($"🔧 Configuring web host to listen on port: {port}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database - PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"📊 Database connection string (partial): {connectionString?.Substring(0, Math.Min(50, connectionString?.Length ?? 0))}...");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// JWT Authentication
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

// CORS - Allow all for Render (simplified for deployment)
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Comment out for Render - HTTPS is handled by Render's load balancer

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Add root endpoint for health check
app.MapGet("/", () => Results.Ok(new { 
    message = "School Yathu API is running!", 
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow 
}));

// Create default admin account and seed data if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database is created
        Console.WriteLine("📊 Checking database connection...");
        dbContext.Database.EnsureCreated();
        Console.WriteLine("✅ Database connection successful");
        
        // Check if admin exists
        var adminExists = dbContext.Users.Any(u => u.Email == "ntcheu@gmail.com");
        Console.WriteLine($"👤 Admin exists: {adminExists}");
        
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
            
            Console.WriteLine("✅ Default Admin created successfully!");
            Console.WriteLine("   Email: ntcheu@gmail.com");
            Console.WriteLine("   Password: admin123");
        }
        
        // Seed subjects if none exist
        if (!dbContext.Subjects.Any())
        {
            Console.WriteLine("📚 Seeding subjects...");
            dbContext.Subjects.AddRange(
                new Subject { Name = "Mathematics", Code = "MATH101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "English", Code = "ENG101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "Chichewa", Code = "CHI101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "Science", Code = "SCI101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "Social Studies", Code = "SST101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "French", Code = "FRE101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "Agriculture", Code = "AGR101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "Biology", Code = "BIO101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "Physics", Code = "PHY101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "Chemistry", Code = "CHE101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "History", Code = "HIS101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "Geography", Code = "GEO101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "Computer Studies", Code = "CSE101", MaxMarks = 100, CreatedAt = DateTime.UtcNow },
                new Subject { Name = "Religious Education", Code = "RE101", MaxMarks = 100, CreatedAt = DateTime.UtcNow }
            );
            dbContext.SaveChanges();
            Console.WriteLine("✅ Default subjects created successfully.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database seeding error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

app.Run();