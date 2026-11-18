using Asset.Application.Features.AI.DTos;
using Asset.Application.Features.AI.IService;
using Microsoft.Extensions.Caching.Memory;

namespace Asset.Application.Features.AI.ServiceImplementation
{
    public class InMemoryConversationStore : IConversationStoreService
    {
        #region Fields
        private readonly IMemoryCache _cache;

        // 30 minutes of silence ends the chat's memory: long enough for a real conversation,
        // short enough that yesterday's context doesn't leak into today's questions.
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

        // Keep only the latest turns: enough context for follow-ups, and a small, cheap prompt.
        private const int MaxTurns = 10;

        // Long answers are cut, the start is enough to remember what was said.
        private const int MaxAnswerLength = 1000;
        #endregion

        #region Constructor
        public InMemoryConversationStore(IMemoryCache cache)
        {
            _cache = cache;
        }
        #endregion

        #region Last filters
        public ParsedAssetQuestion? GetLast(string chatKey)
        {
            _cache.TryGetValue(FiltersKey(chatKey), out ParsedAssetQuestion? parsed);
            return parsed;
        }

        public void SaveLast(string chatKey, ParsedAssetQuestion parsed)
        {
            _cache.Set(FiltersKey(chatKey), parsed, Lifetime);
        }
        #endregion

        #region Chat history
        public IReadOnlyList<ChatTurn> GetHistory(string chatKey)
        {
            _cache.TryGetValue(HistoryKey(chatKey), out List<ChatTurn>? turns);
            return turns ?? new List<ChatTurn>();
        }

        public void AddTurn(string chatKey, string question, string answer)
        {
            if (answer.Length > MaxAnswerLength)
                answer = answer[..MaxAnswerLength] + "...";

            // A NEW list every time (never change the cached one): another request reading
            // the old list at the same moment can't see it half-changed.
            var turns = GetHistory(chatKey)
                        .TakeLast(MaxTurns - 1)
                        .Append(new ChatTurn(question, answer))
                        .ToList();

            _cache.Set(HistoryKey(chatKey), turns, Lifetime);
        }
        #endregion

        #region Keys
        private static string FiltersKey(string chatKey) => $"asset-chat:{chatKey}:filters";
        private static string HistoryKey(string chatKey) => $"asset-chat:{chatKey}:history";
        #endregion
    }
}
