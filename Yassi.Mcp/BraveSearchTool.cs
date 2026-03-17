using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Net.Http.Json;

namespace Yassi.Mcp;

// Brave Search API: https://api.search.brave.com — free tier: 2000 queries/month
[McpServerToolType]
public class BraveSearchTool(HttpClient http, string apiKey)
{
    [McpServerTool, Description("Search the web using Brave Search")]
    public async Task<string> SearchAsync(
        [Description("Search query")] string query,
        CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.search.brave.com/res/v1/web/search?q={Uri.EscapeDataString(query)}&count=5");
        req.Headers.Add("Accept", "application/json");
        req.Headers.Add("X-Subscription-Token", apiKey);

        var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var data = await resp.Content.ReadFromJsonAsync<BraveResult>(cancellationToken: ct);
        var results = data?.Web?.Results ?? [];

        return string.Join("\n\n", results.Select(r => $"**{r.Title}**\n{r.Description}\n{r.Url}"));
    }
}

// ── Response models ──
file record BraveResult(BraveWeb? Web);
file record BraveWeb(List<BraveItem> Results);
file record BraveItem(string Title, string Description, string Url);