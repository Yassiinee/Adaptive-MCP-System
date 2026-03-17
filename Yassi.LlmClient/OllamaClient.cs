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
        string prompt = string.Join("\n", messages.Select(m => $"{m.role}: {m.content}"));

        var body = new { model, prompt, stream = false };

        HttpResponseMessage resp = await http.PostAsJsonAsync("http://localhost:11434/api/generate", body, ct);
        resp.EnsureSuccessStatusCode();

        OllamaResponse? result = await resp.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: ct);
        return result!.Response;
    }
}

file record OllamaResponse(string Response);