
using dershane.Models;

namespace dershane.ViewModels
{
    public class TakeExamVM
    {
        public int ExamId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Lesson { get; set; }
        public int Duration { get; set; }
        public DateTime StartTime { get; set; }
        public List<ExamQuestionDisplayVM> Questions { get; set; } = new List<ExamQuestionDisplayVM>();
        public Dictionary<int, string> StudentAnswers { get; set; } = new Dictionary<int, string>();
    }
}
