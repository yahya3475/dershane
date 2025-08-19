
using System;
using System.ComponentModel.DataAnnotations;

namespace dershane.ViewModels
{
    public class ViewSubmissionVM
    {
        public int HomeworkId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Lesson { get; set; }
        public DateTime DueDate { get; set; }
        public string Answer { get; set; }
        public DateTime SubmissionDate { get; set; }
        public int? Grade { get; set; }
        public string TeacherComment { get; set; }
    }
}
