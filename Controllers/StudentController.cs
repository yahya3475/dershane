
using System;
using System.Linq;
using System.Security.Claims;
using dershane.Data;
using dershane.Filters;
using dershane.Models;
using dershane.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace dershane.Controllers
{
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;

        public StudentController(AppDbContext context)
        {
            _context = context;
        }

        [RoleAuthorize("student")]
        public IActionResult MyAttendance()
        {
            var studentId = HttpContext.Session.GetString("schoolnumber");

            var attendances = (
                from attendance in _context.Attendances
                join teacher in _context.users on attendance.TeacherId equals teacher.dershaneid
                where attendance.StudentId == studentId
                select new StudentAttendanceVM
                {
                    Date = attendance.Date,
                    Lesson = attendance.Lesson,
                    IsPresent = attendance.IsPresent,
                    Note = attendance.Note,
                    TeacherName = teacher.firstname + " " + teacher.lastname,
                }
            )
                .OrderByDescending(a => a.Date)
                .ToList();

            return View(attendances);
        }

        [RoleAuthorize("student")]
        public IActionResult ViewHomeworks()
        {
            if (HttpContext.Session.GetString("role") != "student")
                return Unauthorized();

            var schoolNumber = HttpContext.Session.GetString("schoolnumber");
            if (string.IsNullOrEmpty(schoolNumber))
                return RedirectToAction("Login", "Auth");

            var studentClass = _context
                .Classes.Where(c => c.Student == schoolNumber && !c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(studentClass))
            {
                return View(new List<HomeworkWithSubmissionVM>());
            }

            var homeworks = _context
                .Homeworks.Where(h => h.UClass == studentClass)
                .Select(h => new HomeworkWithSubmissionVM
                {
                    Id = h.Id,
                    Title = h.Title,
                    Description = h.Description,
                    Lesson = h.Lesson,
                    DueDate = h.DueDate,
                    IsSubmitted = _context.HomeworkSubmissions.Any(s =>
                        s.HomeworkId == h.Id && s.StudentId == schoolNumber
                    ),
                    SubmissionDate = _context
                        .HomeworkSubmissions.Where(s =>
                            s.HomeworkId == h.Id && s.StudentId == schoolNumber
                        )
                        .Select(s => s.SubmissionDate)
                        .FirstOrDefault(),
                    Grade = _context
                        .HomeworkSubmissions.Where(s =>
                            s.HomeworkId == h.Id && s.StudentId == schoolNumber
                        )
                        .Select(s => s.Grade)
                        .FirstOrDefault(),
                    TeacherComment = _context
                        .HomeworkSubmissions.Where(s =>
                            s.HomeworkId == h.Id && s.StudentId == schoolNumber
                        )
                        .Select(s => s.TeacherComment)
                        .FirstOrDefault(),
                    SubmissionContent = _context
                        .HomeworkSubmissions.Where(s =>
                            s.HomeworkId == h.Id && s.StudentId == schoolNumber
                        )
                        .Select(s => s.Answer)
                        .FirstOrDefault(),
                    SubmissionFilePath = null,
                })
                .ToList();

            var lessons = homeworks.Select(h => h.Lesson).Distinct().ToList();
            ViewBag.Lessons = lessons
                .Select(l => new SelectListItem { Value = l, Text = l })
                .ToList();

            return View(homeworks);
        }

        [RoleAuthorize("student")]
        public async Task<IActionResult> SubmitHomework(int id)
        {
            Console.WriteLine($"SubmitHomework GET action'ına girdi! ID: {id}");

            var homework = await _context.Homeworks.FindAsync(id);
            Console.WriteLine($"Homework bulundu: {homework != null}");

            if (homework == null)
            {
                Console.WriteLine("Homework NULL!");
                TempData["Error"] = "Ödev bulunamadı!";
                return RedirectToAction("ViewHomeworks");
            }

            Console.WriteLine($"Homework verileri:");
            Console.WriteLine($"  ID: {homework.Id}");
            Console.WriteLine($"  Title: {homework.Title}");
            Console.WriteLine($"  Description: {homework.Description}");
            Console.WriteLine($"  Lesson: {homework.Lesson}");
            Console.WriteLine($"  DueDate: {homework.DueDate}");

            var studentId = HttpContext.Session.GetString("schoolnumber");
            var existingSubmission = await _context.HomeworkSubmissions.FirstOrDefaultAsync(s =>
                s.HomeworkId == id && s.StudentId == studentId
            );

            if (existingSubmission != null)
            {
                TempData["Error"] = "Bu ödevi zaten teslim etmişsin lan!";
                return RedirectToAction("ViewHomeworks");
            }

            var model = new Models.SubmitHomeworkVM
            {
                HomeworkId = homework.Id,
                Title = homework.Title,
                Description = homework.Description,
                Lesson = homework.Lesson,
                DueDate = homework.DueDate,
            };

            Console.WriteLine($"Model oluşturuldu:");
            Console.WriteLine($"  HomeworkId: {model.HomeworkId}");
            Console.WriteLine($"  Title: {model.Title}");
            Console.WriteLine($"  Description: {model.Description}");
            Console.WriteLine($"  Lesson: {model.Lesson}");
            Console.WriteLine($"  DueDate: {model.DueDate}");

            ViewData["DebugModel"] = model;
            ViewBag.DebugHomework = homework;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("student")]
        public async Task<IActionResult> SubmitHomework(Models.SubmitHomeworkVM model)
        {
            ModelState.Remove("Grade");
            ModelState.Remove("TeacherComment");
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var studentId = HttpContext.Session.GetString("schoolnumber");
            var submission = new HomeworkSubmission
            {
                HomeworkId = model.HomeworkId,
                StudentId = studentId,
                Answer = model.Answer,
                SubmissionDate = DateTime.Now,
            };

            _context.HomeworkSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Ödev başarıyla teslim edildi! Şimdi bekle bakalım kaç alacaksın 😏";
            return RedirectToAction("ViewHomeworks");
        }

        [RoleAuthorize("student")]
        public async Task<IActionResult> ViewSubmission(int id)
        {
            var studentId = HttpContext.Session.GetString("schoolnumber");

            var submission = await _context
                .HomeworkSubmissions.Include(s => s.Homework)
                .FirstOrDefaultAsync(s => s.HomeworkId == id && s.StudentId == studentId);

            if (submission == null)
            {
                TempData["Error"] = "Teslim edilen ödev bulunamadı!";
                return RedirectToAction("ViewHomeworks");
            }

            var model = new ViewSubmissionVM
            {
                HomeworkId = submission.HomeworkId,
                Title = submission.Homework.Title,
                Description = submission.Homework.Description,
                Lesson = submission.Homework.Lesson,
                DueDate = submission.Homework.DueDate,
                Answer = submission.Answer,
                SubmissionDate = submission.SubmissionDate,
                Grade = submission.Grade,
                TeacherComment = submission.TeacherComment,
            };

            return View(model);
        }

        [HttpGet]
        [RoleAuthorize("student")]
        public async Task<IActionResult> ViewExamSystem()
        {
            if (HttpContext.Session.GetString("role") != "student")
                return Unauthorized();

            var studentId = HttpContext.Session.GetString("schoolnumber");

            // Öğrencinin sınıfını bul
            var studentClass = await _context
                .Classes.Where(c => c.Student == studentId && !c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefaultAsync();

            if (studentClass == null)
            {
                TempData["Error"] = "Sınıf bilgin bulunamadı lan!";
                return View(new StudentExamSystemVM());
            }

            // Bu sınıfa ait sınavları getir
            var exams = await _context
                .ExamSystem.Include(e => e.Questions)
                .Include(e => e.StudentResults)
                .Where(e => e.IsActive && e.UClass == studentClass)
                .ToListAsync();

            var examItems = new List<StudentExamItemVM>();

            foreach (var exam in exams)
            {
                var studentResult = exam.StudentResults.FirstOrDefault(r =>
                    r.StudentId == studentId
                );

                var examItem = new StudentExamItemVM
                {
                    Id = exam.Id,
                    Title = exam.Title,
                    Description = exam.Description,
                    Lesson = exam.Lesson,
                    ExamDate = exam.ExamDate,
                    Duration = exam.Duration,
                    QuestionCount = exam.Questions.Count,
                    TotalPoints = exam.Questions.Sum(q => q.Points),
                    IsCompleted = studentResult?.IsCompleted ?? false,
                    CanTake =
                        exam.ExamDate <= DateTime.Now
                        && exam.ExamDate.AddMinutes(exam.Duration) > DateTime.Now
                        && studentResult?.IsCompleted != true,
                    Score = studentResult?.Score,
                    CompletedAt = studentResult?.EndTime,
                };

                examItems.Add(examItem);
            }

            var model = new StudentExamSystemVM
            {
                Exams = examItems.OrderBy(e => e.ExamDate).ToList(),
            };

            return View(model);
        }

        [HttpGet]
        [RoleAuthorize("student")]
        public IActionResult TakeExam(int examId)
        {
            var studentId = HttpContext.Session.GetString("schoolnumber");

            // Öğrenci daha önce bu sınavı aldı mı kontrol et
            var existingResult = _context.StudentExamResults.FirstOrDefault(r =>
                r.ExamId == examId && r.StudentId == studentId
            );

            if (existingResult != null && existingResult.IsCompleted)
            {
                TempData["Error"] = "Bu sınavı zaten tamamladın! Bir daha alamazsın 😏";
                return RedirectToAction("ViewExamSystem");
            }

            var exam = _context
                .ExamSystem.Include(e => e.Questions)
                .FirstOrDefault(e => e.Id == examId);

            if (exam == null || !exam.IsActive)
            {
                TempData["Error"] = "Sınav bulunamadı veya aktif değil!";
                return RedirectToAction("ViewExamSystem");
            }

            // Sınav zamanı kontrolü
            if (DateTime.Now < exam.ExamDate)
            {
                TempData["Error"] = "Sınav henüz başlamadı! Sabırlı ol 😎";
                return RedirectToAction("ViewExamSystem");
            }

            var model = new TakeExamVM
            {
                ExamId = exam.Id,
                Title = exam.Title,
                Description = exam.Description,
                Lesson = exam.Lesson,
                Duration = exam.Duration,
                StartTime = DateTime.Now,
                Questions = exam
                    .Questions.OrderBy(q => q.QuestionOrder)
                    .Select(q => new ExamQuestionDisplayVM
                    {
                        Id = q.Id,
                        QuestionText = q.QuestionText,
                        OptionA = q.OptionA,
                        OptionB = q.OptionB,
                        OptionC = q.OptionC,
                        OptionD = q.OptionD,
                        QuestionOrder = q.QuestionOrder,
                    })
                    .ToList(),
            };

            // Eğer öğrenci sınavı başlatmışsa ama tamamlamamışsa devam ettir
            if (existingResult != null && !existingResult.IsCompleted)
            {
                model.StartTime = existingResult.StartTime;
                if (!string.IsNullOrEmpty(existingResult.Answers))
                {
                    model.StudentAnswers = System.Text.Json.JsonSerializer.Deserialize<
                        Dictionary<int, string>
                    >(existingResult.Answers);
                }
            }
            else
            {
                // Yeni sınav kaydı oluştur
                var newResult = new StudentExamResult
                {
                    ExamId = examId,
                    StudentId = studentId,
                    StartTime = DateTime.Now,
                    IsCompleted = false,
                    Answers = "{}",
                };

                _context.StudentExamResults.Add(newResult);
                _context.SaveChanges();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("student")]
        public async Task<IActionResult> SubmitExam(TakeExamVM model)
        {
            var studentId = HttpContext.Session.GetString("schoolnumber");

            var examResult = _context.StudentExamResults.FirstOrDefault(r =>
                r.ExamId == model.ExamId && r.StudentId == studentId && !r.IsCompleted
            );

            if (examResult == null)
            {
                TempData["Error"] = "Sınav kaydı bulunamadı!";
                return RedirectToAction("ViewExamSystem");
            }

            // Süre kontrolü - Bu çok önemli!
            var exam = _context
                .ExamSystem.Include(e => e.Questions)
                .FirstOrDefault(e => e.Id == model.ExamId);
            var timeElapsed = DateTime.Now - examResult.StartTime;

            if (timeElapsed.TotalMinutes > exam.Duration)
            {
                TempData["Warning"] = "Süre doldu! Sınav otomatik olarak teslim edildi 😅";
            }

            // Cevapları JSON olarak kaydet
            var answersJson = System.Text.Json.JsonSerializer.Serialize(model.StudentAnswers);

            // Puanı hesapla - Bu muhteşem bir algoritma!
            int totalScore = 0;
            foreach (var question in exam.Questions)
            {
                if (
                    model.StudentAnswers.ContainsKey(question.Id)
                    && model.StudentAnswers[question.Id] == question.CorrectAnswer
                )
                {
                    totalScore += question.Points;
                }
            }

            examResult.Answers = answersJson;
            examResult.Score = totalScore;
            examResult.EndTime = DateTime.Now;
            examResult.IsCompleted = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Sınav başarıyla teslim edildi! Puanın: {totalScore} 🎉";
            return RedirectToAction("ExamResult", new { examId = examResult.ExamId });
        }

        [HttpGet]
        [RoleAuthorize("student")]
        public IActionResult ExamResult(int examId)
        {
            var schoolNumber = HttpContext.Session.GetString("schoolnumber");
            if (string.IsNullOrEmpty(schoolNumber))
                return RedirectToAction("Login", "Auth");

            // Exam result'ı bul
            var result = _context
                .StudentExamResults.Include(r => r.Exam)
                .ThenInclude(e => e.Questions)
                .FirstOrDefault(r => r.StudentId == schoolNumber && r.ExamId == examId);

            Console.WriteLine("al bakalım:" + examId);
            if (result == null)
                return NotFound("Exam result not found!");

            return View(result);
        }
    }
}
