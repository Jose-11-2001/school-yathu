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
        
        public DbSet<Department> Departments { get; set; }
        public DbSet<FormTeacherClass> FormTeacherClasses { get; set; }
        public DbSet<StudentSubjectSelection> StudentSubjectSelections { get; set; }
        public DbSet<ClassRanking> ClassRankings { get; set; }

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
            ConfigureDepartment(modelBuilder);
            ConfigureFormTeacherClass(modelBuilder);
            ConfigureStudentSubjectSelection(modelBuilder);
        }

        #region Entity Configurations

        private void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

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

                entity.Property(u => u.UpdatedAt)
                    .IsRequired(false);

                // Indexes
                entity.HasIndex(u => u.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_Email");

                entity.HasIndex(u => u.EmployeeId)
                    .IsUnique(false)
                    .HasDatabaseName("IX_Users_EmployeeId");
            });
        }

        private void ConfigureStudent(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(s => s.Id);

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

                entity.HasIndex(s => new { s.Class, s.Stream })
                    .HasDatabaseName("IX_Students_Class_Stream");
            });
        }

        private void ConfigureClass(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Class>(entity =>
            {
                entity.HasKey(c => c.Id);

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
            });
        }

        private void ConfigureSubject(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.Property(s => s.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(s => s.Code)
                    .HasMaxLength(20);

                entity.Property(s => s.Type)
                    .HasMaxLength(50);

                entity.Property(s => s.Description)
                    .HasMaxLength(500);

                // ✅ ADD Department relationship
                entity.HasOne(s => s.Department)
                    .WithMany(d => d.Subjects)
                    .HasForeignKey(s => s.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(s => s.Name)
                    .IsUnique()
                    .HasDatabaseName("IX_Subjects_Name");

                entity.HasIndex(s => s.Code)
                    .IsUnique(false)
                    .HasDatabaseName("IX_Subjects_Code");
            });
        }

        private void ConfigureClassSubject(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClassSubject>(entity =>
            {
                entity.HasKey(cs => cs.Id);

                entity.Property(cs => cs.AssignedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(cs => new { cs.ClassId, cs.SubjectId })
                    .IsUnique()
                    .HasDatabaseName("IX_ClassSubjects_ClassId_SubjectId");
            });
        }

        private void ConfigureStudentSubject(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StudentSubject>(entity =>
            {
                entity.HasKey(ss => ss.Id);

                entity.Property(ss => ss.AcademicYear)
                    .IsRequired();

                entity.Property(ss => ss.Term)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(ss => ss.IsActive)
                    .HasDefaultValue(true);

                entity.HasIndex(ss => new { ss.StudentId, ss.SubjectId, ss.AcademicYear, ss.Term })
                    .IsUnique()
                    .HasDatabaseName("IX_StudentSubjects_UniqueAllocation");
            });
        }

        private void ConfigureTeacherSubject(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeacherSubject>(entity =>
            {
                entity.HasKey(ts => ts.Id);

                entity.HasIndex(ts => new { ts.TeacherId, ts.SubjectId })
                    .IsUnique()
                    .HasDatabaseName("IX_TeacherSubjects_TeacherId_SubjectId");
            });
        }

        private void ConfigureMarks(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Marks>(entity =>
            {
                entity.HasKey(m => m.Id);

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

                entity.HasIndex(m => new { m.StudentId, m.SubjectId, m.Year, m.Term })
                    .IsUnique()
                    .HasDatabaseName("IX_Marks_StudentId_SubjectId_Year_Term");
            });
        }

        private void ConfigureStudentMark(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StudentMark>(entity =>
            {
                entity.HasKey(sm => sm.Id);

                entity.Property(sm => sm.Term)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(sm => sm.Test1)
                    .HasPrecision(5, 2);

                entity.Property(sm => sm.Test2)
                    .HasPrecision(5, 2);

                entity.Property(sm => sm.EndTerm)
                    .HasPrecision(5, 2);

                entity.HasIndex(sm => new { sm.StudentId, sm.SubjectId, sm.Year, sm.Term })
                    .IsUnique()
                    .HasDatabaseName("IX_StudentMarks_StudentId_SubjectId_Year_Term");
            });
        }

        private void ConfigureExam(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Exam>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Venue)
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.HasIndex(e => e.ExamDate)
                    .HasDatabaseName("IX_Exams_ExamDate");
            });
        }

        private void ConfigureExamResult(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExamResult>(entity =>
            {
                entity.HasKey(er => er.Id);

                entity.Property(er => er.Grade)
                    .HasMaxLength(5);

                entity.Property(er => er.Remark)
                    .HasMaxLength(200);

                entity.Property(er => er.Score)
                    .HasPrecision(5, 2);

                entity.HasIndex(er => new { er.ExamId, er.StudentId })
                    .IsUnique()
                    .HasDatabaseName("IX_ExamResults_ExamId_StudentId");
            });
        }

        private void ConfigureNotification(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.Id);

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

                // Indexes
                entity.HasIndex(n => n.CreatedAt)
                    .HasDatabaseName("IX_Notifications_CreatedAt");

                entity.HasIndex(n => n.IsRead)
                    .HasDatabaseName("IX_Notifications_IsRead");
            });
        }

        private void ConfigureTeacherSubjectAllocation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeacherSubjectAllocation>(entity =>
            {
                entity.HasKey(tsa => tsa.Id);

                entity.HasIndex(tsa => new { tsa.ClassId, tsa.SubjectId })
                    .IsUnique()
                    .HasDatabaseName("IX_TeacherSubjectAllocations_ClassId_SubjectId");
            });
        }

        private void ConfigureDepartment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(d => d.Id);

                entity.Property(d => d.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(d => d.Description)
                    .HasMaxLength(200);

                entity.HasMany(d => d.Subjects)
                    .WithOne(s => s.Department)
                    .HasForeignKey(s => s.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(d => d.Teachers)
                    .WithOne(u => u.Department)
                    .HasForeignKey(u => u.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureFormTeacherClass(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FormTeacherClass>(entity =>
            {
                entity.HasKey(ftc => ftc.Id);

                entity.HasIndex(ftc => new { ftc.TeacherId, ftc.ClassId })
                    .IsUnique()
                    .HasDatabaseName("IX_FormTeacherClasses_TeacherId_ClassId");
            });
        }

        private void ConfigureStudentSubjectSelection(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StudentSubjectSelection>(entity =>
            {
                entity.HasKey(sss => sss.Id);

                entity.HasIndex(sss => new { sss.StudentId, sss.SubjectId, sss.AcademicYear })
                    .IsUnique()
                    .HasDatabaseName("IX_StudentSubjectSelections_StudentId_SubjectId_Year");
            });
        }

        #endregion
    }
}