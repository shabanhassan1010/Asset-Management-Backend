using Asset.Application.Features.AI.DTos;

namespace Asset.Application.Features.AI.IService
{
    // Why async: the chat memory now lives in Redis (another process over the network),
    // so every read/write is I/O. Async frees the request thread while waiting for Redis.
    public interface IConversationStoreService
    {
        // The filters of the last question that ran a query (used by the rule-based fallback
        // and given to Gemini as a hint, because the chat text alone doesn't hold exact filters).
        Task<ParsedAssetQuestion?> GetLastAsync(string chatKey, CancellationToken ct);
        Task SaveLastAsync(string chatKey, ParsedAssetQuestion parsed, CancellationToken ct);

        // The whole chat (latest turns only), so a new question can refer to anything said before.
        Task<IReadOnlyList<ChatTurn>> GetHistoryAsync(string chatKey, CancellationToken ct);
        Task AddTurnAsync(string chatKey, string question, string answer, CancellationToken ct);
    }
}
