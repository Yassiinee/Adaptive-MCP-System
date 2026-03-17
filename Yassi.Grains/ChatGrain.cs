using Yassi.Contracts;
using Yassi.Grains.Abs;

namespace Yassi.Grains;

// Each conversation is a virtual actor — Orleans activates/deactivates automatically
public class ChatGrain : Grain, IChatGrain
{
    private readonly IPersistentState<ConversationState> _state;

    public ChatGrain(
        [PersistentState("conversation", "yassiStore")]
        IPersistentState<ConversationState> state)
    {
        _state = state;
    }

    public Task<IReadOnlyList<ChatMessage>> GetHistoryAsync()
    {
        return Task.FromResult<IReadOnlyList<ChatMessage>>(_state.State.Messages);
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        _state.State.Messages.Add(message);
        // Keep last 50 messages to bound memory
        if (_state.State.Messages.Count > 50)
            _state.State.Messages.RemoveAt(0);
        await _state.WriteStateAsync();
    }

    public async Task ClearAsync()
    {
        _state.State.Messages.Clear();
        await _state.WriteStateAsync();
    }
}

[GenerateSerializer]
[Alias("Yassi.Grains.ConversationState")]
public class ConversationState
{
    [Id(0)] public List<ChatMessage> Messages { get; set; } = [];
}