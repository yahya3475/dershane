
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace dershane.ViewModels
{
    public class CreateHomeworkVM
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Lesson is required")]
        public string Lesson { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Due date is required")]
        public DateTime DueDate { get; set; }
        
        public List<SelectListItem> Lessons { get; set; } = new List<SelectListItem>();
    }
}