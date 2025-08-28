using System;
using System.Collections.Generic;

namespace dershane.ViewModels
{
    public class DashboardVM
    {
        public string Role { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public StudentStatsVM StudentStats { get; set; }
        public TeacherStatsVM TeacherStats { get; set; }
        public PrincipalStatsVM PrincipalStats { get; set; }
        public List<ActivityVM> RecentActivities { get; set; } = new List<ActivityVM>();
        public List<ScheduleItemVM> DailySchedule { get; set; } = new List<ScheduleItemVM>();
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

    public class ActivityVM
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimeAgo { get; set; }
        public string IconClass { get; set; }
        public string ColorClass { get; set; }
    }

    public class ScheduleItemVM
    {
        public string Title { get; set; }
        public string Location { get; set; }
        public TimeSpan Time { get; set; }
        public string TimeDisplay { get; set; }
        public string ColorClass { get; set; }
    }
}