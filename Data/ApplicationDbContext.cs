using Microsoft.EntityFrameworkCore;
using School_Yathu.Models;

namespace School_Yathu.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Marks> Marks { get; set; }
        public DbSet<TeacherSubject> TeacherSubjects { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<ClassSubject> ClassSubjects { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Student>()
                .HasIndex(s => s.AdmissionNumber)
                .IsUnique();
            
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            
            modelBuilder.Entity<User>()
                .HasIndex(u => u.EmployeeId)
                .IsUnique();
            
            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.Name)
                .IsUnique();
            
            modelBuilder.Entity<Class>()
                .HasIndex(c => new { c.Name, c.Stream })
                .IsUnique();
            
            // ExamResult composite unique constraint
            modelBuilder.Entity<ExamResult>()
                .HasIndex(er => new { er.ExamId, er.StudentId, er.SubjectId })
                .IsUnique();
        }
    }
}