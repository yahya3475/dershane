
namespace dershane.ViewModels
{
    public class ViewExamResultsVM
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public string Lesson { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public int Duration { get; set; }
        public int TotalPoints { get; set; }
        
        // Statistics
        public int TotalStudents { get; set; }
        public int CompletedCount { get; set; }
        public double AverageScore { get; set; }
        public int HighestScore { get; set; }
        public int LowestScore { get; set; }
        
        // Student results
        public List<ExamResultDetailVM> StudentResults { get; set; } = new List<ExamResultDetailVM>();
    }
}
