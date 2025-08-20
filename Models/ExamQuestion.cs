
using System.ComponentModel.DataAnnotations;

namespace dershane.Models
{
    public class ExamQuestion
    {
        [Key]
        public int Id { get; set; }
        
        public int ExamId { get; set; }
        
        [Required]
        public string QuestionText { get; set; } = string.Empty;
        
        [Required]
        public string OptionA { get; set; } = string.Empty;
        
        [Required]
        public string OptionB { get; set; } = string.Empty;
        
        [Required]
        public string OptionC { get; set; } = string.Empty;
        
        [Required]
        public string OptionD { get; set; } = string.Empty;
        
        [Required]
        public string CorrectAnswer { get; set; } = string.Empty;
        
        [Range(1, 100)]
        public int Points { get; set; } = 1;
        
        public int QuestionOrder { get; set; }
        
        // Navigation properties
        public virtual ExamSystem Exam { get; set; }
    }
}