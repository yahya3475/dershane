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

            // DashboardVM oluştur
            var dashboardVM = new DashboardVM
            {
                Role = role?.ToUpper(),
                UserId = schoolnumber,
                UserName = $"{firstname} {lastname}",
                RecentActivities = await GetRecentActivities(role?.ToUpper(), schoolnumber),
                DailySchedule = await GetDailySchedule(role?.ToUpper(), schoolnumber)
            };

            // Role'e göre istatistikleri hesapla
            switch (role?.ToUpper())
            {
                case "STUDENT":
                    await LoadStudentStats(dashboardVM, schoolnumber);
                    break;
                case "TEACHER":
                    await LoadTeacherStats(dashboardVM, schoolnumber);
                    break;
                case "PRİNCİPAL":
                    await LoadPrincipalStats(dashboardVM);
                    break;
            }

            return View(dashboardVM);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard yüklenirken hata oluştu");
            TempData["Error"] = "Dashboard yüklenirken bir hata oluştu.";

            // Hata durumunda varsayılan model oluştur
            var defaultModel = new DashboardVM
            {
                Role = "STUDENT",
                UserName = "Kullanıcı",
                RecentActivities = new List<ActivityVM>(),
                DailySchedule = new List<ScheduleItemVM>()
            };

            return View(defaultModel);
        }
    }

    private async Task<List<ActivityVM>> GetRecentActivities(string role, string userId)
    {
        var activities = new List<ActivityVM>();
        
        try
        {
            // Kullanıcı rolüne göre gerçek aktiviteleri getir
            if (role == "STUDENT")
            {
                // Sınav sonuçları
                var examResults = await _context.notes
                    .Where(n => n.schoolnumber == userId)
                    .OrderByDescending(n => n.points) // Order by points instead of id
                    .Take(2)
                    .ToListAsync();
                
                foreach (var result in examResults)
                {
                    activities.Add(new ActivityVM
                    {
                        Title = "Sınav Sonucu",
                        Description = $"{result.lesson} sınavından {result.points}/100 aldınız.",
                        Timestamp = DateTime.Now.AddDays(-1), // Gerçek tarih yok, varsayılan kullan
                        TimeAgo = "1 gün önce",
                        IconClass = "bi-clipboard-check",
                        ColorClass = "text-success"
                    });
                }
                
                // Devamsızlık kayıtları
                var attendances = await _context.Attendances
                    .Where(a => a.StudentId == userId)
                    .OrderByDescending(a => a.Date)
                    .Take(2)
                    .ToListAsync();
                
                foreach (var attendance in attendances)
                {
                    activities.Add(new ActivityVM
                    {
                        Title = "Devam Durumu",
                        Description = attendance.IsPresent ? 
                            $"{attendance.Lesson} dersine katıldınız." : 
                            $"{attendance.Lesson} dersine katılmadınız.",
                        Timestamp = attendance.Date,
                        TimeAgo = GetTimeAgo(attendance.Date),
                        IconClass = attendance.IsPresent ? "bi-person-check" : "bi-person-x",
                        ColorClass = attendance.IsPresent ? "text-success" : "text-danger"
                    });
                }
            }
            else if (role == "TEACHER")
            {
                // Öğretmenin sınıfını bul
                var teacherClass = await _context
                    .Classes.Where(c => c.Student == userId && c.IsTeacher)
                    .Select(c => c.UClass)
                    .FirstOrDefaultAsync();
                
                if (teacherClass != null)
                {
                    // Sınıftaki öğrenci sayısı
                    var studentCount = await _context.Classes.CountAsync(c =>
                        c.UClass == teacherClass && !c.IsTeacher
                    );
                    
                    activities.Add(new ActivityVM
                    {
                        Title = "Sınıf Bilgisi",
                        Description = $"{teacherClass} sınıfında {studentCount} öğrenci bulunmaktadır.",
                        Timestamp = DateTime.Now.AddDays(-1),
                        TimeAgo = "1 gün önce",
                        IconClass = "bi-people",
                        ColorClass = "text-primary"
                    });
                }
                
                // Devamsızlık kayıtları
                var attendances = await _context.Attendances
                    .Where(a => a.TeacherId == userId)
                    .OrderByDescending(a => a.Date)
                    .Take(2)
                    .ToListAsync();
                
                foreach (var attendance in attendances)
                {
                    activities.Add(new ActivityVM
                    {
                        Title = "Yoklama Alma",
                        Description = $"{attendance.Lesson} dersi için yoklama aldınız.",
                        Timestamp = attendance.Date,
                        TimeAgo = GetTimeAgo(attendance.Date),
                        IconClass = "bi-list-check",
                        ColorClass = "text-info"
                    });
                }
            }
            else if (role == "PRİNCİPAL")
            {
                // Son eklenen kullanıcılar
                var users = await _context.users
                    .OrderByDescending(u => u.userid)
                    .Take(2)
                    .ToListAsync();
                
                foreach (var user in users)
                {
                    activities.Add(new ActivityVM
                    {
                        Title = "Kullanıcı Ekleme",
                        Description = $"{user.firstname} {user.lastname} kullanıcısı sisteme eklendi.",
                        Timestamp = DateTime.Now.AddDays(-1), // Gerçek tarih yok, varsayılan kullan
                        TimeAgo = "1 gün önce",
                        IconClass = "bi-person-plus",
                        ColorClass = "text-primary"
                    });
                }
                
                // Sınıf sayısı
                var classCount = await _context.Classes
                    .Select(c => c.UClass)
                    .Distinct()
                    .CountAsync();
                
                activities.Add(new ActivityVM
                {
                    Title = "Sınıf Bilgisi",
                    Description = $"Toplam {classCount} aktif sınıf bulunmaktadır.",
                    Timestamp = DateTime.Now.AddDays(-2),
                    TimeAgo = "2 gün önce",
                    IconClass = "bi-building",
                    ColorClass = "text-success"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktiviteler yüklenirken hata oluştu");
        }
        
        // Yeterli aktivite yoksa varsayılan ekle
        if (activities.Count < 5)
        {
            var placeholders = new List<ActivityVM>
            {
                new ActivityVM
                {
                    Title = "Sistem Güncellemesi",
                    Description = "Sistem başarıyla güncellendi.",
                    Timestamp = DateTime.Now.AddHours(-3),
                    TimeAgo = "3 saat önce",
                    IconClass = "bi-gear",
                    ColorClass = "text-info"
                },
                new ActivityVM
                {
                    Title = "Yeni Duyuru",
                    Description = "Yeni dönem kayıtları başlamıştır.",
                    Timestamp = DateTime.Now.AddDays(-1),
                    TimeAgo = "1 gün önce",
                    IconClass = "bi-megaphone",
                    ColorClass = "text-warning"
                },
                new ActivityVM
                {
                    Title = "Bakım Çalışması",
                    Description = "Sistem bakımı tamamlandı.",
                    Timestamp = DateTime.Now.AddDays(-2),
                    TimeAgo = "2 gün önce",
                    IconClass = "bi-tools",
                    ColorClass = "text-secondary"
                }
            };
            
            // 5 aktivite olana kadar ekle
            foreach (var placeholder in placeholders)
            {
                if (activities.Count < 5)
                {
                    activities.Add(placeholder);
                }
                else
                {
                    break;
                }
            }
        }
        
        // Tarihe göre sırala (en yeni en üstte) ve sadece 5 tanesini al
        return activities
            .OrderByDescending(a => a.Timestamp)
            .Take(5)
            .ToList();
    }

    private async Task<List<ScheduleItemVM>> GetDailySchedule(string role, string userId)
    {
        var schedule = new List<ScheduleItemVM>();
        var today = DateTime.Now.DayOfWeek.ToString();
        
        try
        {
            // Kullanıcı rolüne göre gerçek programı getir
            if (role == "STUDENT" || role == "TEACHER")
            {
                // Öğrenci için sınıfı bul
                string classId = null;
                if (role == "STUDENT")
                {
                    var student = await _context.users.FirstOrDefaultAsync(u => u.dershaneid == userId);
                    if (student != null)
                    {
                        classId = student.uclass;
                    }
                }
                
                // Bugünkü programı getir
                var dayOfWeek = (int)DateTime.Now.DayOfWeek; // Convert to int (0-6)
                var schedules = await _context.Schedules
                    .Where(s => s.Day == dayOfWeek && (role == "TEACHER" ? s.TeacherId == userId : s.UClass == classId))
                    .OrderBy(s => s.StartTime)
                    .ToListAsync();
                
                foreach (var item in schedules)
                {
                    // Parse time string to TimeSpan
                    if (TimeSpan.TryParse(item.StartTime, out var timeSpan))
                    {
                        schedule.Add(new ScheduleItemVM
                        {
                            Title = item.Lesson,
                            Location = item.UClass, // Use UClass instead of ClassId
                            Time = timeSpan, // Use parsed TimeSpan
                            TimeDisplay = FormatTime(timeSpan), // Pass TimeSpan to FormatTime
                            ColorClass = GetColorForLesson(item.Lesson)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Program yüklenirken hata oluştu");
        }
        
        // Yeterli program yoksa varsayılan ekle
        if (schedule.Count < 5)
        {
            var placeholders = new List<ScheduleItemVM>
            {
                new ScheduleItemVM
                {
                    Title = "Matematik",
                    Location = "A-101",
                    Time = new TimeSpan(9, 0, 0),
                    TimeDisplay = "09:00",
                    ColorClass = "bg-primary"
                },
                new ScheduleItemVM
                {
                    Title = "Fizik",
                    Location = "B-203",
                    Time = new TimeSpan(10, 30, 0),
                    TimeDisplay = "10:30",
                    ColorClass = "bg-success"
                },
                new ScheduleItemVM
                {
                    Title = "Türkçe",
                    Location = "C-105",
                    Time = new TimeSpan(12, 0, 0),
                    TimeDisplay = "12:00",
                    ColorClass = "bg-warning"
                },
                new ScheduleItemVM
                {
                    Title = "Tarih",
                    Location = "D-302",
                    Time = new TimeSpan(14, 30, 0),
                    TimeDisplay = "14:30",
                    ColorClass = "bg-danger"
                },
                new ScheduleItemVM
                {
                    Title = "İngilizce",
                    Location = "E-201",
                    Time = new TimeSpan(16, 0, 0),
                    TimeDisplay = "16:00",
                    ColorClass = "bg-info"
                }
            };
            
            // 5 program olana kadar ekle
            foreach (var placeholder in placeholders)
            {
                if (schedule.Count < 5)
                {
                    schedule.Add(placeholder);
                }
                else
                {
                    break;
                }
            }
        }
        
        // Saate göre sırala ve sadece 5 tanesini al
        return schedule
            .OrderBy(s => s.Time)
            .Take(5)
            .ToList();
    }

    private string GetTimeAgo(DateTime timestamp)
    {
        var timeSpan = DateTime.Now - timestamp;
        
        if (timeSpan.TotalMinutes < 1)
            return "şimdi";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} dakika önce";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} saat önce";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} gün önce";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} hafta önce";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} ay önce";
        
        return $"{(int)(timeSpan.TotalDays / 365)} yıl önce";
    }

    private string FormatTime(TimeSpan time)
    {
        return $"{time.Hours:D2}:{time.Minutes:D2}";
    }

    private string GetColorForLesson(string lesson)
    {
        // Ders konusuna göre renk ata
        if (lesson.Contains("Matematik") || lesson.Contains("Geometri"))
            return "bg-primary";
        if (lesson.Contains("Fizik") || lesson.Contains("Kimya") || lesson.Contains("Biyoloji"))
            return "bg-success";
        if (lesson.Contains("Türkçe") || lesson.Contains("Edebiyat"))
            return "bg-warning";
        if (lesson.Contains("Tarih") || lesson.Contains("Coğrafya") || lesson.Contains("Sosyal"))
            return "bg-danger";
        if (lesson.Contains("İngilizce") || lesson.Contains("Almanca") || lesson.Contains("Fransızca"))
            return "bg-info";
        if (lesson.Contains("Resim") || lesson.Contains("Müzik") || lesson.Contains("Sanat"))
            return "bg-pink";
        
        // Varsayılan renk
        return "bg-secondary";
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

    [SessionAuthorizer]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var schoolnumber = HttpContext.Session.GetString("schoolnumber");
        if (string.IsNullOrEmpty(schoolnumber))
        {
            return RedirectToAction("Login", "Auth");
        }

        // Kullanıcı bilgilerini al
        var user = await _context.users.FirstOrDefaultAsync(u => u.dershaneid == schoolnumber);
        if (user == null)
        {
            return NotFound();
        }

        // Kullanıcı ek bilgilerini al
        var userInfo = await _context.user_informations.FirstOrDefaultAsync(ui => ui.dershaneid == schoolnumber);

        // View model oluştur
        var model = new EditUserInfoVM
        {
            DershaneId = user.dershaneid,
            FirstName = user.firstname,
            LastName = user.lastname,
            Role = user.role,
            Class = user.uclass,
            Email = userInfo?.email,
            PhoneNumber = userInfo?.phone_number,
            Address = userInfo?.address,
            Parent = userInfo?.parent,
            ParentPhoneNumber = userInfo?.parent_phone_number
        };

        return View(model);
    }

    [SessionAuthorizer]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(EditUserInfoVM model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var schoolnumber = HttpContext.Session.GetString("schoolnumber");
        if (string.IsNullOrEmpty(schoolnumber) || schoolnumber != model.DershaneId)
        {
            return Unauthorized();
        }

        // Kullanıcı bilgilerini güncelle
        var user = await _context.users.FirstOrDefaultAsync(u => u.dershaneid == schoolnumber);
        if (user == null)
        {
            return NotFound();
        }

        // Kullanıcı ek bilgilerini güncelle
        var userInfo = await _context.user_informations.FirstOrDefaultAsync(ui => ui.dershaneid == schoolnumber);
        if (userInfo == null)
        {
            // Eğer kullanıcı ek bilgileri yoksa yeni oluştur
            userInfo = new UserInformation
            {
                dershaneid = schoolnumber,
                created_at = DateTime.Now,
                email = model.Email,
                phone_number = model.PhoneNumber
            };
            _context.user_informations.Add(userInfo);
        }

        // Sadece adres ve veli bilgilerini güncelle
        userInfo.address = model.Address;
        userInfo.parent = model.Parent;
        userInfo.parent_phone_number = model.ParentPhoneNumber;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Profil bilgileriniz başarıyla güncellendi.";
        return RedirectToAction(nameof(Profile));
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
