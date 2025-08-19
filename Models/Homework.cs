
using System.ComponentModel.DataAnnotations;

namespace dershane.Models
{
    public class Homework
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Subject")]
        public string Lesson { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Required]
        [Display(Name = "Class")]
        public string UClass { get; set; }

        [Required]
        [Display(Name = "Teacher")]
        public string TeacherId { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // This property added
        [Display(Name = "Is Active?")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<HomeworkSubmission> Submissions { get; set; }
    }
}