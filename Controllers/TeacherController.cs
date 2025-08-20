using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using dershane.Data;
using dershane.Filters;
using dershane.Models;
using dershane.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace dershane.Controllers
{
    public class TeacherController : Controller
    {
        private readonly AppDbContext _context;

        public TeacherController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult AddExam()
        {
            if (HttpContext.Session.GetString("role") != "teacher")
                return Unauthorized();

            var teacherNumber = HttpContext.Session.GetString("schoolnumber");
            var teacherClass = _context
                .Classes.Where(c => c.Student == teacherNumber && c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefault();

            if (teacherClass == null)
                return Content("Teacher's class not found.");

            var students = _context
                .users.Join(
                    _context.Classes,
                    u => u.dershaneid,
                    c => c.Student,
                    (u, c) => new { User = u, Class = c }
                )
                .Where(uc =>
                    uc.User.role == "student"
                    && uc.Class.UClass == teacherClass
                    && !uc.Class.IsTeacher
                )
                .Select(uc => new SelectListItem
                {
                    Value = uc.User.dershaneid,
                    Text = $"{uc.User.firstname} {uc.User.lastname} ({uc.User.dershaneid})",
                })
                .ToList();

            var lessons = _context
                .Lessons.Select(l => new SelectListItem { Value = l.Name, Text = l.Name })
                .ToList();

            var vm = new AddExamVM { Students = students, Lessons = lessons };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddExam(AddExamVM vm)
        {
            if (HttpContext.Session.GetString("role") != "teacher")
                return Unauthorized();

            if (ModelState.IsValid)
            {
                vm.Students = _context
                    .users.Where(u => u.role == "student")
                    .Select(u => new SelectListItem
                    {
                        Value = u.dershaneid,
                        Text = $"{u.firstname} {u.lastname} ({u.dershaneid})",
                    })
                    .ToList();

                vm.Lessons = _context
                    .Lessons.Select(l => new SelectListItem { Value = l.Name, Text = l.Name })
                    .ToList();

                return View(vm);
            }

            var exam = new Exams
            {
                schoolnumber = vm.SchoolNumber,
                lesson = vm.Lesson,
                points = vm.Points,
            };

            _context.notes.Add(exam);
            _context.SaveChanges();

            TempData["Success"] = "Exam added successfully!";
            return RedirectToAction("AddExam");
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("role") != "teacher")
                return Unauthorized();

            string teacherNumber = HttpContext.Session.GetString("schoolnumber");
            if (string.IsNullOrEmpty(teacherNumber))
                return Content("Teacher's information not found.");

            var teacherClass = _context
                .Classes.Where(c => c.Student == teacherNumber && c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefault();

            if (teacherClass == null)
                return Content("Teacher's class information not found.");

            var students = _context
                .users.Join(
                    _context.Classes,
                    u => u.dershaneid,
                    c => c.Student,
                    (u, c) => new { User = u, Class = c }
                )
                .Where(uc =>
                    uc.User.role == "student"
                    && uc.Class.UClass == teacherClass
                    && !uc.Class.IsTeacher
                )
                .Select(uc => new StudentVM
                {
                    DershaneId = uc.User.dershaneid,
                    FirstName = uc.User.firstname,
                    LastName = uc.User.lastname,
                    UClass = uc.Class.UClass,
                })
                .ToList();

            return View(students);
        }

        public IActionResult ViewExams()
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "teacher")
                return Unauthorized();

            var teacherNumber = HttpContext.Session.GetString("schoolnumber");

            var teacherClass = _context
                .Classes.Where(c => c.Student == teacherNumber && c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefault();

            if (teacherClass == null)
                return Content("Teacher's class not found.");

            var groupedExams = (
                from exam in _context.notes
                join cls in _context.Classes on exam.schoolnumber equals cls.Student
                join student in _context.users on exam.schoolnumber equals student.dershaneid
                where cls.UClass == teacherClass && !cls.IsTeacher
                group new { exam, student } by exam.lesson into g
                select new ExamGroupVM
                {
                    Lesson = g.Key,
                    ExamResults = g.Select(x => new ExamResultVM
                        {
                            Nid = x.exam.nid,
                            StudentNumber = x.exam.schoolnumber,
                            StudentName = x.student.firstname + " " + x.student.lastname,
                            Points = x.exam.points,
                        })
                        .ToList(),
                }
            ).ToList();

            return View(groupedExams);
        }

        [HttpGet]
        public IActionResult EditExam(int nid)
        {
            if (HttpContext.Session.GetString("role") != "teacher")
                return Unauthorized();

            var exam = _context.notes.FirstOrDefault(e => e.nid == nid);
            if (exam == null)
                return NotFound();

            var student = _context.users.FirstOrDefault(u => u.dershaneid == exam.schoolnumber);
            if (student == null)
                return NotFound();

            var viewModel = new EditExamVM
            {
                Nid = exam.nid,
                SchoolNumber = exam.schoolnumber,
                StudentName = $"{student.firstname} {student.lastname}",
                Lesson = exam.lesson,
                Points = exam.points,
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditExam(EditExamVM model)
        {
            if (HttpContext.Session.GetString("role") != "teacher")
                return Unauthorized();

            if (!ModelState.IsValid)
                return View(model);

            var exam = _context.notes.FirstOrDefault(e => e.nid == model.Nid);
            if (exam == null)
                return NotFound();

            exam.points = model.Points;
            _context.SaveChanges();

            TempData["Success"] = "Exam updated successfully!";
            return RedirectToAction("ViewExams");
        }

        [HttpGet]
        public IActionResult TakeAttendance()
        {
            if (HttpContext.Session.GetString("role") != "teacher")
                return Unauthorized();

            var teacherNumber = HttpContext.Session.GetString("schoolnumber");
            var teacherClass = _context
                .Classes.Where(c => c.Student == teacherNumber && c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefault();

            if (teacherClass == null)
                return Content("Teacher's class not found.");

            var lessons = _context
                .Schedules.Where(s => s.TeacherId == teacherNumber)
                .Select(s => s.Lesson)
                .Distinct()
                .ToList();

            ViewBag.Lessons = lessons
                .Select(l => new SelectListItem { Value = l, Text = l })
                .ToList();

            ViewBag.TeacherClass = teacherClass;

            return View();
        }

        [HttpPost]
        public IActionResult GetStudentsForAttendance(string lesson, DateTime date)
        {
            if (HttpContext.Session.GetString("role") != "teacher")
                return Unauthorized();

            var teacherNumber = HttpContext.Session.GetString("schoolnumber");
            var teacherClass = _context
                .Classes.Where(c => c.Student == teacherNumber && c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefault();

            if (teacherClass == null)
                return Json(new { success = false, message = "Teacher's class not found." });

            var students = _context
                .users.Join(
                    _context.Classes,
                    u => u.dershaneid,
                    c => c.Student,
                    (u, c) => new { User = u, Class = c }
                )
                .Where(uc =>
                    uc.User.role == "student"
                    && uc.Class.UClass == teacherClass
                    && !uc.Class.IsTeacher
                )
                .Select(uc => new AttendanceVM
                {
                    StudentId = uc.User.dershaneid,
                    StudentName = $"{uc.User.firstname} {uc.User.lastname}",
                    IsPresent = true,
                })
                .ToList();

            var existingAttendance = _context
                .Attendances.Where(a => a.Lesson == lesson && a.Date.Date == date.Date)
                .ToList();

            foreach (var student in students)
            {
                var existing = existingAttendance.FirstOrDefault(a =>
                    a.StudentId == student.StudentId
                );
                if (existing != null)
                {
                    student.IsPresent = existing.IsPresent;
                    student.Note = existing.Note;
                }
            }

            return Json(new { success = true, students = students });
        }

        [HttpPost]
        public IActionResult SaveAttendance([FromBody] TakeAttendanceVM model)
        {
            if (HttpContext.Session.GetString("role") != "teacher")
                return Json(new { success = false, message = "Unauthorized" });

            var teacherNumber = HttpContext.Session.GetString("schoolnumber");

            try
            {
                var existingAttendance = _context
                    .Attendances.Where(a =>
                        a.Lesson == model.Lesson && a.Date.Date == model.Date.Date
                    )
                    .ToList();

                _context.Attendances.RemoveRange(existingAttendance);

                foreach (var student in model.Students)
                {
                    var attendance = new Attendance
                    {
                        StudentId = student.StudentId,
                        Lesson = model.Lesson,
                        Date = model.Date,
                        IsPresent = student.IsPresent,
                        Note = student.Note,
                        TeacherId = teacherNumber,
                    };

                    _context.Attendances.Add(attendance);
                }

                _context.SaveChanges();
                return Json(new { success = true, message = "Yoklama başarıyla kaydedildi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult AttendanceReports(
            string lesson = null,
            DateTime? startDate = null,
            DateTime? endDate = null
        )
        {
            if (HttpContext.Session.GetString("role") != "teacher")
                return Unauthorized();

            try
            {
                var teacherNumber = HttpContext.Session.GetString("schoolnumber");

                var query =
                    from attendance in _context.Attendances
                    join user in _context.users on attendance.StudentId equals user.dershaneid
                    where attendance.TeacherId == teacherNumber
                    select new AttendanceReportVM
                    {
                        Date = attendance.Date,
                        Lesson = attendance.Lesson,
                        StudentId = attendance.StudentId,
                        StudentName = $"{user.firstname} {user.lastname}",
                        IsPresent = attendance.IsPresent,
                        Note = attendance.Note,
                    };

                var reports = query.ToList();

                if (!string.IsNullOrEmpty(lesson))
                {
                    reports = reports.Where(r => r.Lesson == lesson).ToList();
                }

                if (startDate.HasValue)
                {
                    reports = reports.Where(r => r.Date.Date >= startDate.Value.Date).ToList();
                }

                if (endDate.HasValue)
                {
                    reports = reports.Where(r => r.Date.Date <= endDate.Value.Date).ToList();
                }

                var teacherLessons = _context
                    .Schedules.Where(s => s.TeacherId == teacherNumber)
                    .Select(s => s.Lesson)
                    .Distinct()
                    .ToList();

                ViewBag.Lessons = teacherLessons
                    .Select(l => new SelectListItem
                    {
                        Value = l,
                        Text = l,
                        Selected = l == lesson,
                    })
                    .ToList();

                ViewBag.SelectedLesson = lesson;
                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

                return View(reports);
            }
            catch (Exception ex)
            {
                return View(new List<AttendanceReportVM>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteExamResult(int nid)
        {
            if (HttpContext.Session.GetString("role") != "teacher")
                return Unauthorized();

            var exam = _context.notes.FirstOrDefault(e => e.nid == nid);
            if (exam == null)
            {
                TempData["Error"] = "Exam result not found.";
                return RedirectToAction(nameof(ViewExams));
            }

            _context.notes.Remove(exam);
            _context.SaveChanges();

            TempData["Success"] = "Exam result deleted successfully.";
            return RedirectToAction(nameof(ViewExams));
        }

        [RoleAuthorize("teacher")]
        public IActionResult CreateHomework()
        {
            var model = new ViewModels.CreateHomeworkVM();

            var lessons = _context
                .Lessons.Select(l => new SelectListItem { Value = l.Name, Text = l.Name })
                .ToList();

            model.Lessons = lessons;
            model.DueDate = DateTime.Now.AddDays(7);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("teacher")]
        public async Task<IActionResult> CreateHomework(ViewModels.CreateHomeworkVM model)
        {
            if (!ModelState.IsValid)
            {
                model.Lessons = _context
                    .Lessons.Select(l => new SelectListItem { Value = l.Name, Text = l.Name })
                    .ToList();
                return View(model);
            }

            var teacherId = HttpContext.Session.GetString("schoolnumber");
            var teacherClass = _context
                .Classes.Where(c => c.Student == teacherId && c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefault();

            if (teacherClass == null)
            {
                TempData["Error"] = "Sınıf bilgin bulunamadı lan!";
                return RedirectToAction("Index");
            }

            var homework = new Homework
            {
                Title = model.Title,
                Description = model.Description,
                Lesson = model.Lesson,
                UClass = teacherClass,
                TeacherId = teacherId,
                DueDate = model.DueDate,
                CreatedDate = DateTime.Now,
                IsActive = true,
            };

            _context.Homeworks.Add(homework);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ödev başarıyla oluşturuldu! Öğrenciler ağlayacak şimdi 😈";
            return RedirectToAction("ViewHomeworks");
        }

        [RoleAuthorize("teacher")]
        public async Task<IActionResult> ViewHomeworks()
        {
            var teacherId = HttpContext.Session.GetString("schoolnumber");
            var teacherClass = _context
                .Classes.Where(c => c.Student == teacherId && c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefault();

            var homeworks = await _context
                .Homeworks.Where(h => h.TeacherId == teacherId && h.UClass == teacherClass)
                .Select(h => new HomeworkListVM
                {
                    Id = h.Id,
                    Title = h.Title,
                    Lesson = h.Lesson,
                    DueDate = h.DueDate,
                    IsActive = h.IsActive,
                    SubmissionCount = _context.HomeworkSubmissions.Count(s => s.HomeworkId == h.Id),
                })
                .OrderByDescending(h => h.DueDate)
                .ToListAsync();

            return View(homeworks);
        }

        [RoleAuthorize("teacher")]
        public async Task<IActionResult> ViewSubmissions(int homeworkId)
        {
            var homework = await _context.Homeworks.FindAsync(homeworkId);
            if (homework == null)
            {
                TempData["Error"] = "Ödev bulunamadı lan!";
                return RedirectToAction("ViewHomeworks");
            }

            var submissions = await (
                from submission in _context.HomeworkSubmissions
                join student in _context.users on submission.StudentId equals student.dershaneid
                where submission.HomeworkId == homeworkId
                select new SubmissionListVM
                {
                    Id = submission.Id,
                    Answer = submission.Answer,
                    SubmissionDate = submission.SubmissionDate,
                    Grade = submission.Grade,
                    TeacherComment = submission.TeacherComment,
                    IsGraded = submission.IsGraded,
                    StudentName = student.firstname + " " + student.lastname,
                    StudentNumber = student.dershaneid,
                }
            ).ToListAsync();

            ViewBag.Homework = homework;
            return View(submissions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("teacher")]
        public async Task<IActionResult> GradeHomework(int submissionId, int grade, string comment)
        {
            var submission = await _context.HomeworkSubmissions.FindAsync(submissionId);
            if (submission == null)
            {
                TempData["Error"] = "Teslim bulunamadı lan!";
                return RedirectToAction("ViewHomeworks");
            }

            submission.Grade = grade;
            submission.TeacherComment = comment;
            submission.IsGraded = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Ödev başarıyla notlandı! Öğrenci şimdi ağlayacak 😈";
            return RedirectToAction("ViewSubmissions", new { homeworkId = submission.HomeworkId });
        }

        [HttpGet]
        [RoleAuthorize("teacher")]
        public IActionResult CreateExam()
        {
            var model = new CreateExamVM();
            model.Lessons = _context
                .Lessons.Select(l => new SelectListItem { Value = l.Name, Text = l.Name })
                .ToList();

            // Başlangıçta 5 soru ekleyelim - bu muhteşem olacak!
            for (int i = 0; i < 5; i++)
            {
                model.Questions.Add(new ExamQuestionVM());
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("teacher")]
        public async Task<IActionResult> CreateExam(CreateExamVM model)
        {
            if (!ModelState.IsValid)
            {
                model.Lessons = _context
                    .Lessons.Select(l => new SelectListItem { Value = l.Name, Text = l.Name })
                    .ToList();
                return View(model);
            }

            var teacherId = HttpContext.Session.GetString("schoolnumber");
            var teacherClass = _context
                .Classes.Where(c => c.Student == teacherId && c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefault();

            if (teacherClass == null)
            {
                TempData["Error"] = "Sınıf bilgin bulunamadı lan!";
                return RedirectToAction("Index");
            }

            var exam = new ExamSystem
            {
                Title = model.Title,
                Description = model.Description,
                Lesson = model.Lesson,
                UClass = teacherClass,
                TeacherId = teacherId,
                ExamDate = model.ExamDate,
                Duration = model.Duration,
                CreatedDate = DateTime.Now,
                IsActive = true,
            };

            _context.ExamSystem.Add(exam);
            await _context.SaveChangesAsync();

            // Soruları ekleyelim
            for (int i = 0; i < model.Questions.Count; i++)
            {
                var questionVM = model.Questions[i];
                if (!string.IsNullOrEmpty(questionVM.QuestionText))
                {
                    var examQuestion = new ExamQuestion
                    {
                        ExamId = exam.Id,
                        QuestionText = questionVM.QuestionText,
                        OptionA = questionVM.OptionA,
                        OptionB = questionVM.OptionB,
                        OptionC = questionVM.OptionC,
                        OptionD = questionVM.OptionD,
                        CorrectAnswer = questionVM.CorrectAnswer,
                        Points = questionVM.Points,
                        QuestionOrder = i + 1,
                    };

                    _context.ExamQuestions.Add(examQuestion);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Sınav başarıyla oluşturuldu! Öğrenciler ter dökecek şimdi 😈";
            return RedirectToAction("ViewExamSystem");
        }

        [HttpGet]
        [RoleAuthorize("teacher")]
        public IActionResult ViewExamSystem()
        {
            var teacherId = HttpContext.Session.GetString("schoolnumber");
            var teacherClass = _context
                .Classes.Where(c => c.Student == teacherId && c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefault();

            var exams = _context
                .ExamSystem.Where(e => e.TeacherId == teacherId)
                .Include(e => e.Questions)
                .Include(e => e.StudentResults)
                .OrderByDescending(e => e.CreatedDate)
                .ToList();

            return View(exams);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("teacher")]
        public async Task<IActionResult> DeleteExam(int examId)
        {
            var exam = await _context
                .ExamSystem.Include(e => e.Questions)
                .Include(e => e.StudentResults)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
            {
                TempData["Error"] = "Sınav bulunamadı lan!";
                return RedirectToAction("ViewExamSystem");
            }

            // Öğretmenin kendi sınıfının sınavını sildiğinden emin ol
            var teacherId = HttpContext.Session.GetString("schoolnumber");
            var teacherClass = _context
                .Classes.Where(c => c.Student == teacherId && c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefault();

            if (exam.UClass != teacherClass)
            {
                TempData["Error"] = "Bu sınavı silme yetkin yok!";
                return RedirectToAction("ViewExamSystem");
            }

            // Eğer öğrenciler sınava girmişse uyarı ver
            if (exam.StudentResults.Any(r => r.IsCompleted))
            {
                TempData["Error"] = "Bu sınava öğrenciler girmiş! Artık silemezsin! 😈";
                return RedirectToAction("ViewExamSystem");
            }

            try
            {
                // Önce student results'ları sil
                _context.StudentExamResults.RemoveRange(exam.StudentResults);

                // Sonra questions'ları sil
                _context.ExamQuestions.RemoveRange(exam.Questions);

                // Son olarak exam'ı sil
                _context.ExamSystem.Remove(exam);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Sınav başarıyla silindi! Öğrenciler rahat nefes aldı 😅";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Sınav silinirken hata oluştu: " + ex.Message;
            }

            return RedirectToAction("ViewExamSystem");
        }

        [HttpGet]
        [RoleAuthorize("teacher")]
        public async Task<IActionResult> EditExamSystem(int examId)
        {
            var exam = await _context
                .ExamSystem.Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
            {
                TempData["Error"] = "Sınav bulunamadı!";
                return RedirectToAction("ViewExamSystem");
            }

            var teacherId = HttpContext.Session.GetString("schoolnumber");
            var teacherClass = _context
                .Classes.Where(c => c.Student == teacherId && c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefault();

            if (exam.UClass != teacherClass)
            {
                TempData["Error"] = "Bu sınavı düzenleme yetkin yok!";
                return RedirectToAction("ViewExamSystem");
            }

            if (exam.ExamDate <= DateTime.Now)
            {
                TempData["Error"] = "Başlamış veya bitmiş sınavı düzenleyemezsin!";
                return RedirectToAction("ViewExamSystem");
            }

            var viewModel = new CreateExamVM
            {
                Id = exam.Id,
                Title = exam.Title,
                Description = exam.Description,
                Lesson = exam.Lesson,
                ExamDate = exam.ExamDate,
                Duration = exam.Duration,
                Questions = exam
                    .Questions.OrderBy(q => q.QuestionOrder)
                    .Select(q => new ExamQuestionVM
                    {
                        Id = q.Id,
                        ExamId = q.ExamId,
                        QuestionText = q.QuestionText,
                        OptionA = q.OptionA,
                        OptionB = q.OptionB,
                        OptionC = q.OptionC,
                        OptionD = q.OptionD,
                        CorrectAnswer = q.CorrectAnswer,
                        Points = q.Points,
                        QuestionOrder = q.QuestionOrder,
                    })
                    .ToList(),
                Lessons = _context
                    .Lessons.Select(l => new SelectListItem
                    {
                        Value = l.Name,
                        Text = l.Name,
                        Selected = l.Name == exam.Lesson,
                    })
                    .ToList(),
            };

            return View("CreateExam", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("teacher")]
        public async Task<IActionResult> EditExamSystem(CreateExamVM model)
        {
            if (!ModelState.IsValid)
            {
                model.Lessons = _context
                    .Lessons.Select(l => new SelectListItem { Value = l.Name, Text = l.Name })
                    .ToList();
                return View("CreateExam", model);
            }

            var exam = await _context
                .ExamSystem.Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == model.Id);

            if (exam == null)
            {
                TempData["Error"] = "Sınav bulunamadı!";
                return RedirectToAction("ViewExamSystem");
            }

            // Sınav başlamışsa düzenlemeye izin verme
            if (exam.ExamDate <= DateTime.Now)
            {
                TempData["Error"] = "Başlamış sınavı düzenleyemezsin!";
                return RedirectToAction("ViewExamSystem");
            }

            try
            {
                // Exam bilgilerini güncelle
                exam.Title = model.Title;
                exam.Description = model.Description;
                exam.Lesson = model.Lesson;
                exam.ExamDate = model.ExamDate;
                exam.Duration = model.Duration;

                // Eski soruları sil
                _context.ExamQuestions.RemoveRange(exam.Questions);

                // Yeni soruları ekle
                for (int i = 0; i < model.Questions.Count; i++)
                {
                    var questionVM = model.Questions[i];
                    var question = new ExamQuestion
                    {
                        ExamId = exam.Id,
                        QuestionText = questionVM.QuestionText,
                        OptionA = questionVM.OptionA,
                        OptionB = questionVM.OptionB,
                        OptionC = questionVM.OptionC,
                        OptionD = questionVM.OptionD,
                        CorrectAnswer = questionVM.CorrectAnswer,
                        Points = questionVM.Points,
                        QuestionOrder = i + 1,
                    };
                    _context.ExamQuestions.Add(question);
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Sınav başarıyla güncellendi! Öğrenciler yine ağlayacak 😈";
                return RedirectToAction("ViewExamSystem");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Sınav güncellenirken hata oluştu: " + ex.Message;
                model.Lessons = _context
                    .Lessons.Select(l => new SelectListItem { Value = l.Name, Text = l.Name })
                    .ToList();
                return View("CreateExam", model);
            }
        }

        [HttpGet]
        [RoleAuthorize("teacher")]
        public async Task<IActionResult> ViewExamResults(int examId)
        {
            var exam = await _context
                .ExamSystem.Include(e => e.Questions)
                .Include(e => e.StudentResults)
                .ThenInclude(r => r.Student)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
            {
                TempData["Error"] = "Sınav bulunamadı!";
                return RedirectToAction("ViewExamSystem");
            }

            // Öğretmenin kendi sınıfının sınavını görüntülediğinden emin ol
            var teacherId = HttpContext.Session.GetString("schoolnumber");
            var teacherClass = _context
                .Classes.Where(c => c.Student == teacherId && c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefault();

            if (exam.UClass != teacherClass)
            {
                TempData["Error"] = "Bu sınavın sonuçlarını görme yetkin yok!";
                return RedirectToAction("ViewExamSystem");
            }

            var totalPoints = exam.Questions.Sum(q => q.Points);

            // Sınıftaki tüm öğrencileri al
            var classStudents = await _context
                .users.Join(
                    _context.Classes,
                    u => u.dershaneid,
                    c => c.Student,
                    (u, c) => new { User = u, Class = c }
                )
                .Where(uc =>
                    uc.User.role == "student"
                    && uc.Class.UClass == teacherClass
                    && !uc.Class.IsTeacher
                )
                .Select(uc => uc.User)
                .ToListAsync();

            // Sonuçları hazırla
            var studentResults = new List<ExamResultDetailVM>();

            foreach (var student in classStudents)
            {
                var result = exam.StudentResults.FirstOrDefault(r =>
                    r.StudentId == student.dershaneid
                );

                studentResults.Add(
                    new ExamResultDetailVM
                    {
                        Id = result?.Id ?? 0,
                        StudentId = student.dershaneid,
                        StudentName = $"{student.firstname} {student.lastname}",
                        Score = result?.Score ?? 0,
                        IsCompleted = result?.IsCompleted ?? false,
                        StartTime = result?.StartTime ?? DateTime.MinValue,
                        EndTime = result?.EndTime,
                        Answers = result?.Answers,
                        TotalPoints = totalPoints,
                    }
                );
            }

            var completedResults = studentResults.Where(r => r.IsCompleted).ToList();

            var viewModel = new ViewExamResultsVM
            {
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                Lesson = exam.Lesson,
                ExamDate = exam.ExamDate,
                Duration = exam.Duration,
                TotalPoints = totalPoints,
                TotalStudents = classStudents.Count,
                CompletedCount = completedResults.Count,
                AverageScore = completedResults.Any() ? completedResults.Average(r => r.Score) : 0,
                HighestScore = completedResults.Any() ? completedResults.Max(r => r.Score) : 0,
                LowestScore = completedResults.Any() ? completedResults.Min(r => r.Score) : 0,
                StudentResults = studentResults,
            };

            return View(viewModel);
        }
    }
}
