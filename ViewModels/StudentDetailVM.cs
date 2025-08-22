public class StudentDetailVM
{
    public string DershaneId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UClass { get; set; } = string.Empty;
    public double ExamAverage { get; set; }
    public int TotalExams { get; set; }
    public int AbsenceCount { get; set; }
    public DateTime? LastExamDate { get; set; }

    // View için gerekli ek özellikler
    public string StudentId => DershaneId;
    public string ClassName => UClass;
    public double AverageGrade => ExamAverage;
    public string ProfilePictureUrl => "/images/default-avatar.png"; // Varsayılan avatar

    public string FullName => $"{FirstName} {LastName}";
    public string PerformanceLevel =>
        ExamAverage >= 85 ? "Mükemmel"
        : ExamAverage >= 70 ? "İyi"
        : ExamAverage >= 50 ? "Orta"
        : "Geliştirilmeli";
    public string PerformanceColor =>
        ExamAverage >= 85 ? "success"
        : ExamAverage >= 70 ? "info"
        : ExamAverage >= 50 ? "warning"
        : "danger";
}
