using Asset.Application.Common.Caching;
using Asset.Application.Features.AI.DTos;
using Asset.Application.Features.AI.IService;

namespace Asset.Application.Features.AI.ServiceImplementation
{
    // The AI chat memory, kept in Redis instead of the server's RAM.
    //
    // Why Redis:
    //   1. Restart / deploy: RAM is wiped, Redis keeps the data → chats continue after a restart.
    //   2. Several servers behind a load balancer: each server has its own RAM, but they all
    //      share ONE Redis → the 2nd question can land on any server and still sees the 1st.
    //
    // It uses ICacheService (the same RedisCacheService as the rest of the app), so:
    //   - keys get the app's prefix, values are JSON;
    //   - if Redis is down, reads return "nothing" and writes are skipped (only logged).
    //     The chat still answers, it just forgets the previous questions. No 500 for the user.
    public class RedisConversationStore : IConversationStoreService
    {
        #region Fields
        private readonly ICacheService _cache;

        // 30 minutes of silence ends the chat's memory. Every save sets the 30 minutes again,
        // so an active chat stays alive and a forgotten one cleans itself up (no job needed).
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

        // Keep only the latest turns: enough context for follow-ups, and a small, cheap prompt.
        private const int MaxTurns = 10;

        // Long answers are cut, the start is enough to remember what was said.
        private const int MaxAnswerLength = 1000;
        #endregion

        #region Constructor
        public RedisConversationStore(ICacheService cache)
        {
            _cache = cache;
        }
        #endregion

        #region Last filters
        public Task<ParsedAssetQuestion?> GetLastAsync(string chatKey, CancellationToken ct)
        {
            return _cache.GetAsync<ParsedAssetQuestion>(FiltersKey(chatKey), ct);
        }

        public Task SaveLastAsync(string chatKey, ParsedAssetQuestion parsed, CancellationToken ct)
        {
            return _cache.SetAsync(FiltersKey(chatKey), parsed, Lifetime, ct);
        }
        #endregion

        #region Chat history
        public async Task<IReadOnlyList<ChatTurn>> GetHistoryAsync(string chatKey, CancellationToken ct)
        {
            var turns = await _cache.GetAsync<List<ChatTurn>>(HistoryKey(chatKey), ct);
            return turns ?? new List<ChatTurn>();
        }

        public async Task AddTurnAsync(string chatKey, string question, string answer, CancellationToken ct)
        {
            if (answer.Length > MaxAnswerLength)
                answer = answer[..MaxAnswerLength] + "...";

            // Read → add the new turn → keep the last 10 → write back the whole list.
            var turns = (await GetHistoryAsync(chatKey, ct))
                        .TakeLast(MaxTurns - 1)
                        .Append(new ChatTurn(question, answer))
                        .ToList();

            await _cache.SetAsync(HistoryKey(chatKey), turns, Lifetime, ct);
        }
        #endregion

        #region Keys
        // Same key names as before. chatKey = "{userId}:{sessionId}", so one user never reads another's chat.
        private static string FiltersKey(string chatKey) => $"asset-chat:{chatKey}:filters";
        private static string HistoryKey(string chatKey) => $"asset-chat:{chatKey}:history";
        #endregion
    }
}

/*
            ┌──────────── Load balancer ────────────┐
            ▼                                       ▼
        API server 1                           API server 2
   (Q1: "laptops in IT?")                (Q2: "only the free ones")
            │                                       │
            └──────────────►  Redis  ◄──────────────┘
                     asset-chat:{user}:{session}:history
                     asset-chat:{user}:{session}:filters
                     (expire after 30 min of silence, survive restarts)
*/
