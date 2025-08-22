using System.ComponentModel.DataAnnotations;

public class AIGeneratedQuestionVM
{
    [Required(ErrorMessage = "Soru metni gerekli!")]
    public string QuestionText { get; set; } = string.Empty;

    [Required(ErrorMessage = "A şıkkı gerekli!")]
    public string OptionA { get; set; } = string.Empty;

    [Required(ErrorMessage = "B şıkkı gerekli!")]
    public string OptionB { get; set; } = string.Empty;

    [Required(ErrorMessage = "C şıkkı gerekli!")]
    public string OptionC { get; set; } = string.Empty;

    [Required(ErrorMessage = "D şıkkı gerekli!")]
    public string OptionD { get; set; } = string.Empty;

    [Required(ErrorMessage = "Doğru cevap gerekli!")]
    [RegularExpression("^[ABCD]$", ErrorMessage = "Doğru cevap A, B, C veya D olmalı!")]
    public string CorrectAnswer { get; set; } = string.Empty;

    [Required(ErrorMessage = "Puan gerekli!")]
    [Range(1, 100, ErrorMessage = "Puan 1-100 arasında olmalı!")]
    public int Points { get; set; } = 5;
}
