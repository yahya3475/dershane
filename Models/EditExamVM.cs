using System.ComponentModel.DataAnnotations;

namespace dershane.Models
{
    public class EditExamVM
    {
        public int Nid { get; set; }
        public string SchoolNumber { get; set; }
        public string StudentName { get; set; }
        public string Lesson { get; set; }

        [Required(ErrorMessage = "Points are required")]
        [Range(0, 100, ErrorMessage = "Points must be between 0 and 100")]
        public int Points { get; set; }
    }
}