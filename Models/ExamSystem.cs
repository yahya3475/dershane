
using System.ComponentModel.DataAnnotations;

namespace dershane.Models
{
    public class ExamSystem
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string Lesson { get; set; } = string.Empty;
        
        [Required]
        public string UClass { get; set; } = string.Empty;
        
        [Required]
        public string TeacherId { get; set; } = string.Empty;
        
        public DateTime ExamDate { get; set; }
        
        public int Duration { get; set; } // dakika cinsinden
        
        public DateTime CreatedDate { get; set; }
        
        public bool IsActive { get; set; }
        
        // Navigation properties
        public virtual ICollection<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();
        public virtual ICollection<StudentExamResult> StudentResults { get; set; } = new List<StudentExamResult>();
    }
}
