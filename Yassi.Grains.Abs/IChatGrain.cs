using Yassi.Contracts;

namespace Yassi.Grains.Abs;

public interface IChatGrain : IGrainWithStringKey
{
    public Task<IReadOnlyList<ChatMessage>> GetHistoryAsync();
    public Task AddMessageAsync(ChatMessage message);
    public Task ClearAsync();
}