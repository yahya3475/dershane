using System.ComponentModel.DataAnnotations;

namespace dershane.Models
{
    public class HomeworkSubmission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HomeworkId { get; set; }

        [Required]
        public string StudentId { get; set; }

        [Required(ErrorMessage = "You must write your answer")]
        [Display(Name = "Homework Answer")]
        public string Answer { get; set; }

        [Display(Name = "Submission Date")]
        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        [Display(Name = "Grade")]
        [Range(0, 100, ErrorMessage = "Grade must be between 0-100")]
        public int? Grade { get; set; }

        [Display(Name = "Teacher Comment")]
        public string TeacherComment { get; set; }

        [Display(Name = "Is Graded?")]
        public bool IsGraded { get; set; } = false;

        // Navigation properties
        public virtual Homework Homework { get; set; }
    }
}
