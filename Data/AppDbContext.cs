
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User tablosunda dershaneid'i unique key olarak ayarla
            modelBuilder.Entity<User>()
                .HasAlternateKey(u => u.dershaneid);

            // StudentExamResult ile User arasındaki ilişki
            modelBuilder.Entity<StudentExamResult>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .HasPrincipalKey(u => u.dershaneid);

            // StudentExamResult ile ExamSystem arasındaki ilişki
            modelBuilder.Entity<StudentExamResult>()
                .HasOne(r => r.Exam)
                .WithMany(e => e.StudentResults)
                .HasForeignKey(r => r.ExamId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
