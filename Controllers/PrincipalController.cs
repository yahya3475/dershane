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

        // === Ana Sayfa ===
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

        // === Kullanıcı Ekle - GET ===
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

        // === Kullanıcı Ekle - POST ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(User user)
        {
            if (HttpContext.Session.GetString("role") != "principal")
                return Unauthorized();

            // Formdan uclass ve newClass değerlerini direkt alıyoruz
            string uclass = Request.Form["uclass"];
            string newClass = Request.Form["newClass"];

            // ModelState'deki uclass validasyonunu kaldırıyoruz, çünkü dışarıdan alıyoruz
            ModelState.Remove("uclass");

            if (!ModelState.IsValid)
            {
                // Listeyi tekrar yükle
                ViewBag.Classes = _context.Classes.Select(c => c.UClass).Distinct().ToList();
                return View(user);
            }

            // Okul numarası üret
            Random rnd = new Random();
            string schoolNumber;
            int attempts = 0;
            do
            {
                if (++attempts > 10)
                    return BadRequest("Unique okul numarası üretilemedi.");
                schoolNumber = rnd.Next(1000, 9999).ToString();
            }
            while (_context.users.Any(u => u.dershaneid == schoolNumber));

            user.dershaneid = schoolNumber;
            user.uclass = newClass;

            user.password = BCrypt.Net.BCrypt.HashPassword(user.password);

            _context.users.Add(user);
            _context.SaveChanges();

            // Hangi sınıf seçilmiş veya girilmiş ona karar ver
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

            TempData["Success"] = $"Kullanıcı başarıyla eklendi. Okul No: {schoolNumber}";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult List(string role)
        {
            if (HttpContext.Session.GetString("role") != "principal")
                return Unauthorized();

            // Başlangıçta tüm kullanıcılar
            var users = _context.users.AsQueryable();

            // role parametresi varsa filtre uygula
            if (!string.IsNullOrWhiteSpace(role))
            {
                role = role.Trim().ToLower();
                users = users.Where(u => u.role.ToLower() == role);
            }

            // View'a liste gönder
            return View(users.ToList());
        }


    }
}
