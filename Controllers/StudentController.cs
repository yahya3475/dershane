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
            var homework = await _context.Homeworks.FindAsync(id);
            if (homework == null)
            {
                TempData["Error"] = "Assignment not found!";
                return RedirectToAction("ViewHomeworks");
            }

            var studentId = HttpContext.Session.GetString("schoolnumber");
            var existingSubmission = await _context.HomeworkSubmissions.FirstOrDefaultAsync(s =>
                s.HomeworkId == id && s.StudentId == studentId
            );

            if (existingSubmission != null)
            {
                TempData["Error"] = "You already turned in this assignment!";
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

            ViewData["DebugModel"] = model;
            ViewBag.DebugHomework = homework;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("student")]
        public async Task<IActionResult> SubmitHomework(dershane.Models.SubmitHomeworkVM model)
        {
            ModelState.Remove("Title");
            ModelState.Remove("Description");
            ModelState.Remove("Lesson");
            ModelState.Remove("DueDate");
            ModelState.Remove("Grade");
            ModelState.Remove("TeacherComment");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState INVALID!");
                foreach (var error in ModelState)
                {
                    Console.WriteLine(
                        $"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}"
                    );
                }

                var homework = await _context.Homeworks.FindAsync(model.HomeworkId);
                if (homework != null)
                {
                    model.Title = homework.Title;
                    model.Description = homework.Description;
                    model.Lesson = homework.Lesson;
                    model.DueDate = homework.DueDate;
                }
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

            TempData["Success"] = "Assignment successfully submitted!";
            return RedirectToAction("ViewHomeworks");
        }

        [RoleAuthorize("student")]
        [RoleAuthorize("student")]
        public async Task<IActionResult> ViewSubmission(int id)
        {
            var studentId = HttpContext.Session.GetString("schoolnumber");

            var submission = await _context
                .HomeworkSubmissions.Include(s => s.Homework)
                .FirstOrDefaultAsync(s => s.HomeworkId == id && s.StudentId == studentId);

            if (submission == null)
            {
                TempData["Error"] = "Submitted assignment not found!";
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
                TeacherComment = submission.TeacherComment ?? string.Empty,
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

            var studentClass = await _context
                .Classes.Where(c => c.Student == studentId && !c.IsTeacher)
                .Select(c => c.UClass)
                .FirstOrDefaultAsync();

            if (studentClass == null)
            {
                TempData["Error"] = "No class information found!";
                return View(new StudentExamSystemVM());
            }

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

            var existingResult = _context.StudentExamResults.FirstOrDefault(r =>
                r.ExamId == examId && r.StudentId == studentId
            );

            if (existingResult != null && existingResult.IsCompleted)
            {
                TempData["Error"] = "You have already completed this test! You can't take it again";
                return RedirectToAction("ViewExamSystem");
            }

            var exam = _context
                .ExamSystem.Include(e => e.Questions)
                .FirstOrDefault(e => e.Id == examId);

            if (exam == null || !exam.IsActive)
            {
                TempData["Error"] = "Exam not found or not active!";
                return RedirectToAction("ViewExamSystem");
            }

            if (DateTime.Now < exam.ExamDate)
            {
                TempData["Error"] = "The exam hasn't started yet! Be patient";
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
                TempData["Error"] = "No exam record found!";
                return RedirectToAction("ViewExamSystem");
            }

            var exam = _context
                .ExamSystem.Include(e => e.Questions)
                .FirstOrDefault(e => e.Id == model.ExamId);
            var timeElapsed = DateTime.Now - examResult.StartTime;

            if (timeElapsed.TotalMinutes > exam.Duration)
            {
                TempData["Warning"] = "Time is up! Exam automatically delivered";
            }

            var answersJson = System.Text.Json.JsonSerializer.Serialize(model.StudentAnswers);

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

            TempData["Success"] = $"Exam successfully delivered! Your score : {totalScore} 🎉";
            return RedirectToAction("ExamResult", new { examId = examResult.ExamId });
        }

        [HttpGet]
        [RoleAuthorize("student")]
        public IActionResult ExamResult(int examId)
        {
            var schoolNumber = HttpContext.Session.GetString("schoolnumber");
            if (string.IsNullOrEmpty(schoolNumber))
                return RedirectToAction("Login", "Auth");

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
