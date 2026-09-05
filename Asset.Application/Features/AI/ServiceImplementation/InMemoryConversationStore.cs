using Asset.Application.Features.AI.DTos;
using Asset.Application.Features.AI.IService;
using Microsoft.Extensions.Caching.Memory;
namespace Asset.Application.Features.AI.ServiceImplementation
{
    public class InMemoryConversationStore : IConversationStoreService
    {
        #region Fields
        private readonly IMemoryCache _cache;
        #endregion

        #region Constructor
        public InMemoryConversationStore(IMemoryCache cache)
        {
            _cache = cache;
        }
        #endregion

        public ParsedAssetQuestion? GetLast(string sessionId)
        {
            _cache.TryGetValue(Key(sessionId), out ParsedAssetQuestion? parsed);
            return parsed;
        }

        public void SaveLast(string sessionId, ParsedAssetQuestion parsed)
        {
            // 30 minutes: long enough for a real conversation, short enough that
            // stale context doesn't leak into a new one.
            _cache.Set(Key(sessionId), parsed, TimeSpan.FromMinutes(30));
        }

        private static string Key(string sessionId) => $"asset-chat:{sessionId}";
    }
}