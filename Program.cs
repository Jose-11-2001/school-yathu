using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using School_Yathu.Data;
using School_Yathu.Models;
using School_Yathu.Services;
using BCrypt.Net;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure the port FIRST – Render requires this
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
Console.WriteLine($"Configuring web host to listen on port: {port}");

// 2. Add services
builder.Services.AddControllers();

// 3. Configure Swagger with XML documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "School Yathu API",
        Version = "v1",
        Description = "API for School Yathu - Student Management System",
        Contact = new OpenApiContact
        {
            Name = "School Yathu",
            Email = "support@school-yathu.com",
            Url = new Uri("https://school-yathu.onrender.com")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        },
        TermsOfService = new Uri("https://school-yathu.onrender.com/terms")
    });

    // Enable XML comments for Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 4. Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 5. JWT Authentication
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

// 6. CORS – Allow all for initial deployment
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 7. Email services
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// 8. Root endpoints
app.MapGet("/", () => Results.Ok(new { 
    message = "School Yathu API is running!", 
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow 
}));

// 9. Configure pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "School Yathu API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "School Yathu API Documentation";
        c.EnableTryItOutByDefault();
    });
}

// app.UseHttpsRedirection(); // ❌ IMPORTANT: Comment this out for Render!

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 10. Database seeding
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

Console.WriteLine($"Application starting successfully on port: {port}");
app.Run();