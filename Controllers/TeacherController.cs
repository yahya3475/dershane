using Microsoft.AspNetCore.Mvc;
using dershane.Data;
using dershane.Models;
using System.Collections.Generic;
using System.Linq;

namespace dershane.Controllers
{
    public class TeacherController : Controller
    {
        private readonly AppDbContext _context;

        public TeacherController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string? uclass)
        {
            var schoolNumber = HttpContext.Session.GetString("schoolnumber");

            var classList = _context.Classes
                                    .Where(u => u.Student == schoolNumber && u.IsTeacher)
                                    .Select(c => c.UClass)
                                    .Distinct()
                                    .ToList();

            ViewBag.Classes = classList;
            ViewBag.SelectedClass = uclass;

            List<StudentWithClass> students = new();

            if (!string.IsNullOrEmpty(uclass))
            {
                students = (from cls in _context.Classes
                            join usr in _context.users on cls.Student equals usr.dershaneid
                            where cls.UClass == uclass && !cls.IsTeacher
                            select new StudentWithClass
                            {
                                Id = usr.userid,
                                FullName = usr.firstname + " " + usr.lastname,
                                SchoolNumber = usr.dershaneid,
                                ClassName = cls.UClass
                            }).ToList();
            }

            return View(students);
        }

    }
}
