
using System.ComponentModel.DataAnnotations;
using dershane.Models;

namespace dershane.ViewModels
{
    public class CreateClassViewModel
    {
        [Required(ErrorMessage = "Class name is required")]
        [Display(Name = "Class Name")]
        public string ClassName { get; set; } = string.Empty;

        [Display(Name = "Teacher")]
        public string? TeacherId { get; set; }

        [Display(Name = "Selected Students")]
        public List<string> SelectedStudentIds { get; set; } = new List<string>();

        // For displaying available teachers and students
        public List<User> AvailableTeachers { get; set; } = new List<User>();
        public List<User> AvailableStudents { get; set; } = new List<User>();
    }
}
