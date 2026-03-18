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
        TavilyRequest body = new(apiKey, query, 5, true);

        HttpResponseMessage resp = await http.PostAsJsonAsync(
            "https://api.tavily.com/search", body, TavilyJsonContext.Default.TavilyRequest, ct);
        resp.EnsureSuccessStatusCode();

        TavilyResult? data = await resp.Content.ReadFromJsonAsync(TavilyJsonContext.Default.TavilyResult, cancellationToken: ct);

        string summary = string.IsNullOrEmpty(data?.Answer) ? "" : $"**Summary:** {data.Answer}\n\n";
        string links = string.Join("\n\n", (data?.Results ?? [])
            .Select(r => $"**{r.Title}**\n{r.Content}\n{r.Url}"));

        return summary + links;
    }
}

internal record TavilyResult(
    [property: JsonPropertyName("answer")] string? Answer,
    [property: JsonPropertyName("results")] List<TavilyItem> Results
);
internal record TavilyItem(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("url")] string Url
);

internal record TavilyRequest(
    [property: JsonPropertyName("api_key")] string ApiKey,
    [property: JsonPropertyName("query")] string Query,
    [property: JsonPropertyName("max_results")] int MaxResults,
    [property: JsonPropertyName("include_answer")] bool IncludeAnswer
);

[JsonSerializable(typeof(TavilyRequest))]
[JsonSerializable(typeof(TavilyResult))]
internal partial class TavilyJsonContext : JsonSerializerContext
{
}