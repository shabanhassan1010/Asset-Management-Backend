using Asset.Application.Features.AI.DTos;
using Asset.Application.Features.AI.IService;
using Microsoft.Extensions.Caching.Memory;

namespace Asset.Application.Features.AI.ServiceImplementation
{
    // NOT registered any more: the app uses RedisConversationStore (survives restarts, shared by all servers).
    // Kept only as a simple option for running locally without Redis: swap the line in
    // ModuleInfrastructureDependencies.cs and add services.AddMemoryCache().
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
        // RAM is instant, so these just wrap the result in a finished Task to match the async interface.
        public Task<ParsedAssetQuestion?> GetLastAsync(string chatKey, CancellationToken ct)
        {
            _cache.TryGetValue(FiltersKey(chatKey), out ParsedAssetQuestion? parsed);
            return Task.FromResult(parsed);
        }

        public Task SaveLastAsync(string chatKey, ParsedAssetQuestion parsed, CancellationToken ct)
        {
            _cache.Set(FiltersKey(chatKey), parsed, Lifetime);
            return Task.CompletedTask;
        }
        #endregion

        #region Chat history
        public Task<IReadOnlyList<ChatTurn>> GetHistoryAsync(string chatKey, CancellationToken ct)
        {
            return Task.FromResult(GetHistory(chatKey));
        }

        private IReadOnlyList<ChatTurn> GetHistory(string chatKey)
        {
            _cache.TryGetValue(HistoryKey(chatKey), out List<ChatTurn>? turns);
            return turns ?? new List<ChatTurn>();
        }

        public Task AddTurnAsync(string chatKey, string question, string answer, CancellationToken ct)
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
            return Task.CompletedTask;
        }
        #endregion

        #region Keys
        private static string FiltersKey(string chatKey) => $"asset-chat:{chatKey}:filters";
        private static string HistoryKey(string chatKey) => $"asset-chat:{chatKey}:history";
        #endregion
    }
}
