using Orleans;
using Yassi.Contracts;

namespace Yassi.Grains.Abs;

public interface IChatGrain : IGrainWithStringKey
{
    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync();
    Task AddMessageAsync(ChatMessage message);
    Task ClearAsync();
}