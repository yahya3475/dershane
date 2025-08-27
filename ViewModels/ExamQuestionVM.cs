using System.ComponentModel.DataAnnotations;

namespace dershane.ViewModels
{
    public class ExamQuestionVM
    {
        public int Id { get; set; }
        public int ExamId { get; set; }

        [Required(ErrorMessage = "Question text required!")]
        public string QuestionText { get; set; } = string.Empty;

        [Required(ErrorMessage = "Option A required!")]
        public string OptionA { get; set; } = string.Empty;

        [Required(ErrorMessage = "Option B required!")]
        public string OptionB { get; set; } = string.Empty;

        [Required(ErrorMessage = "Option C required!")]
        public string OptionC { get; set; } = string.Empty;

        [Required(ErrorMessage = "Option D required!")]
        public string OptionD { get; set; } = string.Empty;

        [Required(ErrorMessage = "Correct answer required!")]
        [RegularExpression("^[ABCD]$", ErrorMessage = "The correct answer must be A, B, C or D!")]
        public string CorrectAnswer { get; set; } = string.Empty;

        [Required(ErrorMessage = "We need points!")]
        [Range(1, 100, ErrorMessage = "The score must be between 1-100!")]
        public int Points { get; set; } = 10;

        public int QuestionOrder { get; set; }
    }
}
