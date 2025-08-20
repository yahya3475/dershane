
namespace dershane.ViewModels
{
    public class ExamQuestionDisplayVM
    {
        public int Id { get; set; }
        public string QuestionText { get; set; }
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public int QuestionOrder { get; set; }
        public string SelectedAnswer { get; set; }
    }
}
