namespace Yassi.Contracts;

public record ChatMessage(
    string Role,       // "user" | "assistant" | "system"
    string Content,
    DateTimeOffset Timestamp
);

public record AgentRequest(
    string ConversationId,
    string UserMessage,
    IReadOnlyList<ChatMessage> History
);

public record AgentResponse(
    string ConversationId,
    string AssistantMessage,
    string? AgentUsed,
    bool IsStreaming
);