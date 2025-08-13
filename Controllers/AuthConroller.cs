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

                Console.WriteLine($"User logged in: {user.firstname} {user.lastname}, Role: {user.role}");

                if (user.firstlogin)
                {
                    return RedirectToAction("FirstLogin");
                }

                if (user.role == "principal")
                {
                    return RedirectToAction("Index", "Principal");
                }
                else if (user.role == "teacher")
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
                ViewBag.Hata = "Wrong school number or password!";
                return View();
            }
        }
        [HttpGet]
        public IActionResult FirstLogin()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("schoolnumber")))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
        [HttpPost]
        public IActionResult FirstLogin(string password)
        {
            var schoolnumber = HttpContext.Session.GetString("schoolnumber");
            var user = _context.users.FirstOrDefault(u => u.dershaneid == schoolnumber);

            if (user != null)
            {
                user.password = password;
                user.firstlogin = false;
                _context.SaveChanges();

                if (user.role == "principal")
                {
                    return RedirectToAction("Index", "Principal");
                }
                else if (user.role == "teacher")
                {
                    return RedirectToAction("Index", "Teacher");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Hata = "User not found!";
            return View();
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }
    }
}