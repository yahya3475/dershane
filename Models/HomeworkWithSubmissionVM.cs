namespace dershane.Models
{
    public class HomeworkWithSubmissionVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Lesson { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public bool IsSubmitted { get; set; }
        public DateTime? SubmissionDate { get; set; }
        public int? Grade { get; set; }
        public string? TeacherComment { get; set; }
        public string? SubmissionContent { get; set; }
        public string? SubmissionFilePath { get; set; }
    }
}
