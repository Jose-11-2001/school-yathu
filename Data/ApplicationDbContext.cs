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
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<StudentSubject> StudentSubjects { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<TeacherSubjectAllocation> TeacherSubjectAllocations { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Student configuration
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasIndex(s => s.AdmissionNumber).IsUnique();
                
                entity.HasMany(s => s.Notifications)
                      .WithOne(n => n.SpecificStudent)
                      .HasForeignKey(n => n.SpecificStudentId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasMany(s => s.ExamResults)
                      .WithOne(er => er.Student)
                      .HasForeignKey(er => er.StudentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            
            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
            });
            
            // Subject configuration
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.HasIndex(s => s.Name).IsUnique();
                
                entity.HasMany(s => s.ExamResults)
                      .WithOne(er => er.Subject)
                      .HasForeignKey(er => er.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Marks configuration
            modelBuilder.Entity<Marks>(entity =>
            {
                entity.HasIndex(m => new { m.StudentId, m.SubjectId, m.Year, m.Term })
                      .IsUnique();
            });
            
            // StudentSubject configuration
            modelBuilder.Entity<StudentSubject>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.AcademicYear)
                      .IsRequired();
                
                entity.Property(e => e.Term)
                      .HasMaxLength(20)
                      .IsRequired();
                
                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true);
                
                entity.HasOne(e => e.Student)
                      .WithMany(s => s.StudentSubjects)
                      .HasForeignKey(e => e.StudentId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Subject)
                      .WithMany(s => s.StudentSubjects)
                      .HasForeignKey(e => e.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Teacher)
                      .WithMany()
                      .HasForeignKey(e => e.TeacherId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => new { e.StudentId, e.SubjectId, e.AcademicYear, e.Term })
                      .IsUnique()
                      .HasDatabaseName("IX_StudentSubjects_UniqueAllocation");
            });
            
            // TeacherSubject configuration
            modelBuilder.Entity<TeacherSubject>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Teacher)
                      .WithMany(t => t.TeacherSubjects)
                      .HasForeignKey(e => e.TeacherId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Subject)
                      .WithMany(s => s.TeacherSubjects)
                      .HasForeignKey(e => e.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => new { e.TeacherId, e.SubjectId })
                      .IsUnique();
            });
            
            // Class configuration
            modelBuilder.Entity<Class>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name)
                      .HasMaxLength(50)
                      .IsRequired();
                
                entity.Property(e => e.Stream)
                      .HasMaxLength(20);
                
                entity.HasOne(e => e.Teacher)
                      .WithMany(t => t.Classes)
                      .HasForeignKey(e => e.TeacherId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
            
            // ClassSubject configuration
            modelBuilder.Entity<ClassSubject>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.AssignedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                entity.HasOne(e => e.Class)
                      .WithMany(c => c.ClassSubjects)
                      .HasForeignKey(e => e.ClassId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Subject)
                      .WithMany(s => s.ClassSubjects)
                      .HasForeignKey(e => e.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Teacher)
                      .WithMany()
                      .HasForeignKey(e => e.TeacherId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => new { e.ClassId, e.SubjectId })
                      .IsUnique();
            });
            
            // Exam configuration
            modelBuilder.Entity<Exam>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name)
                      .HasMaxLength(200)
                      .IsRequired();
                
                entity.Property(e => e.Type)
                      .HasMaxLength(50);
                
                entity.Property(e => e.Term)
                      .HasMaxLength(20);
                
                entity.HasMany(e => e.ExamResults)
                      .WithOne(er => er.Exam)
                      .HasForeignKey(er => er.ExamId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            
            // ExamResult configuration
            modelBuilder.Entity<ExamResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Exam)
                      .WithMany(e => e.ExamResults)
                      .HasForeignKey(e => e.ExamId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Student)
                      .WithMany(s => s.ExamResults)
                      .HasForeignKey(e => e.StudentId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Subject)
                      .WithMany(s => s.ExamResults)
                      .HasForeignKey(e => e.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.EnteredByTeacher)
                      .WithMany()
                      .HasForeignKey(e => e.EnteredByTeacherId)
                      .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasIndex(e => new { e.ExamId, e.StudentId })
                      .IsUnique();
            });
            
            // Notification configuration
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Title)
                      .HasMaxLength(200);
                
                entity.Property(e => e.Message)
                      .HasMaxLength(1000);
                
                entity.Property(e => e.Type)
                      .HasMaxLength(50);
                
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsRead);
                entity.HasIndex(e => e.StudentId);
                entity.HasIndex(e => e.TeacherId);
                
                entity.HasOne(n => n.SpecificStudent)
                      .WithMany(s => s.Notifications)
                      .HasForeignKey(n => n.SpecificStudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(n => n.SpecificTeacher)
                      .WithMany()
                      .HasForeignKey(n => n.SpecificTeacherId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(n => n.User)
                      .WithMany()
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            
            // TeacherSubjectAllocation configuration
            modelBuilder.Entity<TeacherSubjectAllocation>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Class)
                      .WithMany()
                      .HasForeignKey(e => e.ClassId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Subject)
                      .WithMany()
                      .HasForeignKey(e => e.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Teacher)
                      .WithMany()
                      .HasForeignKey(e => e.TeacherId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => new { e.ClassId, e.SubjectId })
                      .IsUnique();
            });
        }
    }
}