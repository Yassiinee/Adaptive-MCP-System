using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Yassi.Agents;
using Yassi.Contracts;
using Yassi.Grains.Abs;

namespace Yassi.Orchestrator;

public class OrchestratorService(
    Kernel kernel,
    IClusterClient orleans,
    SearchAgent searchAgent,
    CodeAgent codeAgent)
{
    public async Task<AgentResponse> HandleAsync(AgentRequest request, CancellationToken ct = default)
    {
        // 1. Load history from Orleans grain
        var grain = orleans.GetGrain<IChatGrain>(request.ConversationId);
        var history = await grain.GetHistoryAsync();

        // 2. Decide which agent to use (simple keyword routing — replace with SK planner)
        var lower = request.UserMessage.ToLowerInvariant();
        string agentName;
        string reply;

        if (lower.Contains("search") || lower.Contains("find") || lower.Contains("latest"))
        {
            reply = await searchAgent.RunAsync(request.UserMessage, history, ct);
            agentName = "SearchAgent";
        }
        else if (lower.Contains("code") || lower.Contains("write") || lower.Contains("fix"))
        {
            reply = await codeAgent.RunAsync(request.UserMessage, history, ct);
            agentName = "CodeAgent";
        }
        else
        {
            // Default: direct LLM via Semantic Kernel
            var chat = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory("You are Yassi, a helpful AI assistant.");
            foreach (var m in history)
                chatHistory.AddMessage(new AuthorRole(m.Role), m.Content);
            chatHistory.AddUserMessage(request.UserMessage);

            var result = await chat.GetChatMessageContentAsync(chatHistory, cancellationToken: ct);
            reply = result.Content ?? "(no response)";
            agentName = "DirectLLM";
        }

        // 3. Persist both messages to Orleans grain
        await grain.AddMessageAsync(new ChatMessage("user", request.UserMessage, DateTimeOffset.UtcNow));
        await grain.AddMessageAsync(new ChatMessage("assistant", reply, DateTimeOffset.UtcNow));

        return new AgentResponse(request.ConversationId, reply, agentName, false);
    }
}