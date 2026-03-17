using System.Net.Http.Json;

namespace Yassi.LlmClient;

// Ollama: install from https://ollama.com — run: ollama pull llama3
// Zero cost, runs locally, no API key needed
public class OllamaClient(HttpClient http, string model = "llama3")
{
    public async Task<string> CompleteAsync(
        IEnumerable<(string role, string content)> messages,
        CancellationToken ct = default)
    {
        // Build single prompt from history
        var prompt = string.Join("\n", messages.Select(m => $"{m.role}: {m.content}"));

        var body = new { model, prompt, stream = false };

        var resp = await http.PostAsJsonAsync("http://localhost:11434/api/generate", body, ct);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: ct);
        return result!.Response;
    }
}

file record OllamaResponse(string Response);