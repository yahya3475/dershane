using System.Text;
using System.Text.Json;

public class AIQuestionGeneratorService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public AIQuestionGeneratorService()
    {
        _httpClient = new HttpClient();
        _apiKey = "AIzaSyCSczm_9gQ2C8aFQZ0bYxWFBBbYgMpazyE"; // Bu değeri appsettings.json'dan alın
    }

    public async Task<List<AIGeneratedQuestionVM>> GenerateQuestionsAsync(
        string topic,
        string lesson,
        int count,
        string difficulty
    )
    {
        var prompt = CreatePrompt(topic, lesson, count, difficulty);

        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                temperature = 0.7,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 2048,
            },
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={_apiKey}";

        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Gemini AI service request failed");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);

        return ParseGeminiResponse(geminiResponse.candidates[0].content.parts[0].text);
    }

    private string CreatePrompt(string topic, string lesson, int count, string difficulty)
    {
        var difficultyText = difficulty switch
        {
            "easy" => "kolay",
            "medium" => "orta",
            "hard" => "zor",
            _ => "orta",
        };

        return $@"
'{topic}' konusu hakkında '{lesson}' dersi için {difficultyText} seviyede {count} adet çoktan seçmeli soru oluştur.

Her soruyu tam olarak şu JSON formatında ver:
{{
    ""questionText"": ""Soru metni buraya"",
    ""optionA"": ""A şıkkı metni"",
    ""optionB"": ""B şıkkı metni"",
    ""optionC"": ""C şıkkı metni"",
    ""optionD"": ""D şıkkı metni"",
    ""correctAnswer"": ""A"",
    ""points"": 5
}}

Gereksinimler:
- Sorular Türkçe olmalı
- Her soru konuyu anlayıp anlamadığını test etmeli
- Şıklar mantıklı olmalı ama sadece biri doğru olmalı
- Zorluk seviyesi: {difficultyText}
- Sadece geçerli JSON array döndür
- Ek açıklama veya metin ekleme

JSON array olarak döndür: [soru1, soru2, ...]";
    }

    private List<AIGeneratedQuestionVM> ParseGeminiResponse(string geminiResponse)
    {
        try
        {
            // Clean the response to extract JSON
            var cleanedResponse = geminiResponse.Trim();

            // Remove markdown code blocks if present
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }

            cleanedResponse = cleanedResponse.Trim();

            // Find JSON array bounds
            var jsonStart = cleanedResponse.IndexOf('[');
            var jsonEnd = cleanedResponse.LastIndexOf(']') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = cleanedResponse.Substring(jsonStart, jsonEnd - jsonStart);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                return JsonSerializer.Deserialize<List<AIGeneratedQuestionVM>>(jsonContent, options)
                    ?? new List<AIGeneratedQuestionVM>();
            }

            return CreateFallbackQuestions();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gemini response parsing error: {ex.Message}");
            Console.WriteLine($"Response content: {geminiResponse}");

            // Fallback: create sample questions if AI parsing fails
            return CreateFallbackQuestions();
        }
    }

    private List<AIGeneratedQuestionVM> CreateFallbackQuestions()
    {
        return new List<AIGeneratedQuestionVM>
        {
            new AIGeneratedQuestionVM
            {
                QuestionText = "Gemini AI servisi şu anda kullanılamıyor. Bu örnek sorudur.",
                OptionA = "Seçenek A",
                OptionB = "Seçenek B",
                OptionC = "Seçenek C",
                OptionD = "Seçenek D",
                CorrectAnswer = "A",
                Points = 5,
            },
        };
    }
}

public class GeminiResponse
{
    public Candidate[] candidates { get; set; }
}

public class Candidate
{
    public Content content { get; set; }
}

public class Content
{
    public Part[] parts { get; set; }
}

public class Part
{
    public string text { get; set; }
}
