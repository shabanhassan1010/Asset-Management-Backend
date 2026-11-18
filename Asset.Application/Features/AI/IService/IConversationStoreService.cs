using Asset.Application.Features.AI.DTos;

namespace Asset.Application.Features.AI.IService
{
    public interface IConversationStoreService
    {
        // The filters of the last question that ran a query (used by the rule-based fallback
        // and given to Gemini as a hint, because the chat text alone doesn't hold exact filters).
        ParsedAssetQuestion? GetLast(string chatKey);
        void SaveLast(string chatKey, ParsedAssetQuestion parsed);

        // The whole chat (latest turns only), so a new question can refer to anything said before.
        IReadOnlyList<ChatTurn> GetHistory(string chatKey);
        void AddTurn(string chatKey, string question, string answer);
    }
}
