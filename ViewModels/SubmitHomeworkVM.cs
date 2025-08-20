
using System.ComponentModel.DataAnnotations;

namespace dershane.ViewModels
{
    public class SubmitHomeworkVM
    {
        public int HomeworkId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Lesson { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        
        [Required(ErrorMessage = "Answer is required")]
        public string Answer { get; set; } = string.Empty;
        
        public IFormFile? File { get; set; }
    }
}