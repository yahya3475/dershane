
using System.ComponentModel.DataAnnotations;

namespace dershane.Models
{
    public class Exam
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        public string? Description { get; set; }
        
        [Required]
        public string Lesson { get; set; }
        
        [Required]
        public string UClass { get; set; }
        
        [Required]
        public string TeacherId { get; set; }
        
        [Required]
        public DateTime ExamDate { get; set; }
        
        [Required]
        public int Duration { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public bool IsActive { get; set; }
        
        // Navigation properties
        public virtual ICollection<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();
        public virtual ICollection<StudentExamResult> StudentResults { get; set; } = new List<StudentExamResult>();
    }
}