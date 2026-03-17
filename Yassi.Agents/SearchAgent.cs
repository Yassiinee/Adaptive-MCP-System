using Yassi.Contracts;
using Yassi.LlmClient;
using Yassi.Mcp;

namespace Yassi.Agents;

public class SearchAgent(TavilySearchTool search, GroqClient llm)
{
    public async Task<string> RunAsync(
        string userMessage,
        IReadOnlyList<ChatMessage> history,
        CancellationToken ct = default)
    {
        // Step 1: search the web
        string searchResults = await search.SearchAsync(userMessage, ct);

        // Step 2: ask the LLM to synthesize using search results
        List<(string role, string content)> messages = new()
        {
            ("system", "You are Yassi. Use the search results below to answer the question.\n\n" + searchResults)
        };
        foreach (ChatMessage? m in history.TakeLast(6))
            messages.Add((m.Role, m.Content));
        messages.Add(("user", userMessage));

        return await llm.CompleteAsync(messages, ct);
    }
}