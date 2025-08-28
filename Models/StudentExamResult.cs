using System.ComponentModel.DataAnnotations;

namespace dershane.Models
{
    public class StudentExamResult
    {
        public int Id { get; set; }
        public int ExamId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int TotalScore { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public bool IsCompleted { get; set; }
        public string Answers { get; set; } = string.Empty; // JSON format

        // Navigation properties
        public virtual ExamSystem Exam { get; set; }
        public virtual User Student { get; set; }
    }
}