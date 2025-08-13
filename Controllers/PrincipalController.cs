using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using dershane.Data;
using dershane.Models;
using System;
using System.Linq;
using System.Collections.Generic;

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

            var classList = _context.Classes
                                    .Select(c => c.UClass)
                                    .Distinct()
                                    .ToList();

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
            }
            while (_context.users.Any(u => u.dershaneid == schoolNumber));

            user.dershaneid = schoolNumber;
            user.uclass = newClass;

            user.password = BCrypt.Net.BCrypt.HashPassword(user.password);

            _context.users.Add(user);
            _context.SaveChanges();

            string finalClass = !string.IsNullOrWhiteSpace(uclass) ? uclass.Trim() : newClass?.Trim();

            if (!string.IsNullOrEmpty(finalClass))
            {
                _context.Classes.Add(new UClass1
                {
                    UClass = finalClass,
                    Student = schoolNumber,
                    IsTeacher = user.role == "teacher"
                });
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

            var classes = _context.Classes
                .Select(c => c.UClass)
                .Distinct()
                .ToList();

            var counts = _context.Classes
                .Where(c => !c.IsTeacher)
                .GroupBy(c => c.UClass)
                .Select(g => new { ClassName = g.Key, StudentCount = g.Count() })
                .ToDictionary(x => x.ClassName, x => x.StudentCount);

            var teachers = _context.Classes
                .Where(c => c.IsTeacher)
                .Join(_context.users,
                      c => c.Student,
                      u => u.dershaneid,
                      (c, u) => new { c.UClass, TeacherName = u.firstname + " " + u.lastname })
                .GroupBy(x => x.UClass)
                .Select(g => new { ClassName = g.Key, TeacherName = g.First().TeacherName })
                .ToDictionary(x => x.ClassName, x => x.TeacherName);

            ViewBag.ClassStudentCount = counts;
            ViewBag.ClassTeachers = teachers;

            return View(classes);
        }
    }
}
