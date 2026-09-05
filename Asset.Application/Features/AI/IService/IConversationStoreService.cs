using Asset.Application.Features.AI.DTos;

namespace Asset.Application.Features.AI.IService
{
    public interface IConversationStoreService
    {
        ParsedAssetQuestion? GetLast(string sessionId);
        void SaveLast(string sessionId, ParsedAssetQuestion parsed);
    }
}