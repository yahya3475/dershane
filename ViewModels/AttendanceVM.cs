namespace dershane.ViewModels
{
    public class AttendanceVM
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public bool IsPresent { get; set; }
        public string? Note { get; set; }
    }

    public class TakeAttendanceVM
    {
        public string Lesson { get; set; }
        public DateTime Date { get; set; }
        public List<AttendanceVM> Students { get; set; } = new();
    }

    public class StudentAttendanceVM
    {
        public DateTime Date { get; set; }
        public string Lesson { get; set; }
        public bool IsPresent { get; set; }
        public string Note { get; set; }
        public string TeacherName { get; set; }

        public string Status => IsPresent ? "Here" : "Not Here";
        public string StatusClass => IsPresent ? "success" : "danger";
    }

    public class AttendanceReportVM
    {
        public DateTime Date { get; set; }
        public string Lesson { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public bool IsPresent { get; set; }
        public string? Note { get; set; }
    }
}
