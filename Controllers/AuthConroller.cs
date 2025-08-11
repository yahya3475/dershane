using Microsoft.AspNetCore.Mvc;
using dershane.Data;
using System.Linq;
using Microsoft.AspNetCore.Http; 

namespace dershane.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        public AuthController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("schoolnumber")))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Login(string schoolnumber, string password)
        {
            var user = _context.users.FirstOrDefault(u => u.dershaneid == schoolnumber && u.password == password);

            if (user != null)
            {
                HttpContext.Session.SetString("schoolnumber", user.dershaneid);
                HttpContext.Session.SetString("fullname", user.firstname + " " + user.lastname);
                HttpContext.Session.SetString("role", user.role);

                Console.WriteLine(user.firstname + "asdasd");
                if (user.role == "teacher")
                {
                    return RedirectToAction("Index", "Teacher");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                ViewBag.Hata = "Wrong username or password!";
                return View();
            }
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }
    }
}