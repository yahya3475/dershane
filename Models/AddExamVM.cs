using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace dershane.Models
{
    public class AddExamVM
    {
        [Required(ErrorMessage = "Please select a student.")]
        public string SchoolNumber { get; set; }

        [Required(ErrorMessage = "Lesson is required.")]
        public string Lesson { get; set; }

        [Required(ErrorMessage = "Points are required.")]
        [Range(0, 100, ErrorMessage = "Points must be between 0 and 100.")]
        public int Points { get; set; }

        // Dropdown list
        public List<SelectListItem> Students { get; set; } = new List<SelectListItem>();
    }
}
