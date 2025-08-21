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
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(User user)
        {
            if (HttpContext.Session.GetString("role") != "principal")
                return Unauthorized();

            string uclass = Request.Form["uclass"];
            string newClass = Request.Form["newClass"];

            ModelState.Remove("uclass");

            if (!ModelState.IsValid)
            {
                ViewBag.Classes = _context.Classes.Select(c => c.UClass).Distinct().ToList();
                return View(user);
            }

            Random rnd = new Random();
            string schoolNumber;
            int attempts = 0;
            do
            {
                if (++attempts > 10)
                    return BadRequest("Unique school number generate error.");
                schoolNumber = rnd.Next(1000, 9999).ToString();
            } while (_context.users.Any(u => u.dershaneid == schoolNumber));

            user.dershaneid = schoolNumber;
            user.uclass = newClass;

            user.password = BCrypt.Net.BCrypt.HashPassword(user.password);

            _context.users.Add(user);
            _context.SaveChanges();

            string finalClass = !string.IsNullOrWhiteSpace(uclass)
                ? uclass.Trim()
                : newClass?.Trim();

            if (!string.IsNullOrEmpty(finalClass))
            {
                _context.Classes.Add(
                    new UClass1
                    {
                        UClass = finalClass,
                        Student = schoolNumber,
                        IsTeacher = user.role == "teacher",
                    }
                );
                _context.SaveChanges();
            }

            TempData["Success"] = $"User added successfully. School Number: {schoolNumber}";
            return RedirectToAction("Index");
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

            // Genel istatistikler
            model.TotalStudents = await _context.users.CountAsync(u => u.role == "student");
            model.TotalTeachers = await _context.users.CountAsync(u => u.role == "teacher");
            model.TotalExams = await _context.ExamSystem.CountAsync();
            model.TotalHomeworks = await _context.Homeworks.CountAsync();

            // Ders bazında istatistikler
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

            // Aylık sınav istatistikleri (son 6 ay)
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

            // En başarılı öğrenciler (top 10)
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

            // Devamsızlık raporu
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
    }
}
