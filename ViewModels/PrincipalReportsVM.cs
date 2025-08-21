using System.ComponentModel.DataAnnotations;

namespace dershane.ViewModels
{
    public class PrincipalReportsVM
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalExams { get; set; }
        public int TotalHomeworks { get; set; }

        public List<LessonStatisticVM> LessonStatistics { get; set; } =
            new List<LessonStatisticVM>();
        public List<MonthlyExamStatVM> MonthlyExamStats { get; set; } =
            new List<MonthlyExamStatVM>();
        public List<TopStudentVM> TopStudents { get; set; } = new List<TopStudentVM>();
        public List<AttendancesReportVM> AttendanceReports { get; set; } =
            new List<AttendancesReportVM>();
    }

    public class LessonStatisticVM
    {
        public string LessonName { get; set; } = string.Empty;
        public int ExamCount { get; set; }
        public int HomeworkCount { get; set; }
        public double AverageExamScore { get; set; }
        public double AverageHomeworkGrade { get; set; }
    }

    public class MonthlyExamStatVM
    {
        public string Month { get; set; } = string.Empty;
        public int ExamCount { get; set; }
        public double AverageScore { get; set; }
        public int ParticipantCount { get; set; }
    }

    public class TopStudentVM
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty; // Bu eksikti!
        public double AverageScore { get; set; }
        public double AverageExamScore { get; set; }
        public double AverageHomeworkGrade { get; set; }
        public int ExamCount { get; set; }
        public int HomeworkCount { get; set; }
    }

    public class AttendancesReportVM
    {
        public string ClassName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int PresentToday { get; set; }
        public double AttendanceRate { get; set; }
    }
}
