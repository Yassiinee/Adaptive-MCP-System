using Yassi.Contracts;
using Yassi.LlmClient;

namespace Yassi.Agents;

public class CodeAgent(GroqClient llm)
{
    public async Task<string> RunAsync(
        string userMessage,
        IReadOnlyList<ChatMessage> history,
        CancellationToken ct = default)
    {
        List<(string role, string content)> messages = new()
        {
            ("system", "You are Yassi, an expert software engineer. Write clean, well-commented code.")
        };
        foreach (ChatMessage? m in history.TakeLast(6))
            messages.Add((m.Role, m.Content));
        messages.Add(("user", userMessage));

        return await llm.CompleteAsync(messages, ct);
    }
}