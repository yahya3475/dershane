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

            var model = new SubmitHomeworkVM
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
        public async Task<IActionResult> SubmitHomework(SubmitHomeworkVM model)
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
    }
}
