using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;
using School_Yathu.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [SwaggerTag("Admin Management - Teachers, Classes, Students, Departments")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== TEACHER MANAGEMENT ====================

        /// <summary>
        /// Get all teachers with department info
        /// </summary>
        [HttpGet("teachers")]
        [SwaggerOperation(Summary = "Get all teachers")]
        public async Task<IActionResult> GetTeachers()
        {
            var teachers = await _context.Users
                .Where(u => u.Role == "Teacher" && u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Name,
                    u.PhoneNumber,
                    u.EmployeeId,
                    u.Qualification,
                    u.HireDate,
                    u.DepartmentId,
                    DepartmentName = u.Department != null ? u.Department.Name : "Not Assigned",
                    u.CreatedAt,
                    u.IsActive,
                    u.MustChangePassword
                })
                .ToListAsync();
            return Ok(teachers);
        }

        /// <summary>
        /// Add a new teacher with department
        /// </summary>
        [HttpPost("teachers")]
        [SwaggerOperation(Summary = "Add a new teacher")]
        public async Task<IActionResult> AddTeacher([FromBody] CreateTeacherDTO dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "Email already exists" });

            var teacher = new User
            {
                Email = dto.Email,
                Name = dto.Name,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                PhoneNumber = dto.PhoneNumber,
                EmployeeId = dto.EmployeeId,
                Qualification = dto.Qualification,
                HireDate = dto.HireDate.HasValue ? DateTime.SpecifyKind(dto.HireDate.Value, DateTimeKind.Utc) : DateTime.UtcNow,
                DepartmentId = dto.DepartmentId,
                Role = "Teacher",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                MustChangePassword = true
            };

            _context.Users.Add(teacher);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Teacher added successfully", teacherId = teacher.Id });
        }

        /// <summary>
        /// Update an existing teacher
        /// </summary>
        [HttpPut("teachers/{id}")]
        [SwaggerOperation(Summary = "Update a teacher")]
        public async Task<IActionResult> UpdateTeacher(int id, [FromBody] UpdateTeacherDTO dto)
        {
            var teacher = await _context.Users.FindAsync(id);
            if (teacher == null)
                return NotFound(new { message = "Teacher not found" });

            if (teacher.Role != "Teacher")
                return BadRequest(new { message = "User is not a teacher" });

            if (!string.IsNullOrEmpty(dto.Name))
                teacher.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Email))
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Id != id);
                if (existingUser != null)
                    return BadRequest(new { message = "Email already exists" });
                teacher.Email = dto.Email;
            }

            if (!string.IsNullOrEmpty(dto.PhoneNumber))
                teacher.PhoneNumber = dto.PhoneNumber;

            if (!string.IsNullOrEmpty(dto.EmployeeId))
                teacher.EmployeeId = dto.EmployeeId;

            if (!string.IsNullOrEmpty(dto.Qualification))
                teacher.Qualification = dto.Qualification;

            if (dto.DepartmentId.HasValue)
                teacher.DepartmentId = dto.DepartmentId;

            teacher.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Teacher updated successfully" });
        }

        /// <summary>
        /// Delete/Deactivate a teacher
        /// </summary>
        [HttpDelete("teachers/{id}")]
        [SwaggerOperation(Summary = "Delete a teacher")]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            var teacher = await _context.Users.FindAsync(id);
            if (teacher == null)
                return NotFound(new { message = "Teacher not found" });

            if (teacher.Role != "Teacher")
                return BadRequest(new { message = "User is not a teacher" });

            teacher.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Teacher deactivated successfully" });
        }

        // ==================== DEPARTMENT MANAGEMENT ====================

        /// <summary>
        /// Get all departments with heads
        /// </summary>
        [HttpGet("departments")]
        [SwaggerOperation(Summary = "Get all departments")]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await _context.Departments
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Description,
                    d.HeadOfDepartmentId,
                    HeadName = d.HeadOfDepartment != null ? d.HeadOfDepartment.Name : "Not Assigned",
                    HeadEmail = d.HeadOfDepartment != null ? d.HeadOfDepartment.Email : null,
                    HeadPhone = d.HeadOfDepartment != null ? d.HeadOfDepartment.PhoneNumber : null,
                    TeacherCount = _context.Users.Count(u => u.DepartmentId == d.Id && u.Role == "Teacher" && u.IsActive),
                    SubjectCount = _context.Subjects.Count(s => s.DepartmentId == d.Id)
                })
                .ToListAsync();
            return Ok(departments);
        }

        /// <summary>
        /// Create a new department
        /// </summary>
        [HttpPost("departments")]
        [SwaggerOperation(Summary = "Create a new department")]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDTO dto)
        {
            var department = new Department
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Department created successfully", departmentId = department.Id });
        }

        /// <summary>
        /// Update a department
        /// </summary>
        [HttpPut("departments/{id}")]
        [SwaggerOperation(Summary = "Update a department")]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDTO dto)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound(new { message = "Department not found" });

            if (!string.IsNullOrEmpty(dto.Name))
                department.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Description))
                department.Description = dto.Description;

            department.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Department updated successfully" });
        }

        /// <summary>
        /// Delete a department
        /// </summary>
        [HttpDelete("departments/{id}")]
        [SwaggerOperation(Summary = "Delete a department")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Teachers)
                .Include(d => d.Subjects)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
                return NotFound(new { message = "Department not found" });

            if (department.Teachers.Any() || department.Subjects.Any())
                return BadRequest(new { message = "Cannot delete department with assigned teachers or subjects" });

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Department deleted successfully" });
        }

        /// <summary>
        /// Assign a Head of Department
        /// </summary>
        [HttpPost("assign-head-of-department")]
        [SwaggerOperation(Summary = "Assign Head of Department")]
        public async Task<IActionResult> AssignHeadOfDepartment([FromBody] AssignHeadOfDepartmentDTO dto)
        {
            var department = await _context.Departments.FindAsync(dto.DepartmentId);
            if (department == null)
                return NotFound(new { message = "Department not found" });

            var teacher = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.TeacherId && u.Role == "Teacher");

            if (teacher == null)
                return NotFound(new { message = "Teacher not found" });

            department.HeadOfDepartmentId = dto.TeacherId;
            await _context.SaveChangesAsync();

            // Update user role to HeadOfDepartment
            teacher.Role = "HeadOfDepartment";
            await _context.SaveChangesAsync();

            // Send notification
            var notification = new Notification
            {
                Title = "👔 Head of Department Appointment",
                Message = $"You have been appointed as Head of {department.Name} Department.",
                Type = "HODAppointment",
                TeacherId = dto.TeacherId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Head of Department assigned successfully" });
        }

        /// <summary>
        /// Remove Head of Department
        /// </summary>
        [HttpPost("remove-head-of-department")]
        [SwaggerOperation(Summary = "Remove Head of Department")]
        public async Task<IActionResult> RemoveHeadOfDepartment([FromBody] int departmentId)
        {
            var department = await _context.Departments.FindAsync(departmentId);
            if (department == null)
                return NotFound(new { message = "Department not found" });

            if (!department.HeadOfDepartmentId.HasValue)
                return BadRequest(new { message = "No Head of Department assigned" });

            var head = await _context.Users.FindAsync(department.HeadOfDepartmentId.Value);
            if (head != null && head.Role == "HeadOfDepartment")
            {
                head.Role = "Teacher";
                await _context.SaveChangesAsync();
            }

            department.HeadOfDepartmentId = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Head of Department removed successfully" });
        }

        // ==================== CLASS MANAGEMENT ====================

        /// <summary>
        /// Get all classes with form teachers
        /// </summary>
        [HttpGet("classes")]
        [SwaggerOperation(Summary = "Get all classes")]
        public async Task<IActionResult> GetClasses()
        {
            var classes = await _context.Classes
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Stream,
                    c.TeacherId,
                    TeacherName = c.Teacher != null ? c.Teacher.Name : "Not Assigned",
                    c.FormTeacherId,
                    FormTeacherName = c.FormTeacher != null ? c.FormTeacher.Name : "Not Assigned",
                    FormTeacherEmail = c.FormTeacher != null ? c.FormTeacher.Email : null,
                    FormTeacherPhone = c.FormTeacher != null ? c.FormTeacher.PhoneNumber : null,
                    c.Capacity,
                    StudentCount = _context.Students.Count(s => s.ClassId == c.Id),
                    c.CreatedAt
                })
                .ToListAsync();
            return Ok(classes);
        }

        /// <summary>
        /// Add a new class
        /// </summary>
        [HttpPost("classes")]
        [SwaggerOperation(Summary = "Add a new class")]
        public async Task<IActionResult> AddClass([FromBody] CreateClassDTO dto)
        {
            var newClass = new Class
            {
                Name = dto.Name,
                Stream = dto.Stream,
                TeacherId = dto.TeacherId,
                FormTeacherId = dto.FormTeacherId,
                Capacity = dto.Capacity,
                CreatedAt = DateTime.UtcNow
            };

            _context.Classes.Add(newClass);
            await _context.SaveChangesAsync();

            // If form teacher is assigned, add to FormTeacherClass
            if (dto.FormTeacherId.HasValue)
            {
                var formTeacherClass = new FormTeacherClass
                {
                    TeacherId = dto.FormTeacherId.Value,
                    ClassId = newClass.Id,
                    AssignedAt = DateTime.UtcNow
                };
                _context.FormTeacherClasses.Add(formTeacherClass);

                // Send notification
                var notification = new Notification
                {
                    Title = "🏫 Form Teacher Assignment",
                    Message = $"You have been assigned as Form Teacher for {newClass.Name} {newClass.Stream}",
                    Type = "FormTeacherAssignment",
                    TeacherId = dto.FormTeacherId.Value,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Class added successfully", classId = newClass.Id });
        }

        /// <summary>
        /// Update an existing class
        /// </summary>
        [HttpPut("classes/{id}")]
        [SwaggerOperation(Summary = "Update a class")]
        public async Task<IActionResult> UpdateClass(int id, [FromBody] CreateClassDTO dto)
        {
            var classEntity = await _context.Classes.FindAsync(id);
            if (classEntity == null)
                return NotFound(new { message = "Class not found" });

            classEntity.Name = dto.Name;
            classEntity.Stream = dto.Stream;
            classEntity.TeacherId = dto.TeacherId;
            classEntity.Capacity = dto.Capacity;

            // Update form teacher
            if (dto.FormTeacherId.HasValue && dto.FormTeacherId != classEntity.FormTeacherId)
            {
                classEntity.FormTeacherId = dto.FormTeacherId;

                var existing = await _context.FormTeacherClasses
                    .FirstOrDefaultAsync(ftc => ftc.ClassId == id);

                if (existing != null)
                {
                    existing.TeacherId = dto.FormTeacherId.Value;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var formTeacherClass = new FormTeacherClass
                    {
                        TeacherId = dto.FormTeacherId.Value,
                        ClassId = id,
                        AssignedAt = DateTime.UtcNow
                    };
                    _context.FormTeacherClasses.Add(formTeacherClass);
                }

                // Send notification
                var notification = new Notification
                {
                    Title = "🏫 Form Teacher Assignment",
                    Message = $"You have been assigned as Form Teacher for {classEntity.Name} {classEntity.Stream}",
                    Type = "FormTeacherAssignment",
                    TeacherId = dto.FormTeacherId.Value,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Class updated successfully" });
        }

        /// <summary>
        /// Delete a class
        /// </summary>
        [HttpDelete("classes/{id}")]
        [SwaggerOperation(Summary = "Delete a class")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var classEntity = await _context.Classes.FindAsync(id);
            if (classEntity == null)
                return NotFound(new { message = "Class not found" });

            var formTeacherClass = await _context.FormTeacherClasses
                .FirstOrDefaultAsync(ftc => ftc.ClassId == id);

            if (formTeacherClass != null)
                _context.FormTeacherClasses.Remove(formTeacherClass);

            _context.Classes.Remove(classEntity);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Class deleted successfully" });
        }

        /// <summary>
        /// Assign a form teacher to a class
        /// </summary>
        [HttpPost("assign-form-teacher")]
        [SwaggerOperation(Summary = "Assign form teacher to class")]
        public async Task<IActionResult> AssignFormTeacher([FromBody] AssignFormTeacherDTO dto)
        {
            var classEntity = await _context.Classes.FindAsync(dto.ClassId);
            if (classEntity == null)
                return NotFound(new { message = "Class not found" });

            var teacher = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.TeacherId && u.Role == "Teacher");

            if (teacher == null)
                return NotFound(new { message = "Teacher not found" });

            classEntity.FormTeacherId = dto.TeacherId;

            var existing = await _context.FormTeacherClasses
                .FirstOrDefaultAsync(ftc => ftc.ClassId == dto.ClassId);

            if (existing != null)
            {
                existing.TeacherId = dto.TeacherId;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var formTeacherClass = new FormTeacherClass
                {
                    TeacherId = dto.TeacherId,
                    ClassId = dto.ClassId,
                    AssignedAt = DateTime.UtcNow
                };
                _context.FormTeacherClasses.Add(formTeacherClass);
            }

            await _context.SaveChangesAsync();

            // Send notification to teacher
            var notification = new Notification
            {
                Title = "🏫 Form Teacher Assignment",
                Message = $"You have been assigned as Form Teacher for {classEntity.Name} {classEntity.Stream}",
                Type = "FormTeacherAssignment",
                TeacherId = dto.TeacherId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Form teacher assigned successfully" });
        }

        // ==================== STUDENT MANAGEMENT ====================

        /// <summary>
        /// Get all students
        /// </summary>
        [HttpGet("all-students")]
        [SwaggerOperation(Summary = "Get all students")]
        public async Task<IActionResult> GetAllStudents([FromQuery] int? classId)
        {
            var query = _context.Students.AsQueryable();

            if (classId.HasValue)
                query = query.Where(s => s.ClassId == classId.Value);

            var students = await query
                .Select(s => new
                {
                    s.Id,
                    s.AdmissionNumber,
                    s.FullName,
                    s.Class,
                    s.Stream,
                    s.Email,
                    s.PhoneNumber,
                    s.ClassId,
                    s.CreatedAt,
                    s.UpdatedAt,
                    HasSelections = _context.StudentSubjectSelections.Any(sss => sss.StudentId == s.Id && !sss.IsApproved)
                })
                .ToListAsync();

            return Ok(students);
        }

        /// <summary>
        /// Get students by class
        /// </summary>
        [HttpGet("students-by-class/{classId}")]
        [SwaggerOperation(Summary = "Get students by class")]
        public async Task<IActionResult> GetStudentsByClass(int classId)
        {
            var classEntity = await _context.Classes.FindAsync(classId);
            if (classEntity == null)
                return NotFound(new { message = "Class not found" });

            var students = await _context.Students
                .Where(s => s.ClassId == classId)
                .Select(s => new
                {
                    s.Id,
                    s.AdmissionNumber,
                    s.FullName,
                    s.Email,
                    s.PhoneNumber,
                    s.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                Class = new { classEntity.Name, classEntity.Stream },
                Students = students
            });
        }

        /// <summary>
        /// Update an existing student
        /// </summary>
        [HttpPut("students/{id}")]
        [SwaggerOperation(Summary = "Update a student")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDTO dto)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            if (!string.IsNullOrEmpty(dto.FullName))
                student.FullName = dto.FullName;

            if (!string.IsNullOrEmpty(dto.Class))
                student.Class = dto.Class;

            if (!string.IsNullOrEmpty(dto.Stream))
                student.Stream = dto.Stream;

            student.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Student updated successfully",
                student = new
                {
                    student.Id,
                    student.AdmissionNumber,
                    student.FullName,
                    student.Class,
                    student.Stream,
                    student.UpdatedAt
                }
            });
        }

        /// <summary>
        /// Delete a student
        /// </summary>
        [HttpDelete("students/{id}")]
        [SwaggerOperation(Summary = "Delete a student")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Student deleted successfully" });
        }

        // ==================== SUBJECTS MANAGEMENT ====================

        /// <summary>
        /// Get all subjects
        /// </summary>
        [HttpGet("subjects")]
        [SwaggerOperation(Summary = "Get all subjects")]
        public async Task<IActionResult> GetSubjects()
        {
            var subjects = await _context.Subjects
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Code,
                    s.Type,
                    s.DepartmentId,
                    DepartmentName = s.Department != null ? s.Department.Name : "Not Assigned",
                    s.CreatedAt
                })
                .ToListAsync();
            return Ok(subjects);
        }

        /// <summary>
        /// Create a new subject
        /// </summary>
        [HttpPost("subjects")]
        [SwaggerOperation(Summary = "Create a new subject")]
        public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectDTO dto)
        {
            var subject = new Subject
            {
                Name = dto.Name,
                Code = dto.Code,
                Type = dto.Type,
                DepartmentId = dto.DepartmentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Subject created successfully", subjectId = subject.Id });
        }

        /// <summary>
        /// Update a subject
        /// </summary>
        [HttpPut("subjects/{id}")]
        [SwaggerOperation(Summary = "Update a subject")]
        public async Task<IActionResult> UpdateSubject(int id, [FromBody] UpdateSubjectDTO dto)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound(new { message = "Subject not found" });

            if (!string.IsNullOrEmpty(dto.Name))
                subject.Name = dto.Name;

            if (!string.IsNullOrEmpty(dto.Code))
                subject.Code = dto.Code;

            if (!string.IsNullOrEmpty(dto.Type))
                subject.Type = dto.Type;

            subject.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subject updated successfully" });
        }

        /// <summary>
        /// Delete a subject
        /// </summary>
        [HttpDelete("subjects/{id}")]
        [SwaggerOperation(Summary = "Delete a subject")]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.ClassSubjects)
                .Include(s => s.TeacherSubjects)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
                return NotFound(new { message = "Subject not found" });

            if (subject.ClassSubjects.Any() || subject.TeacherSubjects.Any())
                return BadRequest(new { message = "Cannot delete subject with existing allocations" });

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subject deleted successfully" });
        }

        // ==================== TEACHER-SUBJECT ALLOCATION ====================

        /// <summary>
        /// Allocate a teacher to a subject
        /// </summary>
        [HttpPost("allocate-teacher")]
        [SwaggerOperation(Summary = "Allocate teacher to subject")]
        public async Task<IActionResult> AllocateTeacher([FromBody] AllocateTeacherDTO dto)
        {
            var exists = await _context.ClassSubjects
                .AnyAsync(cs => cs.ClassId == dto.ClassId && cs.SubjectId == dto.SubjectId);

            if (exists)
                return BadRequest(new { message = "Teacher already allocated to this subject in this class" });

            var allocation = new ClassSubject
            {
                ClassId = dto.ClassId,
                SubjectId = dto.SubjectId,
                TeacherId = dto.TeacherId,
                AssignedAt = DateTime.UtcNow
            };

            _context.ClassSubjects.Add(allocation);
            await _context.SaveChangesAsync();

            // Send notification to teacher
            var teacher = await _context.Users.FindAsync(dto.TeacherId);
            var subject = await _context.Subjects.FindAsync(dto.SubjectId);
            var classEntity = await _context.Classes.FindAsync(dto.ClassId);

            if (teacher != null && subject != null && classEntity != null)
            {
                var notification = new Notification
                {
                    Title = "📚 Subject Allocation",
                    Message = $"You have been assigned to teach {subject.Name} for {classEntity.Name} {classEntity.Stream}",
                    Type = "SubjectAllocation",
                    TeacherId = dto.TeacherId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Teacher allocated successfully" });
        }

        /// <summary>
        /// Get class allocations
        /// </summary>
        [HttpGet("class-allocations/{classId}")]
        [SwaggerOperation(Summary = "Get class allocations")]
        public async Task<IActionResult> GetClassAllocations(int classId)
        {
            var allocations = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .Where(cs => cs.ClassId == classId)
                .Select(cs => new
                {
                    cs.Id,
                    cs.ClassId,
                    cs.SubjectId,
                    SubjectName = cs.Subject != null ? cs.Subject.Name : "Unknown",
                    cs.TeacherId,
                    TeacherName = cs.Teacher != null ? cs.Teacher.Name : "Unknown",
                    TeacherEmail = cs.Teacher != null ? cs.Teacher.Email : null,
                    TeacherPhone = cs.Teacher != null ? cs.Teacher.PhoneNumber : null,
                    cs.AssignedAt
                })
                .ToListAsync();
            return Ok(allocations);
        }

        /// <summary>
        /// Get teacher's subjects
        /// </summary>
        [HttpGet("teacher-subjects/{teacherId}")]
        [SwaggerOperation(Summary = "Get teacher's subjects")]
        public async Task<IActionResult> GetTeacherSubjects(int teacherId)
        {
            var subjects = await _context.ClassSubjects
                .Include(cs => cs.Subject)
                .Include(cs => cs.Class)
                .Where(cs => cs.TeacherId == teacherId)
                .Select(cs => new
                {
                    cs.Id,
                    cs.SubjectId,
                    SubjectName = cs.Subject != null ? cs.Subject.Name : "Unknown",
                    cs.ClassId,
                    ClassName = cs.Class != null ? cs.Class.Name : "Unknown",
                    Stream = cs.Class != null ? cs.Class.Stream : "",
                    cs.AssignedAt
                })
                .ToListAsync();
            return Ok(subjects);
        }

        /// <summary>
        /// Remove an allocation
        /// </summary>
        [HttpDelete("allocations/{id}")]
        [SwaggerOperation(Summary = "Remove an allocation")]
        public async Task<IActionResult> RemoveAllocation(int id)
        {
            var allocation = await _context.ClassSubjects.FindAsync(id);
            if (allocation == null)
                return NotFound();

            _context.ClassSubjects.Remove(allocation);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Allocation removed successfully" });
        }

        // ==================== RESULTS APPROVAL ====================

        /// <summary>
        /// Get pending results for approval
        /// </summary>
        [HttpGet("pending-results")]
        [SwaggerOperation(Summary = "Get pending results")]
        public async Task<IActionResult> GetPendingResults()
        {
            var pendingResults = await _context.Marks
                .Include(m => m.Subject)
                .Include(m => m.Class)
                .Where(m => m.IsApproved == false && m.TotalScore.HasValue)
                .GroupBy(m => new { m.SubjectId, m.Year, m.Term, m.ClassId })
                .Select(g => new
                {
                    SubjectId = g.Key.SubjectId,
                    SubjectName = g.First().Subject != null ? g.First().Subject.Name : "",
                    ClassName = g.First().Class != null ? g.First().Class.Name : "",
                    Stream = g.First().Class != null ? g.First().Class.Stream : "",
                    Year = g.Key.Year,
                    Term = g.Key.Term,
                    StudentCount = g.Count(),
                    EnteredByTeacher = g.First().EnteredByTeacher != null ? g.First().EnteredByTeacher.Name : ""
                })
                .ToListAsync();

            return Ok(pendingResults);
        }

        /// <summary>
        /// Approve results
        /// </summary>
        [HttpPost("approve-results")]
        [SwaggerOperation(Summary = "Approve results")]
        public async Task<IActionResult> ApproveResults([FromBody] ApproveResultsDTO dto)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var marks = await _context.Marks
                .Include(m => m.Student)
                .Where(m => m.SubjectId == dto.SubjectId && m.Year == dto.Year && m.Term == dto.Term)
                .ToListAsync();

            if (!marks.Any())
                return BadRequest(new { message = "No results found for this subject/term/year" });

            foreach (var mark in marks)
            {
                mark.IsApproved = true;
                mark.ApprovedAt = DateTime.UtcNow;
                mark.ApprovedByAdminId = adminId;
            }

            await _context.SaveChangesAsync();

            var subject = await _context.Subjects.FindAsync(dto.SubjectId);
            var subjectName = subject?.Name ?? "Unknown Subject";

            var studentIds = marks.Select(m => m.StudentId).Distinct().ToList();

            // Send notifications to students
            foreach (var studentId in studentIds)
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student != null)
                {
                    var notification = new Notification
                    {
                        Title = "📢 Exam Results Published",
                        Message = $"Your results for {subjectName} ({dto.Term} {dto.Year}) have been approved and published.",
                        Type = "ExamResults",
                        StudentId = studentId,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    _context.Notifications.Add(notification);
                }
            }

            // Send notification to teachers
            var teacherIds = marks.Select(m => m.EnteredByTeacherId).Where(t => t.HasValue).Select(t => t.Value).Distinct().ToList();
            foreach (var teacherId in teacherIds)
            {
                var notification = new Notification
                {
                    Title = "✅ Results Approved",
                    Message = $"The results for {subjectName} ({dto.Term} {dto.Year}) have been approved by the Headteacher.",
                    Type = "Success",
                    TeacherId = teacherId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Results for {subjectName} ({dto.Term} {dto.Year}) have been approved and published.",
                studentCount = studentIds.Count,
                teacherCount = teacherIds.Count
            });
        }

        /// <summary>
        /// Get approved results
        /// </summary>
        [HttpGet("approved-results")]
        [SwaggerOperation(Summary = "Get approved results")]
        public async Task<IActionResult> GetApprovedResults()
        {
            var approvedResults = await _context.Marks
                .Include(m => m.Subject)
                .Include(m => m.Class)
                .Where(m => m.IsApproved == true)
                .GroupBy(m => new { m.SubjectId, m.Year, m.Term, m.ClassId })
                .Select(g => new
                {
                    SubjectId = g.Key.SubjectId,
                    SubjectName = g.First().Subject != null ? g.First().Subject.Name : "",
                    ClassName = g.First().Class != null ? g.First().Class.Name : "",
                    Stream = g.First().Class != null ? g.First().Class.Stream : "",
                    Year = g.Key.Year,
                    Term = g.Key.Term,
                    StudentCount = g.Count(),
                    ApprovedAt = g.Max(m => m.ApprovedAt)
                })
                .ToListAsync();

            return Ok(approvedResults);
        }

        // ==================== SYSTEM STATISTICS ====================

        /// <summary>
        /// Get system statistics
        /// </summary>
        [HttpGet("statistics")]
        [SwaggerOperation(Summary = "Get system statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var stats = new
            {
                TotalStudents = await _context.Students.CountAsync(),
                TotalTeachers = await _context.Users.CountAsync(u => u.Role == "Teacher" && u.IsActive),
                TotalDepartments = await _context.Departments.CountAsync(),
                TotalSubjects = await _context.Subjects.CountAsync(),
                TotalClasses = await _context.Classes.CountAsync(),
                TotalMarks = await _context.Marks.CountAsync(m => m.TotalScore.HasValue),
                PendingApprovals = await _context.Marks.CountAsync(m => !m.IsApproved && m.TotalScore.HasValue),
                TotalNotifications = await _context.Notifications.CountAsync()
            };

            return Ok(stats);
        }
    }

    // ==================== DTOs ====================

    public class CreateDepartmentDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateDepartmentDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public class CreateSubjectDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Type { get; set; }
        public int? DepartmentId { get; set; }
    }

    public class UpdateSubjectDTO
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Type { get; set; }
    }

    public class AssignFormTeacherDTO
    {
        public int ClassId { get; set; }
        public int TeacherId { get; set; }
    }

    public class AssignHeadOfDepartmentDTO
    {
        public int DepartmentId { get; set; }
        public int TeacherId { get; set; }
    }

    public class UpdateTeacherDTO
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmployeeId { get; set; }
        public string? Qualification { get; set; }
        public int? DepartmentId { get; set; }
    }

    public class ApproveResultsDTO
    {
        public int SubjectId { get; set; }
        public int Year { get; set; }
        public string Term { get; set; } = string.Empty;
    }
}