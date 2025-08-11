using Microsoft.AspNetCore.Mvc;
using dershane.Data;
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

        public IActionResult Index()
        {
            var students = _context.users
                                   .Where(u => u.role == "student")
                                   .ToList();

            return View(students);
        }
    }
}
