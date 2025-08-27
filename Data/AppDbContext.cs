
using dershane.Models;
using Microsoft.EntityFrameworkCore;

namespace dershane.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> users { get; set; }
        public DbSet<UClass1> Classes { get; set; }
        public DbSet<Exams> notes { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Homework> Homeworks { get; set; }
        public DbSet<HomeworkSubmission> HomeworkSubmissions { get; set; }

        public DbSet<ExamSystem> ExamSystem { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<StudentExamResult> StudentExamResults { get; set; }
        public DbSet<UserInformation> user_informations { get; set; }
    }
}
