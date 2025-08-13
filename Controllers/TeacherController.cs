using Microsoft.AspNetCore.Mvc;
using dershane.Data;
using dershane.Models;
using System.Collections.Generic;
using System.Linq;
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

            var students = _context.users
                                   .Where(u => u.role == "student")
                                   .Select(u => new SelectListItem
                                   {
                                       Value = u.dershaneid,
                                       Text = $"{u.firstname} {u.lastname} ({u.dershaneid})"
                                   }).ToList();

            var vm = new AddExamVM
            {
                Students = students
            };

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
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage)
                                              .ToList();
                foreach (var err in errors)
                    Console.WriteLine(err); // Hangi alan hatalı bakmak için

                vm.Students = _context.users
                    .Where(u => u.role == "student")
                    .Select(u => new SelectListItem
                    {
                        Value = u.dershaneid,
                        Text = $"{u.firstname} {u.lastname} ({u.dershaneid})"
                    }).ToList();

                return View(vm);
            }

            var exam = new Exams
            {
                schoolnumber = vm.SchoolNumber,
                lesson = vm.Lesson,
                points = vm.Points
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

            // Oturumdan öğretmenin sınıfını al
            string teacherClass = HttpContext.Session.GetString("uclass");
            if (string.IsNullOrEmpty(teacherClass))
                return Content("Öğretmenin sınıf bilgisi bulunamadı.");

            var students = _context.users
                                   .Where(u => u.role == "student" && u.uclass == teacherClass)
                                   .Select(u => new StudentVM
                                   {
                                       DershaneId = u.dershaneid,
                                       FirstName = u.firstname,
                                       LastName = u.lastname,
                                       UClass = u.uclass
                                   })
                                   .ToList();

            return View(students);
        }

    }
}
