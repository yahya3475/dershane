using System;
using System.Collections.Generic;
using System.Linq;
using dershane.Data;
using dershane.Filters;
using dershane.Models;
using dershane.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dershane.Controllers
{
    public class PrincipalController : Controller
    {
        private readonly AppDbContext _context;

        public PrincipalController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult AddUser()
        {
            if (HttpContext.Session.GetString("role") != "principal")
                return Unauthorized();

            var classList = _context.Classes.Select(c => c.UClass).Distinct().ToList();

            ViewBag.Classes = classList;
            return View();
        }

        [HttpPost]
        [RoleAuthorize("principal")]
        public async Task<IActionResult> AddUser(AddUserVM model)
        {
            // ViewBag'i her durumda set et
            var classList = _context.Classes.Select(c => c.UClass).Distinct().ToList();
            ViewBag.Classes = classList;

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid:");
                return View(model);
            }

            // Dershane ID'yi otomatik üret
            string dershaneId;
            do
            {
                dershaneId = GenerateDershaneId();
            } while (await _context.users.AnyAsync(u => u.dershaneid == dershaneId));

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Kullanıcıyı oluştur
                var user = new User
                {
                    firstname = model.FirstName,
                    lastname = model.LastName,
                    dershaneid = dershaneId, // Otomatik üretilen ID
                    password = BCrypt.Net.BCrypt.HashPassword(model.Password), // BCrypt ile şifrele
                    role = model.Role.ToLower(),
                    firstlogin = true, // İlk giriş için true yap
                    uclass = model.UClass,
                };

                _context.users.Add(user);
                await _context.SaveChangesAsync();

                // Kullanıcı bilgilerini ekle
                var userInfo = new UserInformation
                {
                    dershaneid = dershaneId, // Otomatik üretilen ID
                    email = model.Email,
                    phone_number = model.PhoneNumber,
                    parent = model.Parent,
                    parent_phone_number = model.ParentPhoneNumber,
                    address = model.Address,
                };

                _context.user_informations.Add(userInfo);
                await _context.SaveChangesAsync();

                if (model.Role.ToLower() == "student" && !string.IsNullOrEmpty(model.UClass))
                {
                    var classInfo = new UClass1
                    {
                        Student = dershaneId, // Otomatik üretilen ID
                        UClass = model.UClass,
                        IsTeacher = false,
                    };

                    _context.Classes.Add(classInfo);
                    await _context.SaveChangesAsync();
                }
                else if (model.Role.ToLower() == "teacher" && !string.IsNullOrEmpty(model.UClass))
                {
                    var teacherClassInfo = new UClass1
                    {
                        Student = dershaneId,
                        UClass = model.UClass,
                        IsTeacher = true,
                    };

                    _context.Classes.Add(teacherClassInfo);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                TempData["Success"] = $"Kullanıcı başarıyla eklendi! Dershane ID: {dershaneId}";
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Kullanıcı eklenirken bir hata oluştu: " + ex.Message;
                return View(model);
            }
        }

        // Dershane ID üretme metodu ekle
        private string GenerateDershaneId()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        [HttpGet]
        public IActionResult List(string role)
        {
            if (HttpContext.Session.GetString("role") != "principal")
                return Unauthorized();

            var users = _context.users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
            {
                role = role.Trim().ToLower();
                users = users.Where(u => u.role.ToLower() == role);
            }

            return View(users.ToList());
        }

        [HttpGet]
        public IActionResult Classes()
        {
            if (HttpContext.Session.GetString("role") != "principal")
                return Unauthorized();

            var classes = _context.Classes.Select(c => c.UClass).Distinct().ToList();

            var counts = _context
                .Classes.Where(c => !c.IsTeacher)
                .GroupBy(c => c.UClass)
                .Select(g => new { ClassName = g.Key, StudentCount = g.Count() })
                .ToDictionary(x => x.ClassName, x => x.StudentCount);

            var teachers = _context
                .Classes.Where(c => c.IsTeacher)
                .Join(
                    _context.users,
                    c => c.Student,
                    u => u.dershaneid,
                    (c, u) => new { c.UClass, TeacherName = u.firstname + " " + u.lastname }
                )
                .GroupBy(x => x.UClass)
                .Select(g => new { ClassName = g.Key, TeacherName = g.First().TeacherName })
                .ToDictionary(x => x.ClassName, x => x.TeacherName);

            ViewBag.ClassStudentCount = counts;
            ViewBag.ClassTeachers = teachers;

            return View(classes);
        }

        public IActionResult EditUser(string id)
        {
            var user = _context.users.FirstOrDefault(u => u.dershaneid == id);
            if (user == null)
            {
                return NotFound();
            }
            ViewBag.Classes = _context.Classes.Select(c => c.UClass).Distinct().ToList();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(User user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = _context.users.FirstOrDefault(u =>
                    u.dershaneid == user.dershaneid
                );
                if (existingUser == null)
                {
                    return NotFound();
                }

                existingUser.firstname = user.firstname;
                existingUser.lastname = user.lastname;
                existingUser.role = user.role;
                existingUser.uclass = user.uclass;

                _context.Update(existingUser);
                _context.SaveChanges();

                return RedirectToAction(nameof(List));
            }
            ViewBag.Classes = _context.Classes.Select(c => c.UClass).Distinct().ToList();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(string id)
        {
            if (HttpContext.Session.GetString("role") != "principal")
                return Unauthorized();

            var user = _context.users.FirstOrDefault(u => u.dershaneid == id);
            if (user == null)
            {
                return NotFound();
            }

            var classRecords = _context.Classes.Where(c => c.Student == id);
            _context.Classes.RemoveRange(classRecords);

            _context.users.Remove(user);
            _context.SaveChanges();

            TempData["Success"] = "User deleted successfully.";
            return RedirectToAction(nameof(List));
        }

        [HttpGet]
        public IActionResult EditClass(string className)
        {
            if (HttpContext.Session.GetString("role") != "principal")
                return Unauthorized();

            try
            {
                var classData = _context.Classes.Where(c => c.UClass == className).ToList();
                if (classData == null || !classData.Any())
                {
                    return NotFound();
                }

                var viewModel = new dershane.Models.EditClassViewModel
                {
                    ClassName = className,
                    NewClassName = className,
                    Students = _context
                        .users.Where(u => u.uclass == className && u.role == "student")
                        .ToList(),
                    Teacher = _context.users.FirstOrDefault(u =>
                        u.uclass == className && u.role == "teacher"
                    ),
                    TeacherId = _context
                        .users.FirstOrDefault(u => u.uclass == className && u.role == "teacher")
                        ?.dershaneid,
                };

                ViewBag.AllTeachers = _context.users.Where(u => u.role == "teacher").ToList();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in EditClass: {ex.Message}");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditClass(Models.EditClassViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.Remove("Students");
                ModelState.Remove("Teacher");

                if (!ModelState.IsValid)
                {
                    ViewBag.AllTeachers = _context.users.Where(u => u.role == "teacher").ToList();
                    return View(model);
                }
            }

            var existingClassRecords = _context
                .Classes.Where(c => c.UClass == model.ClassName)
                .ToList();
            if (!existingClassRecords.Any())
            {
                return NotFound();
            }

            foreach (var record in existingClassRecords)
            {
                record.UClass = model.NewClassName;
            }

            var currentTeacherRecord = existingClassRecords.FirstOrDefault(c => c.IsTeacher);
            if (currentTeacherRecord != null)
            {
                _context.Classes.Remove(currentTeacherRecord);
            }

            var newTeacher = _context.users.FirstOrDefault(u =>
                u.dershaneid == model.TeacherId && u.role == "teacher"
            );
            if (newTeacher != null)
            {
                _context.Classes.Add(
                    new UClass1
                    {
                        UClass = model.NewClassName,
                        Student = newTeacher.dershaneid,
                        IsTeacher = true,
                    }
                );
                newTeacher.uclass = model.NewClassName;
            }

            var studentsInClass = _context.users.Where(u =>
                u.uclass == model.ClassName && u.role == "student"
            );
            foreach (var student in studentsInClass)
            {
                student.uclass = model.NewClassName;
                var studentClassRecord = existingClassRecords.FirstOrDefault(c =>
                    c.Student == student.dershaneid
                );
                if (studentClassRecord != null)
                {
                    studentClassRecord.UClass = model.NewClassName;
                }
            }

            _context.SaveChanges();

            return RedirectToAction(nameof(Classes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteClass(string className)
        {
            if (HttpContext.Session.GetString("role") != "principal")
                return Unauthorized();

            var classRecords = _context.Classes.Where(c => c.UClass == className);
            if (classRecords == null || !classRecords.Any())
            {
                return NotFound();
            }

            _context.Classes.RemoveRange(classRecords);

            var usersInClass = _context.users.Where(u => u.uclass == className);
            foreach (var user in usersInClass)
            {
                user.uclass = null;
            }

            _context.SaveChanges();

            TempData["Success"] = "Class deleted successfully.";
            return RedirectToAction(nameof(Classes));
        }

        [HttpGet]
        [RoleAuthorize("principal")]
        public async Task<IActionResult> Index()
        {
            var model = new PrincipalDashboardVM();

            // Temel istatistikler
            model.TotalStudents = await _context.users.CountAsync(u => u.role == "student");
            model.TotalTeachers = await _context.users.CountAsync(u => u.role == "teacher");
            model.TotalClasses = await _context
                .Classes.Select(c => c.UClass)
                .Distinct()
                .CountAsync();
            model.TotalExams = await _context.ExamSystem.CountAsync();
            model.TotalHomeworks = await _context.Homeworks.CountAsync();

            // Sınıf istatistikleri
            var classStats = await _context
                .Classes.Where(c => !c.IsTeacher)
                .GroupBy(c => c.UClass)
                .Select(g => new ClassStatisticVM { ClassName = g.Key, StudentCount = g.Count() })
                .ToListAsync();

            // Her sınıf için ortalama hesapla
            foreach (var classStat in classStats)
            {
                var classStudentIds = await _context
                    .Classes.Where(c => c.UClass == classStat.ClassName && !c.IsTeacher)
                    .Select(c => c.Student)
                    .ToListAsync();

                // Sınav ortalaması
                var examScores = await _context
                    .StudentExamResults.Where(r =>
                        classStudentIds.Contains(r.StudentId) && r.IsCompleted
                    )
                    .Select(r => r.Score)
                    .ToListAsync();

                classStat.AverageScore = examScores.Any() ? examScores.Average() : 0;

                // Devamsızlık oranı hesapla
                var totalAttendances = await _context
                    .Attendances.Where(a => classStudentIds.Contains(a.StudentId))
                    .CountAsync();

                var presentAttendances = await _context
                    .Attendances.Where(a => classStudentIds.Contains(a.StudentId) && a.IsPresent)
                    .CountAsync();

                classStat.AttendanceRate =
                    totalAttendances > 0 ? (double)presentAttendances / totalAttendances * 100 : 0;
            }

            model.ClassStatistics = classStats;

            // Öğretmen performansları
            var teachers = await _context.users.Where(u => u.role == "teacher").ToListAsync();

            var teacherPerformances = new List<TeacherPerformanceVM>();

            foreach (var teacher in teachers)
            {
                var examsCreated = await _context.ExamSystem.CountAsync(e =>
                    e.TeacherId == teacher.dershaneid
                );

                var homeworksCreated = await _context.Homeworks.CountAsync(h =>
                    h.TeacherId == teacher.dershaneid
                );

                // Öğretmenin sınavlarındaki ortalama
                var teacherExamIds = await _context
                    .ExamSystem.Where(e => e.TeacherId == teacher.dershaneid)
                    .Select(e => e.Id)
                    .ToListAsync();

                var avgScore =
                    await _context
                        .StudentExamResults.Where(r =>
                            teacherExamIds.Contains(r.ExamId) && r.IsCompleted
                        )
                        .AverageAsync(r => (double?)r.Score) ?? 0;

                teacherPerformances.Add(
                    new TeacherPerformanceVM
                    {
                        TeacherName = $"{teacher.firstname} {teacher.lastname}",
                        TeacherId = teacher.dershaneid,
                        ExamsCreated = examsCreated,
                        HomeworksCreated = homeworksCreated,
                        AverageStudentScore = avgScore,
                    }
                );
            }

            model.TeacherPerformances = teacherPerformances
                .OrderByDescending(t => t.AverageStudentScore)
                .ToList();

            // Son sınavlar
            model.RecentExams = await _context
                .ExamSystem.OrderByDescending(e => e.ExamDate)
                .Take(5)
                .Select(e => new ExamStatisticVM
                {
                    ExamTitle = e.Title,
                    Lesson = e.Lesson,
                    ExamDate = e.ExamDate,
                    ParticipantCount = e.StudentResults.Count(r => r.IsCompleted),
                    AverageScore =
                        e.StudentResults.Where(r => r.IsCompleted).Average(r => (double?)r.Score)
                        ?? 0,
                })
                .ToListAsync();

            return View(model);
        }

        [HttpGet]
        [RoleAuthorize("principal")]
        public async Task<IActionResult> Reports()
        {
            var model = new PrincipalReportsVM();

            model.TotalStudents = await _context.users.CountAsync(u => u.role == "student");
            model.TotalTeachers = await _context.users.CountAsync(u => u.role == "teacher");
            model.TotalExams = await _context.ExamSystem.CountAsync();
            model.TotalHomeworks = await _context.Homeworks.CountAsync();

            var lessonStats = await _context
                .Lessons.Select(l => new LessonStatisticVM
                {
                    LessonName = l.Name,
                    ExamCount = _context.ExamSystem.Count(e => e.Lesson == l.Name),
                    HomeworkCount = _context.Homeworks.Count(h => h.Lesson == l.Name),
                    AverageExamScore =
                        _context
                            .StudentExamResults.Where(r => r.Exam.Lesson == l.Name && r.IsCompleted)
                            .Average(r => (double?)r.Score) ?? 0,
                    AverageHomeworkGrade =
                        _context
                            .HomeworkSubmissions.Where(s =>
                                s.Homework.Lesson == l.Name && s.Grade.HasValue
                            )
                            .Average(s => (double?)s.Grade) ?? 0,
                })
                .ToListAsync();

            model.LessonStatistics = lessonStats;

            var monthlyExamStats = new List<MonthlyExamStatVM>();
            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = DateTime.Now.AddMonths(-i);
                var examCount = await _context.ExamSystem.CountAsync(e =>
                    e.ExamDate.Month == targetMonth.Month && e.ExamDate.Year == targetMonth.Year
                );

                var avgScore =
                    await _context
                        .StudentExamResults.Where(r =>
                            r.Exam.ExamDate.Month == targetMonth.Month
                            && r.Exam.ExamDate.Year == targetMonth.Year
                            && r.IsCompleted
                        )
                        .AverageAsync(r => (double?)r.Score) ?? 0;

                monthlyExamStats.Add(
                    new MonthlyExamStatVM
                    {
                        Month = targetMonth.ToString("MMMM yyyy"),
                        ExamCount = examCount,
                        AverageScore = avgScore,
                        ParticipantCount = await _context.StudentExamResults.CountAsync(r =>
                            r.Exam.ExamDate.Month == targetMonth.Month
                            && r.Exam.ExamDate.Year == targetMonth.Year
                            && r.IsCompleted
                        ),
                    }
                );
            }

            model.MonthlyExamStats = monthlyExamStats;

            var topStudents = await _context
                .users.Where(u => u.role == "student")
                .Select(u => new TopStudentVM
                {
                    StudentId = u.dershaneid,
                    StudentName = u.firstname + " " + u.lastname,
                    AverageExamScore =
                        _context
                            .StudentExamResults.Where(r =>
                                r.StudentId == u.dershaneid && r.IsCompleted
                            )
                            .Average(r => (double?)r.Score) ?? 0,
                    AverageHomeworkGrade =
                        _context
                            .HomeworkSubmissions.Where(s =>
                                s.StudentId == u.dershaneid && s.Grade.HasValue
                            )
                            .Average(s => (double?)s.Grade) ?? 0,
                    ExamCount = _context.StudentExamResults.Count(r =>
                        r.StudentId == u.dershaneid && r.IsCompleted
                    ),
                    HomeworkCount = _context.HomeworkSubmissions.Count(s =>
                        s.StudentId == u.dershaneid
                    ),
                })
                .Where(s => s.ExamCount > 0)
                .OrderByDescending(s => s.AverageExamScore)
                .Take(10)
                .ToListAsync();

            model.TopStudents = topStudents;

            var attendanceReport = await _context
                .Classes.Where(c => !c.IsTeacher)
                .GroupBy(c => c.UClass)
                .Select(g => new AttendancesReportVM
                {
                    ClassName = g.Key,
                    TotalStudents = g.Count(),
                    PresentToday = _context.Attendances.Count(a =>
                        a.Date.Date == DateTime.Today
                        && a.IsPresent
                        && _context.Classes.Any(c =>
                            c.Student == a.StudentId && c.UClass == g.Key && !c.IsTeacher
                        )
                    ),
                    AttendanceRate = _context
                        .Attendances.Where(a =>
                            _context.Classes.Any(c =>
                                c.Student == a.StudentId && c.UClass == g.Key && !c.IsTeacher
                            )
                        )
                        .GroupBy(a => 1)
                        .Select(group => group.Average(a => a.IsPresent ? 100.0 : 0.0))
                        .FirstOrDefault(),
                })
                .ToListAsync();

            model.AttendanceReports = attendanceReport;

            return View(model);
        }

        [HttpGet]
        [RoleAuthorize("principal")]
        public async Task<IActionResult> Lessons()
        {
            var lessons = await _context.Lessons.ToListAsync();

            var examCounts = new Dictionary<string, int>();
            var homeworkCounts = new Dictionary<string, int>();

            foreach (var lesson in lessons)
            {
                examCounts[lesson.Name] = await _context.ExamSystem.CountAsync(e =>
                    e.Lesson == lesson.Name
                );
                homeworkCounts[lesson.Name] = await _context.Homeworks.CountAsync(h =>
                    h.Lesson == lesson.Name
                );
            }

            ViewBag.ExamCounts = examCounts;
            ViewBag.HomeworkCounts = homeworkCounts;

            return View(lessons);
        }

        [HttpGet]
        [RoleAuthorize("principal")]
        public async Task<IActionResult> CreateClass()
        {
            var model = new CreateClassViewModel();

            // Get available teachers (not assigned to any class)
            var assignedTeacherIds = await _context
                .Classes.Where(c => c.IsTeacher)
                .Select(c => c.Student)
                .Distinct()
                .ToListAsync();

            model.AvailableTeachers = await _context
                .users.Where(u => u.role == "teacher" && !assignedTeacherIds.Contains(u.dershaneid))
                .ToListAsync();

            // Get available students (not assigned to any class)
            var assignedStudentIds = await _context
                .Classes.Where(c => !c.IsTeacher)
                .Select(c => c.Student)
                .Distinct()
                .ToListAsync();

            model.AvailableStudents = await _context
                .users.Where(u => u.role == "student" && !assignedStudentIds.Contains(u.dershaneid))
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("principal")]
        public async Task<IActionResult> CreateClass(CreateClassViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload available teachers and students
                var assignedTeacherIds = await _context
                    .Classes.Where(c => c.IsTeacher)
                    .Select(c => c.Student)
                    .Distinct()
                    .ToListAsync();

                model.AvailableTeachers = await _context
                    .users.Where(u =>
                        u.role == "teacher" && !assignedTeacherIds.Contains(u.dershaneid)
                    )
                    .ToListAsync();

                var assignedStudentIds = await _context
                    .Classes.Where(c => !c.IsTeacher)
                    .Select(c => c.Student)
                    .Distinct()
                    .ToListAsync();

                model.AvailableStudents = await _context
                    .users.Where(u =>
                        u.role == "student" && !assignedStudentIds.Contains(u.dershaneid)
                    )
                    .ToListAsync();

                return View(model);
            }

            try
            {
                // Check if class name already exists
                var existingClass = await _context.Classes.FirstOrDefaultAsync(c =>
                    c.UClass == model.ClassName
                );

                if (existingClass != null)
                {
                    ModelState.AddModelError("ClassName", "A class with this name already exists");

                    // Reload data
                    var assignedTeacherIds = await _context
                        .Classes.Where(c => c.IsTeacher)
                        .Select(c => c.Student)
                        .Distinct()
                        .ToListAsync();

                    model.AvailableTeachers = await _context
                        .users.Where(u =>
                            u.role == "teacher" && !assignedTeacherIds.Contains(u.dershaneid)
                        )
                        .ToListAsync();

                    var assignedStudentIds = await _context
                        .Classes.Where(c => !c.IsTeacher)
                        .Select(c => c.Student)
                        .Distinct()
                        .ToListAsync();

                    model.AvailableStudents = await _context
                        .users.Where(u =>
                            u.role == "student" && !assignedStudentIds.Contains(u.dershaneid)
                        )
                        .ToListAsync();

                    return View(model);
                }

                // Add teacher to class if selected
                if (!string.IsNullOrEmpty(model.TeacherId))
                {
                    var teacherClass = new UClass1
                    {
                        UClass = model.ClassName,
                        Student = model.TeacherId,
                        IsTeacher = true,
                    };
                    _context.Classes.Add(teacherClass);
                }

                // Add selected students to class
                foreach (var studentId in model.SelectedStudentIds)
                {
                    var studentClass = new UClass1
                    {
                        UClass = model.ClassName,
                        Student = studentId,
                        IsTeacher = false,
                    };
                    _context.Classes.Add(studentClass);
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Class created successfully!";
                return RedirectToAction("Classes");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error creating class: " + ex.Message;

                // Reload data
                var assignedTeacherIds = await _context
                    .Classes.Where(c => c.IsTeacher)
                    .Select(c => c.Student)
                    .Distinct()
                    .ToListAsync();

                model.AvailableTeachers = await _context
                    .users.Where(u =>
                        u.role == "teacher" && !assignedTeacherIds.Contains(u.dershaneid)
                    )
                    .ToListAsync();

                var assignedStudentIds = await _context
                    .Classes.Where(c => !c.IsTeacher)
                    .Select(c => c.Student)
                    .Distinct()
                    .ToListAsync();

                model.AvailableStudents = await _context
                    .users.Where(u =>
                        u.role == "student" && !assignedStudentIds.Contains(u.dershaneid)
                    )
                    .ToListAsync();

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("principal")]
        public async Task<IActionResult> CreateLesson(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Course name cannot be blank!";
                return RedirectToAction("Lessons");
            }

            var existingLesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Name == name);
            if (existingLesson != null)
            {
                TempData["Error"] = "This course is already available!";
                return RedirectToAction("Lessons");
            }

            var lesson = new Lesson { Name = name, Description = description };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Course successfully added!";
            return RedirectToAction("Lessons");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("principal")]
        public async Task<IActionResult> UpdateLesson(int id, string name, string description)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
            {
                TempData["Error"] = "Course not found!";
                return RedirectToAction("Lessons");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Course name cannot be blank!";
                return RedirectToAction("Lessons");
            }

            var existingLesson = await _context.Lessons.FirstOrDefaultAsync(l =>
                l.Name == name && l.Id != id
            );
            if (existingLesson != null)
            {
                TempData["Error"] = "This course name is already in use!";
                return RedirectToAction("Lessons");
            }

            lesson.Name = name;
            lesson.Description = description;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Course successfully updated!";
            return RedirectToAction("Lessons");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("principal")]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
            {
                TempData["Error"] = "Course not found!";
                return RedirectToAction("Lessons");
            }

            // Check if lesson is being used in exams or homeworks
            var hasExams = await _context.ExamSystem.AnyAsync(e => e.Lesson == lesson.Name);
            var hasHomeworks = await _context.Homeworks.AnyAsync(h => h.Lesson == lesson.Name);
            var hasSchedules = await _context.Schedules.AnyAsync(s => s.Lesson == lesson.Name);

            if (hasExams || hasHomeworks || hasSchedules)
            {
                TempData["Error"] = "This lesson cannot be deleted because it is in use!";
                return RedirectToAction("Lessons");
            }

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Course successfully deleted!";
            return RedirectToAction("Lessons");
        }
    }
}
