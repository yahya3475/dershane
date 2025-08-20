using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace dershane.Models
{
    public class AddExamVM
    {
        [Required(ErrorMessage = "Student is required")]
        public string SchoolNumber { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Lesson is required")]
        public string Lesson { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Points are required")]
        [Range(0, 100, ErrorMessage = "Points must be between 0 and 100")]
        public int Points { get; set; }
        
        public List<SelectListItem> Students { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Lessons { get; set; } = new List<SelectListItem>();
    }
}