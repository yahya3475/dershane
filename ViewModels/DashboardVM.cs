namespace dershane.ViewModels
{
    public class DashboardVM
    {
        public string Role { get; set; }
        public string UserId { get; set; }
        public StudentStatsVM StudentStats { get; set; }
        public TeacherStatsVM TeacherStats { get; set; }
        public PrincipalStatsVM PrincipalStats { get; set; }
    }

    public class StudentStatsVM
    {
        public double AverageGrade { get; set; }
        public int TotalExams { get; set; }
        public double AttendanceRate { get; set; }
        public int PendingHomeworks { get; set; }
    }

    public class TeacherStatsVM
    {
        public int TotalStudents { get; set; }
        public int TotalExams { get; set; }
        public int ActiveHomeworks { get; set; }
        public int WeeklyLessons { get; set; }
    }

    public class PrincipalStatsVM
    {
        public int TotalUsers { get; set; }
        public int TotalClasses { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalSchedules { get; set; }
    }
}