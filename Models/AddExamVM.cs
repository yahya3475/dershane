using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace dershane.Models
{
    public class AddExamVM
    {
        [Required(ErrorMessage = "Student selection is required.")]
        [Display(Name = "Student")]
        public string SchoolNumber { get; set; }

        [Required(ErrorMessage = "Lesson selection is required.")]
        [Display(Name = "Lesson")]
        public string Lesson { get; set; }

        [Required(ErrorMessage = "Points entry is required.")]
        [Range(0, 100, ErrorMessage = "Points must be between 0 and 100.")]
        [Display(Name = "Points")]
        public int Points { get; set; }

        public List<SelectListItem> Students { get; set; }
        public List<SelectListItem> Lessons { get; set; }
    }
}
