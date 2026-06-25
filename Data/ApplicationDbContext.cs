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

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<ClassSubject> ClassSubjects { get; set; }
        public DbSet<StudentSubject> StudentSubjects { get; set; }
        public DbSet<TeacherSubject> TeacherSubjects { get; set; }
        public DbSet<Marks> Marks { get; set; }
        public DbSet<StudentMark> StudentMarks { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<TeacherSubjectAllocation> TeacherSubjectAllocations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure all entities
            ConfigureUser(modelBuilder);
            ConfigureStudent(modelBuilder);
            ConfigureClass(modelBuilder);
            ConfigureSubject(modelBuilder);
            ConfigureClassSubject(modelBuilder);
            ConfigureStudentSubject(modelBuilder);
            ConfigureTeacherSubject(modelBuilder);
            ConfigureMarks(modelBuilder);
            ConfigureStudentMark(modelBuilder);
            ConfigureExam(modelBuilder);
            ConfigureExamResult(modelBuilder);
            ConfigureNotification(modelBuilder);
            ConfigureTeacherSubjectAllocation(modelBuilder);
        }

        #region Entity Configurations

        private void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                // Primary Key
                entity.HasKey(u => u.Id);

                // Properties
                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.PasswordHash)
                    .IsRequired();

                entity.Property(u => u.PhoneNumber)
                    .HasMaxLength(20);

                entity.Property(u => u.EmployeeId)
                    .HasMaxLength(50);

                entity.Property(u => u.Qualification)
                    .HasMaxLength(100);

                entity.Property(u => u.Role)
                    .IsRequired()
                    .HasMaxLength(20);

                // Indexes
                entity.HasIndex(u => u.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_Email");

                entity.HasIndex(u => u.EmployeeId)
                    .IsUnique(false)
                    .HasDatabaseName("IX_Users_EmployeeId");

                // Relationships
                entity.HasMany(u => u.ClassesAsTeacher)
                    .WithOne(c => c.Teacher)
                    .HasForeignKey(c => c.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(u => u.TeacherSubjects)
                    .WithOne(ts => ts.Teacher)
                    .HasForeignKey(ts => ts.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.Notifications)
                    .WithOne(n => n.User)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.StudentSubjects)
                    .WithOne(ss => ss.Teacher)
                    .HasForeignKey(ss => ss.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureStudent(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Student>(entity =>
    {
        // Primary Key
        entity.HasKey(s => s.Id);

        // Properties
        entity.Property(s => s.AdmissionNumber)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(s => s.FullName)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(s => s.Class)
            .HasMaxLength(50);

        entity.Property(s => s.Stream)
            .HasMaxLength(50);

        entity.Property(s => s.Email)
            .HasMaxLength(100);

        entity.Property(s => s.PhoneNumber)
            .HasMaxLength(20);

        entity.Property(s => s.Address)
            .HasMaxLength(200);

        entity.Property(s => s.Gender)
            .HasMaxLength(10);

        // Indexes
        entity.HasIndex(s => s.AdmissionNumber)
            .IsUnique()
            .HasDatabaseName("IX_Students_AdmissionNumber");

        entity.HasIndex(s => s.Email)
            .IsUnique(false)
            .HasDatabaseName("IX_Students_Email");

        entity.HasIndex(s => new { s.Class, s.Stream })
            .HasDatabaseName("IX_Students_Class_Stream");

        // Relationships - Only ONE notification relationship
        entity.HasOne(s => s.Teacher)
            .WithMany(u => u.Students)
            .HasForeignKey(s => s.TeacherId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(s => s.ClassEntity)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.SetNull);

        // This is the ONLY notification relationship - REMOVED SpecificStudent
        entity.HasMany(s => s.Notifications)
            .WithOne(n => n.Student)
            .HasForeignKey(n => n.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(s => s.ExamResults)
            .WithOne(er => er.Student)
            .HasForeignKey(er => er.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(s => s.StudentSubjects)
            .WithOne(ss => ss.Student)
            .HasForeignKey(ss => ss.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(s => s.Marks)
            .WithOne(m => m.Student)
            .HasForeignKey(m => m.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(s => s.StudentMarks)
            .WithOne(sm => sm.Student)
            .HasForeignKey(sm => sm.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}
        private void ConfigureClass(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Class>(entity =>
            {
                // Primary Key
                entity.HasKey(c => c.Id);

                // Properties
                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(c => c.Stream)
                    .HasMaxLength(50);

                entity.Property(c => c.Capacity)
                    .IsRequired(false);

                // Indexes
                entity.HasIndex(c => new { c.Name, c.Stream })
                    .IsUnique()
                    .HasDatabaseName("IX_Classes_Name_Stream");

                // Relationships
                entity.HasOne(c => c.Teacher)
                    .WithMany(u => u.ClassesAsTeacher)
                    .HasForeignKey(c => c.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(c => c.ClassSubjects)
                    .WithOne(cs => cs.Class)
                    .HasForeignKey(cs => cs.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(c => c.Students)
                    .WithOne(s => s.ClassEntity)
                    .HasForeignKey(s => s.ClassId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(c => c.Marks)
                    .WithOne(m => m.Class)
                    .HasForeignKey(m => m.ClassId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(c => c.Exams)
                    .WithOne(e => e.Class)
                    .HasForeignKey(e => e.ClassId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureSubject(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Subject>(entity =>
            {
                // Primary Key
                entity.HasKey(s => s.Id);

                // Properties
                entity.Property(s => s.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(s => s.Code)
                    .HasMaxLength(20);

                entity.Property(s => s.Type)
                    .HasMaxLength(50);

                entity.Property(s => s.Description)
                    .HasMaxLength(500);

                // Indexes
                entity.HasIndex(s => s.Name)
                    .IsUnique()
                    .HasDatabaseName("IX_Subjects_Name");

                entity.HasIndex(s => s.Code)
                    .IsUnique(false)
                    .HasDatabaseName("IX_Subjects_Code");

                // Relationships
                entity.HasMany(s => s.ClassSubjects)
                    .WithOne(cs => cs.Subject)
                    .HasForeignKey(cs => cs.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(s => s.TeacherSubjects)
                    .WithOne(ts => ts.Subject)
                    .HasForeignKey(ts => ts.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(s => s.StudentSubjects)
                    .WithOne(ss => ss.Subject)
                    .HasForeignKey(ss => ss.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(s => s.Marks)
                    .WithOne(m => m.Subject)
                    .HasForeignKey(m => m.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(s => s.StudentMarks)
                    .WithOne(sm => sm.Subject)
                    .HasForeignKey(sm => sm.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(s => s.ExamResults)
                    .WithOne(er => er.Subject)
                    .HasForeignKey(er => er.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(s => s.Exams)
                    .WithOne(e => e.Subject)
                    .HasForeignKey(e => e.SubjectId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureClassSubject(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClassSubject>(entity =>
            {
                // Primary Key
                entity.HasKey(cs => cs.Id);

                // Properties
                entity.Property(cs => cs.AssignedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Indexes
                entity.HasIndex(cs => new { cs.ClassId, cs.SubjectId })
                    .IsUnique()
                    .HasDatabaseName("IX_ClassSubjects_ClassId_SubjectId");

                // Relationships
                entity.HasOne(cs => cs.Class)
                    .WithMany(c => c.ClassSubjects)
                    .HasForeignKey(cs => cs.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cs => cs.Subject)
                    .WithMany(s => s.ClassSubjects)
                    .HasForeignKey(cs => cs.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cs => cs.Teacher)
                    .WithMany(u => u.ClassSubjects)
                    .HasForeignKey(cs => cs.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureStudentSubject(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StudentSubject>(entity =>
            {
                // Primary Key
                entity.HasKey(ss => ss.Id);

                // Properties
                entity.Property(ss => ss.AcademicYear)
                    .IsRequired();

                entity.Property(ss => ss.Term)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(ss => ss.IsActive)
                    .HasDefaultValue(true);

                // Indexes
                entity.HasIndex(ss => new { ss.StudentId, ss.SubjectId, ss.AcademicYear, ss.Term })
                    .IsUnique()
                    .HasDatabaseName("IX_StudentSubjects_UniqueAllocation");

                // Relationships
                entity.HasOne(ss => ss.Student)
                    .WithMany(s => s.StudentSubjects)
                    .HasForeignKey(ss => ss.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ss => ss.Subject)
                    .WithMany(s => s.StudentSubjects)
                    .HasForeignKey(ss => ss.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ss => ss.Teacher)
                    .WithMany(u => u.StudentSubjects)
                    .HasForeignKey(ss => ss.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureTeacherSubject(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeacherSubject>(entity =>
            {
                // Primary Key
                entity.HasKey(ts => ts.Id);

                // Indexes
                entity.HasIndex(ts => new { ts.TeacherId, ts.SubjectId })
                    .IsUnique()
                    .HasDatabaseName("IX_TeacherSubjects_TeacherId_SubjectId");

                // Relationships
                entity.HasOne(ts => ts.Teacher)
                    .WithMany(u => u.TeacherSubjects)
                    .HasForeignKey(ts => ts.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ts => ts.Subject)
                    .WithMany(s => s.TeacherSubjects)
                    .HasForeignKey(ts => ts.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ts => ts.Class)
                    .WithMany()
                    .HasForeignKey(ts => ts.ClassId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureMarks(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Marks>(entity =>
            {
                // Primary Key
                entity.HasKey(m => m.Id);

                // Properties
                entity.Property(m => m.Grade)
                    .HasMaxLength(5);

                entity.Property(m => m.Remark)
                    .HasMaxLength(200);

                entity.Property(m => m.Term)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(m => m.ContinuousTest1)
                    .HasPrecision(5, 2);

                entity.Property(m => m.ContinuousTest2)
                    .HasPrecision(5, 2);

                entity.Property(m => m.EndTermExam)
                    .HasPrecision(5, 2);

                entity.Property(m => m.TotalScore)
                    .HasPrecision(5, 2);

                // Indexes
                entity.HasIndex(m => new { m.StudentId, m.SubjectId, m.Year, m.Term })
                    .IsUnique()
                    .HasDatabaseName("IX_Marks_StudentId_SubjectId_Year_Term");

                // Relationships
                entity.HasOne(m => m.Student)
                    .WithMany(s => s.Marks)
                    .HasForeignKey(m => m.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Subject)
                    .WithMany(s => s.Marks)
                    .HasForeignKey(m => m.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Class)
                    .WithMany(c => c.Marks)
                    .HasForeignKey(m => m.ClassId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureStudentMark(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StudentMark>(entity =>
            {
                // Primary Key
                entity.HasKey(sm => sm.Id);

                // Properties
                entity.Property(sm => sm.Term)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(sm => sm.Test1)
                    .HasPrecision(5, 2);

                entity.Property(sm => sm.Test2)
                    .HasPrecision(5, 2);

                entity.Property(sm => sm.EndTerm)
                    .HasPrecision(5, 2);

                // Indexes
                entity.HasIndex(sm => new { sm.StudentId, sm.SubjectId, sm.Year, sm.Term })
                    .IsUnique()
                    .HasDatabaseName("IX_StudentMarks_StudentId_SubjectId_Year_Term");

                // Relationships
                entity.HasOne(sm => sm.Student)
                    .WithMany(s => s.StudentMarks)
                    .HasForeignKey(sm => sm.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(sm => sm.Subject)
                    .WithMany(s => s.StudentMarks)
                    .HasForeignKey(sm => sm.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureExam(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Exam>(entity =>
            {
                // Primary Key
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Venue)
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                // Indexes
                entity.HasIndex(e => e.ExamDate)
                    .HasDatabaseName("IX_Exams_ExamDate");

                // Relationships
                entity.HasOne(e => e.Subject)
                    .WithMany(s => s.Exams)
                    .HasForeignKey(e => e.SubjectId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Class)
                    .WithMany(c => c.Exams)
                    .HasForeignKey(e => e.ClassId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(e => e.ExamResults)
                    .WithOne(er => er.Exam)
                    .HasForeignKey(er => er.ExamId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureExamResult(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExamResult>(entity =>
            {
                // Primary Key
                entity.HasKey(er => er.Id);

                // Properties
                entity.Property(er => er.Grade)
                    .HasMaxLength(5);

                entity.Property(er => er.Remark)
                    .HasMaxLength(200);

                entity.Property(er => er.Score)
                    .HasPrecision(5, 2);

                // Indexes
                entity.HasIndex(er => new { er.ExamId, er.StudentId })
                    .IsUnique()
                    .HasDatabaseName("IX_ExamResults_ExamId_StudentId");

                // Relationships
                entity.HasOne(er => er.Exam)
                    .WithMany(e => e.ExamResults)
                    .HasForeignKey(er => er.ExamId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(er => er.Student)
                    .WithMany(s => s.ExamResults)
                    .HasForeignKey(er => er.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(er => er.Subject)
                    .WithMany(s => s.ExamResults)
                    .HasForeignKey(er => er.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(er => er.EnteredByTeacher)
                    .WithMany()
                    .HasForeignKey(er => er.EnteredByTeacherId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureNotification(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Notification>(entity =>
    {
        // Primary Key
        entity.HasKey(n => n.Id);

        // Properties
        entity.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(1000);

        entity.Property(n => n.Type)
            .HasMaxLength(50);

        entity.Property(n => n.Role)
            .HasMaxLength(20);

        entity.Property(n => n.Link)
            .HasMaxLength(500);

        // Indexes for performance
        entity.HasIndex(n => n.CreatedAt)
            .HasDatabaseName("IX_Notifications_CreatedAt");

        entity.HasIndex(n => n.IsRead)
            .HasDatabaseName("IX_Notifications_IsRead");

        entity.HasIndex(n => n.UserId)
            .HasDatabaseName("IX_Notifications_UserId");

        entity.HasIndex(n => n.StudentId)
            .HasDatabaseName("IX_Notifications_StudentId");

        entity.HasIndex(n => n.TeacherId)
            .HasDatabaseName("IX_Notifications_TeacherId");

        entity.HasIndex(n => n.AdminId)
            .HasDatabaseName("IX_Notifications_AdminId");

        entity.HasIndex(n => new { n.Type, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_Type_CreatedAt");

        // Relationships - Only ONE relationship per navigation
        entity.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(n => n.Student)
            .WithMany(s => s.Notifications)
            .HasForeignKey(n => n.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(n => n.Teacher)
            .WithMany() // No navigation property on User for this relationship
            .HasForeignKey(n => n.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(n => n.Admin)
            .WithMany() // No navigation property on User for this relationship
            .HasForeignKey(n => n.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
    });
}

        private void ConfigureTeacherSubjectAllocation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeacherSubjectAllocation>(entity =>
            {
                // Primary Key
                entity.HasKey(tsa => tsa.Id);

                // Indexes
                entity.HasIndex(tsa => new { tsa.ClassId, tsa.SubjectId })
                    .IsUnique()
                    .HasDatabaseName("IX_TeacherSubjectAllocations_ClassId_SubjectId");

                // Relationships
                entity.HasOne(tsa => tsa.Class)
                    .WithMany()
                    .HasForeignKey(tsa => tsa.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tsa => tsa.Subject)
                    .WithMany()
                    .HasForeignKey(tsa => tsa.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tsa => tsa.Teacher)
                    .WithMany()
                    .HasForeignKey(tsa => tsa.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        #endregion
    }
}