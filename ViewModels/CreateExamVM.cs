
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace dershane.ViewModels
{
    public class CreateExamVM
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Sınav başlığı gerekli!")]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Ders seçimi gerekli!")]
        public string Lesson { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Sınav tarihi gerekli!")]
        public DateTime ExamDate { get; set; } = DateTime.Now.AddDays(1);
        
        [Required(ErrorMessage = "Süre gerekli!")]
        [Range(10, 300, ErrorMessage = "Süre 10-300 dakika arasında olmalı!")]
        public int Duration { get; set; } = 60;
        
        public List<ExamQuestionVM> Questions { get; set; } = new List<ExamQuestionVM>();
        
        public List<SelectListItem> Lessons { get; set; } = new List<SelectListItem>();
    }

    public class CreateQuestionVM
    {
        [Required(ErrorMessage = "Question text is required")]
        public string QuestionText { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Option A is required")]
        public string OptionA { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Option B is required")]
        public string OptionB { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Option C is required")]
        public string OptionC { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Option D is required")]
        public string OptionD { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Correct answer is required")]
        public string CorrectAnswer { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Points are required")]
        [Range(1, 100, ErrorMessage = "Points must be between 1 and 100")]
        public int Points { get; set; }
    }
}
