using System;
using System.Collections.Generic;
using System.Linq;
using dershane.Data;
using dershane.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("role") != "principal")
                return Unauthorized();

            ViewBag.StudentCount = _context.users.Count(u => u.role == "student");
            ViewBag.TeacherCount = _context.users.Count(u => u.role == "teacher");
            ViewBag.ClassCount = _context.Classes.Select(c => c.UClass).Distinct().Count();
            ViewBag.username = HttpContext.Session.GetString("fullname");

            return View();
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

                var viewModel = new EditClassViewModel
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
        public IActionResult EditClass(EditClassViewModel model)
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
    }
}
