using System.ComponentModel.DataAnnotations;

public class AIGeneratedQuestionVM
{
    [Required(ErrorMessage = "Question text required!")]
    public string QuestionText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Option A is necessary!")]
    public string OptionA { get; set; } = string.Empty;

    [Required(ErrorMessage = "Option B is necessary!")]
    public string OptionB { get; set; } = string.Empty;

    [Required(ErrorMessage = "Option C is necessary!")]
    public string OptionC { get; set; } = string.Empty;

    [Required(ErrorMessage = "Option D is necessary!")]
    public string OptionD { get; set; } = string.Empty;

    [Required(ErrorMessage = "The right answer is needed!")]
    [RegularExpression("^[ABCD]$", ErrorMessage = "The correct answer is A, B, C or D!")]
    public string CorrectAnswer { get; set; } = string.Empty;

    [Required(ErrorMessage = "Points required!")]
    [Range(1, 100, ErrorMessage = "Score should be between 1-100!")]
    public int Points { get; set; } = 5;
}
