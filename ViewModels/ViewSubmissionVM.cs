using System;
using System.ComponentModel.DataAnnotations;

namespace dershane.ViewModels
{
public class ViewSubmissionVM
{
    public int HomeworkId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Lesson { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string Answer { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; }
    public int? Grade { get; set; } // Nullable
    public string? TeacherComment { get; set; } // Nullable yap
}
}
