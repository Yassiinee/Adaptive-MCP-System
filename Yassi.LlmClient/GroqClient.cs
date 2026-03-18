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
        GroqRequest body = new(
            model,
            messages.Select(m => new GroqRequestMessage(m.role, m.content)).ToList(),
            1024
        );

        using HttpRequestMessage req = new(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        req.Headers.Add("Authorization", $"Bearer {apiKey}");
        req.Content = JsonContent.Create(body, GroqJsonContext.Default.GroqRequest);

        HttpResponseMessage resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        GroqResponse? result = await resp.Content.ReadFromJsonAsync(GroqJsonContext.Default.GroqResponse, cancellationToken: ct);

        return result!.Choices[0].Message.Content;
    }
}

// ── Minimal response models ──
internal record GroqResponse(
    [property: JsonPropertyName("choices")] List<GroqChoice> Choices
);
internal record GroqChoice(
    [property: JsonPropertyName("message")] GroqMessage Message
);
internal record GroqMessage(
    [property: JsonPropertyName("content")] string Content
);

internal record GroqRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] List<GroqRequestMessage> Messages,
    [property: JsonPropertyName("max_tokens")] int MaxTokens
);
internal record GroqRequestMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);

[JsonSerializable(typeof(GroqRequest))]
[JsonSerializable(typeof(GroqResponse))]
internal partial class GroqJsonContext : JsonSerializerContext
{
}