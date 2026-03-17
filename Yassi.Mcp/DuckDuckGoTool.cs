using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;

namespace Yassi.Mcp;

[McpServerToolType]
public class DuckDuckGoTool(HttpClient http)
{
    [McpServerTool, Description("Quick factual lookup via DuckDuckGo (no API key needed)")]
    public async Task<string> SearchAsync(
        [Description("Search query")] string query,
        CancellationToken ct = default)
    {
        string url = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json&no_redirect=1";
        DdgResult? data = await http.GetFromJsonAsync<DdgResult>(url, ct);

        return !string.IsNullOrEmpty(data?.AbstractText)
            ? $"{data.AbstractText}\nSource: {data.AbstractSource}"
            : data?.RelatedTopics is { Count: > 0 }
            ? string.Join("\n", data.RelatedTopics.Take(3).Select(t => t.Text))
            : "No instant answer found. Try Serper or Tavily for full web results.";
    }
}

file record DdgResult(
    [property: JsonPropertyName("AbstractText")] string? AbstractText,
    [property: JsonPropertyName("AbstractSource")] string? AbstractSource,
    [property: JsonPropertyName("RelatedTopics")] List<DdgTopic>? RelatedTopics
);
file record DdgTopic([property: JsonPropertyName("Text")] string Text);