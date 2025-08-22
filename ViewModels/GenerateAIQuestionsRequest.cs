
public class GenerateAIQuestionsRequest
{
    public string Topic { get; set; } = string.Empty;
    public string Lesson { get; set; } = string.Empty;
    public int Count { get; set; } = 3;
    public string Difficulty { get; set; } = "medium";
}
