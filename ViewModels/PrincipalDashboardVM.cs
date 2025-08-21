
public class PrincipalDashboardVM
{
    public int TotalStudents { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalClasses { get; set; }
    public int TotalExams { get; set; }
    public int TotalHomeworks { get; set; }
    
    public List<ClassStatisticVM> ClassStatistics { get; set; } = new();
    public List<TeacherPerformanceVM> TeacherPerformances { get; set; } = new();
    public List<ExamStatisticVM> RecentExams { get; set; } = new();
}

public class ClassStatisticVM
{
    public string ClassName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public double AverageScore { get; set; }
    public double AttendanceRate { get; set; }
}

public class TeacherPerformanceVM
{
    public string TeacherName { get; set; } = string.Empty;
    public string TeacherId { get; set; } = string.Empty;
    public int ExamsCreated { get; set; }
    public int HomeworksCreated { get; set; }
    public double AverageStudentScore { get; set; }
}

public class ExamStatisticVM
{
    public string ExamTitle { get; set; } = string.Empty;
    public string Lesson { get; set; } = string.Empty;
    public DateTime ExamDate { get; set; }
    public int ParticipantCount { get; set; }
    public double AverageScore { get; set; }
}
