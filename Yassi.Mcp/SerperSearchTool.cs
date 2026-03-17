using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;

namespace Yassi.Mcp;

[McpServerToolType]
public class SerperSearchTool(HttpClient http, string apiKey)
{
    [McpServerTool, Description("Search Google via Serper API")]
    public async Task<string> SearchAsync(
        [Description("Search query")] string query,
        CancellationToken ct = default)
    {
        var body = new { q = query, num = 5 };

        using HttpRequestMessage req = new(HttpMethod.Post,
            "https://google.serper.dev/search");
        req.Headers.Add("X-API-KEY", apiKey);
        req.Content = JsonContent.Create(body);

        HttpResponseMessage resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        SerperResult? data = await resp.Content.ReadFromJsonAsync<SerperResult>(cancellationToken: ct);
        List<SerperItem> items = data?.Organic ?? [];

        return string.Join("\n\n", items.Select(r =>
            $"**{r.Title}**\n{r.Snippet}\n{r.Link}"));
    }
}

file record SerperResult([property: JsonPropertyName("organic")] List<SerperItem> Organic);
file record SerperItem(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("snippet")] string Snippet,
    [property: JsonPropertyName("link")] string Link
);