using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;

namespace Yassi.Mcp;

[McpServerToolType]
public class TavilySearchTool(HttpClient http, string apiKey)
{
    [McpServerTool, Description("Search the web via Tavily — optimised for AI agents")]
    public async Task<string> SearchAsync(
        [Description("Search query")] string query,
        CancellationToken ct = default)
    {
        var body = new
        {
            api_key = apiKey,
            query,
            max_results = 5,
            include_answer = true   // Tavily gives a pre-summarised answer too
        };

        HttpResponseMessage resp = await http.PostAsJsonAsync(
            "https://api.tavily.com/search", body, ct);
        resp.EnsureSuccessStatusCode();

        TavilyResult? data = await resp.Content.ReadFromJsonAsync<TavilyResult>(cancellationToken: ct);

        string summary = string.IsNullOrEmpty(data?.Answer) ? "" : $"**Summary:** {data.Answer}\n\n";
        string links = string.Join("\n\n", (data?.Results ?? [])
            .Select(r => $"**{r.Title}**\n{r.Content}\n{r.Url}"));

        return summary + links;
    }
}

file record TavilyResult(
    [property: JsonPropertyName("answer")] string? Answer,
    [property: JsonPropertyName("results")] List<TavilyItem> Results
);
file record TavilyItem(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("url")] string Url
);