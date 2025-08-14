using System.Collections.Generic;
using System.Linq;
using dershane.Data;
using dershane.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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

            var students = _context
                .users.Where(u => u.role == "student")
                .Select(u => new SelectListItem
                {
                    Value = u.dershaneid,
                    Text = $"{u.firstname} {u.lastname} ({u.dershaneid})",
                })
                .ToList();

            var vm = new AddExamVM { Students = students };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddExam(AddExamVM vm)
        {
            if (HttpContext.Session.GetString("role") != "teacher")
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                foreach (var err in errors)
                    Console.WriteLine(err);

                vm.Students = _context
                    .users.Where(u => u.role == "student")
                    .Select(u => new SelectListItem
                    {
                        Value = u.dershaneid,
                        Text = $"{u.firstname} {u.lastname} ({u.dershaneid})",
                    })
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

            string teacherClass = HttpContext.Session.GetString("uclass");
            if (string.IsNullOrEmpty(teacherClass))
                return Content("Öğretmenin sınıf bilgisi bulunamadı.");

            var students = _context
                .users.Where(u => u.role == "student" && u.uclass == teacherClass)
                .Select(u => new StudentVM
                {
                    DershaneId = u.dershaneid,
                    FirstName = u.firstname,
                    LastName = u.lastname,
                    UClass = u.uclass,
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
    }
}
