using System.Linq;
using dershane.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
            var user = _context.users.FirstOrDefault(u => u.dershaneid == schoolnumber);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.password))
            {
                HttpContext.Session.SetString("schoolnumber", user.dershaneid);
                HttpContext.Session.SetString("uclass", user.uclass);
                HttpContext.Session.SetString("fullname", user.firstname + " " + user.lastname);
                HttpContext.Session.SetString("role", user.role);

                Console.WriteLine(
                    $"User logged in: {user.firstname} {user.lastname}, Role: {user.role}"
                );

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
                if (BCrypt.Net.BCrypt.Verify(password, user.password))
                {
                    ViewBag.Hata = "New password cannot be the same as the current one";
                    return View();
                }
                else
                {
                    // Hash and save the new password
                    user.password = BCrypt.Net.BCrypt.HashPassword(password);
                    user.firstlogin = false;
                    _context.SaveChanges();

                    Console.WriteLine($"New hash: {user.password}");

                    // Redirect based on user role
                    return RedirectToAction(
                        "Index",
                        user.role switch
                        {
                            "principal" => "Principal",
                            "teacher" => "Teacher",
                            _ => "Home",
                        }
                    );
                }
            }

            ViewBag.Hata = "User not found";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }
    }
}
