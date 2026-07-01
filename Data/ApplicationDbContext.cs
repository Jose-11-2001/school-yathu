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

                // ✅ Relationship: User as Teacher -> Classes they teach
                entity.HasMany(u => u.ClassesAsTeacher)
                    .WithOne(c => c.Teacher)
                    .HasForeignKey(c => c.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Classes_TeacherId");

                // ✅ Relationship: User as Form Teacher -> Classes they are form teacher for
                entity.HasMany(u => u.FormTeacherClasses)
                    .WithOne(c => c.FormTeacher)
                    .HasForeignKey(c => c.FormTeacherId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Classes_FormTeacherId");

                // ✅ Relationship: Subjects taught by this teacher
                entity.HasMany(u => u.TeacherSubjects)
                    .WithOne(ts => ts.Teacher)
                    .HasForeignKey(ts => ts.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_TeacherSubjects_TeacherId");

                // ✅ Relationship: Students assigned to this teacher
                entity.HasMany(u => u.Students)
                    .WithOne(s => s.Teacher)
                    .HasForeignKey(s => s.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Students_TeacherId");

                // ✅ Relationship: Notifications for this user
                entity.HasMany(u => u.Notifications)
                    .WithOne(n => n.User)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Notifications_UserId");
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

                entity.HasIndex(s => s.Email)
                    .IsUnique(false)
                    .HasDatabaseName("IX_Students_Email");

                entity.HasIndex(s => new { s.Class, s.Stream })
                    .HasDatabaseName("IX_Students_Class_Stream");

                // ✅ Relationship: Student -> Teacher
                entity.HasOne(s => s.Teacher)
                    .WithMany(u => u.Students)
                    .HasForeignKey(s => s.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Students_TeacherId");

                // ✅ Relationship: Student -> Class
                entity.HasOne(s => s.ClassEntity)
                    .WithMany(c => c.Students)
                    .HasForeignKey(s => s.ClassId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Students_ClassId");

                // ✅ Relationship: Student -> Notifications
                entity.HasMany(s => s.Notifications)
                    .WithOne(n => n.Student)
                    .HasForeignKey(n => n.StudentId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Notifications_StudentId");

                // ✅ Relationship: Student -> ExamResults
                entity.HasMany(s => s.ExamResults)
                    .WithOne(er => er.Student)
                    .HasForeignKey(er => er.StudentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ExamResults_StudentId");

                // ✅ Relationship: Student -> StudentSubjects
                entity.HasMany(s => s.StudentSubjects)
                    .WithOne(ss => ss.Student)
                    .HasForeignKey(ss => ss.StudentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_StudentSubjects_StudentId");

                // ✅ Relationship: Student -> Marks
                entity.HasMany(s => s.Marks)
                    .WithOne(m => m.Student)
                    .HasForeignKey(m => m.StudentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Marks_StudentId");

                // ✅ Relationship: Student -> StudentMarks
                entity.HasMany(s => s.StudentMarks)
                    .WithOne(sm => sm.Student)
                    .HasForeignKey(sm => sm.StudentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_StudentMarks_StudentId");
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

                // ✅ Relationship: Class -> Teacher (Class Teacher)
                entity.HasOne(c => c.Teacher)
                    .WithMany(u => u.ClassesAsTeacher)
                    .HasForeignKey(c => c.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Classes_TeacherId");

                // ✅ Relationship: Class -> FormTeacher
                entity.HasOne(c => c.FormTeacher)
                    .WithMany(u => u.FormTeacherClasses)
                    .HasForeignKey(c => c.FormTeacherId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Classes_FormTeacherId");

                // ✅ Relationship: Class -> ClassSubjects
                entity.HasMany(c => c.ClassSubjects)
                    .WithOne(cs => cs.Class)
                    .HasForeignKey(cs => cs.ClassId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ClassSubjects_ClassId");

                // ✅ Relationship: Class -> Students
                entity.HasMany(c => c.Students)
                    .WithOne(s => s.ClassEntity)
                    .HasForeignKey(s => s.ClassId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Students_ClassId");

                // ✅ Relationship: Class -> Marks
                entity.HasMany(c => c.Marks)
                    .WithOne(m => m.Class)
                    .HasForeignKey(m => m.ClassId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Marks_ClassId");

                // ✅ Relationship: Class -> Exams
                entity.HasMany(c => c.Exams)
                    .WithOne(e => e.Class)
                    .HasForeignKey(e => e.ClassId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Exams_ClassId");
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

                // Indexes
                entity.HasIndex(s => s.Name)
                    .IsUnique()
                    .HasDatabaseName("IX_Subjects_Name");

                entity.HasIndex(s => s.Code)
                    .IsUnique(false)
                    .HasDatabaseName("IX_Subjects_Code");

                // ✅ Relationship: Subject -> Department
                entity.HasOne(s => s.Department)
                    .WithMany(d => d.Subjects)
                    .HasForeignKey(s => s.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Subjects_DepartmentId");

                // ✅ Relationship: Subject -> ClassSubjects
                entity.HasMany(s => s.ClassSubjects)
                    .WithOne(cs => cs.Subject)
                    .HasForeignKey(cs => cs.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ClassSubjects_SubjectId");

                // ✅ Relationship: Subject -> TeacherSubjects
                entity.HasMany(s => s.TeacherSubjects)
                    .WithOne(ts => ts.Subject)
                    .HasForeignKey(ts => ts.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_TeacherSubjects_SubjectId");

                // ✅ Relationship: Subject -> StudentSubjects
                entity.HasMany(s => s.StudentSubjects)
                    .WithOne(ss => ss.Subject)
                    .HasForeignKey(ss => ss.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_StudentSubjects_SubjectId");

                // ✅ Relationship: Subject -> Marks
                entity.HasMany(s => s.Marks)
                    .WithOne(m => m.Subject)
                    .HasForeignKey(m => m.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Marks_SubjectId");

                // ✅ Relationship: Subject -> StudentMarks
                entity.HasMany(s => s.StudentMarks)
                    .WithOne(sm => sm.Subject)
                    .HasForeignKey(sm => sm.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_StudentMarks_SubjectId");

                // ✅ Relationship: Subject -> ExamResults
                entity.HasMany(s => s.ExamResults)
                    .WithOne(er => er.Subject)
                    .HasForeignKey(er => er.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ExamResults_SubjectId");

                // ✅ Relationship: Subject -> Exams
                entity.HasMany(s => s.Exams)
                    .WithOne(e => e.Subject)
                    .HasForeignKey(e => e.SubjectId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Exams_SubjectId");
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

                // ✅ Relationship: ClassSubject -> Class
                entity.HasOne(cs => cs.Class)
                    .WithMany(c => c.ClassSubjects)
                    .HasForeignKey(cs => cs.ClassId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ClassSubjects_ClassId");

                // ✅ Relationship: ClassSubject -> Subject
                entity.HasOne(cs => cs.Subject)
                    .WithMany(s => s.ClassSubjects)
                    .HasForeignKey(cs => cs.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ClassSubjects_SubjectId");

                // ✅ Relationship: ClassSubject -> Teacher
                entity.HasOne(cs => cs.Teacher)
                    .WithMany(u => u.ClassSubjects)
                    .HasForeignKey(cs => cs.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_ClassSubjects_TeacherId");
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

                // ✅ Relationship: StudentSubject -> Student
                entity.HasOne(ss => ss.Student)
                    .WithMany(s => s.StudentSubjects)
                    .HasForeignKey(ss => ss.StudentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_StudentSubjects_StudentId");

                // ✅ Relationship: StudentSubject -> Subject
                entity.HasOne(ss => ss.Subject)
                    .WithMany(s => s.StudentSubjects)
                    .HasForeignKey(ss => ss.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_StudentSubjects_SubjectId");

                // ✅ Relationship: StudentSubject -> Teacher
                entity.HasOne(ss => ss.Teacher)
                    .WithMany(u => u.StudentSubjects)
                    .HasForeignKey(ss => ss.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_StudentSubjects_TeacherId");
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

                // ✅ Relationship: TeacherSubject -> Teacher
                entity.HasOne(ts => ts.Teacher)
                    .WithMany(u => u.TeacherSubjects)
                    .HasForeignKey(ts => ts.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_TeacherSubjects_TeacherId");

                // ✅ Relationship: TeacherSubject -> Subject
                entity.HasOne(ts => ts.Subject)
                    .WithMany(s => s.TeacherSubjects)
                    .HasForeignKey(ts => ts.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_TeacherSubjects_SubjectId");

                // ✅ Relationship: TeacherSubject -> Class
                entity.HasOne(ts => ts.Class)
                    .WithMany()
                    .HasForeignKey(ts => ts.ClassId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_TeacherSubjects_ClassId");
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

                // ✅ Relationship: Marks -> Student
                entity.HasOne(m => m.Student)
                    .WithMany(s => s.Marks)
                    .HasForeignKey(m => m.StudentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Marks_StudentId");

                // ✅ Relationship: Marks -> Subject
                entity.HasOne(m => m.Subject)
                    .WithMany(s => s.Marks)
                    .HasForeignKey(m => m.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Marks_SubjectId");

                // ✅ Relationship: Marks -> Class
                entity.HasOne(m => m.Class)
                    .WithMany(c => c.Marks)
                    .HasForeignKey(m => m.ClassId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Marks_ClassId");

                // ✅ Relationship: Marks -> EnteredByTeacher
                entity.HasOne(m => m.EnteredByTeacher)
                    .WithMany(u => u.EnteredMarks)
                    .HasForeignKey(m => m.EnteredByTeacherId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Marks_EnteredByTeacherId");

                // ✅ Relationship: Marks -> ApprovedByAdmin
                entity.HasOne(m => m.ApprovedByAdmin)
                    .WithMany(u => u.ApprovedMarks)
                    .HasForeignKey(m => m.ApprovedByAdminId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Marks_ApprovedByAdminId");
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

                // ✅ Relationship: StudentMark -> Student
                entity.HasOne(sm => sm.Student)
                    .WithMany(s => s.StudentMarks)
                    .HasForeignKey(sm => sm.StudentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_StudentMarks_StudentId");

                // ✅ Relationship: StudentMark -> Subject
                entity.HasOne(sm => sm.Subject)
                    .WithMany(s => s.StudentMarks)
                    .HasForeignKey(sm => sm.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_StudentMarks_SubjectId");
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

                // ✅ Relationship: Exam -> Subject
                entity.HasOne(e => e.Subject)
                    .WithMany(s => s.Exams)
                    .HasForeignKey(e => e.SubjectId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Exams_SubjectId");

                // ✅ Relationship: Exam -> Class
                entity.HasOne(e => e.Class)
                    .WithMany(c => c.Exams)
                    .HasForeignKey(e => e.ClassId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Exams_ClassId");

                // ✅ Relationship: Exam -> ExamResults
                entity.HasMany(e => e.ExamResults)
                    .WithOne(er => er.Exam)
                    .HasForeignKey(er => er.ExamId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ExamResults_ExamId");
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

                // ✅ Relationship: ExamResult -> Exam
                entity.HasOne(er => er.Exam)
                    .WithMany(e => e.ExamResults)
                    .HasForeignKey(er => er.ExamId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ExamResults_ExamId");

                // ✅ Relationship: ExamResult -> Student
                entity.HasOne(er => er.Student)
                    .WithMany(s => s.ExamResults)
                    .HasForeignKey(er => er.StudentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ExamResults_StudentId");

                // ✅ Relationship: ExamResult -> Subject
                entity.HasOne(er => er.Subject)
                    .WithMany(s => s.ExamResults)
                    .HasForeignKey(er => er.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ExamResults_SubjectId");

                // ✅ Relationship: ExamResult -> EnteredByTeacher
                entity.HasOne(er => er.EnteredByTeacher)
                    .WithMany(u => u.ExamResults)
                    .HasForeignKey(er => er.EnteredByTeacherId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_ExamResults_EnteredByTeacherId");
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

                // ✅ Relationship: Notification -> User
                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Notifications_UserId");

                // ✅ Relationship: Notification -> Student
                entity.HasOne(n => n.Student)
                    .WithMany(s => s.Notifications)
                    .HasForeignKey(n => n.StudentId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Notifications_StudentId");

                // ✅ Relationship: Notification -> Teacher
                entity.HasOne(n => n.Teacher)
                    .WithMany()
                    .HasForeignKey(n => n.TeacherId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Notifications_TeacherId");

                // ✅ Relationship: Notification -> Admin
                entity.HasOne(n => n.Admin)
                    .WithMany()
                    .HasForeignKey(n => n.AdminId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Notifications_AdminId");
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

                // ✅ Relationship: TeacherSubjectAllocation -> Class
                entity.HasOne(tsa => tsa.Class)
                    .WithMany()
                    .HasForeignKey(tsa => tsa.ClassId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_TeacherSubjectAllocations_ClassId");

                // ✅ Relationship: TeacherSubjectAllocation -> Subject
                entity.HasOne(tsa => tsa.Subject)
                    .WithMany()
                    .HasForeignKey(tsa => tsa.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_TeacherSubjectAllocations_SubjectId");

                // ✅ Relationship: TeacherSubjectAllocation -> Teacher
                entity.HasOne(tsa => tsa.Teacher)
                    .WithMany()
                    .HasForeignKey(tsa => tsa.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_TeacherSubjectAllocations_TeacherId");
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

                // ✅ Relationship: Department -> Subjects
                entity.HasMany(d => d.Subjects)
                    .WithOne(s => s.Department)
                    .HasForeignKey(s => s.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Subjects_DepartmentId");

                // ✅ Relationship: Department -> Teachers
                entity.HasMany(d => d.Teachers)
                    .WithOne(u => u.Department)
                    .HasForeignKey(u => u.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Users_DepartmentId");

                // ✅ Relationship: Department -> HeadOfDepartment
                entity.HasOne(d => d.HeadOfDepartment)
                    .WithMany()
                    .HasForeignKey(d => d.HeadOfDepartmentId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Departments_HeadOfDepartmentId");
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

                // ✅ Relationship: FormTeacherClass -> Teacher
                entity.HasOne(ftc => ftc.Teacher)
                    .WithMany(u => u.FormTeacherClassAssignments)
                    .HasForeignKey(ftc => ftc.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_FormTeacherClasses_TeacherId");

                // ✅ Relationship: FormTeacherClass -> Class
                entity.HasOne(ftc => ftc.Class)
                    .WithMany()
                    .HasForeignKey(ftc => ftc.ClassId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_FormTeacherClasses_ClassId");
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

                // ✅ Relationship: StudentSubjectSelection -> Student
                entity.HasOne(sss => sss.Student)
                    .WithMany()
                    .HasForeignKey(sss => sss.StudentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_StudentSubjectSelections_StudentId");

                // ✅ Relationship: StudentSubjectSelection -> Subject
                entity.HasOne(sss => sss.Subject)
                    .WithMany()
                    .HasForeignKey(sss => sss.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_StudentSubjectSelections_SubjectId");

                // ✅ Relationship: StudentSubjectSelection -> ApprovedByFormTeacher
                entity.HasOne(sss => sss.ApprovedByFormTeacher)
                    .WithMany()
                    .HasForeignKey(sss => sss.ApprovedByFormTeacherId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_StudentSubjectSelections_ApprovedByFormTeacherId");
            });
        }

        #endregion
    }
}