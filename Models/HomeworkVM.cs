using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace dershane.Models
{
    public class CreateHomeworkVM
    {
        [Required(ErrorMessage = "Title is required")]
        [Display(Name = "Homework Title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required for students to understand")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please select a subject")]
        [Display(Name = "Subject")]
        public string Lesson { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        [Display(Name = "Due Date")]
        [DataType(DataType.DateTime)]
        public DateTime DueDate { get; set; }

        public List<SelectListItem> Lessons { get; set; } = new List<SelectListItem>();
    }

    public class HomeworkListVM
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Lesson { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsActive { get; set; }
        public int SubmissionCount { get; set; }
        public bool IsSubmitted { get; set; } // For student
        public bool IsGraded { get; set; } // For student
    }

    public class SubmitHomeworkVM
    {
        public int HomeworkId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Lesson { get; set; }
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Answer field is required!")]
        [MinLength(10, ErrorMessage = "You must write at least 10 characters!")]
        public string Answer { get; set; }
    }
}
