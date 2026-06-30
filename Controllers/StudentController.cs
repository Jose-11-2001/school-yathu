using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_Yathu.Data;
using School_Yathu.DTOs;
using School_Yathu.Models;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace School_Yathu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [SwaggerTag("Student Management - Complete student operations")]
    public class StudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StudentController> _logger;

        public StudentController(ApplicationDbContext context, ILogger<StudentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Student Information Endpoints

        /// <summary>
        /// Get all students (Admin/Teacher only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        [SwaggerOperation(Summary = "Get all students", Description = "Retrieves a list of all students")]
        public async Task<IActionResult> GetAllStudents()
        {
            try
            {
                var students = await _context.Students
                    .Select(s => new
                    {
                        s.Id,
                        s.AdmissionNumber,
                        s.FullName,
                        s.Class,
                        s.Stream,
                        s.Email,
                        s.CreatedAt
                    })
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all students");
                return StatusCode(500, new { message = "An error occurred while retrieving students" });
            }
        }

        /// <summary>
        /// Get student by email
        /// </summary>
        [HttpGet("student-by-email")]
        [SwaggerOperation(Summary = "Get student by email", Description = "Retrieves student information by email")]
        public async Task<IActionResult> GetStudentByEmail([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    return BadRequest(new { message = "Email is required" });

                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Email != null && s.Email.ToLower() == email.ToLower());

                if (student == null)
                {
                    // Try to find by admission number (if email contains admission number)
                    var admissionNumber = email.Split('@')[0];
                    student = await _context.Students
                        .FirstOrDefaultAsync(s => s.AdmissionNumber != null && s.AdmissionNumber.ToLower() == admissionNumber.ToLower());
                }

                if (student == null)
                    return NotFound(new { message = "Student not found" });

                return Ok(new StudentDTO
                {
                    Id = student.Id,
                    AdmissionNumber = student.AdmissionNumber,
                    FullName = student.FullName,
                    Class = student.Class,
                    Stream = student.Stream,
                    Email = student.Email,
                    CreatedAt = student.CreatedAt,
                    UpdatedAt = student.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student by email: {Email}", email);
                return StatusCode(500, new { message = "An error occurred while retrieving student data" });
            }
        }

        /// <summary>
        /// Get student by name
        /// </summary>
        [HttpGet("student-by-name")]
        [SwaggerOperation(Summary = "Get student by name", Description = "Retrieves student information by name")]
        public async Task<IActionResult> GetStudentByName([FromQuery] string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                    return BadRequest(new { message = "Name is required" });

                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.FullName != null && s.FullName.ToLower() == name.ToLower());

                if (student == null)
                    return NotFound(new { message = "Student not found" });

                return Ok(new StudentDTO
                {
                    Id = student.Id,
                    AdmissionNumber = student.AdmissionNumber,
                    FullName = student.FullName,
                    Class = student.Class,
                    Stream = student.Stream,
                    Email = student.Email,
                    CreatedAt = student.CreatedAt,
                    UpdatedAt = student.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student by name: {Name}", name);
                return StatusCode(500, new { message = "An error occurred while retrieving student data" });
            }
        }

        /// <summary>
        /// Get student by admission number
        /// </summary>
        [HttpGet("by-admission/{admissionNumber}")]
        [SwaggerOperation(Summary = "Get student by admission number", Description = "Retrieves student information by admission number")]
        public async Task<IActionResult> GetStudentByAdmission(string admissionNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(admissionNumber))
                    return BadRequest(new { message = "Admission number is required" });

                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.AdmissionNumber == admissionNumber);

                if (student == null)
                    return NotFound(new { message = "Student not found" });

                return Ok(new StudentDTO
                {
                    Id = student.Id,
                    AdmissionNumber = student.AdmissionNumber,
                    FullName = student.FullName,
                    Class = student.Class,
                    Stream = student.Stream,
                    Email = student.Email,
                    CreatedAt = student.CreatedAt,
                    UpdatedAt = student.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student by admission: {AdmissionNumber}", admissionNumber);
                return StatusCode(500, new { message = "An error occurred while retrieving student data" });
            }
        }

        #endregion

        #region Student Creation and Management Endpoints

        /// <summary>
        /// Create a new student
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")]
        [SwaggerOperation(Summary = "Create a new student", Description = "Creates a new student record")]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDTO dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Invalid student data" });

                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.AdmissionNumber == dto.AdmissionNumber);

                if (existingStudent != null)
                    return BadRequest(new { message = $"Student with admission number '{dto.AdmissionNumber}' already exists" });

                var student = new Student
                {
                    AdmissionNumber = dto.AdmissionNumber,
                    FullName = dto.FullName,
                    Class = dto.Class,
                    Stream = dto.Stream,
                    Email = dto.Email,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Student created successfully",
                    student = new
                    {
                        student.Id,
                        student.AdmissionNumber,
                        student.FullName,
                        student.Class,
                        student.Stream,
                        student.Email
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student");
                return StatusCode(500, new { message = "An error occurred while creating student" });
            }
        }

        /// <summary>
        /// Update an existing student
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        [SwaggerOperation(Summary = "Update student", Description = "Updates an existing student's information")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDTO dto)
        {
            try
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

                if (!string.IsNullOrEmpty(dto.Email))
                    student.Email = dto.Email;

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
                        student.Email
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student");
                return StatusCode(500, new { message = "An error occurred while updating student" });
            }
        }

        /// <summary>
        /// Delete a student
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Delete student", Description = "Permanently deletes a student")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Student deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student");
                return StatusCode(500, new { message = "An error occurred while deleting student" });
            }
        }

        #endregion

        #region Subject Management Endpoints

        /// <summary>
        /// Get student subjects with teacher information
        /// </summary>
        [HttpGet("student-subjects")]
        [SwaggerOperation(Summary = "Get student subjects", Description = "Retrieves all subjects for a student with teacher details")]
        public async Task<IActionResult> GetStudentSubjects([FromQuery] string className, [FromQuery] string stream)
        {
            try
            {
                if (string.IsNullOrEmpty(className))
                    return BadRequest(new { message = "Class name is required" });

                // Find the class
                var classEntity = await _context.Classes
                    .FirstOrDefaultAsync(c => c.Name == className && (string.IsNullOrEmpty(stream) || c.Stream == stream));

                if (classEntity == null)
                    return NotFound(new { message = "Class not found" });

                // Get subject allocations for this class
                var allocations = await _context.ClassSubjects
                    .Include(cs => cs.Subject)
                    .Include(cs => cs.Teacher)
                    .Where(cs => cs.ClassId == classEntity.Id)
                    .Select(cs => new StudentSubjectDTO
                    {
                        Id = cs.Id,
                        Name = cs.Subject != null ? cs.Subject.Name : "Unknown",
                        Code = cs.Subject != null ? cs.Subject.Code : "N/A",
                        Type = cs.Subject != null ? cs.Subject.Type : "Core",
                        TeacherId = cs.TeacherId,
                        TeacherName = cs.Teacher != null ? cs.Teacher.Name : "Not assigned",
                        TeacherEmail = cs.Teacher != null ? cs.Teacher.Email : null,
                        ClassName = classEntity.Name,
                        Stream = classEntity.Stream,
                        AssignedAt = cs.AssignedAt
                    })
                    .ToListAsync();

                return Ok(new { subjects = allocations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student subjects for class: {Class}, stream: {Stream}", className, stream);
                return StatusCode(500, new { message = "An error occurred while retrieving subjects" });
            }
        }

        /// <summary>
        /// Get subjects for the currently logged-in student
        /// </summary>
        [HttpGet("my-subjects")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "Get my subjects", Description = "Retrieves subjects allocated to the logged-in student")]
        public async Task<IActionResult> GetMySubjects()
        {
            try
            {
                var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var currentYear = DateTime.Now.Year;

                // Get student's class and stream
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                // Get subjects allocated to this student
                var subjects = await _context.StudentSubjects
                    .Include(ss => ss.Subject)
                    .Include(ss => ss.Teacher)
                    .Where(ss => ss.StudentId == studentId && ss.IsActive && ss.AcademicYear == currentYear)
                    .Select(ss => new
                    {
                        ss.Id,
                        ss.SubjectId,
                        SubjectName = ss.Subject != null ? ss.Subject.Name : "",
                        SubjectCode = ss.Subject != null ? ss.Subject.Code : "",
                        ss.TeacherId,
                        TeacherName = ss.Teacher != null ? ss.Teacher.Name : "",
                        TeacherEmail = ss.Teacher != null ? ss.Teacher.Email : "",
                        ss.AcademicYear,
                        ss.Term,
                        HasMarks = _context.Marks.Any(m => m.StudentId == studentId &&
                                                           m.SubjectId == ss.SubjectId &&
                                                           m.Year == ss.AcademicYear)
                    })
                    .ToListAsync();

                // If no subjects assigned directly, get from class
                if (!subjects.Any())
                {
                    var classSubjects = await _context.ClassSubjects
                        .Include(cs => cs.Subject)
                        .Include(cs => cs.Teacher)
                        .Where(cs => cs.Class != null && cs.Class.Name == student.Class)
                        .Select(cs => new
                        {
                            Id = 0,
                            SubjectId = cs.Subject != null ? cs.Subject.Id : 0,
                            SubjectName = cs.Subject != null ? cs.Subject.Name : "",
                            SubjectCode = cs.Subject != null ? cs.Subject.Code : "",
                            TeacherId = cs.TeacherId,
                            TeacherName = cs.Teacher != null ? cs.Teacher.Name : "",
                            TeacherEmail = cs.Teacher != null ? cs.Teacher.Email : "",
                            AcademicYear = currentYear,
                            Term = "Term 1",
                            HasMarks = false
                        })
                        .ToListAsync();

                    return Ok(classSubjects);
                }

                return Ok(subjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my subjects");
                return StatusCode(500, new { message = "An error occurred while retrieving your subjects" });
            }
        }

        #endregion

        #region Marks and Results Endpoints

        /// <summary>
        /// Get student results/marks
        /// </summary>
        [HttpGet("student-results")]
        [SwaggerOperation(Summary = "Get student results", Description = "Retrieves student marks for a specific term and year")]
        public async Task<IActionResult> GetStudentResults(
            [FromQuery] string admissionNumber,
            [FromQuery] int year,
            [FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrEmpty(admissionNumber))
                    return BadRequest(new { message = "Admission number is required" });

                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.AdmissionNumber == admissionNumber);

                if (student == null)
                    return NotFound(new { message = "Student not found" });

                var marks = await GetStudentMarksData(student.Id, year, term);
                var ranking = await CalculateStudentRanking(student.Id, student.Class, student.Stream, year, term);
                var gradeInfo = CalculateGradeAndRemarks(marks.Average);

                return Ok(new StudentResultDTO
                {
                    StudentId = student.Id,
                    AdmissionNumber = student.AdmissionNumber,
                    FullName = student.FullName,
                    Class = student.Class,
                    Stream = student.Stream,
                    Year = year,
                    Term = term,
                    Marks = marks.Marks,
                    Ranking = new RankingDTO
                    {
                        TotalMarks = marks.Total,
                        Average = marks.Average,
                        Position = ranking.Position,
                        TotalStudents = ranking.TotalStudents,
                        Grade = gradeInfo.Grade,
                        Remarks = gradeInfo.Remarks
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student results for admission: {AdmissionNumber}", admissionNumber);
                return StatusCode(500, new { message = "An error occurred while retrieving results" });
            }
        }

        /// <summary>
        /// Get marks for a specific subject for the logged-in student
        /// </summary>
        [HttpGet("my-marks/{subjectId}")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "Get my marks", Description = "Retrieves marks for a specific subject for the logged-in student")]
        public async Task<IActionResult> GetMyMarks(int subjectId, [FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var marks = await _context.Marks
                    .Include(m => m.Subject)
                    .FirstOrDefaultAsync(m => m.StudentId == studentId &&
                                              m.SubjectId == subjectId &&
                                              m.Year == year &&
                                              m.Term == term);

                if (marks == null)
                    return NotFound(new { message = "No marks found for this subject" });

                return Ok(new
                {
                    marks.ContinuousTest1,
                    marks.ContinuousTest2,
                    marks.EndTermExam,
                    marks.TotalScore,
                    marks.Grade,
                    marks.Remark,
                    SubjectName = marks.Subject != null ? marks.Subject.Name : "",
                    SubjectCode = marks.Subject != null ? marks.Subject.Code : ""
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my marks for subject: {SubjectId}", subjectId);
                return StatusCode(500, new { message = "An error occurred while retrieving your marks" });
            }
        }

        /// <summary>
        /// Get all marks for a student by year and term
        /// </summary>
        [HttpGet("marks/{studentId}")]
        [Authorize(Roles = "Admin,Teacher")]
        [SwaggerOperation(Summary = "Get student marks", Description = "Retrieves all marks for a student by year and term")]
        public async Task<IActionResult> GetStudentMarks(int studentId, [FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                var marks = await _context.Marks
                    .Include(m => m.Subject)
                    .Where(m => m.StudentId == studentId && m.Year == year && m.Term == term)
                    .Select(m => new
                    {
                        m.Id,
                        SubjectName = m.Subject != null ? m.Subject.Name : "",
                        SubjectCode = m.Subject != null ? m.Subject.Code : "",
                        m.ContinuousTest1,
                        m.ContinuousTest2,
                        m.EndTermExam,
                        m.TotalScore,
                        m.Grade,
                        m.Remark,
                        m.Year,
                        m.Term
                    })
                    .ToListAsync();

                if (!marks.Any())
                    return NotFound(new { message = "No marks found for this student" });

                var totalScore = marks.Sum(m => m.TotalScore ?? 0);
                var averageScore = marks.Average(m => m.TotalScore ?? 0);
                var gradeInfo = CalculateGradeAndRemarks(averageScore);

                return Ok(new
                {
                    Subjects = marks,
                    Summary = new
                    {
                        TotalSubjects = marks.Count,
                        TotalScore = totalScore,
                        AverageScore = Math.Round(averageScore, 2),
                        OverallGrade = gradeInfo.Grade,
                        OverallRemark = gradeInfo.Remarks
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting marks for student: {StudentId}", studentId);
                return StatusCode(500, new { message = "An error occurred while retrieving marks" });
            }
        }

        /// <summary>
        /// Get student rank in class
        /// </summary>
        [HttpGet("rank/{studentId}")]
        [SwaggerOperation(Summary = "Get student rank", Description = "Retrieves the rank of a student in their class")]
        public async Task<IActionResult> GetStudentRank(int studentId, [FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                var ranking = await CalculateStudentRanking(studentId, student.Class, student.Stream, year, term);

                return Ok(new
                {
                    student.AdmissionNumber,
                    student.FullName,
                    student.Class,
                    student.Stream,
                    Position = ranking.Position,
                    TotalStudents = ranking.TotalStudents,
                    Grade = ranking.Grade,
                    Remarks = ranking.Remarks,
                    TopStudents = ranking.TopStudents
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student ranking");
                return StatusCode(500, new { message = "An error occurred while retrieving ranking" });
            }
        }

        /// <summary>
        /// Get class rankings for a specific class (UNIFIED VERSION - KEEP THIS ONE)
        /// </summary>
        [HttpGet("class-ranking")]
        [Authorize(Roles = "Admin,Teacher")]
        [SwaggerOperation(Summary = "Get class rankings", Description = "Retrieves rankings for all students in a class")]
        public async Task<IActionResult> GetClassRankings([FromQuery] string className, [FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrEmpty(className))
                    return BadRequest(new { message = "Class name is required" });

                // Get teacher's assigned class if they are a Teacher
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var students = await _context.Students
                    .Where(s => s.Class == className)
                    .ToListAsync();

                if (!students.Any())
                    return Ok(new { message = "No students found in this class", rankings = new List<object>() });

                // If Teacher, verify they are assigned to this class
                if (userRole == "Teacher")
                {
                    var isAssigned = await _context.Classes
                        .AnyAsync(c => c.Name == className && c.TeacherId == userId);
                    
                    if (!isAssigned)
                        return Unauthorized(new { message = "You are not assigned to this class" });
                }

                var classRankings = new List<object>();

                foreach (var student in students)
                {
                    var marks = await _context.Marks
                        .Where(m => m.StudentId == student.Id && m.Year == year && m.Term == term)
                        .ToListAsync();

                    if (marks.Any())
                    {
                        var totalScore = marks.Sum(m => m.TotalScore ?? 0);
                        var average = marks.Average(m => m.TotalScore ?? 0);
                        var gradeInfo = CalculateGradeAndRemarks(average);

                        classRankings.Add(new
                        {
                            student.Id,
                            student.AdmissionNumber,
                            student.FullName,
                            TotalMarks = totalScore,
                            Average = Math.Round(average, 2),
                            Grade = gradeInfo.Grade,
                            Remark = gradeInfo.Remarks,
                            SubjectScores = marks.Select(m => new
                            {
                                SubjectName = m.Subject != null ? m.Subject.Name : "Unknown",
                                Score = m.TotalScore,
                                Grade = m.Grade
                            })
                        });
                    }
                }

                var rankedStudents = classRankings
                    .OrderByDescending(s => ((dynamic)s).TotalMarks)
                    .Select((s, index) => new
                    {
                        Position = index + 1,
                        AdmissionNumber = ((dynamic)s).AdmissionNumber,
                        FullName = ((dynamic)s).FullName,
                        TotalMarks = ((dynamic)s).TotalMarks,
                        Average = ((dynamic)s).Average,
                        Grade = ((dynamic)s).Grade,
                        Remark = ((dynamic)s).Remark,
                        SubjectScores = ((dynamic)s).SubjectScores
                    })
                    .ToList();

                return Ok(new
                {
                    Class = className,
                    Year = year,
                    Term = term,
                    TotalStudents = students.Count,
                    Rankings = rankedStudents,
                    GeneratedBy = User.Identity?.Name,
                    GeneratedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting class rankings");
                return StatusCode(500, new { message = "An error occurred while retrieving class rankings" });
            }
        }

        #endregion

        #region Student Dashboard Endpoint

        /// <summary>
        /// Get student dashboard summary
        /// </summary>
        [HttpGet("dashboard")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "Get student dashboard", Description = "Retrieves a summary of the student dashboard")]
        public async Task<IActionResult> GetStudentDashboard()
        {
            try
            {
                var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var currentYear = DateTime.Now.Year;

                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                // Get subjects
                var subjects = await _context.StudentSubjects
                    .Include(ss => ss.Subject)
                    .Include(ss => ss.Teacher)
                    .Where(ss => ss.StudentId == studentId && ss.IsActive && ss.AcademicYear == currentYear)
                    .ToListAsync();

                // Get marks
                var marks = await _context.Marks
                    .Include(m => m.Subject)
                    .Where(m => m.StudentId == studentId && m.Year == currentYear)
                    .ToListAsync();

                // Calculate performance
                var totalScore = marks.Sum(m => m.TotalScore ?? 0);
                var averageScore = marks.Any() ? marks.Average(m => m.TotalScore ?? 0) : 0;
                var gradeInfo = CalculateGradeAndRemarks(averageScore);

                // Get notifications
                var notifications = await _context.Notifications
                    .Where(n => n.StudentId == studentId && !n.IsRead)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Get upcoming exams
                var upcomingExams = await _context.Exams
                    .Include(e => e.Subject)
                    .Where(e => e.Class != null && e.Class.Name == student.Class && e.ExamDate > DateTime.UtcNow)
                    .OrderBy(e => e.ExamDate)
                    .Take(3)
                    .ToListAsync();

                return Ok(new
                {
                    Student = new
                    {
                        student.Id,
                        student.AdmissionNumber,
                        student.FullName,
                        student.Class,
                        student.Stream
                    },
                    Subjects = subjects.Select(s => new
                    {
                        s.SubjectId,
                        SubjectName = s.Subject != null ? s.Subject.Name : "",
                        TeacherName = s.Teacher != null ? s.Teacher.Name : "",
                        HasMarks = marks.Any(m => m.SubjectId == s.SubjectId)
                    }),
                    Performance = new
                    {
                        TotalSubjects = subjects.Count,
                        SubjectsWithMarks = marks.Select(m => m.SubjectId).Distinct().Count(),
                        TotalScore = totalScore,
                        AverageScore = Math.Round(averageScore, 2),
                        Grade = gradeInfo.Grade,
                        Remark = gradeInfo.Remarks
                    },
                    RecentNotifications = notifications.Select(n => new
                    {
                        n.Id,
                        n.Title,
                        n.Message,
                        n.CreatedAt
                    }),
                    UnreadNotificationCount = notifications.Count,
                    UpcomingExams = upcomingExams.Select(e => new
                    {
                        e.Id,
                        e.Title,
                        e.ExamDate,
                        SubjectName = e.Subject != null ? e.Subject.Name : "N/A"
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student dashboard");
                return StatusCode(500, new { message = "An error occurred while retrieving dashboard data" });
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Get student marks data for a specific term and year
        /// </summary>
        private async Task<(List<StudentMarkDTO> Marks, double Total, double Average)> GetStudentMarksData(int studentId, int year, string term)
        {
            var marks = await _context.Marks
                .Include(m => m.Subject)
                .Where(m => m.StudentId == studentId && m.Year == year && m.Term == term)
                .Select(m => new StudentMarkDTO
                {
                    SubjectId = m.SubjectId,
                    SubjectName = m.Subject != null ? m.Subject.Name : "Unknown",
                    ContinuousTest1 = m.ContinuousTest1,
                    ContinuousTest2 = m.ContinuousTest2,
                    EndTermExam = m.EndTermExam,
                    TotalScore = m.TotalScore,
                    OverallPercentage = m.TotalScore,
                    Year = m.Year,
                    Term = m.Term,
                    Test1 = m.ContinuousTest1,
                    Test2 = m.ContinuousTest2,
                    EndTerm = m.EndTermExam
                })
                .ToListAsync();

            var total = marks.Sum(m => m.OverallPercentage ?? 0);
            var average = marks.Any() ? total / marks.Count : 0;

            return (marks, total, average);
        }

        /// <summary>
        /// Calculate student ranking within their class
        /// </summary>
        private async Task<(int Position, int TotalStudents, string Grade, string Remarks, List<object> TopStudents)> CalculateStudentRanking(
            int studentId, string className, string stream, int year, string term)
        {
            // Handle null values
            if (string.IsNullOrEmpty(className))
                className = string.Empty;
            if (string.IsNullOrEmpty(stream))
                stream = string.Empty;

            var classStudents = await _context.Students
                .Where(s => s.Class != null && s.Class == className && s.Stream != null && s.Stream == stream)
                .Select(s => s.Id)
                .ToListAsync();

            var allMarks = await _context.Marks
                .Where(m => classStudents.Contains(m.StudentId) && m.Year == year && m.Term == term)
                .GroupBy(m => m.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    Average = g.Average(m => m.TotalScore ?? 0)
                })
                .OrderByDescending(x => x.Average)
                .ToListAsync();

            var position = allMarks.FindIndex(x => x.StudentId == studentId) + 1;
            var studentData = allMarks.FirstOrDefault(x => x.StudentId == studentId);
            var gradeInfo = CalculateGradeAndRemarks(studentData?.Average ?? 0);

            var topStudents = allMarks.Take(10).Select((s, index) => new
            {
                Position = index + 1,
                StudentId = s.StudentId,
                Average = Math.Round(s.Average, 2)
            }).ToList<object>();

            return (
                Position: position > 0 ? position : allMarks.Count + 1,
                TotalStudents: allMarks.Count,
                Grade: gradeInfo.Grade,
                Remarks: gradeInfo.Remarks,
                TopStudents: topStudents
            );
        }

        /// <summary>
        /// Calculate grade and remarks based on score
        /// </summary>
        private (string Grade, string Remarks) CalculateGradeAndRemarks(double score)
        {
            if (score >= 90) return ("A+", "Excellent performance! Outstanding work!");
            if (score >= 80) return ("A", "Excellent performance!");
            if (score >= 75) return ("A-", "Very good performance!");
            if (score >= 70) return ("B+", "Good performance!");
            if (score >= 65) return ("B", "Good performance! Keep it up!");
            if (score >= 60) return ("B-", "Satisfactory performance!");
            if (score >= 55) return ("C+", "Average performance. Can improve!");
            if (score >= 50) return ("C", "Average performance. Needs improvement!");
            if (score >= 45) return ("C-", "Below average. Requires more effort!");
            if (score >= 40) return ("D", "Poor performance! Need serious improvement!");
            return ("E", "Failed. Please work much harder!");
        }

        /// <summary>
        /// Get rankings for the teacher's assigned class
        /// </summary>
        [HttpGet("teacher-class-rankings")]
        [Authorize(Roles = "Teacher")]
        [SwaggerOperation(Summary = "Get teacher's class rankings", Description = "Retrieves rankings for the teacher's assigned class")]
        public async Task<IActionResult> GetTeacherClassRankings([FromQuery] int year, [FromQuery] string term)
        {
            try
            {
                var teacherId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Get the class assigned to this teacher
                var teacherClass = await _context.Classes
                    .FirstOrDefaultAsync(c => c.TeacherId == teacherId);

                if (teacherClass == null)
                    return NotFound(new { message = "You are not assigned to any class" });

                // Get students in this class
                var students = await _context.Students
                    .Where(s => s.Class == teacherClass.Name && s.Stream == teacherClass.Stream)
                    .ToListAsync();

                if (!students.Any())
                    return Ok(new { message = "No students found in your class", rankings = new List<object>() });

                var classRankings = new List<object>();

                foreach (var student in students)
                {
                    var marks = await _context.Marks
                        .Include(m => m.Subject)
                        .Where(m => m.StudentId == student.Id && m.Year == year && m.Term == term)
                        .ToListAsync();

                    if (marks.Any())
                    {
                        var totalScore = marks.Sum(m => m.TotalScore ?? 0);
                        var average = marks.Average(m => m.TotalScore ?? 0);
                        var gradeInfo = CalculateGradeAndRemarks(average);

                        classRankings.Add(new
                        {
                            student.Id,
                            student.AdmissionNumber,
                            student.FullName,
                            TotalMarks = totalScore,
                            Average = Math.Round(average, 2),
                            Grade = gradeInfo.Grade,
                            Remark = gradeInfo.Remarks,
                            SubjectScores = marks.Select(m => new
                            {
                                SubjectName = m.Subject != null ? m.Subject.Name : "Unknown",
                                Score = m.TotalScore,
                                Grade = m.Grade
                            })
                        });
                    }
                }

                var rankedStudents = classRankings
                    .OrderByDescending(s => ((dynamic)s).TotalMarks)
                    .Select((s, index) => new
                    {
                        Position = index + 1,
                        AdmissionNumber = ((dynamic)s).AdmissionNumber,
                        FullName = ((dynamic)s).FullName,
                        TotalMarks = ((dynamic)s).TotalMarks,
                        Average = ((dynamic)s).Average,
                        Grade = ((dynamic)s).Grade,
                        Remark = ((dynamic)s).Remark,
                        SubjectScores = ((dynamic)s).SubjectScores
                    })
                    .ToList();

                return Ok(new
                {
                    Class = teacherClass.Name,
                    Stream = teacherClass.Stream,
                    Year = year,
                    Term = term,
                    TotalStudents = students.Count,
                    Rankings = rankedStudents,
                    TeacherName = User.Identity?.Name,
                    GeneratedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher class rankings");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get available subjects for student selection
        /// </summary>
        [HttpGet("available-subjects")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "Get available subjects for selection")]
        public async Task<IActionResult> GetAvailableSubjects()
        {
            try
            {
                var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                var classSubjects = await _context.ClassSubjects
                    .Include(cs => cs.Subject)
                    .Where(cs => cs.ClassId == student.ClassId && cs.IsActive)
                    .Select(cs => cs.Subject)
                    .ToListAsync();

                var selectedSubjectIds = await _context.StudentSubjectSelections
                    .Where(sss => sss.StudentId == studentId && !sss.IsApproved)
                    .Select(sss => sss.SubjectId)
                    .ToListAsync();

                var approvedSubjectIds = await _context.StudentSubjects
                    .Where(ss => ss.StudentId == studentId && ss.IsActive)
                    .Select(ss => ss.SubjectId)
                    .ToListAsync();

                return Ok(new
                {
                    AvailableSubjects = classSubjects
                        .Where(s => !approvedSubjectIds.Contains(s.Id))
                        .Select(s => new
                        {
                            s.Id,
                            s.Name,
                            s.Code,
                            s.Type,
                            IsSelected = selectedSubjectIds.Contains(s.Id)
                        }),
                    SelectedSubjects = classSubjects
                        .Where(s => selectedSubjectIds.Contains(s.Id))
                        .Select(s => new
                        {
                            s.Id,
                            s.Name,
                            s.Code,
                            s.Type
                        }),
                    ApprovedSubjects = classSubjects
                        .Where(s => approvedSubjectIds.Contains(s.Id))
                        .Select(s => new
                        {
                            s.Id,
                            s.Name,
                            s.Code,
                            s.Type
                        })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available subjects");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Select a subject
        /// </summary>
        [HttpPost("select-subject")]
        [Authorize(Roles = "Student")]
        [SwaggerOperation(Summary = "Select a subject for approval")]
        public async Task<IActionResult> SelectSubject([FromBody] SelectSubjectDTO dto)
        {
            try
            {
                var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var currentYear = DateTime.Now.Year;

                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return NotFound(new { message = "Student not found" });

                var exists = await _context.StudentSubjectSelections
                    .AnyAsync(sss => sss.StudentId == studentId && sss.SubjectId == dto.SubjectId);

                if (exists)
                    return BadRequest(new { message = "Subject already selected" });

                var selection = new StudentSubjectSelection
                {
                    StudentId = studentId,
                    SubjectId = dto.SubjectId,
                    AcademicYear = currentYear,
                    Term = dto.Term ?? "Term 1",
                    CreatedAt = DateTime.UtcNow,
                    IsApproved = false
                };

                _context.StudentSubjectSelections.Add(selection);
                await _context.SaveChangesAsync();

                // Send notification to form teacher
                var classEntity = await _context.Classes.FindAsync(student.ClassId);
                if (classEntity != null)
                {
                    var formTeachers = await _context.FormTeacherClasses
                        .Include(ftc => ftc.Teacher)
                        .Where(ftc => ftc.ClassId == classEntity.Id)
                        .Select(ftc => ftc.TeacherId)
                        .ToListAsync();

                    foreach (var teacherId in formTeachers)
                    {
                        var notification = new Notification
                        {
                            Title = "📝 Subject Selection Pending",
                            Message = $"Student {student.FullName} has selected a subject for approval.",
                            Type = "SubjectSelection",
                            TeacherId = teacherId,
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        };
                        _context.Notifications.Add(notification);
                    }
                }
                await _context.SaveChangesAsync();

                return Ok(new { message = "Subject selected successfully. Waiting for form teacher approval." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting subject");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        #endregion
    }

    public class SelectSubjectDTO
    {
        public int SubjectId { get; set; }
        public string? Term { get; set; }
    }
}