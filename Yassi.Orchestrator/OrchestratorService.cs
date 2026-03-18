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
        IChatGrain grain = orleans.GetGrain<IChatGrain>(request.ConversationId);
        IReadOnlyList<ChatMessage> history = await grain.GetHistoryAsync();

        // 2. Decide which agent to use (simple keyword routing — replace with SK planner)
        string lower = request.UserMessage.ToLowerInvariant();
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
            IChatCompletionService chat = kernel.GetRequiredService<IChatCompletionService>();
            ChatHistory chatHistory = new("You are Yassi, a helpful AI assistant.");
            foreach (ChatMessage m in history)
                chatHistory.AddMessage(new AuthorRole(m.Role), m.Content);
            chatHistory.AddUserMessage(request.UserMessage);

            ChatMessageContent result = await chat.GetChatMessageContentAsync(chatHistory, cancellationToken: ct);
            reply = result.Content ?? "(no response)";
            agentName = "DirectLLM";
        }

        // 3. Persist both messages to Orleans grain
        await Task.WhenAll(
            grain.AddMessageAsync(new ChatMessage("user", request.UserMessage, DateTimeOffset.UtcNow)),
            grain.AddMessageAsync(new ChatMessage("assistant", reply, DateTimeOffset.UtcNow))
        );

        return new AgentResponse(request.ConversationId, reply, agentName, false);
    }
}