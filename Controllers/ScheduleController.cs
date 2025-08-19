using System.Linq;
using dershane.Data;
using dershane.Filters;
using dershane.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace dershane.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly AppDbContext _context;

        public ScheduleController(AppDbContext context)
        {
            _context = context;
        }

        [RoleAuthorize("teacher")]
        public async Task<IActionResult> Index()
        {
            var schedules = await _context.Schedules.ToListAsync();
            return View(schedules);
        }

        [RoleAuthorize("teacher")]
        public async Task<IActionResult> Create()
        {
            await PrepareViewBagForSchedule();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("teacher")]
        public async Task<IActionResult> Create(
            [Bind("Lesson,UClass,Day,StartTime,EndTime")] Schedule schedule
        )
        {
            var teacherId = HttpContext.Session.GetString("schoolnumber");

            schedule.TeacherId = teacherId;
            ModelState.Remove("TeacherId");
            if (ModelState.IsValid)
            {
                Console.WriteLine(
                    $"Creating schedule for lesson: {schedule.Lesson}, class: {schedule.UClass}, day: {schedule.Day}, start time: {schedule.StartTime}, end time: {schedule.EndTime}"
                );
                if (string.IsNullOrEmpty(teacherId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                schedule.TeacherId = teacherId;
                _context.Add(schedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            Console.WriteLine("Model state is invalid. Returning to create view.");

            await PrepareViewBagForSchedule();
            return View(schedule);
        }

        private async Task PrepareViewBagForSchedule()
        {
            ViewBag.Lessons = new SelectList(
                await _context.Lessons.Select(l => l.Name).ToListAsync()
            );
            ViewBag.Classes = new SelectList(
                await _context.Classes.Select(c => c.UClass).Distinct().ToListAsync()
            );
            ViewBag.Days = new SelectList(
                Enumerable
                    .Range(0, 7)
                    .Select(i => new { Id = i, Name = ((DayOfWeek)i).ToString() }),
                "Id",
                "Name"
            );
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return NotFound();
            }

            ViewBag.Lessons = new SelectList(await _context.Lessons.ToListAsync(), "Name", "Name");
            ViewBag.Classes = new SelectList(
                await _context.Classes.Select(c => c.UClass).Distinct().ToListAsync()
            );
            ViewBag.Days = new SelectList(
                Enumerable
                    .Range(0, 7)
                    .Select(i => new SelectListItem
                    {
                        Value = i.ToString(),
                        Text = ((DayOfWeek)i).ToString(),
                    }),
                "Value",
                "Text",
                schedule.Day
            );
            ViewBag.Teachers = new SelectList(
                await _context.users.Where(u => u.role == "teacher").ToListAsync(),
                "dershaneid",
                "firstname"
            );

            return View(schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Lesson,UClass,Day,StartTime,EndTime,TeacherId")] Schedule schedule
        )
        {
            if (id != schedule.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(schedule);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ScheduleExists(schedule.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Lessons = new SelectList(
                await _context.Lessons.ToListAsync(),
                "Name",
                "Name",
                schedule.Lesson
            );
            ViewBag.Classes = new SelectList(
                await _context.Classes.Select(c => c.UClass).Distinct().ToListAsync(),
                schedule.UClass
            );
            ViewBag.Days = new SelectList(
                Enumerable
                    .Range(0, 7)
                    .Select(i => new SelectListItem
                    {
                        Value = i.ToString(),
                        Text = ((DayOfWeek)i).ToString(),
                    }),
                "Value",
                "Text",
                schedule.Day
            );
            ViewBag.Teachers = new SelectList(
                await _context.users.Where(u => u.role == "teacher").ToListAsync(),
                "dershaneid",
                "firstname",
                schedule.TeacherId
            );

            return View(schedule);
        }

        private bool ScheduleExists(int id)
        {
            return _context.Schedules.Any(e => e.Id == id);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules.FirstOrDefaultAsync(m => m.Id == id);
            if (schedule == null)
            {
                return NotFound();
            }

            return View(schedule);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [RoleAuthorize("student")]
        public async Task<IActionResult> ViewSchedule()
        {
            var schedules = await _context
                .Schedules.OrderBy(s => s.Day)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            var teacherIds = schedules.Select(s => s.TeacherId).Distinct().ToList();
            var teachers = await _context
                .users.Where(u => teacherIds.Contains(u.dershaneid))
                .ToDictionaryAsync(u => u.dershaneid, u => u);

            var viewModel = schedules
                .Select(s => new ScheduleViewModel
                {
                    Id = s.Id,
                    Lesson = s.Lesson,
                    UClass = s.UClass,
                    Day = s.Day,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    TeacherName = teachers.ContainsKey(s.TeacherId)
                        ? $"{teachers[s.TeacherId].firstname} {teachers[s.TeacherId].lastname}"
                        : "Unknown",
                })
                .ToList();

            return View(viewModel);
        }
    }
}
