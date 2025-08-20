
using System.ComponentModel.DataAnnotations;

namespace dershane.ViewModels
{
    public class EditClassViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Class name is required")]
        public string ClassName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Grade is required")]
        public string Grade { get; set; } = string.Empty;
        
        public List<StudentInClassVM> Students { get; set; } = new List<StudentInClassVM>();
        public List<TeacherInClassVM> Teachers { get; set; } = new List<TeacherInClassVM>();
    }

    public class StudentInClassVM
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class TeacherInClassVM
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
