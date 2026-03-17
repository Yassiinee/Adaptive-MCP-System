using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Yassi.LlmClient;

// Groq free tier: https://console.groq.com — sign up, get free API key
// Free models: llama3-8b-8192, mixtral-8x7b-32768, gemma2-9b-it
public class GroqClient(HttpClient http, string apiKey, string model = "llama3-8b-8192")
{
    public async Task<string> CompleteAsync(
        IEnumerable<(string role, string content)> messages,
        CancellationToken ct = default)
    {
        var body = new
        {
            model,
            messages = messages.Select(m => new { role = m.role, content = m.content }),
            max_tokens = 1024
        };

        using HttpRequestMessage req = new(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        req.Headers.Add("Authorization", $"Bearer {apiKey}");
        req.Content = JsonContent.Create(body);

        HttpResponseMessage resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<GroqResponse>(cancellationToken: ct);
        return result!.Choices[0].Message.Content;
    }
}

// ── Minimal response models ──
file record GroqResponse(
    [property: JsonPropertyName("choices")] List<GroqChoice> Choices
);
file record GroqChoice(
    [property: JsonPropertyName("message")] GroqMessage Message
);
file record GroqMessage(
    [property: JsonPropertyName("content")] string Content
);