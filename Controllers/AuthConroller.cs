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
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password, string role)
        {
            var user = _context.users.FirstOrDefault(u => u.username == username && u.password == password && u.role == role);
            Console.WriteLine("Seçilen rol: " + role);

            if (user != null)
            {
                HttpContext.Session.SetString("username", user.username);
                HttpContext.Session.SetString("role", user.role);

                if (user.role == "admin")
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