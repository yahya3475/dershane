using System.Diagnostics;
using dershane.Data;
using dershane.Filters;
using dershane.Models;
using dershane.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace dershane.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context; // Bu satırı eklemeyi unutmuşsun amk!
    }

    [SessionAuthorizer]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            // Session'dan kullanıcı bilgilerini al
            var schoolnumber = HttpContext.Session.GetString("schoolnumber");
            var role = HttpContext.Session.GetString("role");
            var firstname = HttpContext.Session.GetString("firstname");
            var lastname = HttpContext.Session.GetString("lastname");

            if (string.IsNullOrEmpty(schoolnumber))
            {
                return RedirectToAction("Index", "Auth");
            }

            // ViewBag'leri set et - BU ÇOK ÖNEMLİ!
            ViewBag.username = $"{firstname} {lastname}";
            ViewBag.role = role?.ToUpper();

            // Role'e göre istatistikleri hesapla
            switch (role?.ToUpper())
            {
                case "STUDENT":
                    await SetStudentStats(schoolnumber);
                    break;
                case "TEACHER":
                    await SetTeacherStats(schoolnumber);
                    break;
                case "PRİNCİPAL":
                    await SetPrincipalStats();
                    break;
            }

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard yüklenirken hata oluştu");
            TempData["Error"] = "Dashboard yüklenirken bir hata oluştu.";

            // Hata durumunda bile ViewBag'leri set et
            ViewBag.username = "Kullanıcı";
            ViewBag.role = "STUDENT";

            return View();
        }
    }

    private async Task SetStudentStats(string schoolnumber)
    {
        try
        {
            // Öğrenci sınav sonuçları
            var examResults = await _context
                .notes.Where(n => n.schoolnumber == schoolnumber)
                .ToListAsync();

            // Devamsızlık bilgileri
            var attendances = await _context
                .Attendances.Where(a => a.StudentId == schoolnumber)
                .ToListAsync();

            // İstatistikleri hesapla
            var averageGrade = examResults.Any() ? examResults.Average(er => er.points) : 0;
            var attendanceRate = attendances.Any()
                ? (attendances.Count(a => a.IsPresent) * 100.0 / attendances.Count)
                : 100;

            ViewBag.AverageGrade = Math.Round(averageGrade, 0);
            ViewBag.AttendanceRate = Math.Round(attendanceRate, 0);
            ViewBag.PendingHomeworks = 5; // Sabit değer - homework tablosu yok
            ViewBag.TodayClasses = 6; // Sabit değer
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Öğrenci istatistikleri hesaplanırken hata");
            // Varsayılan değerler
            ViewBag.AverageGrade = 85;
            ViewBag.AttendanceRate = 92;
            ViewBag.PendingHomeworks = 5;
            ViewBag.TodayClasses = 6;
        }
    }

    private async Task SetTeacherStats(string schoolnumber)
    {
        try
        {
            // Öğretmenin sınıfını bul
            var teacherClass = await _context
                .Classes.Where(c => c.Student == schoolnumber && c.IsTeacher)
                .FirstOrDefaultAsync();

            var studentCount = 0;
            if (teacherClass != null)
            {
                studentCount = await _context
                    .Classes.Where(c => c.UClass == teacherClass.UClass && !c.IsTeacher)
                    .CountAsync();
            }

            // Sınav sayısı
            var examCount = await _context
                .notes.Where(n => n.lesson != null) // Öğretmenin verdiği dersler
                .Select(n => n.lesson)
                .Distinct()
                .CountAsync();

            ViewBag.StudentCount = studentCount;
            ViewBag.ExamCount = examCount;
            ViewBag.ActiveHomeworks = 8; // Sabit değer
            ViewBag.WeeklyClasses = 24; // Sabit değer
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Öğretmen istatistikleri hesaplanırken hata");
            // Varsayılan değerler
            ViewBag.StudentCount = 32;
            ViewBag.ExamCount = 12;
            ViewBag.ActiveHomeworks = 8;
            ViewBag.WeeklyClasses = 24;
        }
    }

    private async Task SetPrincipalStats()
    {
        try
        {
            // Müdür istatistikleri
            var totalUsers = await _context.users.CountAsync();
            var activeClasses = await _context
                .Classes.Select(c => c.UClass)
                .Distinct()
                .CountAsync();
            var teacherCount = await _context.users.Where(u => u.role == "teacher").CountAsync();
            var totalPrograms = await _context.Schedules.CountAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.ActiveClasses = activeClasses;
            ViewBag.TeacherCount = teacherCount;
            ViewBag.TotalPrograms = totalPrograms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Müdür istatistikleri hesaplanırken hata");
            // Varsayılan değerler
            ViewBag.TotalUsers = 156;
            ViewBag.ActiveClasses = 8;
            ViewBag.TeacherCount = 15;
            ViewBag.TotalPrograms = 45;
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new dershane.ViewModels.ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            }
        );
    }

    [Route("Error/{statusCode}")]
    public IActionResult Error(int statusCode)
    {
        if (statusCode == 404)
        {
            return View("NotFound");
        }

        return View(
            new dershane.ViewModels.ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            }
        );
    }

    private async Task LoadStudentStats(DashboardVM model, string schoolnumber)
    {
        // Öğrenci istatistikleri
        var examResults = await _context
            .StudentExamResults.Where(r => r.StudentId == schoolnumber && r.IsCompleted)
            .ToListAsync();

        model.StudentStats = new StudentStatsVM
        {
            AverageGrade = examResults.Any() ? examResults.Average(r => r.Score) : 0,
            TotalExams = examResults.Count,
            AttendanceRate = await CalculateAttendanceRate(schoolnumber),
            PendingHomeworks = await _context.HomeworkSubmissions.CountAsync(s =>
                s.StudentId == schoolnumber && !s.Grade.HasValue
            ),
        };
    }

    private async Task LoadTeacherStats(DashboardVM model, string schoolnumber)
    {
        // Öğretmen sınıfını bul
        var teacherClass = await _context
            .Classes.Where(c => c.Student == schoolnumber && c.IsTeacher)
            .Select(c => c.UClass)
            .FirstOrDefaultAsync();

        if (teacherClass != null)
        {
            var studentCount = await _context.Classes.CountAsync(c =>
                c.UClass == teacherClass && !c.IsTeacher
            );

            model.TeacherStats = new TeacherStatsVM
            {
                TotalStudents = studentCount,
                TotalExams = await _context.ExamSystem.CountAsync(e => e.TeacherId == schoolnumber),
                ActiveHomeworks = await _context.Homeworks.CountAsync(h =>
                    h.TeacherId == schoolnumber
                ),
                WeeklyLessons = 24, // Sabit değer, gerçek veri için schedule tablosundan alınabilir
            };
        }
    }

    private async Task LoadPrincipalStats(DashboardVM model)
    {
        model.PrincipalStats = new PrincipalStatsVM
        {
            TotalUsers = await _context.users.CountAsync(),
            TotalClasses = await _context.Classes.Select(c => c.UClass).Distinct().CountAsync(),
            TotalTeachers = await _context.users.CountAsync(u => u.role == "teacher"),
            TotalSchedules = 45, // Sabit değer, gerçek veri için schedule tablosundan alınabilir
        };
    }

    private async Task<double> CalculateAttendanceRate(string schoolnumber)
    {
        var totalAttendances = await _context.Attendances.CountAsync(a =>
            a.StudentId == schoolnumber
        );

        if (totalAttendances == 0)
            return 100;

        var presentAttendances = await _context.Attendances.CountAsync(a =>
            a.StudentId == schoolnumber && a.IsPresent
        );

        return (double)presentAttendances / totalAttendances * 100;
    }
}
