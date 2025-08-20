
namespace dershane.ViewModels
{
    public class ExamResultVM
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; }
        public string Lesson { get; set; }
        public int Score { get; set; }
        public int TotalPoints { get; set; }
        public DateTime CompletedAt { get; set; }
        public int Duration { get; set; }
        public List<QuestionResultVM> QuestionResults { get; set; } = new List<QuestionResultVM>();
    }
}
