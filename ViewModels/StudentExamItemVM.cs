
using System;

namespace dershane.ViewModels
{
    public class StudentExamItemVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Lesson { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public int Duration { get; set; }
        public int QuestionCount { get; set; }
        public int TotalPoints { get; set; }
        public bool IsCompleted { get; set; }
        public bool CanTake { get; set; }
        public int? Score { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
