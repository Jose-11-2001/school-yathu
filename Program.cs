using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using School_Yathu.Data;
using School_Yathu.Models;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// CORS
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

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Create default admin account and seed data if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
    
    // Check if admin exists
    var adminExists = await dbContext.Users.AnyAsync(u => u.Email == "loyola@gmail.com");
    
    if (!adminExists)
    {
        var admin = new User
        {
            Email = "loyola@gmail.com",
            Name = "Headteacher",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            MustChangePassword = false
        };
        
        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync();
        
        Console.WriteLine("✅ Default Admin created:");
        Console.WriteLine("   Email: loyola@gmail.com");
        Console.WriteLine("   Password: admin123");
    }
    
    // Seed subjects if none exist
    if (!dbContext.Subjects.Any())
    {
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
        await dbContext.SaveChangesAsync();
        Console.WriteLine("✅ Default subjects created.");
    }
}

app.Run();