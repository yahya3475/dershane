using Microsoft.EntityFrameworkCore;
using dershane.Models;

namespace dershane.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> users { get; set; }
        public DbSet<UClass1> Classes { get; set; }
        public DbSet<Exams> notes { get; set; }
        public DbSet<Schedule> Schedules { get; set; } // Yeni eklenen satır
        public DbSet<Lesson> Lessons { get; set; }
    }
}
