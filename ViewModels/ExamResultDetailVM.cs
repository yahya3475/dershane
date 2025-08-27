namespace dershane.ViewModels
{
    public class ExamResultDetailVM
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int Score { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Answers { get; set; }
        public int TotalPoints { get; set; }

        // Computed properties
        public double Percentage => TotalPoints > 0 ? (Score * 100.0 / TotalPoints) : 0;
        public string Grade =>
            Percentage >= 85 ? "A"
            : Percentage >= 70 ? "B"
            : Percentage >= 50 ? "C"
            : "F";
        public string Status =>
            Percentage >= 85 ? "Excellent"
            : Percentage >= 70 ? "Good"
            : Percentage >= 50 ? "Average"
            : "Poor";
    }
}
