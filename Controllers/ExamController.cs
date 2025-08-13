using Microsoft.AspNetCore.Mvc;
using dershane.Data;
using Microsoft.AspNetCore.Http;
using System.Linq;
using dershane.Models;

namespace dershane.Controllers
{
    public class ExamController : Controller
    {
        private readonly AppDbContext _context;
        public ExamController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var schoolnumber = HttpContext.Session.GetString("schoolnumber");

            if (string.IsNullOrEmpty(schoolnumber))
                return RedirectToAction("Login", "Auth");

            var Exams = _context.notes
                .Where(n => n.schoolnumber == schoolnumber)
                .ToList();

            return View(Exams);
        }
    }
}
